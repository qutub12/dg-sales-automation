# IndiaMART and Justdial lead intake

Both intake adapters are disabled by default. A successfully parsed new lead is stored and immediately given one queued voice-call job. Repeated webhook deliveries and repeated email scans are idempotent. Messages without a safe phone number are retained as `ReviewRequired`; the software never guesses between multiple phone numbers.

## IndiaMART email

Configure `LeadIntake__IndiaMart__*` values and then set `ENABLED=true`. For Gmail, enable IMAP and use an app password (not the normal mailbox password); a dedicated inbox or forwarding mailbox is recommended. Set `SenderContains` and `SubjectContains` to match real IndiaMART notifications. Only unread messages matching both filters are processed, and a message is marked read only after it is saved.

Keep the mailbox password in deployment secrets. Do not commit it. Review the first few messages in `inbound_lead_messages` before leaving automation unattended because IndiaMART email layouts can change.

## Justdial portal collector

Justdial WhatsApp notifications do not contain the complete buyer details for this account. Until Justdial provides its promised API credentials, the application can read the authenticated business lead portal with Playwright. This is an interim adapter and must be used only with the business's own account and in accordance with its Justdial agreement.

1. Install the browser once after publishing: `pwsh bin/Release/net10.0/playwright.ps1 install chromium` (Linux containers may use `install --with-deps chromium`).
2. Set `PortalUrl`, `BrowserProfilePath`, and the four selectors. Use Playwright codegen or browser developer tools against your own portal to obtain stable selectors; prefer `data-*`, role, or test-id attributes over CSS class names.
3. On a trusted desktop, set `Headless=false` and `PortalEnabled=true`, then start the API. Complete Justdial login/OTP in the browser that opens. The persistent profile saves the authenticated cookies. Stop the API after the lead page loads.
4. Set `Headless=true`, keep the profile directory on an encrypted persistent volume, and restart. The worker checks the newest leads every five minutes.

The worker does not store a Justdial password, bypass OTP/CAPTCHA, or call private endpoints. It hashes the portal lead identifier for idempotency and feeds the visible lead text into the normal parser. A changed page layout or expired session stops collection and emits an operational error instead of guessing. Run only one portal worker instance because a persistent Chromium profile cannot be safely shared by multiple processes.

The earlier verified WhatsApp webhook remains available as an optional notification trigger, but `LeadIntake__Justdial__Enabled` should remain false unless the configured Justdial sender actually includes parseable lead details.

## Operations and privacy

Inbound message text contains customer personal data. Restrict database access, encrypt backups, and choose a retention/deletion period appropriate for the business. Logs intentionally avoid full message bodies and phone numbers. Use synthetic data when refining parser tests.
