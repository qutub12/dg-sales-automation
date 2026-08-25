# Voice call flow

1. Confirm customer name and convenient time.
   If the customer is busy or asks for a later call in Hindi, English or Marathi, stop qualification,
   ask for a convenient callback time, confirm it, schedule it and end the call politely.
2. Disclose that this is an automated sales assistant and that the call may be recorded.
3. Detect Hindi, English or Marathi and allow language switching.
4. Ask purchase versus rental.
5. Ask location and intended use.
6. Ask requested kVA; when unknown, gather equipment, quantity, HP/kW/ampere, phase, motor starting method and simultaneous usage.
7. Ask preferred brand, silent/acoustic requirement, urgency, budget and transport/installation needs.
8. Repeat the structured requirement for confirmation.
9. If standard and complete, prepare quotation; otherwise escalate.
10. Confirm that the quotation will be sent on WhatsApp.

The language model conducts the conversation but never calculates capacity or price. Those decisions belong to versioned application rules.
Callback requests are persisted as `CustomerRequestedCallback`; the current call is completed and a new
call job is scheduled. When no exact time is provided, the configurable default is two hours. The normal
Monday-Saturday 10:00-19:00 IST calling-hours gate still applies, so an out-of-hours request waits until
the next allowed calling window.

## Runtime integration

The voice worker is disabled by default. When enabled, it claims queued calls, sends the customer and
language context plus these instructions to the configured HTTPS voice gateway, and stores the returned
provider call ID. The gateway can later be implemented with the selected Indian telephony/voice vendor.

The provider posts its final structured result to `/api/webhooks/voice/call-result` with an
`X-Voice-Signature` HMAC-SHA256 signature over the exact request body. Duplicate callbacks are accepted
without duplicating requirements. Transcripts are stored only when recording consent is reported.

Required secret configuration:

- `Voice__StartCallEndpoint`
- `Voice__ApiToken`
- `Voice__PublicBaseUrl`
- `Voice__WebhookSecret` (at least 32 characters)
- `Voice__Enabled=true` only after the gateway is verified

## Selected telephony provider: Exotel

The MVP uses Exotel Voice v1 in the Mumbai region. The application calls the customer through an active
Exotel flow using the business ExoPhone as caller ID. The flow must contain the required disclosure and
VoiceBot applet. Exotel then opens a bidirectional WebSocket to the AI media bridge.

Configure the account SID, API key/token, ExoPhone caller ID, active flow URL and a random callback token.
The API key/token use HTTP Basic authentication and are never returned by application endpoints.
`Voice__Enabled` remains false until the Exotel account, flow and media bridge have been tested.

The status callback handles `failed`, `busy` and `no-answer`; successful conversational output is handled
by the realtime structured-completion tool. The signed result webhook remains available for other voice
gateways. Exotel recommends using its Call Details API as a
fallback because status callback delivery can be delayed or fail; that reconciliation job remains a
deployment-hardening task.

## Realtime media bridge

The `/api/voice/exotel-media` WebSocket bridges Exotel AgentStream to the OpenAI Realtime API. Configure
the Exotel VoiceBot applet for bidirectional raw PCM at 24 kHz and protect the WSS endpoint with the
configured Basic-auth username/password. Audio stays at 24 kHz in both directions, avoiding resampling.

The bridge:

- sends Exotel media frames as `input_audio_buffer.append` events;
- returns `response.output_audio.delta` frames immediately to the phone call;
- uses semantic voice-activity detection;
- sends Exotel `clear` when the customer interrupts the assistant;
- starts with the automation disclosure and language preference;
- limits sessions to ten minutes by default and rejects messages over 2 MB;
- keeps the OpenAI API key only on the server.

Keep both `Voice__Enabled` and `Voice__Realtime__Enabled` false until the Exotel flow, WSS authentication,
24 kHz setting and a non-production test number are verified. Call-cost and latency telemetry remain a
deployment-hardening task.

## Structured completion tool

After repeating the requirement and receiving confirmation, the realtime model calls
`submit_sales_requirement` exactly once. The JSON schema requires capacity, phase, application, location,
commercial exceptions, validation flags, disclosure/consent state and detected language.

The model only supplies facts. Server-side code then:

1. validates and persists the requirement and call result;
2. stores the detected Hindi, English, Marathi or mixed language;
3. checks for an active privately approved price and all quotation guardrails;
4. creates an immutable quotation and queues WhatsApp delivery when eligible; or
5. marks the lead escalated and returns the review reasons to the voice agent.

Unknown capacity is submitted as `null` with a validation flag and is always escalated; the model is never
allowed to estimate kVA. Duplicate tool calls are idempotent through the unique call-result constraint.
