# AGENTS.md — Project Conventions & Guide

This file documents the SRM project structure, architecture, and conventions for developers and AI agents.

---

## 1. Project Overview

**SRM (Server Room Monitoring)** is a .NET 10.0 web application that monitors server rooms via on-site agents and Shelly sensor devices.

| Service | Type | Purpose |
|---------|------|---------|
| `SRMAuth` | ASP.NET Core API | Authentication, user/agent management, JWT **issuance** (RS256) |
| `SRMCore` | ASP.NET Core API | Business logic, CRUD endpoints, JWT **validation** (RS256), Redmine ticket sync |
| `SRMApp` | Blazor Server | Management UI (login, dashboard, CRUD pages) |
| `SRMAgent` | .NET Hosted Service | On-site monitoring agent (polls Shelly + ICMP) |
| `SRMShared` | .NET Class Library | Shared entities, DTOs, mappers, config models |
| `SRMUnitTests` | NUnit | Unit tests (in-memory EF, DTO validation) |
| `SRMIntegrationTests` | NUnit | Integration tests (real SQL Server via Docker) |

**Key rule:** Only `SRMAuth` creates JWTs (private key). `SRMCore` and `SRMAgent` only consume/validate them (public key).

---

## 2. Solution Structure

```
SRM/
├── SRM.sln
├── SRMAuth/                         # Auth service (JWT issuer)
│   ├── Program.cs                   # DI, auth middleware, signing cert resolution
│   ├── appsettings.json             # Non-secret defaults (Jwt, JwtSigningCertificate, Redis, BootstrapAdmin)
│   ├── Configuration/JwtOptions.cs  # Jwt: section (issuer, audience, lifetimes)
│   ├── Services/
│   │   ├── AuthService.cs           # Login, logout, refresh, password reset
│   │   └── JwtTokenService.cs       # RS256 token issuance (IOptions<JwtCertificateOptions>)
│   ├── Data/
│   │   ├── SrmAuthDbContext.cs      # EF Core context (identity data)
│   │   └── AuthDbSeeder.cs          # Bootstrap admin seeding
│   └── Middleware/AuthorizationExceptionMiddleware.cs
├── SRMCore/                         # Core API (JWT validator)
│   ├── Program.cs                   # DI, validation cert resolution, Redmine worker
│   ├── appsettings.json             # Non-secret defaults (Jwt, JwtValidationCertificate, Redis, Redmine)
│   ├── Controllers/                 # CRUD REST endpoints (CrudControllerBase<T>)
│   ├── Services/                    # Business logic per entity
│   ├── Mappings/                    # DTO mappers (ICrudDtoMapper<T>)
│   ├── Data/SrmCoreDbContext.cs     # EF Core context (domain data)
│   └── Middleware/AuthorizationExceptionMiddleware.cs
├── SRMApp/                          # Blazor Server frontend
│   ├── Pages/                       # Razor pages (login, dashboard, CRUD)
│   └── Services/                    # Typed HTTP clients for SRMCore
├── SRMAgent/                        # Monitoring agent
│   ├── Program.cs
│   └── Services/                    # Polling, ping, reporting, orchestrator
├── SRMShared/                       # Shared library
│   ├── Configuration/
│   │   ├── JwtCertificateOptions.cs # Shared cert config (Thumbprint/Store/Path/Password)
│   │   ├── DevelopmentEnvironment.cs # Dev env var → config key mapping
│   │   └── ...
│   ├── Entities/                    # Customer, ServerRoom, Agent, ShellyDevice,
│   │                                # MonitoredDevice(PingResult), MaintenanceWindow,
│   │                                # SensorReading, Incident(IncidentEvent), TicketLink,
│   │                                # AuthUser, AgentCredential
│   ├── DTOs/                        # One folder per entity: <Entity>Create/Update/ReadDto + mapper
│   └── Auth/                        # AuthClaimTypes, AuthRoles, AuthRoleType, RedisTokenStateStore
├── SRMUnitTests/                    # NUnit unit tests
├── SRMIntegrationTests/             # NUnit integration tests (real SQL Server)
├── ContainerServices/
│   ├── docker-compose.yml           # Infra + app containers
│   ├── .env                         # Docker env vars (gitignored; source of truth for compose)
│   ├── certs/                       # jwt-signing.pfx (SRMAuth) + jwt-certificate.crt (SRMCore)
│   └── AGENTS.md                    # Agent-specific docs
├── PythonShelly/                    # Shelly simulator containers
├── env-development                  # Local dev env vars (dotnet run; NOT used by docker compose)
├── JWT_CERTIFICATE_SETUP.md         # Certificate generation & deployment guide
├── TECHNICAL_DOCUMENTATION.md
├── USER_MANUAL.md
└── README.md
```

