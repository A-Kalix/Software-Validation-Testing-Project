#!/usr/bin/env bash
set -euo pipefail

if [ -z "${SONAR_TOKEN:-}" ]; then
  echo "ERROR: SONAR_TOKEN must be set"
  exit 1
fi

cd "$(dirname "$0")/.."
if [ -f ".env" ]; then
  set -o allexport
  source ".env"
  set +o allexport
fi

# restore and build the backend
cd Backend
dotnet restore

dotnet sonarscanner begin \
  /k:"software-validation-testing-project" \
  /d:sonar.host.url="${SONAR_HOST_URL:-http://localhost:9000}" \
  /d:sonar.login="$SONAR_TOKEN" \
  /d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml" \
  /d:sonar.cs.vstest.reportsPaths="**/TestResults/*.trx" \
  /d:sonar.coverage.exclusions="**/Migrations/**,**/obj/**,**/bin/**"

dotnet build
cd ..
mkdir -p TestResults

dotnet test "Backend.Tests/Backend.Tests.csproj" \
  --collect:"XPlat Code Coverage;Format=opencover" \
  --logger "trx;LogFileName=backend.trx" \
  --results-directory TestResults

echo "Starting database, backend, and frontend for UI tests..."

# Start database container
docker compose up -d db

# Start backend server
DOTNET_BACKEND_LOG="/tmp/backend-sonar.log"
cd Backend
dotnet run --urls=http://127.0.0.1:5005 > "$DOTNET_BACKEND_LOG" 2>&1 &
BACKEND_PID=$!
cd ..

# Start frontend
VITE_LOG="/tmp/vite-sonar.log"
cd Frontend
if [ ! -d "node_modules" ]; then
  npm install
fi
npm run dev -- --host 127.0.0.1 --port 5173 > "$VITE_LOG" 2>&1 &
VITE_PID=$!
cd ..

cleanup() {
  echo "Cleaning up backend and frontend processes..."
  kill "$VITE_PID" 2>/dev/null || true
  kill "$BACKEND_PID" 2>/dev/null || true
}
trap cleanup EXIT

echo "Waiting for backend and frontend to be ready..."
for i in {1..30}; do
  if curl -sSf http://127.0.0.1:5005/swagger/index.html >/dev/null 2>&1 && curl -sSf http://127.0.0.1:5173 >/dev/null 2>&1; then
    echo "Services are ready."
    break
  fi
  sleep 2
  echo -n '.'
done

if ! curl -sSf http://127.0.0.1:5005/swagger/index.html >/dev/null 2>&1; then
  echo "ERROR: backend did not start in time. Check logs at $DOTNET_BACKEND_LOG"
  exit 1
fi

if ! curl -sSf http://127.0.0.1:5173 >/dev/null 2>&1; then
  echo "ERROR: frontend did not start in time. Check logs at $VITE_LOG"
  exit 1
fi

dotnet test "Backend.Tests.UI/Backend.Tests.UI.csproj" \
  --collect:"XPlat Code Coverage;Format=opencover" \
  --logger "trx;LogFileName=ui.trx" \
  --results-directory TestResults

dotnet sonarscanner end /d:sonar.login="$SONAR_TOKEN"
