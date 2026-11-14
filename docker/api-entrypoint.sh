#!/bin/sh
set -eu
mkdir -p /data/images /data/keys /data/identity
if [ "${1:-}" = "migrate" ]; then
  exec /app/efbundle
fi
exec dotnet /app/BrokerHub.Api.dll
