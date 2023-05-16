#!/bin/sh

echo "Pre starting..."
/bin/sh /nakama/pre_start.sh

echo "Running Nakama..."
exec /nakama/nakama --name nakama1 --database.address "$DATABASE_URL" --logger.level DEBUG --session.token_expiry_sec 7200 --metrics.prometheus_port 9091

