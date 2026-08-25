# Owner WhatsApp quotation approval

The MVP does not require the owner console for day-to-day quotation approval. After the voice agent
captures and validates the customer's generator requirement, the system sends a WhatsApp pricing
request to the configured owner number. Every request contains a unique code so simultaneous leads
cannot be mixed up.

## Owner reply format

Reply with the selling price before GST and transport charge. Use zero when transport is free:

`PRICE VSS-ABC123 SELLING 500000 TRANSPORT 15000`

When the customer requested installation, the message also asks for its charge:

`PRICE VSS-ABC123 SELLING 500000 TRANSPORT 15000 INSTALLATION 25000`

The application accepts Indian comma formatting, validates that the selling price is positive,
calculates GST at the configured 18%, generates the branded PDF and sends the PDF plus totals back to
the owner. It does not send anything to the customer at this stage.

Approve the exact quotation with:

`APPROVE VSS-ABC123`

Reject it, optionally with a reason, with:

`REJECT VSS-ABC123 transport too high`

Only `APPROVE` changes the quotation to `Approved` and queues customer delivery. Duplicate webhook
deliveries and replies received in the wrong workflow state do not send a quotation twice.

## Meta templates

Two owner-facing templates must be approved and configured:

- `WhatsApp__OwnerApproval__PricingTemplate__Name`: five body parameters: request code, customer,
  generator specification, installation location and exact reply example.
- `WhatsApp__OwnerApproval__ApprovalTemplate__Name`: document header plus five body parameters:
  request code, customer, amount summary, approval reply and rejection reply.

The customer quotation templates remain separate. A public HTTPS document base URL and signing secret
are required so Meta can retrieve the temporary PDF link.
