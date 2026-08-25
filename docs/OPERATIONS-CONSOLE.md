# Operations console

The owner console now supports lead search, full requirement/call/transcript/quotation/follow-up/reply history, retry of failed calls and follow-ups, and customer do-not-contact controls. Blocking contact immediately cancels queued follow-ups; call and delivery workers independently enforce the restriction before contacting anyone.

Calls, customer WhatsApp delivery, and follow-ups can be paused independently from the dashboard. Pauses are stored in PostgreSQL so they survive restarts. Use these controls before price maintenance, provider incidents, or any unexpected automation behaviour.

Contact corrections are exposed through the authenticated API and enforce the same normalized unique-phone rule as intake. Operator status changes and contact restrictions stop queued follow-ups. A later audit-hardening slice will record the authenticated actor and before/after values for every mutation.
