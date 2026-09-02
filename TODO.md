# Remaining Work

This file lists verified gaps after the code and documentation review on September 1, 2026. Completed work is documented in the relevant specification.

## Required before production use

- Replace `Database.EnsureCreated()` with versioned EF Core migrations for both SQL databases. Existing databases must currently be recreated after model changes.
- Add dependency-aware readiness checks for SQL Server, Redis, Redmine, Auth, and Core. The current `/health` endpoints only prove that the process is running.
- Protect the Agent's Shelly webhook with the authentication mechanism supported by the selected physical Shelly model. The exact mechanism remains undecided until that device capability is confirmed.
- Add retention/archival rules for sensor readings, ping results, incident events, and ticket synchronization history.
- Validate external `CustomerId` and `AgentId` references across the Core/Auth service boundary through an explicit provisioning workflow.

## Test coverage

- Add Redmine adapter/worker integration tests covering creation, retry, duplicate suppression, and resolution comments.
- Extend the HTTP-level authorization matrix to every protected controller. Foundational tests already cover all roles, customer filtering, anonymous requests, and read-only customer mutation attempts for server rooms.
- Add end-to-end Blazor tests for password/MFA login, MFA enrollment and recovery, forced password change, role-aware controls, configuration, and incidents.
- Add Agent tests with a real or protocol-compatible physical Shelly device.

## Optional or deferred functionality

- Add alert rules for low battery and abnormal brightness if those optional requirements are selected.
- Add Agent-offline and Shelly-unreachable incidents.
- Add incident acknowledgement and manual lifecycle actions if required.
- Add pagination to growing telemetry and incident lists. Filtering and sorting are implemented for incidents, sensor readings, and monitored-device ping results.
- Add administrative user/credential deletion or explicit revocation workflows.