---

## 3. JWT Authentication (Asymmetric, RS256)

### 3.1 Architecture

```
SRMApp ──POST /api/auth/login──► SRMAuth (issuer, private key: jwt-signing.pfx)
SRMApp ◄── RS256-signed JWT ────
SRMApp ──Bearer: <JWT>─────────► SRMCore (validator, public key: jwt-certificate.crt)
SRMAgent ──POST /api/auth/agent/login──► SRMAuth ──► agent JWT ──► SRMCore validates
```

- **Algorithm:** RS256. **There is no symmetric signing key anywhere** (`Jwt:SigningKey` was removed).
- **SRMAuth** signs with the private key (`.pfx` file path or Windows cert store lookup).
- **SRMCore** validates with the public key (`.crt` file path or Windows cert store lookup).
- Both keys come from the **same certificate pair** (verify via matching SHA1 thumbprint).
- If no certificate is configured, startup/request fails with a clear error — there is no fallback.

### 3.2 Configuration Sections

| Section | Bound to | Service |
|---------|----------|---------|
| `Jwt:` | `JwtOptions` (Issuer, Audience, lifetimes) | SRMAuth + SRMCore |
| `JwtSigningCertificate:` | `JwtCertificateOptions` | SRMAuth |
| `JwtValidationCertificate:` | `JwtCertificateOptions` | SRMCore |

**Common pitfall:** certificate settings live in `JwtSigningCertificate:` / `JwtValidationCertificate:`, **not** in `Jwt:`. `JwtTokenService` injects `IOptions<JwtCertificateOptions>` — reading cert properties from `JwtOptions` will silently yield nulls.

### 3.3 Environment Variables

```
# SRMAuth (signing)
SRM_JWT_SIGNING_CERTIFICATE_THUMBPRINT=          # or leave empty and use PATH
SRM_JWT_SIGNING_CERTIFICATE_STORE=
SRM_JWT_SIGNING_CERTIFICATE_STORE_LOCATION=
SRM_JWT_SIGNING_CERTIFICATE_PATH=/certs/jwt-signing.pfx
SRM_JWT_SIGNING_CERTIFICATE_PASSWORD=

# SRMCore (validation)
SRM_JWT_VALIDATION_CERTIFICATE_THUMBPRINT=
SRM_JWT_VALIDATION_CERTIFICATE_STORE=
SRM_JWT_VALIDATION_CERTIFICATE_STORE_LOCATION=
SRM_JWT_VALIDATION_CERTIFICATE_PATH=/certs/jwt-certificate.crt
SRM_JWT_VALIDATION_CERTIFICATE_PASSWORD=
```

- `Path` takes precedence over `Thumbprint`.
- `Thumbprint` uses the **Windows certificate store** (`X509Store`) — it does **not** work inside Linux containers. In Docker, always use `Path`.
- `JwtSigningCertificate__Path` / `JwtValidationCertificate__Path` are what `docker-compose.yml` passes into the containers. **Single-file bind mounts** enforce key separation: `srm-auth` gets only `jwt-signing.pfx` at `/certs/`, `srm-core` gets only `jwt-certificate.crt` at `/certs/` (the private key is never visible to SRMCore).

### 3.4 Token Lifecycle

1. `SRMAuth` issues the JWT (RS256, `kid` = cert thumbprint, includes `jti`).
2. `SRMCore` validates signature/issuer/audience/lifetime via `X509SecurityKey`, then checks the JTI in Redis for revocation.
3. Logout revokes the refresh token and stores the access token JTI in Redis.

---

## 4. Architecture Patterns

### 4.1 SRMCore — CRUD Flow

```
Controller (CrudControllerBase<T>) → Service → ICrudDtoMapper → EF Core DbContext
```

- **Controllers:** one REST controller per entity, inheriting `CrudControllerBase<T>`.
- **DTOs:** `<Entity>CreateDto`, `<Entity>UpdateDto`, `<Entity>ReadDto` in `SRMShared/DTOs/<Entity>/` with a `<Entity>DtoMapper`.
- **Services:** application logic per entity in `SRMCore/Services/`.
- **Validation:** Data annotations on DTOs; violations return `400` with detailed messages.

### 4.2 Entity Conventions

