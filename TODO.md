# TODO

## Deferred Features

- Implement the ticket integration defined in `TICKET_INTEGRATION_SPECIFICATION.md`.
- Move `SRMAuth` token state from SQL Server to Redis so the implementation matches the required target architecture.
- Keep SQL Server for identity data only in `SRMAuth`, and use Redis for refresh tokens and access-token revocation.
- Add optional brightness and battery monitoring in the UI and alerting logic.
- Add alerting rules and escalation logic.
- Add ticket creation rules derived from sensor readings, ping failures, and maintenance windows.
- Add duplicate suppression and correlation for repeated incident conditions.
- Add comment-only ticket resolution behavior once incident lifecycle rules are implemented.
- Add automated integration coverage for the Redmine worker and Redmine API synchronization flow.
- Add retry/backoff refinement and operational monitoring for failed Redmine synchronizations.
- Add audit logging for configuration changes and critical events.
- Add soft delete or archival strategy for historical business data where required.
- Add database migrations and a repeatable database recreation workflow for the SQL Server Docker container.
- Add health checks, resiliency policies, and service-to-service retry handling.
- Add validation, pagination, filtering, and sorting to all list endpoints.
- Run and expand `SRMIntegrationTests` regularly against the real SQL Server container.
- Add consistent API error response documentation for validation and business-rule failures.
- Decide whether `Database.EnsureCreated()` should be replaced with EF Core migrations.
- Review and explicitly define cascade delete behavior for all parent-child relations.
- Add uniqueness constraints and matching API/database enforcement where business keys must be unique.
- Add service-layer business validation beyond DTO annotations where required.
- Review whether `AgentReadDto.ApiKeyReference` should remain exposed in API responses.
- Keep auth role definitions aligned between enum-backed code roles and seeded database roles.
- Add Redis-backed integration tests for refresh-token rotation, logout, and token revocation once Redis is introduced.
- Expand tests for customer-scoped authorization behavior in `SRMCore`.
- Add user deletion or deactivation strategy decision for administrative user management.
- Refine policy-based authorization where broader or more explicit policies are still useful.
- Extend role-aware frontend behavior beyond the current navigation and user-management entry points.
- Add a clearer user-facing distinction between deactivate, reactivate, and delete operations in the administrative UI.
- Add broader automated tests for password-policy validation and password-change failure cases.
- Add integration tests for `SRMAuth` authorization scope and password-management endpoints.
- Further harden the current `SRMApp` server-side authenticated session model for production deployment.
- Add regression tests for the forced password-change flow in `SRMApp` and `SRMAuth`.
- Add end-to-end tests for password change, password reset, profile update, and agent monitoring flows.
- Consider whether agent credential management should later support delete, revoke, or multiple credentials per agent.
- Fix the remaining `SRMIntegrationTests` project-level restore/build issue so the newer agent integration tests can be executed reliably.
- Add alert/ticket generation rules based on persisted monitored-device ping results and failure-threshold state.
- Add richer webhook hardening if the final Shelly delivery model requires additional validation or shared-secret protection.

## Current Scope

