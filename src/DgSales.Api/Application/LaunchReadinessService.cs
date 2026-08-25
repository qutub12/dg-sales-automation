namespace DgSales.Api.Application;

public sealed record LaunchReadinessCheck(string Name, bool Ready, string Detail);
public sealed record LaunchReadinessReport(bool Ready, IReadOnlyCollection<LaunchReadinessCheck> Checks);

public sealed class LaunchReadinessService(IConfiguration config, IHostEnvironment environment)
{
    public LaunchReadinessReport Assess()
    {
        var checks = new List<LaunchReadinessCheck>();
        var connection = config.GetConnectionString("SalesDatabase") ?? Environment.GetEnvironmentVariable("CONNECTION_STRING");
        checks.Add(new("Database", !string.IsNullOrWhiteSpace(connection) && !connection.Contains("change-me", StringComparison.OrdinalIgnoreCase),
            string.IsNullOrWhiteSpace(connection) ? "Not configured." : connection.Contains("change-me", StringComparison.OrdinalIgnoreCase) ? "Default password must be replaced." : "Configured."));
        Required(checks, "Admin username", config["Admin:Username"]);
        checks.Add(new("Admin password hash", !string.IsNullOrWhiteSpace(config["Admin:PasswordHash"]),
            "Use a PBKDF2 password hash in production; a plain password is development-only."));
        Strong(checks, "Admin cookie signing key", config["Admin:CookieSigningKey"], 32);
        RequiredFile(checks, "Quotation font", config["QuotationBranding:FontPath"]);
        RequiredFile(checks, "Quotation logo", config["QuotationBranding:LogoPath"]);
        Strong(checks, "Quotation link signing secret", config["QuotationDocuments:SigningSecret"], 32);
        AbsoluteHttps(checks, "Quotation public URL", config["QuotationDocuments:PublicBaseUrl"]);

        if (config.GetValue<bool>("WhatsApp:Enabled"))
        {
            Required(checks, "WhatsApp API version", config["WhatsApp:GraphApiVersion"]);
            Required(checks, "WhatsApp phone number ID", config["WhatsApp:PhoneNumberId"]);
            Required(checks, "WhatsApp access token", config["WhatsApp:AccessToken"]);
            Strong(checks, "WhatsApp app secret", config["WhatsApp:AppSecret"], 20);
            Strong(checks, "WhatsApp webhook token", config["WhatsApp:WebhookVerifyToken"], 20);
            Required(checks, "Owner pricing template", config["WhatsApp:OwnerApproval:PricingTemplate:Name"]);
            Required(checks, "Owner approval template", config["WhatsApp:OwnerApproval:ApprovalTemplate:Name"]);
            Required(checks, "Customer English quotation template", config["WhatsApp:Templates:English:Name"]);
        }
        else checks.Add(new("WhatsApp enabled", false, "Enable after Meta verification and template approval."));

        if (config.GetValue<bool>("Voice:Enabled"))
        {
            Required(checks, "Exotel account SID", config["Voice:Exotel:AccountSid"]);
            Required(checks, "Exotel API key", config["Voice:Exotel:ApiKey"]);
            Required(checks, "Exotel API token", config["Voice:Exotel:ApiToken"]);
            Required(checks, "Exotel caller ID", config["Voice:Exotel:CallerId"]);
            Required(checks, "Exotel flow URL", config["Voice:Exotel:FlowUrl"]);
            Strong(checks, "Exotel callback token", config["Voice:Exotel:CallbackToken"], 24);
            AbsoluteHttps(checks, "Voice public URL", config["Voice:PublicBaseUrl"]);
            Required(checks, "OpenAI realtime API key", config["Voice:Realtime:ApiKey"]);
        }
        else checks.Add(new("Voice calling enabled", false, "Enable after Exotel and OpenAI Realtime pilot configuration."));

        if (config.GetValue<bool>("LeadIntake:IndiaMart:Enabled"))
        {
            Required(checks, "IndiaMART mailbox username", config["LeadIntake:IndiaMart:Username"]);
            Required(checks, "IndiaMART mailbox app password", config["LeadIntake:IndiaMart:Password"]);
        }
        else checks.Add(new("IndiaMART intake enabled", false, "Enable after Gmail app-password configuration."));
        return new(checks.All(x => x.Ready), checks);
    }

    private static void Required(List<LaunchReadinessCheck> checks, string name, string? value) =>
        checks.Add(new(name, !string.IsNullOrWhiteSpace(value), string.IsNullOrWhiteSpace(value) ? "Not configured." : "Configured."));
    private static void Strong(List<LaunchReadinessCheck> checks, string name, string? value, int length) =>
        checks.Add(new(name, value?.Length >= length, value?.Length >= length ? "Configured." : $"Must contain at least {length} characters."));
    private void RequiredFile(List<LaunchReadinessCheck> checks, string name, string? path)
    {
        var resolved = string.IsNullOrWhiteSpace(path) ? null : Path.IsPathRooted(path) ? path : Path.Combine(environment.ContentRootPath, path);
        checks.Add(new(name, resolved is not null && File.Exists(resolved), resolved is not null && File.Exists(resolved) ? "Available." : "File is missing."));
    }
    private static void AbsoluteHttps(List<LaunchReadinessCheck> checks, string name, string? value) =>
        checks.Add(new(name, Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps,
            "Must be an absolute HTTPS URL."));
}
