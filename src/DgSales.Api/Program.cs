using DgSales.Api.Application;
using DgSales.Api.Domain;
using DgSales.Api.Infrastructure;
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
builder.Services.AddSingleton<ServiceAreaMatcher>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
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

app.Run();

public partial class Program;
