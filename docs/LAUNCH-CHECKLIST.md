# Launch checklist

## Development complete

- Lead intake, duplicate protection and IndiaMART parsing
- Hindi, English and Marathi voice requirement collection
- Busy/customer-requested callback scheduling
- Deterministic generator sizing and technical catalogue mapping
- Owner pricing and approval entirely through WhatsApp
- Automatic 18% GST, branded PDF and approval-gated customer delivery
- Day 1/3/6/7 follow-ups, opt-out and escalation
- Signed quotation links, calling-hours controls, retries and owner operations console
- Database migrations, container image, health endpoints and backup procedure

## External configuration required before launch

- Purchase/configure a domain and HTTPS API subdomain
- Choose the low-cost Linux hosting account and create the VM
- Complete Meta business verification and WhatsApp Cloud API/coexistence onboarding
- Approve owner pricing, owner approval, customer quotation and follow-up templates
- Complete Exotel KYC, number/flow setup and existing-business-number routing decision
- Create an OpenAI API project, add billing and issue a restricted API key
- Create a Gmail app password for the confirmed IndiaMART mailbox
- Confirm the quotation email and IndiaMART mailbox spellings
- Configure secrets and run `/api/admin/launch-readiness` until every check passes
- Confirm a privacy/retention period and call-recording policy with the business owner

## Pilot acceptance

- Ten test calls covering Hindi, English, Marathi, mixed language, interruptions and background noise
- Busy/later-call phrases schedule correctly without continuing the sales conversation
- Three simultaneous quotation requests cannot be mixed up
- Incorrect owner reply formats never send a customer quotation
- Owner rejection stops delivery; owner approval sends exactly once
- Customer STOP blocks future calls and WhatsApp messages
- Failed provider calls visibly alert operations and retry safely
- Backup restore tested before accepting live leads
- One supervised week completed before unattended automation
