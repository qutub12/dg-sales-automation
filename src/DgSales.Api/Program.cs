using DgSales.Api.Application;
using DgSales.Api.Domain;
using DgSales.Api.Infrastructure;
using DgSales.Api.Integrations.WhatsApp;
using DgSales.Api.Integrations.IndiaMart;
using DgSales.Api.Integrations.Justdial;
using DgSales.Api.Integrations.Voice;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var connectionString = builder.Configuration.GetConnectionString("SalesDatabase")
    ?? Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? throw new InvalidOperationException("Sales database connection string is not configured.");
builder.Services.AddDbContext<SalesDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<ILeadRepository, EfLeadRepository>();
builder.Services.AddScoped<ICallJobRepository, EfCallJobRepository>();
builder.Services.AddSingleton<LeadMessageParser>();
builder.Services.AddScoped<InboundLeadProcessor>();
builder.Services.AddHostedService<IndiaMartMailboxWorker>();
builder.Services.AddHostedService<JustdialPortalWorker>();
builder.Services.AddSingleton<ServiceAreaMatcher>();
builder.Services.AddSingleton<GeneratorSizingService>();
builder.Services.AddSingleton<QuotationEligibilityService>();
builder.Services.AddSingleton<StandardQuotationCalculator>();
builder.Services.AddSingleton<FollowUpScheduleService>();
builder.Services.AddSingleton<ReferenceCatalogueService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ApprovedPriceCatalogueService>();
builder.Services.AddSingleton<QuotationPdfService>();
builder.Services.AddSingleton<QuotationDocumentTokenService>();
builder.Services.AddHttpClient<IWhatsAppProvider, MetaWhatsAppProvider>(client =>
    client.BaseAddress = new Uri("https://graph.facebook.com/"));
builder.Services.AddHostedService<WhatsAppDeliveryWorker>();
builder.Services.AddSingleton<WhatsAppWebhookService>();
builder.Services.AddSingleton<VoiceAgentInstructions>();
builder.Services.AddSingleton<VoiceWebhookSignatureService>();
builder.Services.AddHttpClient<HttpVoiceCallProvider>();
builder.Services.AddHttpClient<ExotelVoiceCallProvider>();
builder.Services.AddTransient<IVoiceCallProvider>(services =>
    string.Equals(builder.Configuration["Voice:Provider"], "Exotel", StringComparison.OrdinalIgnoreCase)
        ? services.GetRequiredService<ExotelVoiceCallProvider>()
        : services.GetRequiredService<HttpVoiceCallProvider>());
builder.Services.AddHostedService<VoiceCallWorker>();
builder.Services.AddSingleton<OpenAiRealtimeVoiceBridge>();
builder.Services.AddScoped<VoiceRequirementToolService>();

var app = builder.Build();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(20) });
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/api/webhooks/whatsapp", (HttpRequest request, IConfiguration configuration) =>
{
    var valid = request.Query["hub.mode"] == "subscribe"
        && !string.IsNullOrWhiteSpace(configuration["WhatsApp:WebhookVerifyToken"])
        && request.Query["hub.verify_token"] == configuration["WhatsApp:WebhookVerifyToken"];
    return valid ? Results.Text(request.Query["hub.challenge"].ToString(), "text/plain") : Results.Unauthorized();
});

app.MapPost("/api/webhooks/whatsapp", async (HttpRequest request, WhatsAppWebhookService webhook,
    IConfiguration configuration, IServiceScopeFactory scopes, CancellationToken cancellationToken) =>
{
    using var buffer = new MemoryStream();
    await request.Body.CopyToAsync(buffer, cancellationToken);
    var body = buffer.ToArray();
    if (!webhook.Verify(body, request.Headers["X-Hub-Signature-256"].FirstOrDefault())) return Results.Unauthorized();
    if (!configuration.GetValue<bool>("LeadIntake:Justdial:Enabled")) return Results.Ok();
    var allowed = configuration.GetSection("LeadIntake:Justdial:AllowedSenderNumbers").Get<string[]>() ?? [];
    var allowedDigits = allowed.Select(x => new string(x.Where(char.IsDigit).ToArray())).ToHashSet(StringComparer.Ordinal);
    foreach (var message in webhook.ReadMessages(body).Where(x => allowedDigits.Contains(new string(x.From.Where(char.IsDigit).ToArray()))))
    {
        await using var scope = scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<InboundLeadProcessor>().ProcessAsync(
            InboundLeadChannel.JustdialWhatsApp, message.Id, message.From, null, message.Text, cancellationToken);
    }
    return Results.Ok();
});
app.MapGet("/health/ready", async (SalesDbContext db, CancellationToken cancellationToken) =>
    await db.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ready" })
        : Results.Problem("Database is unavailable.", statusCode: 503));

