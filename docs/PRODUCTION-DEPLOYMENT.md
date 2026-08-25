# Production deployment and launch runbook

## Low-cost deployment recommendation

Start with one small Linux VM in an Indian region, Docker Compose, PostgreSQL with a persistent volume,
and Cloudflare DNS/proxy for HTTPS. Keep the database port closed to the internet. Expose only HTTPS;
restrict SSH by source IP and key authentication. Move PostgreSQL to a managed service only when the
cost is justified by lead volume or uptime requirements.

Use a dedicated API subdomain such as `sales-api.<domain>`. The public HTTPS URL is required by Meta,
Exotel and signed quotation links. Do not enable provider workers until their webhooks have been tested.

## First deployment

1. Install Docker Engine and the Compose plugin on the VM.
2. Clone the repository and copy `.env.example` to `.env`.
3. Replace every blank secret and every `change-me` value. Never commit `.env`.
4. Generate the admin password hash on Windows:
   `./scripts/generate-admin-password-hash.ps1 -Password (Read-Host -AsSecureString)`
5. Put the output in `ADMIN__PASSWORDHASH`; leave `ADMIN__PASSWORD` blank.
6. Set the public quotation and voice URLs to the HTTPS API origin.
7. Start PostgreSQL: `docker compose up -d postgres`.
8. Apply migrations from a one-off SDK container or deployment pipeline before starting the API.
9. Start the API: `docker compose up -d --build api`.
10. Verify `/health`, `/health/ready`, `/health/operations` and authenticated
    `/api/admin/launch-readiness`.

## Safe provider activation order

1. IndiaMART mailbox on a test label/filter.
2. WhatsApp webhook verification and owner-only pricing/approval templates.
3. Customer quotation template delivery to test numbers.
4. Exotel calling to the owner's/test numbers with recording disabled.
5. OpenAI Realtime multilingual conversations and callback behaviour.
6. Follow-ups after Meta approves all language templates.
7. Live IndiaMART leads with daily supervision for at least one week.

Keep `Voice__Enabled`, `WhatsApp__Enabled`, `LeadIntake__IndiaMart__Enabled` and
`FollowUps__Enabled` false until the corresponding step passes.

## Backup and recovery

Run `bash scripts/backup-postgres.sh` daily from cron. Copy encrypted backups to a separate storage
account/bucket; a backup on the same VM is not disaster recovery. The script retains 14 local days by
default. Test restoration monthly in a separate database using `pg_restore`.

Before every deployment, take a database backup. Deploy an immutable image, apply migrations once,
check readiness, and then enable workers. To roll back, disable all automation controls first, restore
the previous image, and restore the database only when a migration is not backward-compatible.

## Monitoring

Poll `/health/ready` every minute and `/health/operations` every five minutes. Alert the owner/technical
contact after two consecutive failures. Review authenticated `/api/admin/operations-health` daily for
failed WhatsApp jobs, owner requests, follow-ups, stale queues and unparsed lead messages.

Do not log access tokens, customer recordings or complete webhook bodies. Rotate Meta, Exotel, OpenAI,
mailbox, cookie-signing and document-signing secrets immediately if exposure is suspected.

Call transcripts are retained only when recording consent was captured. After the owner approves the
policy, set `Privacy__RecordingRetentionEnabled=true` and choose `Privacy__RecordingRetentionDays`
(30 days is the supplied default). The daily retention worker clears expired transcript text while
preserving non-audio operational facts such as outcome, consent and callback time.
