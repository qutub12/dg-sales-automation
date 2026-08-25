#!/usr/bin/env bash
set -euo pipefail

backup_dir="${DG_BACKUP_DIR:-./backups}"
retention_days="${DG_BACKUP_RETENTION_DAYS:-14}"
mkdir -p "$backup_dir"
stamp="$(date -u +%Y%m%dT%H%M%SZ)"
output="$backup_dir/dgsales-$stamp.dump"
docker compose exec -T postgres pg_dump -U "${POSTGRES_USER:-dgsales}" -d "${POSTGRES_DB:-dgsales}" -Fc > "$output"
test -s "$output"
find "$backup_dir" -type f -name 'dgsales-*.dump' -mtime "+$retention_days" -delete
echo "$output"
