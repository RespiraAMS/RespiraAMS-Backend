# RespiraAMS-Backend

This is the backend project of RespiraAMS

## Structure

The project is a microservices architecture, where the source code is located in `./src`, while the test code is located in `./test`.

Each microservice follow _**Clean**_ architecture, with 4 projects:

- `Domain`: holds domain model, enums, constants and business logics
- `Application`: holds all the features (use cases) of the application,
  which used _**Vertical Slice**_ architecture internally
- `Infrastructure`: holds all the infrastructure code, including database,
  cache, external services setup, etc.
- `API`: holds the API layer, which are used to expose the features

There are also 3 external projects:

- `./src/Respira.AppHost/`: entry point, which used `Aspire` for orchestration
- `./src/Respira.Gateway/`: API gateway
- `./src/Respira.ServiceDefaults/`: shared building blocks used between microservices

## How to run

### Prerequisites

- `.NET` >= 10.0
- `Aspire` >= 13.4.6 (https://aspire.dev/get-started/install-cli/)
- `Docker` installed and running

### Run

```bash
# Setup aspire certs (not mandatory, but recommended) (https://aspire.dev/app-host/certificate-configuration/#use-the-development-certificate)
aspire certs trust

# In some cases, you may need to delete the whole docker volume to see the seed data
docker volume prune -f -a

# Run with aspire
aspire run

# Navigating to the aspire dashboard and start exploring. When starting, it will prompt you
# to fill in the missing configurations. You can ignore it of the feature you want to use
# does not require any configuration.
```
