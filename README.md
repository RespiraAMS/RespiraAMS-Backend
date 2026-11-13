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
