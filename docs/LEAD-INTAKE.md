# IndiaMART and Justdial lead intake

Both intake adapters are disabled by default. A successfully parsed new lead is stored and immediately given one queued voice-call job. Repeated webhook deliveries and repeated email scans are idempotent. Messages without a safe phone number are retained as `ReviewRequired`; the software never guesses between multiple phone numbers.

## IndiaMART email

Configure `LeadIntake__IndiaMart__*` values and then set `ENABLED=true`. For Gmail, enable IMAP and use an app password (not the normal mailbox password); a dedicated inbox or forwarding mailbox is recommended. Set `SenderContains` and `SubjectContains` to match real IndiaMART notifications. Only unread messages matching both filters are processed, and a message is marked read only after it is saved.

Keep the mailbox password in deployment secrets. Do not commit it. Review the first few messages in `inbound_lead_messages` before leaving automation unattended because IndiaMART email layouts can change.

## Justdial WhatsApp

This uses the same Meta WhatsApp Cloud API app as quotation delivery. Configure:

- `WhatsApp__AppSecret` with the Meta app secret.
- `WhatsApp__WebhookVerifyToken` with a new random secret.
- Meta callback URL: `https://YOUR_PUBLIC_HOST/api/webhooks/whatsapp`.
- `LeadIntake__Justdial__AllowedSenderNumbers__0` with the exact WhatsApp sender number used by Justdial.
- Set `LeadIntake__Justdial__Enabled=true` only after webhook verification succeeds.

POST requests require Meta's `X-Hub-Signature-256` signature. Messages from any number outside the allowlist are acknowledged but ignored, preventing normal customer chats from being imported as Justdial leads.

## Operations and privacy

Inbound message text contains customer personal data. Restrict database access, encrypt backups, and choose a retention/deletion period appropriate for the business. Logs intentionally avoid full message bodies and phone numbers. Use synthetic data when refining parser tests.