- Backend-first implementation.
- `SRMCore` is testable through Scalar.
- Shared domain entities are implemented in `SRMShared`.
- DTOs are implemented in `SRMShared/DTOs` with per-entity `Base`, `Create`, `Update`, and `Read` models.
- SQL Server-backed CRUD controllers are implemented in `SRMCore` for all current domain entities.
- Controllers delegate business access to service classes in `SRMCore`.
- Unit tests exist for the current CRUD services and controllers in `SRMUnitTests`.
- A separate `SRMIntegrationTests` project exists for real SQL Server-backed integration tests.
- DTO validation is implemented for required fields, formats, ranges, non-empty GUIDs, and cross-field business checks.
- DTO-based controllers use a shared generic `CrudControllerBase`.
- DTO mapping is handled through injectable generic mapper classes implementing `ICrudDtoMapper`.
- `SRMApp` provides a bilingual English/German hierarchical UI over the current Core API.
- CRUD pages exist for the current domain entities, including `MonitoredDevicePingResult`.
- Authentication is active across `SRMAuth`, `SRMCore`, and the current `SRMApp` UI flow.
- The authentication and authorization concept is documented in `AUTHENTICATION_AUTHORIZATION_CONCEPT.md`.
- The current auth implementation still stores token state in SQL Server and therefore does not yet fully match the required SQL-plus-Redis target architecture.
- A dedicated authenticated agent reporting path exists in `SRMCore`.
- `SRMAgent` loads runtime configuration from `SRMCore`, polls configured virtual Shelly devices, and executes monitored-device ping checks in a background worker.
- Monitored-device ping results are persisted in `SRMCore`, and the agent reports consecutive failure counts plus failure-threshold state.
- Incident persistence is implemented in `SRMCore` for door, temperature, and monitored-device failure conditions.
- Agent-reported sensor readings and ping results now trigger backend incident evaluation.
- Ticket links are persisted in `SRMCore`, and a queued Redmine worker processes `PendingCreate` and `PendingComment` entries.
- Machine principals are managed as `AgentCredential` entries and can be maintained from the UI.
- Door state remains part of `SensorReading` instead of being modeled as a separate domain entity.
- `SensorReading` references only `ShellyDevice`; `Agent` and `ServerRoom` are derived relations.
- Human users can manage their own profile and password.
- Access tokens can be refreshed and revoked through `SRMAuth`.
- Logout revokes both refresh tokens and the active access-token JTI.
- Administrative password resets force the target user to change the password on next login.
- Development-only demo data has been removed from the tracked application startup flow.
- Application secrets and runtime-specific endpoints are configured through environment variables and the root `.env` file for local Docker-based execution.
- `docker-compose.yml` can start the full local stack, including SQL Server, Redmine, `SRMAuth`, `SRMCore`, `SRMAgent`, and `SRMApp`.
- `docker-compose.yml` also starts the local virtual Shelly simulators `shelly1`, `shelly2`, and `shelly3`.

## Operational Notes

- Local SQL Server runs in Docker via `docker-compose.yml`.
- Local application secrets and connection values are read from the root `.env` file when the Docker stack is started.
- The current implementation uses `Database.EnsureCreated()` instead of migrations.
- When the entity model changes, the SQL Server Docker data volume must be recreated so the schema can be rebuilt from the new model.
- Because this version includes the persisted `MonitoredDevicePingResult` table, the SQL Server Docker data volume must be recreated before runtime testing if the old Core schema still exists.
- Because this version renamed the auth-side machine principal model to `AgentCredential`, the auth SQL Server data volume or database must be recreated before runtime testing if the old auth schema still exists.
- Because this version removed the obsolete `AgentCredentials -> Agent` foreign key from `SRMAuth`, the auth SQL Server data volume or database must be recreated before runtime testing if the old auth schema still exists.
- Because this version adds the `Incident`, `IncidentEvent`, and `TicketLink` tables to `SRMCore`, the SQL Server Docker data volume or database must be recreated before runtime testing if the old Core schema still exists.
- Local Redmine now runs through `docker-compose.yml`, but it still requires manual project creation and API-key configuration before ticket synchronization can succeed.
- When you start the next manual test round, include authentication, forced password change after admin reset, self-service profile update, password-policy validation, and the agent monitoring flow in the test scope.
- When the auth schema changes again, the SQL Server Docker database or volume must be recreated unless the project is moved to EF Core migrations.

## Frontend Scope

- Keep the bilingual home page and language switching aligned with the latest navigation structure.
- Keep the dashboard page aligned with the currently available backend data.
- Maintain the hierarchical customer, server room, agent, Shelly device, monitored device, monitored-device ping result, maintenance window, and sensor reading pages.
- Maintain the help and contact pages.
- Extend the current user management UI with search, filtering, and pagination.