app.MapPost("/api/leads", async (CreateLeadRequest request, ILeadRepository repository, ServiceAreaMatcher serviceAreas, CancellationToken cancellationToken) =>
{
    var validation = request.Validate();
    if (validation is not null) return Results.BadRequest(new { error = validation });

    var existing = await repository.FindByPhoneAsync(request.Phone, cancellationToken);
    if (existing is not null)
        return Results.Ok(new { lead = existing, duplicate = true });

    var lead = Lead.Create(request, serviceAreas.IsSupported(request.City));
    await repository.AddAsync(lead, cancellationToken);
    await repository.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/leads/{lead.Id}", new { lead, duplicate = false });
});

app.MapGet("/api/leads", (ILeadRepository repository, CancellationToken cancellationToken) =>
    repository.ListAsync(cancellationToken));
app.MapGet("/api/leads/{id:guid}", async (Guid id, ILeadRepository repository, CancellationToken cancellationToken) =>
    await repository.GetAsync(id, cancellationToken) is { } lead ? Results.Ok(lead) : Results.NotFound());

app.MapGet("/api/inbound-leads/review", async (SalesDbContext db, CancellationToken cancellationToken) =>
    Results.Ok(await db.InboundLeadMessages.AsNoTracking()
        .Where(x => x.Status == InboundLeadStatus.ReviewRequired)
        .OrderByDescending(x => x.ReceivedAtUtc)
        .Select(x => new { x.Id, x.Channel, x.Sender, x.Subject, x.ProcessingNote, x.ReceivedAtUtc })
        .Take(100).ToListAsync(cancellationToken)));

app.MapPost("/api/leads/{id:guid}/queue-call", async (Guid id, ILeadRepository repository, ICallJobRepository callJobs, CancellationToken cancellationToken) =>
{
    var lead = await repository.GetAsync(id, cancellationToken);
    if (lead is null) return Results.NotFound();
    var existingJob = await callJobs.FindQueuedAsync(id, cancellationToken);
    if (existingJob is not null) return Results.Ok(new { callJob = existingJob, duplicate = true });
    lead.QueueCall();
    var callJob = CallJob.Queue(id);
    await callJobs.AddAsync(callJob, cancellationToken);
    await repository.SaveChangesAsync(cancellationToken);
    return Results.Accepted($"/api/leads/{id}", new { lead, callJob, duplicate = false });
});

app.MapPost("/api/tools/sizing", (SizingRequest request, GeneratorSizingService sizing) =>
{
    var result = sizing.Calculate(request);
    return result.RecommendedKva is null ? Results.BadRequest(result) : Results.Ok(result);
});

app.MapPost("/api/tools/quotation-eligibility", (QuotationEligibilityRequest request, QuotationEligibilityService eligibility) =>
    Results.Ok(eligibility.Assess(request)));

