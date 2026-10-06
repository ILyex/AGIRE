#!/usr/bin/env bash
set -euo pipefail
umask 077

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${ENV_FILE:-$ROOT_DIR/deploy/.env.production}"
BACKUP_FILE="${BACKUP_FILE:-}"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
TARGET_DB="${TARGET_DB:-hawdh_restore_$STAMP}"
COMPOSE_FILE="$ROOT_DIR/docker-compose.production.yml"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Production environment file not found: $ENV_FILE" >&2
  exit 1
fi
if [[ -z "$BACKUP_FILE" || ! -f "$BACKUP_FILE" ]]; then
  echo "Set BACKUP_FILE to an existing PostgreSQL custom-format dump." >&2
  exit 1
fi
if [[ ! "$TARGET_DB" =~ ^[A-Za-z0-9_]{1,63}$ ]]; then
  echo "TARGET_DB must contain only ASCII letters, digits, and underscores (up to 63 characters)." >&2
  exit 1
fi
if [[ ! -f "$BACKUP_FILE.sha256" ]]; then
  echo "Backup checksum file not found: $BACKUP_FILE.sha256" >&2
  exit 1
fi

BACKUP_FILE="$(cd "$(dirname "$BACKUP_FILE")" && pwd)/$(basename "$BACKUP_FILE")"
ENV_FILE="$(cd "$(dirname "$ENV_FILE")" && pwd)/$(basename "$ENV_FILE")"

(
  cd "$(dirname "$BACKUP_FILE")"
  sha256sum --check "$(basename "$BACKUP_FILE").sha256"
)

compose() {
  docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" "$@"
}

production_db="$(compose exec -T database sh -ec 'printf "%s" "$POSTGRES_DB"')"
if [[ "${TARGET_DB,,}" == "${production_db,,}" ]]; then
  echo "Refusing to restore over the production database. Choose a separate TARGET_DB." >&2
  exit 1
fi

cat "$BACKUP_FILE" | compose exec -T database pg_restore --list > /dev/null
compose exec -e "TARGET_DB=$TARGET_DB" -T database sh -ec 'createdb -U "$POSTGRES_USER" "$TARGET_DB"'
cat "$BACKUP_FILE" | compose exec -e "TARGET_DB=$TARGET_DB" -T database sh -ec \
  'exec pg_restore --exit-on-error --no-owner --no-privileges -U "$POSTGRES_USER" -d "$TARGET_DB"'
compose exec -e "TARGET_DB=$TARGET_DB" -T database sh -ec \
  'psql -U "$POSTGRES_USER" -d "$TARGET_DB" -Atqc "SELECT 1"' > /dev/null

echo "Restore completed in isolated database: $TARGET_DB"
echo "The production database was not overwritten. Validate the restored records before directing an application instance to them."
