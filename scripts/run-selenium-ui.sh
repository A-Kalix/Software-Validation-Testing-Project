#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
FRONTEND_DIR="$ROOT_DIR/Frontend"
TEST_PROJECT="$ROOT_DIR/Backend.Tests.UI/Backend.Tests.UI.csproj"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: dotnet is not installed or not on PATH"
  exit 1
fi

cd "$FRONTEND_DIR"

if [ ! -d "node_modules" ]; then
  echo "Installing frontend dependencies..."
  npm install
fi

# Start Vite in the background
npm run dev -- --host 127.0.0.1 --port 5173 > /tmp/vite-selenium.log 2>&1 &
VITE_PID=$!
trap 'echo "Stopping Vite..."; kill $VITE_PID 2>/dev/null || true' EXIT

# Wait for frontend to be ready
echo "Waiting for frontend to start at http://127.0.0.1:5173..."
for i in {1..30}; do
  if curl -sSf http://127.0.0.1:5173 >/dev/null 2>&1; then
    echo "Frontend is ready."
    break
  fi
  sleep 1
  echo -n '.'
done

echo
if ! curl -sSf http://127.0.0.1:5173 >/dev/null 2>&1; then
  echo "ERROR: frontend did not start in time. Check logs at /tmp/vite-selenium.log"
  exit 1
fi

cd "$ROOT_DIR"
export UI_TEST_HEADLESS=true

dotnet test "$TEST_PROJECT"
