using System.Net;
using DgSales.Api.Integrations.Voice;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DgSales.Domain.Tests;

public sealed class ExotelVoiceCallProviderTests
{
    [Fact]
    public async Task StartsCustomerToFlowCallWithStatusCallback()
    {
        string? submitted = null;
        var handler = new StubHandler(async request =>
        {
            submitted = await request.Content!.ReadAsStringAsync();
            return new(HttpStatusCode.OK) { Content = new StringContent("{\"Call\":{\"Sid\":\"call-1\",\"Status\":\"queued\"}}") };
        });
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Voice:Exotel:AccountSid"] = "account",
            ["Voice:Exotel:ApiKey"] = "key",
            ["Voice:Exotel:ApiToken"] = "token",
            ["Voice:Exotel:CallerId"] = "08000000000",
            ["Voice:Exotel:FlowUrl"] = "https://my.exotel.com/account/exoml/start_voice/1"
        }).Build();
        var provider = new ExotelVoiceCallProvider(new HttpClient(handler), configuration);

        var result = await provider.StartAsync(new(
            Guid.NewGuid(), Guid.NewGuid(), "+919876543210", "Customer", "Nagpur", "Hindi",
            new Uri("https://example.com/status"), "instructions"), CancellationToken.None);

        Assert.Equal("call-1", result.ProviderCallId);
        Assert.Contains("From=%2B919876543210", submitted);
        Assert.Contains("StatusCallback=https%3A%2F%2Fexample.com%2Fstatus", submitted);
        Assert.Contains("CustomField=", submitted);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => callback(request);
    }
}
