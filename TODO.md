# TODO

## Deferred Features

- Add authentication and authorization with a dedicated token service.
- Integrate an on-premise ticket system and define the ticket creation workflow.
- Add Shelly webhook handling for immediate door status updates.
- Add periodic temperature polling from the agent to the Shelly device.
- Add optional brightness and battery monitoring.
- Add alerting rules and escalation logic.
- Add ticket creation rules derived from sensor readings and maintenance windows.
- Add audit logging for configuration changes and critical events.
- Add soft delete or archival strategy for historical business data where required.
- Add database migrations and a repeatable database recreation workflow for the SQL Server Docker container.
- Add health checks, resiliency policies, and service-to-service retry handling.
- Add validation, pagination, filtering, and sorting to all list endpoints.
- Add automated tests for domain logic, controllers, and persistence.

## Current Scope

- Backend-first implementation.
- Core API should be testable through Scalar.
- Shared domain entities are implemented in `SRMShared`.
- DTOs are implemented in `SRMShared/DTOs` with per-entity `Base`, `Create`, `Update`, and `Read` models.
- SQL Server-backed CRUD controllers are implemented in `SRMCore` for all current domain entities.
- Controllers delegate business access to service classes in `SRMCore`.
- Unit tests are implemented for all current CRUD services and controllers in `SRMUnitTests`.
- A separate `SRMIntegrationTests` project exists for real SQL Server-backed integration tests.
- DTO validation is implemented for required fields, formats, ranges, non-empty GUIDs, and cross-field business checks.
- The DTO-based controllers use a shared generic `CrudControllerBase` again to reduce repetition.
- DTO mapping is now handled through injectable generic mapper classes implementing `ICrudDtoMapper`.
- Full CRUD will be implemented for domain entities.
- Authentication and ticket integration are intentionally out of scope for the first iteration.
- Door state remains part of `SensorReading` instead of being modeled as a separate domain entity.
- `SensorReading` references only `ShellyDevice`; `Agent` and `ServerRoom` are derived relations.

## Operational Notes

- Local SQL Server runs in Docker via `docker-compose.yml`.
- The current implementation uses `Database.EnsureCreated()` instead of migrations.
- When the entity model changes, the SQL Server Docker data volume must be recreated so the schema can be rebuilt from the new model.