Every entity has: `Id` (Guid PK), `CreatedAtUtc`, `UpdatedAtUtc` (set on create/update).

### 4.3 Auth Flow

1. `SRMApp` POSTs credentials to `SRMAuth /api/auth/login`.
2. `SRMAuth` validates against SQL identity data, issues RS256 JWT.
3. `SRMApp` stores the token in the Blazor Server session and forwards it as Bearer.
4. `SRMCore` validates via public key + Redis JTI check, then processes the request.

### 4.4 Agent Flow

1. `SRMAgent` POSTs machine credentials to `SRMAuth /api/auth/agent/login`.
2. `SRMAuth` issues an RS256 agent JWT (requires a seeded `AgentCredential` — only the bootstrap admin is auto-seeded).
3. Agent GETs runtime config from `SRMCore`, polls Shelly devices + ICMP pings.
4. Agent POSTs sensor readings / ping results; `SRMCore` validates the JWT and enforces agent scope.

---

## 5. Environment Files (Two Separate Sources of Truth)

| File | Used by | Notes |
|------|---------|-------|
| `ContainerServices/.env` | `docker compose` (auto-loaded from compose dir) | In-container hostnames: `srm-sqlserver,1433`, `srm-redis:6379`, `http://srm-auth:8080/`. Cert paths: `/certs/...` (volume mount). |
| `env-development` (repo root) | Local `dotnet run` | Host-mapped ports: `localhost,1434`, `localhost:6381`, `http://localhost:7031/`. Cert paths: `../ContainerServices/certs/...` (relative to the project dir `dotnet run` starts in). |

**Docker compose does NOT read `env-development`.** New variables must be added to `ContainerServices/.env` for containerized runs and to `env-development` for local runs.

Host port mapping (docker): SQL `1434→1433`, Redis `6381→6379`, SRMApp `7001`, SRMAuth `7031`, SRMCore `7030`, SRMAgent `7032`, Redmine `3000`, Shelly sims `5000–5002`.

---

## 6. Docker & Deployment

### 6.1 Local Development (hybrid)

```bash
cd ContainerServices
docker compose up -d                    # infra + all app containers
# or, for hot-reload of app services:
docker compose up -d srm-sqlserver srm-redis srm-redmine srm-redmine-db shelly1 shelly2 shelly3
dotnet run --project SRMAuth            # uses env-development
dotnet run --project SRMCore
dotnet run --project SRMApp
dotnet run --project SRMAgent
```

### 6.2 Full Docker Build

```bash
cd ContainerServices
docker compose up --build -d
docker compose logs srm-auth srm-core   # check for certificate errors
```

### 6.3 Azure Deployment

```powershell
powershell -ExecutionPolicy Bypass -File .\Refresh-AzureEnvironmentFile.ps1
powershell -ExecutionPolicy Bypass -File .\Apply-AzureContainerAppConfiguration.ps1
```

---

## 7. Testing

### 7.1 Prerequisites

- **Unit tests:** no external dependencies (in-memory EF, fakes for JWT/Redis — `FakeJwtTokenService` means the RS256 certificate code is NOT covered by tests).
- **Integration tests:** a running SQL Server (e.g. `docker compose up -d srm-sqlserver`). They connect via the `SRM_TEST_SQL_CORE_CONNECTION` / `SRM_TEST_SQL_AUTH_CONNECTION` **environment variables** (or `ConnectionStrings__SrmCoreDatabase` / `ConnectionStrings__SrmAuthDatabase` env vars). Values from `env-development` / `.env` files are NOT used for connections.

### 7.2 Running

If your host has the full .NET 10 SDK **including the ASP.NET Core shared framework** (`dotnet --list-runtimes` must show `Microsoft.AspNetCore.App`):

```bash
dotnet test SRMUnitTests
dotnet test SRMIntegrationTests   # with the SRM_TEST_SQL_* vars exported
```

Otherwise (e.g. host only has `Microsoft.NETCore.App`), run via the SDK Docker image:

```bash
# Unit tests
docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test SRMUnitTests

# Integration tests (--network host so localhost,1434 reaches the SQL container)
docker run --rm --network host -v "$PWD":/src -w /src \
  -e "SRM_TEST_SQL_CORE_CONNECTION=Server=localhost,1434;Database=SRMCoreIntegrationTestsDb;User Id=sa;Password=ChangeThisSqlPassword123!;TrustServerCertificate=True;Encrypt=False" \
  -e "SRM_TEST_SQL_AUTH_CONNECTION=Server=localhost,1434;Database=SRMAuthIntegrationTestsDb;User Id=sa;Password=ChangeThisSqlPassword123!;TrustServerCertificate=True;Encrypt=False" \
  mcr.microsoft.com/dotnet/sdk:10.0 dotnet test SRMIntegrationTests
```

