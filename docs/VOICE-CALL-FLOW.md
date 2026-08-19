# Voice call flow

1. Confirm customer name and convenient time.
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
