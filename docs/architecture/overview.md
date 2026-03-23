# Dead-Mans Architecture Overview

## Активный контур

В активной разработке участвуют только:

- `frontend/`
- `backend/`

`legacy-v1/` хранится как reference-only слой. Его можно читать для анализа UX и старой логики, но он не считается частью текущего продукта.

## Архитектурная схема

```mermaid
flowchart LR
  userAction[UserAction] --> frontendFeature[FrontendFeature]
  frontendFeature --> queryLayer[QueryLayer]
  queryLayer --> apiTransport[ApiTransport]
  apiTransport --> backendApi[BackendApi]
  backendApi --> appLayer[ApplicationUseCases]
  appLayer --> domainLayer[DomainRules]
  appLayer --> repositoryPorts[RepositoryPorts]
  repositoryPorts --> infrastructureAdapters[InfrastructureAdapters]
  infrastructureAdapters --> storageLayer[(InMemoryOrDatabase)]
```

## Frontend

- `app/` содержит bootstrap, providers, theme и router wiring.
- `features/` содержит feature-facing UI, hooks и data access.
- `shared/api/client/` содержит HTTP transport.
- `shared/api/contracts/` содержит generated transport types из OpenAPI и friendly aliases.
- `shared/api/mocks/` содержит mock adapters для локальной разработки.
- `locales/` содержит переводы по языкам.

Frontend-переключение между mock и HTTP идет через `VITE_API_MODE`, а не через изменения page-компонентов.

## Backend

- `Api/Contracts/` содержит HTTP DTO.
- `Application/` содержит use-case сервисы и repository ports.
- `Domain/` содержит доменные сущности и инварианты.
- `Infrastructure/` содержит in-memory adapters и DI.
- `Controllers/` остаются тонкими и маппят application-модели в HTTP DTO.

In-memory слой больше не играет роль application-сервисов; он существует только как adapter хранения.

## Контракты

Канонический source of truth для transport-моделей:

- `backend/openapi/deadmans.v1.yaml`

Frontend генерирует типы через:

```bash
npm --prefix frontend run generate:contracts
```

Это позволяет не поддерживать transport types в двух местах вручную.
