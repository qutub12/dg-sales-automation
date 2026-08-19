namespace DgSales.Api.Domain;

public sealed class AutomationControl
{
    private AutomationControl() { }
    public string Name { get; private set; } = string.Empty;
    public bool IsPaused { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public static AutomationControl Create(string name) => new() { Name = name };
    public void Set(bool paused, string? reason) { IsPaused = paused; Reason = paused ? reason?.Trim() : null; UpdatedAtUtc = DateTimeOffset.UtcNow; }
}

public sealed record SetAutomationControlRequest(bool IsPaused, string? Reason);
