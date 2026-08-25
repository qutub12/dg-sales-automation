using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using DgSales.Api.Application;

namespace DgSales.Api.Integrations.Voice;

public sealed class OpenAiRealtimeVoiceBridge(
    IConfiguration configuration,
    VoiceAgentInstructions instructions,
    IServiceScopeFactory scopeFactory,
    ILogger<OpenAiRealtimeVoiceBridge> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task RunAsync(WebSocket exotel, CancellationToken requestAborted)
    {
        var apiKey = Required("Voice:Realtime:ApiKey");
        var model = Required("Voice:Realtime:Model");
        var voice = configuration["Voice:Realtime:Voice"] ?? "marin";
        var maxMinutes = Math.Clamp(configuration.GetValue<int?>("Voice:Realtime:MaxSessionMinutes") ?? 10, 1, 60);
        using var sessionTimeout = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
        sessionTimeout.CancelAfter(TimeSpan.FromMinutes(maxMinutes));
        var cancellationToken = sessionTimeout.Token;

        using var realtime = new ClientWebSocket();
        realtime.Options.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        var endpoint = new Uri($"wss://api.openai.com/v1/realtime?model={Uri.EscapeDataString(model)}");
        await realtime.ConnectAsync(endpoint, cancellationToken);

        await SendJsonAsync(realtime, new
        {
            type = "session.update",
            session = new
            {
                type = "realtime",
                model,
                output_modalities = new[] { "audio" },
                audio = new
                {
                    input = new
                    {
                        format = new { type = "audio/pcm", rate = 24000 },
                        turn_detection = new { type = "semantic_vad" }
                    },
                    output = new { format = new { type = "audio/pcm" }, voice }
                },
                instructions = instructions.Build("Hindi, with natural English and Marathi switching"),
                tools = new[]
                {
                    new
                    {
                        type = "function",
                        name = "submit_sales_requirement",
                        description = "Submit only the customer-confirmed generator requirement. The server decides sizing, price and quotation eligibility.",
                        parameters = new
                        {
                            type = "object",
                            additionalProperties = false,
                            properties = new
                            {
                                requestedKva = new { type = new[] { "number", "null" } },
                                phaseCount = new { type = "integer", @enum = new[] { 1, 3 } },
                                preferredBrand = new { type = new[] { "string", "null" } },
                                application = new { type = "string" },
                                installationLocation = new { type = "string" },
                                sizingConfirmed = new { type = "boolean" },
                                customDiscountRequested = new { type = "boolean" },
                                nonStandardTermsRequested = new { type = "boolean" },
                                deliveryPromiseRequired = new { type = "boolean" },
                                validationFlags = new { type = "array", items = new { type = "string" } },
                                automationDisclosed = new { type = "boolean" },
                                recordingConsentGiven = new { type = "boolean" },
                                detectedLanguage = new { type = "string", @enum = new[] { "Hindi", "English", "Marathi", "Mixed" } }
                            },
                            required = new[]
                            {
                                "requestedKva", "phaseCount", "preferredBrand", "application", "installationLocation",
                                "sizingConfirmed", "customDiscountRequested", "nonStandardTermsRequested",
                                "deliveryPromiseRequired", "validationFlags", "automationDisclosed",
                                "recordingConsentGiven", "detectedLanguage"
                            }
                        }
                    },
                    new
                    {
                        type = "function",
                        name = "request_customer_callback",
                        description = "Stop the sales conversation and schedule a callback when the customer is busy or asks to be called later.",
                        parameters = new
                        {
                            type = "object",
                            additionalProperties = false,
                            properties = new
                            {
                                callbackAtUtc = new { type = new[] { "string", "null" }, description = "Confirmed callback time as ISO 8601 UTC, or null when the customer gives no time." },
                                requestedTimeText = new { type = "string" },
                                detectedLanguage = new { type = "string", @enum = new[] { "Hindi", "English", "Marathi", "Mixed" } },
                                automationDisclosed = new { type = "boolean" }
                            },
                            required = new[] { "callbackAtUtc", "requestedTimeText", "detectedLanguage", "automationDisclosed" }
                        }
                    }
                },
                tool_choice = "auto"
            }
        }, cancellationToken);

        await SendJsonAsync(realtime, new
        {
            type = "conversation.item.create",
            item = new
            {
                type = "message",
                role = "user",
                content = new[] { new { type = "input_text", text = "Begin the call with the required automated-assistant disclosure and ask which language the customer prefers." } }
            }
        }, cancellationToken);
        await SendJsonAsync(realtime, new { type = "response.create" }, cancellationToken);

        string? streamSid = null;
        string? providerCallId = null;
        using var completed = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var inbound = PumpExotelToRealtimeAsync(exotel, realtime, (stream, call) => { streamSid = stream; providerCallId = call; }, completed.Token);
        var outbound = PumpRealtimeToExotelAsync(realtime, exotel, () => streamSid, () => providerCallId, completed.Token);
        await Task.WhenAny(inbound, outbound);
        completed.Cancel();
        try { await Task.WhenAll(inbound, outbound); } catch (OperationCanceledException) { }

        await CloseSafelyAsync(realtime, cancellationToken);
        await CloseSafelyAsync(exotel, cancellationToken);
    }

    private async Task PumpExotelToRealtimeAsync(
        WebSocket exotel, WebSocket realtime, Action<string, string> setSessionIds, CancellationToken cancellationToken)
    {
        while (exotel.State == WebSocketState.Open && realtime.State == WebSocketState.Open)
        {
            var message = await ReceiveTextAsync(exotel, cancellationToken);
            if (message is null) break;
            using var json = JsonDocument.Parse(message);
            var root = json.RootElement;
            var eventType = GetString(root, "event");
            if (eventType == "start")
            {
                var start = root.GetProperty("start");
                var streamSid = GetString(start, "stream_sid") ?? GetString(start, "streamSid")
                    ?? throw new InvalidOperationException("Exotel start event did not contain stream SID.");
                ValidateMediaFormat(start);
                var callSid = GetString(start, "call_sid") ?? GetString(start, "callSid")
                    ?? throw new InvalidOperationException("Exotel start event did not contain call SID.");
                setSessionIds(streamSid, callSid);
            }
            else if (eventType == "media")
            {
                var payload = GetString(root.GetProperty("media"), "payload");
                if (!string.IsNullOrWhiteSpace(payload))
                    await SendJsonAsync(realtime, new { type = "input_audio_buffer.append", audio = payload }, cancellationToken);
            }
            else if (eventType == "stop") break;
        }
    }

    private async Task PumpRealtimeToExotelAsync(
        WebSocket realtime, WebSocket exotel, Func<string?> getStreamSid, Func<string?> getProviderCallId, CancellationToken cancellationToken)
    {
        long sequence = 1;
        while (realtime.State == WebSocketState.Open && exotel.State == WebSocketState.Open)
        {
            var message = await ReceiveTextAsync(realtime, cancellationToken);
            if (message is null) break;
            using var json = JsonDocument.Parse(message);
            var root = json.RootElement;
            var eventType = GetString(root, "type");
            var streamSid = getStreamSid();
            if (streamSid is null) continue;

            if (eventType == "response.output_audio.delta")
            {
                var audio = GetString(root, "delta");
                if (!string.IsNullOrWhiteSpace(audio))
                    await SendJsonAsync(exotel, new
                    {
                        @event = "media",
                        sequence_number = sequence++,
                        stream_sid = streamSid,
                        media = new { payload = audio }
                    }, cancellationToken);
            }
            else if (eventType == "input_audio_buffer.speech_started")
            {
                await SendJsonAsync(exotel, new { @event = "clear", stream_sid = streamSid }, cancellationToken);
            }
            else if (eventType == "response.done")
            {
                await HandleToolCallsAsync(root, realtime, getProviderCallId(), cancellationToken);
            }
            else if (eventType == "error")
            {
                logger.LogError("Realtime voice session error: {Error}", message);
                throw new InvalidOperationException("Realtime voice session failed.");
            }
        }
    }

    private async Task HandleToolCallsAsync(
        JsonElement root, WebSocket realtime, string? providerCallId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providerCallId)
            || !root.TryGetProperty("response", out var response)
            || !response.TryGetProperty("output", out var output)) return;
        foreach (var item in output.EnumerateArray())
        {
            if (GetString(item, "type") != "function_call") continue;
            var toolName = GetString(item, "name");
            if (toolName is not "submit_sales_requirement" and not "request_customer_callback") continue;
            var callId = GetString(item, "call_id");
            var arguments = GetString(item, "arguments");
            if (string.IsNullOrWhiteSpace(callId) || string.IsNullOrWhiteSpace(arguments)) continue;
            await using var scope = scopeFactory.CreateAsyncScope();
            object result;
            try
            {
                result = toolName == "request_customer_callback"
                    ? await scope.ServiceProvider.GetRequiredService<VoiceCallbackToolService>().ExecuteAsync(providerCallId, arguments, cancellationToken)
                    : await scope.ServiceProvider.GetRequiredService<VoiceRequirementToolService>().ExecuteAsync(providerCallId, arguments, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Voice tool {ToolName} failed for call {ProviderCallId}.", toolName, providerCallId);
                result = new VoiceRequirementToolResult(false, "review_required", null, ["The voice request could not be processed automatically."]);
            }
            await SendJsonAsync(realtime, new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "function_call_output",
                    call_id = callId,
                    output = JsonSerializer.Serialize(result, JsonOptions)
                }
            }, cancellationToken);
            await SendJsonAsync(realtime, new { type = "response.create" }, cancellationToken);
        }
    }

    private static void ValidateMediaFormat(JsonElement start)
    {
        if (!start.TryGetProperty("media_format", out var format) && !start.TryGetProperty("mediaFormat", out format)) return;
        var rate = format.TryGetProperty("sample_rate", out var snakeRate) ? snakeRate.GetInt32()
            : format.TryGetProperty("sampleRate", out var camelRate) ? camelRate.GetInt32() : 24000;
        if (rate != 24000)
            throw new InvalidOperationException($"Exotel VoiceBot must be configured for 24000 Hz; received {rate} Hz.");
    }

    private static async Task<string?> ReceiveTextAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        using var stream = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close) return null;
            if (result.MessageType != WebSocketMessageType.Text) throw new InvalidOperationException("Only JSON text WebSocket messages are supported.");
            await stream.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
            if (stream.Length > 2 * 1024 * 1024) throw new InvalidOperationException("WebSocket message exceeded 2 MB.");
        } while (!result.EndOfMessage);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static async Task SendJsonAsync(WebSocket socket, object value, CancellationToken cancellationToken)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        await socket.SendAsync(bytes.AsMemory(), WebSocketMessageType.Text, true, cancellationToken);
    }

    private static async Task CloseSafelyAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "session ended", cancellationToken); } catch { }
    }

    private static string? GetString(JsonElement value, string name) =>
        value.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;

    private string Required(string key) => string.IsNullOrWhiteSpace(configuration[key])
        ? throw new InvalidOperationException($"{key} is required when realtime voice is enabled.")
        : configuration[key]!;
}
