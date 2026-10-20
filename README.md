# Grafirio.Packages

Shared NuGet packages for Grafirio services, published to GitHub Packages.

## Packages

| Package | Description |
|---|---|
| `Grafirio.Contracts.AI` | MassTransit message contracts for AI services (data analysis, Q&A, graph data, PyCaret). |
| `Grafirio.Shared.Infrastructure` | ServiceResult pattern, exception/correlation/request-logging middleware, API versioning, validation filter, MassTransit/RabbitMQ setup. |
| `Grafirio.Shared.Identity` | Keycloak integration, current-user abstraction, company-based authorization policies, handlers, attributes and filters. |

## Company scope and permission decisions

Human API clients select a workspace with `X-Company-Id`. `CompanyRequestScope` validates explicit header, route and query selections together; conflicting or malformed selections fail closed. Both permission filters and `IIdentityService.CurrentCompanyId` use this scope. The token company is a compatibility default, not the authorization source. Bridge credentials cannot select a different company.

Permissions are loaded from Identity and cached only within the current HTTP request. A following request observes membership/permission revocation immediately. Account lookup distinguishes a missing account from provider failure; exact email matching never accepts a partial search result. Keycloak management tokens use `KeycloakAdmin:AdminRealm` (default `master`) while user operations target `KeycloakAdmin:Realm`.

`IOrderPaid.CompanyId` is optional for older messages. New publishers include the company selected at payment; consumers must validate the buyer's permissions and must not guess among multiple companies.

Run the synthetic identity contract tests with `dotnet test tests/Grafirio.Shared.Identity.Tests -c Release -p:RestoreSources=https://api.nuget.org/v3/index.json`.

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
<add key="grafirio" value="https://nuget.pkg.github.com/Grafirio/index.json" />
```
