#!/bin/bash

# -----------------------------
# Get host IP dynamically
# -----------------------------
HOST_IP=$(ip route | awk '/default/ {print $3}')
export OLLAMA_BASE_URL=http://$HOST_IP:11434

echo "Using OLLAMA at: $OLLAMA_BASE_URL"

# -----------------------------
# Get images from docker compose
# -----------------------------
IMAGES=$(docker compose config --images)

BUILD_NEEDED=true

for IMAGE in $IMAGES; do
  if docker image inspect "$IMAGE" >/dev/null 2>&1; then
    BUILD_NEEDED=false
    break
  fi
done

# -----------------------------
# Run accordingly
# -----------------------------
if [ "$BUILD_NEEDED" = true ]; then
  echo "No images found → building..."
  docker compose up --build
else
  echo "Images found → starting without build..."
  docker compose up
fi