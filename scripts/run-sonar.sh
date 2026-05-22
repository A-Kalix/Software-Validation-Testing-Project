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
  /d:sonar.host.url=http://localhost:9000 \
  /d:sonar.login="$SONAR_TOKEN" \
  /d:sonar.cs.opencover.reportsPaths="**/TestResults/*-coverage.cobertura.xml" \
  /d:sonar.cs.vstest.reportsPaths="**/TestResults/*.trx" \
  /d:sonar.coverage.exclusions="**/Migrations/**,**/obj/**,**/bin/**"

dotnet build
cd ..
mkdir -p TestResults

dotnet test "Backend.Tests/Backend.Tests.csproj" \
  --collect:"XPlat Code Coverage" \
  --logger "trx;LogFileName=backend.trx" \
  --results-directory TestResults \
  /p:CoverletOutputFormat=cobertura \
  /p:CoverletOutput="TestResults/backend-coverage.cobertura.xml"

dotnet test "Backend.Tests.UI/Backend.Tests.UI.csproj" \
  --collect:"XPlat Code Coverage" \
  --logger "trx;LogFileName=ui.trx" \
  --results-directory TestResults \
  /p:CoverletOutputFormat=cobertura \
  /p:CoverletOutput="TestResults/ui-coverage.cobertura.xml"

dotnet sonarscanner end /d:sonar.login="$SONAR_TOKEN"
