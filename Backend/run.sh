#!/bin/bash

set -e

REBUILD=false

if [ "$1" = "--build" ] || [ "$1" = "build" ]; then
    REBUILD=true
fi

# -----------------------------
# Get host IP dynamically
# -----------------------------
HOST_IP=$(ip route | awk '/default/ {print $3}')
export OLLAMA_BASE_URL=http://$HOST_IP:11434

echo "Using OLLAMA at: $OLLAMA_BASE_URL"

# -----------------------------
# Optionally build local services
# -----------------------------
if [ "$REBUILD" = true ]; then
    echo "Building local Docker services..."
    docker compose build
fi

# -----------------------------
# Start all services
# -----------------------------
echo "Starting Docker Compose stack..."
docker compose up
