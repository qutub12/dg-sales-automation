using DgSales.Api.Domain;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ILeadRepository, InMemoryLeadRepository>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapPost("/api/leads", (CreateLeadRequest request, ILeadRepository repository) =>
{
    var validation = request.Validate();
    if (validation is not null) return Results.BadRequest(new { error = validation });

    var existing = repository.FindByPhone(request.Phone);
    if (existing is not null)
        return Results.Ok(new { lead = existing, duplicate = true });

    var lead = Lead.Create(request);
    repository.Add(lead);
    return Results.Created($"/api/leads/{lead.Id}", new { lead, duplicate = false });
});

app.MapGet("/api/leads", (ILeadRepository repository) => repository.List());
app.MapGet("/api/leads/{id:guid}", (Guid id, ILeadRepository repository) =>
    repository.Get(id) is { } lead ? Results.Ok(lead) : Results.NotFound());

app.MapPost("/api/leads/{id:guid}/queue-call", (Guid id, ILeadRepository repository) =>
{
    var lead = repository.Get(id);
    if (lead is null) return Results.NotFound();
    lead.QueueCall();
    return Results.Accepted($"/api/leads/{id}", lead);
});

app.Run();

public partial class Program;

