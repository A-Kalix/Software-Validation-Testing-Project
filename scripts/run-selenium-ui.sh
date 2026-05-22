#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
FRONTEND_DIR="$ROOT_DIR/Frontend"
BACKEND_DIR="$ROOT_DIR/Backend"
TEST_PROJECT="$ROOT_DIR/Backend.Tests.UI/Backend.Tests.UI.csproj"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: dotnet is not installed or not on PATH"
  exit 1
fi

cd "$ROOT_DIR"
if [ -f ".env" ]; then
  set -o allexport
  source ".env"
  set +o allexport
fi

# Start database container if not already running
docker compose up -d db

echo "Waiting for SQL Server to boot..."
sleep 15

# Start backend
cd "$BACKEND_DIR"
dotnet restore
DOTNET_BACKEND_LOG="/tmp/backend-selenium.log"
dotnet run --urls=http://127.0.0.1:5005 > "$DOTNET_BACKEND_LOG" 2>&1 &
BACKEND_PID=$!

echo "Started backend with PID $BACKEND_PID"

# Start frontend
cd "$FRONTEND_DIR"
if [ ! -d "node_modules" ]; then
  echo "Installing frontend dependencies..."
  npm install
fi
VITE_LOG="/tmp/vite-selenium.log"
npm run dev -- --host 127.0.0.1 --port 5173 > "$VITE_LOG" 2>&1 &
VITE_PID=$!

echo "Started frontend with PID $VITE_PID"

function cleanup {
  echo "Stopping frontend and backend..."
  kill "$VITE_PID" 2>/dev/null || true
  kill "$BACKEND_PID" 2>/dev/null || true
}
trap cleanup EXIT

# Wait for backend
echo "Waiting for backend on http://127.0.0.1:5005..."
for i in {1..30}; do
  if curl -sSf http://127.0.0.1:5005/swagger/index.html >/dev/null 2>&1; then
    echo "Backend is ready."
    break
  fi
  sleep 2
  echo -n '.'
done

echo
if ! curl -sSf http://127.0.0.1:5005/swagger/index.html >/dev/null 2>&1; then
  echo "ERROR: backend did not start in time. Check logs at $DOTNET_BACKEND_LOG"
  exit 1
fi

# Wait for frontend
echo "Waiting for frontend on http://127.0.0.1:5173..."
for i in {1..30}; do
  if curl -sSf http://127.0.0.1:5173 >/dev/null 2>&1; then
    echo "Frontend is ready."
    break
  fi
  sleep 2
  echo -n '.'
done

echo
if ! curl -sSf http://127.0.0.1:5173 >/dev/null 2>&1; then
  echo "ERROR: frontend did not start in time. Check logs at $VITE_LOG"
  exit 1
fi

cd "$ROOT_DIR"
export UI_TEST_HEADLESS=${UI_TEST_HEADLESS:-true}

mkdir -p TestResults

dotnet test "$TEST_PROJECT" \
  --collect:"XPlat Code Coverage;Format=opencover" \
  --logger "trx;LogFileName=ui.trx" \
  --results-directory TestResults
