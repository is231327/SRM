# Remaining Work

This file lists verified gaps after the code and documentation review on August 31, 2026. Completed work is documented in the relevant specification.

## Required before production use

- Replace `Database.EnsureCreated()` with versioned EF Core migrations for both SQL databases. Existing databases must currently be recreated after model changes.
- Add dependency-aware readiness checks for SQL Server, Redis, Redmine, Auth, and Core. The current `/health` endpoints only prove that the process is running.
- Protect the Agent's Shelly webhook with the authentication mechanism supported by the selected physical Shelly model. The exact mechanism remains undecided until that device capability is confirmed.
- Add audit records for login failures, password changes, user/credential administration, and monitoring configuration changes.
- Make refresh-token rotation atomic in Redis so concurrent use of one refresh token cannot create more than one successor session.
- Add retention/archival rules for sensor readings, ping results, incident events, and ticket synchronization history.
- Validate external `CustomerId` and `AgentId` references across the Core/Auth service boundary through an explicit provisioning workflow.

## Test coverage

- Add automated Redis integration tests for refresh rotation, logout, and access-token revocation.
- Add Redmine adapter/worker integration tests covering creation, retry, duplicate suppression, and resolution comments.
- Add HTTP-level authorization tests for every role, including read-only customer mutation attempts and cross-customer access.
- Add end-to-end Blazor tests for login, forced password change, role-aware controls, configuration, and incidents.
- Add Agent tests with a real or protocol-compatible physical Shelly device.

## Optional or deferred functionality

- Add alert rules for low battery and abnormal brightness if those optional requirements are selected.
- Add Agent-offline and Shelly-unreachable incidents.
- Add incident acknowledgement and manual lifecycle actions if required.
- Add pagination, filtering, and sorting for growing telemetry and incident lists.
- Add administrative user/credential deletion or explicit revocation workflows.
