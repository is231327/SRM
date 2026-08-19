# User Manual

## Purpose

This manual describes the currently planned backend usage for the Server Room Monitoring project.

At this stage, the backend is the main focus. The Core API will later be tested through Scalar.

The shared domain model currently exists in `SRMShared` and is the basis for the upcoming backend implementation.
The API payload contracts are defined through DTO classes in `SRMShared/DTOs`.
These DTO contracts now also enforce input validation rules for required fields, formats, and value ranges.

The backend solution also contains an automated unit test project named `SRMUnitTests`.
The solution also contains `SRMIntegrationTests` for tests that require a running SQL Server Docker container.

## Planned Backend Usage

The Core API will allow a user or tester to:

- Create and manage customers.
- Create and manage server rooms.
- Register and manage deployed agents.
- Register and manage Shelly devices.
- Create and manage monitored devices for network monitoring.
- Create and manage maintenance windows.
- View and manage stored sensor readings.

## Planned API Access

Once implemented, the Core API can be tested through the Scalar UI exposed by the `SRMCore` service in development mode.

The current implementation uses SQL Server in a Docker container. Created data is persisted in the Docker volume until that volume is removed.

Internally, the API controllers delegate data access and business operations to backend service classes.

The API scope for each domain entity is standard CRUD:

- Create
- Read
- Update
- Delete

## Current Limitations

- Authentication is not implemented yet.
- Ticket system integration is not implemented yet.
- The frontend is not part of the current implementation phase.
- The SQL Server schema is currently initialized through application startup.
- Door incidents are currently represented through sensor readings and are not stored in a separate table.
- The SQL Server Docker data volume must be recreated after entity model changes with the current setup.
