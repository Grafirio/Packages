# Grafirio.Packages

Shared NuGet packages for Grafirio services, published to GitHub Packages.

## Packages

| Package | Description |
|---|---|
| `Grafirio.Contracts.AI` | MassTransit message contracts for AI services (data analysis, Q&A, graph data, PyCaret). |
| `Grafirio.Shared.Infrastructure` | ServiceResult pattern, exception/correlation/request-logging middleware, API versioning, validation filter, MassTransit/RabbitMQ setup. |
| `Grafirio.Shared.Identity` | Keycloak integration, current-user abstraction, company-based authorization policies, handlers, attributes and filters. |

## Publishing

Push a version tag to publish all packages:

```bash
git tag v1.0.0
git push origin v1.0.0
```

The `publish.yml` workflow builds, packs and pushes to GitHub Packages using the tag version.

## Consuming

Add the feed to your `nuget.config` (authenticate with a PAT that has `read:packages` scope, supplied via environment variable — never commit tokens):

```xml
<add key="grafirio" value="https://nuget.pkg.github.com/OWNER/index.json" />
```
