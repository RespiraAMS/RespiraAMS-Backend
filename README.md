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

### Configuration

The AppHost holds every setting the services share, so there is a single place to
look at and to change:

- `src/Respira.AppHost/appsettings.json` holds the shared configuration:
  - `Parameters` holds the secrets. They are injected as parameters, so the
    Aspire dashboard masks them.
  - `Shared` holds the settings themselves, grouped per configuration section
    (`Jwt`, `Email`, `EmailVerification`, `PasswordReset`, `R2`, ...). The AppHost
    forwards them to the services as environment variables, so a service reads them
    from its own configuration like any other value.
- Every service keeps only its own `appsettings.json` (logging, allowed hosts,
  connection strings and, for the gateway, the reverse proxy routes).

The secrets are meant to be replaced per developer, so they never reach the
repository. They are stored as [user
secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) of the
AppHost project:

```bash
cd src/Respira.AppHost

dotnet user-secrets set "Parameters:jwt-secret" "<a long random secret>"
dotnet user-secrets set "Parameters:email-user-name" "<your smtp user>"
dotnet user-secrets set "Parameters:email-password" "<your smtp password>"
dotnet user-secrets set "Parameters:r2-access-key" "<your r2 access key>"
dotnet user-secrets set "Parameters:r2-secret-key" "<your r2 secret key>"
```

The values committed in `appsettings.json` are development placeholders
(`change-me`). The JWT secret is used by the authentication service to sign the
tokens and by the gateway, the analysis and the saga services to validate them, so
they all share the same one.

### Seed data

The seed data is applied by EF Core, through the migrations:

- The accounts of the authentication service are described in
  `src/Respira.Authentication/Authentication.Infrastructure/Data/seed-accounts.jsonc`
  and turned into model seed data when the EF Core model is built. A change to the
  file requires a new migration:

  ```bash
  dotnet ef migrations add <name> \
    --project src/Respira.Authentication/Authentication.Infrastructure \
    --startup-project src/Respira.Authentication/Authentication.Infrastructure \
    --namespace Authentication.Infrastructure.Migrations \
    --output-dir Migrations
  ```

  The file holds the BCrypt hash of the password, never the password itself. The
  seeded administrator signs in with `admin@respira.ams` / `Admin@123`. The file is
  optional: without it the model simply holds no seed data and the services still
  start.

- The clinical reference data (variables, criteria, metrics, pathogens,
  suspected causes, antibiotic groups, antibiotics and treatment protocols) lives in
  `src/Respira.Clinical/Infrastructure/Data/seed-data.jsonc` and is loaded when the
  clinical database is still empty. A missing or broken file is reported in the
  logs and the service keeps running without it.

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
rm -rf **/TestResults

# Start the SonarQube scanner. The coverage reports are written by each test
# project into bin/<Configuration>/<tfm>/TestResults/*.cobertura.xml, so we
# point the scanner at them with a wildcard.
dotnet sonarscanner begin \
    /k:"RespiraAMS" /d:sonar.host.url="http://localhost:9000" \
    /d:sonar.token="<YOUR_SONAR_TOKEN>" \
    /d:sonar.cs.cobertura.reportsPaths="test/**/TestResults/*.cobertura.xml"

# Build the solution
dotnet build --no-incremental

# Run the tests and collect the code coverage report.
# coverage.settings.xml excludes source-generated files (obj/, source generators)
# and EF Core migration artifacts from coverage. The settings path must be absolute,
# because each test project receives the same value and resolves it from its
# own directory.
dotnet test --coverage --coverage-output-format cobertura --coverage-settings "$(pwd)/coverage.settings.xml"

# Finish the SonarQube analysis and import the results
dotnet sonarscanner end /d:sonar.token="<YOUR_SONAR_TOKEN>"

# Go to http://localhost:9000 to see the results

# You can also run this command to see the test report
dotnet reportgenerator -reports:"test/**/TestResults/*.cobertura.xml" -targetdir:"docs/test-rep
ort" -reporttypes:"Html_Dark;Cobertura" -assemblyfilters:"+Respira*;-[*]*"
open docs/test-report/index.html
```

> **NOTE:** `sonar.cs.cobertura.reportsPaths` requires SonarQube **2026.3** or
> newer. On an older version, replace `--coverage-output-format cobertura` with
> `--coverage-output-format xml` and the scanner option with
> `/d:sonar.cs.vscoveragexml.reportsPaths=test/**/TestResults/*.xml`.

### Test report generators

```bash
# Ensure that the build and test steps (similar to SonarQube setup) is run first
dotnet reportgenerator -reports:"test/**/TestResults/*.cobertura.xml" \
-targetdir:"docs/test-report" -reporttypes:"Html_Dark;Cobertura" -assemblyfilters:"+Respira*;-[*]*"

open docs/test-report/index.html
```

### Development notes

Some distinctive features in this project has some special rules that is applied
for all services, so it's recommended to refer to the `docs` directory and read
the rules first

## Authentication

The authentication service exposes the session and account endpoints under `/api/v1/auth`:

```http
GET  /api/v1/auth/accounts?page=1&size=10&role=MedicalRegulationReviewer&status=Active&isEmailConfirmed=true
POST /api/v1/auth/login
POST /api/v1/auth/logout
POST /api/v1/auth/refresh
POST /api/v1/auth/email-verification
POST /api/v1/auth/email-verification/confirm
```

`GET /api/v1/auth/accounts` lists the accounts page by page and accepts optional
`role` (`Doctor`, `HeadDepartmentDoctor`, `MedicalRegulationReviewer`, `Admin`),
`status` (`Active`, `Inactive`), and `isEmailConfirmed` (`true`, `false`) filters.
The endpoint requires `Admin` or `HeadDepartmentDoctor` authorization (`Admin` may
view any role, while `HeadDepartmentDoctor` is scoped to `Doctor` accounts).

A user who forgot its password resets it by email. The first endpoint issues a
single-use token and mails a link that carries it, the second one consumes the
token and stores the new password:

```http
POST /api/v1/auth/forgot-password          { "email": "doctor@respira.ams" }
POST /api/v1/auth/forgot-password/confirm  { "token": "<token from the link>",
                                             "newPassword": "<new password>",
                                             "confirmPassword": "<new password>" }
```

The link points at the `PasswordReset:ResetUrl` of the AppHost configuration and
carries the token as a `token` query parameter. Only the SHA-256 hash of the token
is stored, the raw one only travels by email. The token expires after
`PasswordReset:TokenExpiresHours` hours and only the newest one stays valid, so a
reset link can be used once. A password has to be at least 8 characters long and
contain a letter and a digit.

Changing the password revokes every refresh token of the account, so the sessions
opened with the previous password cannot be used anymore. The user signs in again
with the new one.