---

## 8. Key Conventions

- **Secrets:** never in source or tracked `appsettings.json`. Only env vars / gitignored `.env` files. `appsettings.json` holds structural defaults only.
- **Configuration:** `IOptions<T>`; `appsettings.json` + env vars; `DevelopmentEnvironment.cs` maps `SRM_*` vars to config keys in dev (e.g. `SRM_JWT_SIGNING_CERTIFICATE_PATH` → `JwtSigningCertificate:Path`).
- **Error handling:** `AuthorizationExceptionMiddleware` returns consistent auth failure responses; DTO validation → `400`.
- **Audit fields:** `CreatedAtUtc` / `UpdatedAtUtc` on every entity.
- **Token revocation:** access tokens via Redis JTI check on every SRMCore request; refresh tokens Redis-backed (target architecture).
- **No EF Core migrations** — `Database.EnsureCreated()` is used. Schema changes require volume reset.

---

## 9. Certificate Management

Full guide: `JWT_CERTIFICATE_SETUP.md`.

1. Generate an RSA certificate (self-signed or CA-signed).
2. Export `jwt-signing.pfx` (private) and `jwt-certificate.crt` (public) into `ContainerServices/certs/`.
3. Verify the pair matches:
   ```bash
   openssl x509 -in ContainerServices/certs/jwt-certificate.crt -noout -fingerprint -sha1
   openssl pkcs12 -in ContainerServices/certs/jwt-signing.pfx -nokeys -clcerts -passin pass: \
     | openssl x509 -noout -fingerprint -sha1
   ```
4. Set `SRM_JWT_SIGNING_CERTIFICATE_PATH` / `SRM_JWT_VALIDATION_CERTIFICATE_PATH` in both env files.
5. Restart: `docker compose up --build -d srm-auth srm-core`.

**Rotation:** generate a new pair, replace the files, restart both services. Old tokens expire naturally (no forced logout).

---

## 10. Common Tasks

### 10.1 Add a New Entity

1. Entity in `SRMShared/Entities/` (Guid Id + audit fields).
2. DTOs + mapper in `SRMShared/DTOs/<Entity>/`.
3. `ICrudDtoMapper` implementation in `SRMCore/Mappings/`.
4. Service in `SRMCore/Services/`.
5. Controller in `SRMCore/Controllers/` inheriting `CrudControllerBase<T>`.
6. Register in `Program.cs`; EF Core entity (schema via `EnsureCreated`).
7. Frontend pages in `SRMApp/Pages/` + typed HTTP client in `SRMApp/Services/`.
8. Unit tests in `SRMUnitTests/`.

### 10.2 Add an Environment Variable

1. Add to **both** `ContainerServices/.env` (container values) and `env-development` (local values).
2. Add the mapping in `SRMShared/Configuration/DevelopmentEnvironment.cs` if used by local dev.
3. Add the `environment:` entry in `docker-compose.yml` for the target service(s).

### 10.3 Troubleshooting JWT / Docker

- **`Certificate thumbprint or path must be configured`** → the `*Certificate__Path` env var is empty in the container (`docker inspect <c> --format '{{range .Config.Env}}...' | grep -i cert`); check `ContainerServices/.env`.
- **`401` from SRMCore after successful login** → cert pair mismatch or issuer/audience mismatch; check `docker compose logs srm-core | grep -i "certificate\|x509"`.
- **Agent login loop in logs** → no `AgentCredential` seeded yet (expected; create one via the UI).
- **Port conflicts** → compose maps SQL to `1434` and Redis to `6381` on the host to avoid clashes; in-container ports remain `1433`/`6379`.

---

## 11. Important Notes

- **Target framework:** .NET 10.0 (`net10.0`).
- **Seed data:** only the bootstrap admin user (see `SRM_BOOTSTRAP_ADMIN_*` vars). No demo data.
- **Redmine integration:** disabled by default (`SRM_REDMINE_ENABLED=false`); requires project + API key setup.
- **Shelly simulators:** three containers from `PythonShelly/`, host ports 5000–5002.
- **HTTPS:** plain HTTP between containers; HTTPS redirection is dev-only.
- **`X509Store` fallback:** the thumbprint/store lookup path targets Windows only — do not rely on it in containers.
