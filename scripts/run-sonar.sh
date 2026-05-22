#!/usr/bin/env bash
set -euo pipefail

if [ -z "${SONAR_TOKEN:-}" ]; then
  echo "ERROR: SONAR_TOKEN must be set"
  exit 1
fi

cd "$(dirname "$0")/.."

# restore and build the backend
cd Backend
dotnet restore

dotnet sonarscanner begin \
  /k:"software-validation-testing-project" \
  /d:sonar.host.url=http://localhost:9000 \
  /d:sonar.login="$SONAR_TOKEN" \
  /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml" \
  /d:sonar.coverage.exclusions="**/Migrations/**,**/obj/**,**/bin/**"

dotnet build
mkdir -p ../TestResults

dotnet test "Backend.Tests/Backend.Tests.csproj" \
  --collect:"XPlat Code Coverage" \
  /p:CoverletOutputFormat=cobertura \
  /p:CoverletOutput="../TestResults/coverage.cobertura.xml"

dotnet sonarscanner end /d:sonar.login="$SONAR_TOKEN"