app.MapPost("/api/tools/standard-quotation", (StandardQuotationRequest request, StandardQuotationCalculator calculator) =>
{
    var result = calculator.Calculate(request);
    return result.IsValid ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapGet("/api/reference/catalogue/technical", (ReferenceCatalogueService catalogue) => catalogue.GetTechnical());

app.MapPost("/api/leads/{id:guid}/requirements", async (
    Guid id, CaptureRequirementRequest request, SalesDbContext db, CancellationToken cancellationToken) =>
{
    if (await db.Leads.FindAsync([id], cancellationToken) is null) return Results.NotFound();
    if (request.Validate() is { } error) return Results.BadRequest(new { error });

    var requirement = CustomerRequirement.Capture(id, request);
    db.CustomerRequirements.Add(requirement);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/leads/{id}/requirements/{requirement.Id}", requirement);
});

app.MapGet("/api/leads/{id:guid}/requirements", async (Guid id, SalesDbContext db, CancellationToken cancellationToken) =>
    Results.Ok(await db.CustomerRequirements.AsNoTracking()
        .Where(x => x.LeadId == id)
        .OrderByDescending(x => x.CapturedAtUtc)
        .ToListAsync(cancellationToken)));

app.MapPost("/api/leads/{leadId:guid}/requirements/{requirementId:guid}/quotation", async (
    Guid leadId, Guid requirementId, SalesDbContext db, ApprovedPriceCatalogueService prices,
    QuotationEligibilityService eligibility, StandardQuotationCalculator calculator, CancellationToken cancellationToken) =>
{
    var requirement = await db.CustomerRequirements.SingleOrDefaultAsync(
        x => x.Id == requirementId && x.LeadId == leadId, cancellationToken);
    if (requirement is null) return Results.NotFound();

    var existingQuotation = await db.Quotations.AsNoTracking()
        .SingleOrDefaultAsync(x => x.RequirementId == requirementId, cancellationToken);
    if (existingQuotation is not null)
        return Results.Ok(new { quotation = existingQuotation, duplicate = true });

    var price = requirement.RequestedKva is { } kva
        ? prices.Find(kva, requirement.PhaseCount, requirement.PreferredBrand)
        : null;
    var assessment = eligibility.Assess(new(
        requirement.IsComplete,
        requirement.SizingConfirmed,
        price is not null,
        price is not null,
        requirement.CustomDiscountRequested,
        requirement.NonStandardTermsRequested,
        requirement.DeliveryPromiseRequired,
        requirement.HasValidationFlags));
    if (!assessment.CanSendAutomatically)
        return Results.Conflict(new { status = "ReviewRequired", assessment.ReviewReasons });

    var calculated = calculator.Calculate(new(
        price!.BasePrice, price.StandardMarkup, price.TransportCharge,
        price.InstallationCharge, price.AccessoryCharge, price.GstPercent));
    if (!calculated.IsValid) return Results.Conflict(new { status = "ReviewRequired", calculated.Errors });

    var quotation = Quotation.Generate(
        leadId, requirementId, price.Version, price.Brand, price.GensetModel,
        price.Kva, price.PhaseCount, calculated.Subtotal, calculated.GstAmount, calculated.GrandTotal);
    db.Quotations.Add(quotation);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/quotations/{quotation.Id}", new { quotation, duplicate = false });
});

app.MapGet("/api/quotations/{id:guid}", async (Guid id, SalesDbContext db, CancellationToken cancellationToken) =>
    await db.Quotations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken) is { } quotation
        ? Results.Ok(quotation)
        : Results.NotFound());

app.MapPost("/api/quotations/{id:guid}/whatsapp-delivery", async (
    Guid id, SalesDbContext db, CancellationToken cancellationToken) =>
{
    var quotation = await db.Quotations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (quotation is null) return Results.NotFound();
    var existing = await db.WhatsAppDeliveryJobs.AsNoTracking()
        .SingleOrDefaultAsync(x => x.QuotationId == id && x.Status == WhatsAppDeliveryStatus.Queued, cancellationToken);
    if (existing is not null) return Results.Ok(new { deliveryJob = existing, duplicate = true });

    var job = WhatsAppDeliveryJob.Queue(id, quotation.LeadId);
    db.WhatsAppDeliveryJobs.Add(job);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Accepted($"/api/quotations/{id}", new { deliveryJob = job, duplicate = false });
});

app.MapPost("/api/quotations/{id:guid}/document-link", async (
    Guid id, SalesDbContext db, QuotationDocumentTokenService tokens, IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (!await db.Quotations.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken)) return Results.NotFound();
    var baseUrl = configuration["QuotationDocuments:PublicBaseUrl"]?.TrimEnd('/');
    if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
        return Results.Problem("Quotation document public base URL is not configured.", statusCode: 503);

    var token = tokens.Create(id, TimeSpan.FromHours(24));
    var url = $"{baseUrl}/api/public/quotations/{id}/pdf?expires={token.ExpiresUnix}&signature={token.Signature}";
    return Results.Ok(new { url, expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(token.ExpiresUnix) });
});

app.MapGet("/api/public/quotations/{id:guid}/pdf", async (
    Guid id, long expires, string signature, SalesDbContext db,
    QuotationDocumentTokenService tokens, QuotationPdfService pdfs, CancellationToken cancellationToken) =>
{
    if (!tokens.Validate(id, expires, signature)) return Results.Unauthorized();
    var quotation = await db.Quotations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (quotation is null) return Results.NotFound();
    var lead = await db.Leads.AsNoTracking().SingleAsync(x => x.Id == quotation.LeadId, cancellationToken);
    var requirement = await db.CustomerRequirements.AsNoTracking()
        .SingleAsync(x => x.Id == quotation.RequirementId, cancellationToken);
    var bytes = pdfs.Render(quotation, lead, requirement);
    return Results.File(bytes, "application/pdf", $"{quotation.QuotationNumber}.pdf");
});

