# Owner console

The owner console is available at `/admin/`. It provides lead-stage totals, recent leads, failed/review/escalation counts, manual won/lost/escalated actions, and an immutable approved-price history.

Configure `Admin__Username`, `Admin__Password`, and a random `Admin__CookieSigningKey` of at least 32 bytes through deployment secrets. Never commit them. Keep `Admin__SecureCookies=true` behind HTTPS; it may be false only for local HTTP development. Sessions are signed, HTTP-only, same-site cookies that expire after eight hours.

Adding a price deactivates the current entry for the same brand, kVA, and phase and creates a new version with its required change reason. Existing quotations remain unchanged because they retain their original price version and calculated totals. Database prices take precedence over the optional read-only JSON catalogue.

All normal `/api` endpoints now require an owner session. Health checks, signed public quotation PDFs, provider webhooks, and the separately authenticated Exotel media socket remain accessible for their intended integrations.
