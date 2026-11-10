# RespiraAMS

This is the back-end project of RespiraAMS

## About the project

RespiraAMS is a clinical decision support system (CDSS) that helps in diagnosing
Community-acquired Pneumonia (CAP) patients. It also implement the Antimicrobial
Stewardship program (AMS), which aims to reduce the misuse of antibiotics in hospital
environment.

## Structure

The project is a micro-services architecture, where the source code is located in
`./src`, while the test code is located in `./test`.

Each micro-service follow _**Clean**_ architecture, with 4 projects:

- `Domain`: holds domain model, enumerated type, constants and business logics
- `Application`: holds all the features (use cases) of the application,
  which used _**Vertical Slice**_ architecture internally
- `Infrastructure`: holds all the infrastructure code, including database,
  cache, external services setup, etc.
- `API`: holds the API layer, which are used to expose the features

There are also 3 external projects:

- `./src/Respira.AppHost/`: entry point, which used `Aspire` for orchestration
- `./src/Respira.Gateway/`: API gateway
- `./src/Respira.ServiceDefaults/`: shared building blocks used between micro-service

## How to run

### Prerequisites

- `.NET` >= 10.0
- `Aspire` >= 13.4.6 (<https://aspire.dev/get-started/install-cli/>)
- `Docker` installed and running

### Run

```bash
# Setup aspire certs (not mandatory, but recommended) (https://aspire.dev/app-host/certificate-configuration/#use-the-development-certificate)
aspire certs trust

# In some cases, you may need to delete the whole docker volume to see the seed
# data
docker volume prune -f -a

# Run with aspire
aspire run

# Navigating to the aspire dashboard and start exploring. When starting, it will
# prompt you to fill in the missing configurations. You can ignore it of the
# feature you want to use does not require any configuration.
```

## Development setup

### SonarQube setup

```bash
# Install SonarQube scanner tool
dotnet tool install --global dotnet-sonarscanner

# Run the local SonarQube server
docker run \
    -d --name sonarqube -e SONAR_ES_BOOTSTRAP_CHECKS_DISABLE=true \
    -p 9000:9000 sonarqube:latest

# Clean project to get rid of all stale data
dotnet clean

# Start the SonarQube scanner. The coverage reports are written by each test
# project into bin/<Configuration>/<tfm>/TestResults/*.cobertura.xml, so we
# point the scanner at them with a wildcard.
dotnet sonarscanner begin \
    /k:"RespiraAMS" /d:sonar.host.url="http://localhost:9000" \
    /d:sonar.token="<YOUR_SONAR_TOKEN>" \
    /d:sonar.cs.cobertura.reportsPaths="test/**/TestResults/*.cobertura.xml"

# Build the solution
dotnet build

# Run the tests and collect the code coverage report
dotnet test --coverage --coverage-output-format cobertura

# Finish the SonarQube analysis and import the results
dotnet sonarscanner end /d:sonar.token="<YOUR_SONAR_TOKEN>"

# Go to http://localhost:9000 to see the results
```

> **NOTE:** `sonar.cs.cobertura.reportsPaths` requires SonarQube **2026.3** or
> newer. On an older version, replace `--coverage-output-format cobertura` with
> `--coverage-output-format xml` and the scanner option with
> `/d:sonar.cs.vscoveragexml.reportsPaths=test/**/TestResults/*.xml`.

## Audit logs and Clinical rollback

Clinical, Patient, and Analysis record data changes in an `audit_entries` table
in the same database transaction as the changed data. Each entry includes the
actor and role, action, entity type and id, timestamp, correlation and trace
ids, request path, changed fields, and before/after snapshots where available.
Entries from one `SaveChanges` call share a `changeSetId`; rolling back an entry
therefore previews and reverses the whole set. Audit rows are immutable history:
a rollback writes a separate `Clinical.AuditRollback` entry.

The services forward audit events asynchronously to Saga's audit log. Use the
Saga endpoint to search across forwarded events:

```http
GET /api/v1/audit-logs?entityType=Respira.Clinical.Domain.Entities.Antibiotic&page=1&pageSize=50
```

Saga results expose an `id` that is the source service's audit entry id. Use it
with the Clinical endpoints below to inspect the local record and preview or
apply a rollback:

```http
GET  /api/v1/clinical/audit-logs/{id}
POST /api/v1/clinical/audit-logs/{id}/rollback/preview
POST /api/v1/clinical/audit-logs/{id}/rollback
```

The endpoints require the `Admin` role. The gateway authenticates the caller
and forwards identity headers; ServiceDefaults turns those headers into claims,
then Clinical authorization applies its service-specific role rules. Review the
preview before applying. Preview returns each proposed change and any conflicts;
the apply endpoint returns `409 Conflict` if current data no longer matches the
audited after-state. This prevents overwriting later edits. The transaction
restores updates and deletes, removes rows created by the change set, and
reverses many-to-many relationship changes. Rollback audit entries cannot
themselves be rolled back.

Audit forwarding is asynchronous, so a newly committed change may take a short
time to appear in Saga search. Rollback reads the local Clinical database and
can use the local audit entry as soon as it is committed. Snapshots omit
non-owned relationship graphs; the Clinical rollback handles captured
many-to-many join rows separately.
