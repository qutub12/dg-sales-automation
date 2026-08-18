# MVP decisions

## Confirmed

- Voice sales agent is mandatory in Release 1.
- Supported languages: Hindi, English and Marathi, including mixed conversation.
- Current business calling number is a normal mobile SIM.
- Current messaging account is WhatsApp Business.
- Standard quotations may be sent automatically.
- Brother handles uncertain sizing, negotiation, non-standard prices and high-value escalation.

## Calling-number decision

A normal mobile SIM cannot be used directly as the programmable outbound caller ID. Release 1 will provision a telephony number and apply verified business identity. The existing SIM is retained for business continuity and human escalation.

## Standard quotation eligibility

The agent may send automatically only when all conditions pass:

1. Customer identity, location and requirement are complete.
2. Capacity was selected by deterministic sizing rules or explicitly confirmed by the customer.
3. Selected generator exists in the approved catalogue.
4. Price and charges come only from active catalogue versions.
5. No custom discount, unusual payment term or unsupported accessory is requested.
6. Stock and delivery commitments are not invented.
7. No safety, confidence or validation flag exists.

Every failed condition creates an owner-review task.