app.MapPost("/api/webhooks/voice/call-result", async (
    HttpRequest request, SalesDbContext db, VoiceWebhookSignatureService signatures, CancellationToken cancellationToken) =>
{
    using var buffer = new MemoryStream();
    await request.Body.CopyToAsync(buffer, cancellationToken);
    var body = buffer.ToArray();
    if (!signatures.Validate(body, request.Headers["X-Voice-Signature"].FirstOrDefault()))
        return Results.Unauthorized();

    var options = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
    options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    var payload = System.Text.Json.JsonSerializer.Deserialize<VoiceCallResultWebhook>(body, options);
    if (payload is null || string.IsNullOrWhiteSpace(payload.ProviderCallId))
        return Results.BadRequest(new { error = "Invalid call result." });

    if (await db.VoiceCallResults.AsNoTracking().AnyAsync(x => x.CallJobId == payload.CallJobId, cancellationToken))
        return Results.Ok(new { duplicate = true });
    var job = await db.CallJobs.SingleOrDefaultAsync(x => x.Id == payload.CallJobId, cancellationToken);
    if (job is null) return Results.NotFound();
    if (!string.Equals(job.ProviderCallId, payload.ProviderCallId, StringComparison.Ordinal))
        return Results.BadRequest(new { error = "Provider call ID does not match the active job." });

    var result = VoiceCallResult.Capture(
        job.Id, job.LeadId, payload.ProviderCallId, payload.Outcome, payload.DetectedLanguage,
        payload.AutomationDisclosed, payload.RecordingConsentGiven,
        payload.RecordingConsentGiven ? payload.Transcript : null);
    db.VoiceCallResults.Add(result);

    if (payload.Outcome == VoiceCallOutcome.Completed)
    {
        job.MarkCompleted();
        if (payload.Requirement is { } requirement && requirement.Validate() is null)
            db.CustomerRequirements.Add(CustomerRequirement.Capture(job.LeadId, requirement));
    }
    else
    {
        job.MarkFailed($"Call ended with {payload.Outcome}.", false, DateTimeOffset.UtcNow);
    }
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { duplicate = false });
});

app.MapPost("/api/webhooks/voice/exotel-status", async (
    HttpRequest request, SalesDbContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    var expectedToken = configuration["Voice:Exotel:CallbackToken"];
    if (string.IsNullOrWhiteSpace(expectedToken)
        || !string.Equals(request.Query["token"].ToString(), expectedToken, StringComparison.Ordinal))
        return Results.Unauthorized();

    var payload = await System.Text.Json.JsonSerializer.DeserializeAsync<ExotelStatusCallback>(
        request.Body, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web), cancellationToken);
    if (payload is null || string.IsNullOrWhiteSpace(payload.CallSid)) return Results.BadRequest();
    var job = await db.CallJobs.SingleOrDefaultAsync(x => x.ProviderCallId == payload.CallSid, cancellationToken);
    if (job is null) return Results.NotFound();
    var status = payload.Status.ToLowerInvariant();
    if (status is "failed" or "busy" or "no-answer")
    {
        job.MarkFailed($"Exotel call ended with {status}.", false, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }
    return Results.Ok();
});

app.Map("/api/voice/exotel-media", async (
    HttpContext context, IConfiguration configuration, OpenAiRealtimeVoiceBridge bridge) =>
{
    if (!configuration.GetValue<bool>("Voice:Realtime:Enabled"))
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return;
    }
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }
    var username = configuration["Voice:Realtime:MediaUsername"];
    var password = configuration["Voice:Realtime:MediaPassword"];
    var expected = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));
    var supplied = context.Request.Headers.Authorization.ToString();
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)
        || !string.Equals(supplied, $"Basic {expected}", StringComparison.Ordinal))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await bridge.RunAsync(socket, context.RequestAborted);
});

app.Run();

public partial class Program;

public sealed record ExotelStatusCallback(string CallSid, string Status, string? CustomField, int? ConversationDuration);
