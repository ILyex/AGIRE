#!/usr/bin/env bash
set -euo pipefail
umask 077

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${ENV_FILE:-$ROOT_DIR/deploy/.env.production}"
BACKUP_DIR="${BACKUP_DIR:-$ROOT_DIR/backups}"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
DEST="$BACKUP_DIR/hawdh-$STAMP.dump"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Production environment file not found: $ENV_FILE" >&2
  exit 1
fi

mkdir -p "$BACKUP_DIR"
chmod 700 "$BACKUP_DIR"
LOCK_DIR="$BACKUP_DIR/.hawdh-backup.lock"
if ! mkdir "$LOCK_DIR" 2>/dev/null; then
  echo "Another backup is running, or the backup lock needs manual inspection: $LOCK_DIR" >&2
  exit 1
fi
TMP_DUMP=""
TMP_CHECKSUM=""
cleanup() {
  if [[ -n "$TMP_DUMP" ]]; then rm -f "$TMP_DUMP"; fi
  if [[ -n "$TMP_CHECKSUM" ]]; then rm -f "$TMP_CHECKSUM"; fi
  rmdir "$LOCK_DIR" 2>/dev/null || true
}
trap cleanup EXIT
TMP_DUMP="$(mktemp "$BACKUP_DIR/.hawdh-$STAMP.dump.XXXXXX")"
TMP_CHECKSUM="$TMP_DUMP.sha256"

docker compose --env-file "$ENV_FILE" -f "$ROOT_DIR/docker-compose.production.yml" \
  exec -T database sh -ec 'pg_dump -Fc -U "$POSTGRES_USER" "$POSTGRES_DB"' > "$TMP_DUMP"
chmod 600 "$TMP_DUMP"
cat "$TMP_DUMP" | docker compose --env-file "$ENV_FILE" -f "$ROOT_DIR/docker-compose.production.yml" \
  exec -T database pg_restore --list > /dev/null
sha256sum "$TMP_DUMP" | awk -v name="$(basename "$DEST")" '{ print $1 "  " name }' > "$TMP_CHECKSUM"
chmod 600 "$TMP_CHECKSUM"
if [[ -e "$DEST" || -e "$DEST.sha256" ]]; then
  echo "Backup destination already exists: $DEST" >&2
  exit 1
fi
mv -- "$TMP_DUMP" "$DEST"
mv -- "$TMP_CHECKSUM" "$DEST.sha256"
cleanup
trap - EXIT
echo "Created backup: $DEST"
