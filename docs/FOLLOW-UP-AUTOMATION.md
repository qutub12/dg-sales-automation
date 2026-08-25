# Follow-up automation

The schedule begins only after Meta accepts the quotation message: day 1, day 3, day 6, and day 7. The final step closes an unanswered lead as `Lost`; a later customer response can still reopen it through the normal reply handling.

Customer WhatsApp replies are signature-verified and matched by normalized phone number. The first reply cancels every unsent follow-up. Deterministic Hindi, English, and Marathi phrase rules classify acceptance, rejection, interest, and requests for a person. Acceptance marks the lead won; rejection marks it lost. Interest, human-help requests, and unclear replies escalate the lead. Everything except a rejection queues a WhatsApp notification to the configured owner phone.

## Configuration

Keep `FollowUps__Enabled=false` until the four Meta templates are approved:

- One customer follow-up template per language. It must contain two ordered body variables: customer name and follow-up purpose.
- One owner template containing three ordered variables: customer name, customer phone, and reply disposition.
- `FollowUps__OwnerPhone` must be the owner's E.164 number, for example `+91...`.

The customer must have opted in and templates must comply with Meta's current business-messaging rules. Enabling follow-ups also requires the existing WhatsApp Cloud API configuration and inbound webhook. Monitor `/api/follow-ups` and `/api/owner-escalations` during the pilot.
