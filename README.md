# DG Sales Automation

Voice-first sales automation for a diesel-generator business. The MVP captures IndiaMART and Justdial leads, calls customers in Hindi, English or Marathi, records structured requirements, produces standard quotations, sends them on WhatsApp Business and schedules follow-ups.

## Release 1 scope

- Lead intake API with duplicate protection
- Voice-call queue and provider-neutral telephony adapter
- Structured DG requirement capture
- Deterministic sizing and quotation eligibility rules
- Automatic standard quotation workflow
- Human escalation for uncertainty, negotiation or non-standard pricing
- WhatsApp quotation delivery and Day 1/3/6/7 follow-ups

## Technology

- ASP.NET Core 10 Web API
- PostgreSQL
- Angular PWA (next slice)
- n8n for integrations and scheduled orchestration
- Exotel-compatible telephony boundary
- WhatsApp Cloud API boundary

## Run locally

1. Install the latest patched .NET 10 SDK and Docker.
2. Copy `.env.example` to `.env` and set local values.
3. Run `docker compose up -d postgres`.
4. Run `dotnet run --project src/DgSales.Api`.
5. Open `/swagger`.

To enable automatic standard quotations, create an approved catalogue outside the repository using
[`docs/APPROVED-PRICE-CATALOGUE.md`](docs/APPROVED-PRICE-CATALOGUE.md), then set
`Pricing__ApprovedCataloguePath` to its absolute path. If it is absent, inactive, expired, or does not
match the requirement, the API safely returns `ReviewRequired` and does not generate a quotation.

The API includes versioned EF Core migrations. Apply them explicitly during deployment, or run `dotnet ef database update --project src/DgSales.Api` for local development.

Never commit production credentials. The existing mobile SIM remains the WhatsApp Business number. Automated calls use a telephony number with the verified business identity.
