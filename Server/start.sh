#!/bin/sh

set -eu

sh /nakama/pre_start.sh


echo "Starting to Fly..."


exec /nakama/nakama --name sussy-gunner1 \
  --database.address "$DATABASE_URL" \
  --logger.level DEBUG \
  --session.token_expiry_sec 7200 \
  --metrics.prometheus_port 9091
