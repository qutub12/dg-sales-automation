# Quotation document and WhatsApp delivery

## PDF configuration

Configure `QuotationBranding` with the business name, address, phone, email, GST number and standard
terms. `FontPath` must point to a TrueType/OpenType font available to the deployed API. A Unicode font
such as Noto Sans is recommended for customer names captured in Hindi or Marathi. `BoldFontPath` is
optional.

The PDF contains the quotation snapshot already stored in PostgreSQL. It never recalculates against a
newer price version, so a previously generated offer cannot silently change.

## Secure document links

Set `QuotationDocuments__PublicBaseUrl` to the HTTPS API address and provide a random
`QuotationDocuments__SigningSecret` of at least 32 characters through the deployment secret store.
Document links expire after 24 hours and use an HMAC-SHA256 signature. Do not place the signing secret
in source control.

## WhatsApp boundary

`POST /api/quotations/{id}/whatsapp-delivery` creates a duplicate-safe delivery job. It does not send a
message until a configured WhatsApp provider worker claims the job. The worker will use an approved
WhatsApp template and the expiring PDF URL; provider credentials remain outside the repository.

The Meta Cloud API worker is disabled by default. To enable it, configure the Graph API version,
phone-number ID, access token and approved template names for English, Hindi and Marathi, then set
`WhatsApp__Enabled=true`. The worker removes `+` from E.164 recipient numbers as required by Meta,
stores the returned message ID, and retries a failed job at most three times with backoff.

Run one worker instance in the low-cost MVP deployment. The PostgreSQL `xmin` concurrency token protects
job updates, but horizontal worker scaling should add an explicit `FOR UPDATE SKIP LOCKED` claim query.
