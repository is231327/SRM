# Ticket Integration Specification

## Purpose

This document defines the target integration concept for the Redmine-based on-premise ticket system of the Server Room Monitoring project.

The goal is to describe:

- which monitoring events create tickets
- which service is responsible for ticket decisions
- how duplicate tickets are avoided
- which data must be sent to the ticket system
- which implementation steps are still open

This is a specification only. It does not mean that ticket creation is already implemented.

## Scope

The ticket integration will be implemented in the company-hosted backend, not in the customer-side agent.

The agent is responsible for:

- collecting Shelly readings
- collecting monitored-device ping results
- sending raw monitoring data to `SRMCore`

`SRMCore` is responsible for:

- persisting monitoring data
- evaluating alert conditions
- checking maintenance windows
- deciding whether a ticket must be created, updated, or closed
- calling the future ticket-system integration adapter

This keeps business rules centralized and prevents duplicated alert logic in multiple agents.

## Architectural Decision

The recommended design is:

- `SRMAgent` reports monitoring facts
- `SRMCore` creates and manages incidents
- `SRMCore` stores ticket work items for queued processing
- a background worker in `SRMCore` sends ticket commands to an internal ticket-integration component
- the ticket-integration component talks to the selected on-premise ticket system over its API

The ticket system should not be called directly by the agent.

Reasons:

- maintenance-window logic belongs in the backend
- duplicate suppression must be global and centralized
- severity mapping must be consistent across all customers
- the monitoring-data ingestion path must not fail because Redmine is temporarily unavailable
- future manual actions such as acknowledge, reopen, and resolve should belong to the backend business layer

## Ticket-Relevant Event Types

The following events should be considered ticket-relevant.

### Door Open Outside Maintenance Window

Trigger condition:

- a `SensorReading` reports `DoorOpen = true`
- there is no active `MaintenanceWindow` for the related `ServerRoom` at the reading time

Expected result:

- create a high-priority incident
- create a ticket if no open ticket for the same condition already exists

### Temperature Warning Threshold Exceeded

Trigger condition:

- a `SensorReading` reports a temperature greater than or equal to `ServerRoom.TemperatureWarningThreshold`
- the temperature is still below the critical threshold

Expected result:

- create or update a warning-level incident
- create a ticket immediately

### Temperature Critical Threshold Exceeded

Trigger condition:

- a `SensorReading` reports a temperature greater than or equal to `ServerRoom.TemperatureCriticalThreshold`

Expected result:

- create or escalate an incident immediately
- create a high-priority or urgent ticket

### Monitored Device Failure Threshold Reached

Trigger condition:

- a persisted `MonitoredDevicePingResult` reports that the configured failure threshold has been reached or exceeded

Expected result:

- create or update a connectivity incident
- create a ticket if no open ticket exists for the same monitored device outage

### Optional Future Events

These events are not part of the first ticket iteration but should stay compatible with the design:

- Shelly offline or unreachable
- low battery threshold reached
- abnormal brightness detection
- repeated vibration or tamper indicators
- agent offline for a defined period

## Incident Model

The cleanest design is to distinguish between raw monitoring data and derived incidents.

Recommended future entities:

- `AlertRule`
- `Incident`
- `IncidentEvent`
- `TicketLink`

For the first implementation, `AlertRule` can stay implicit in code and configuration, but `Incident` and `TicketLink` should eventually exist as persisted backend concepts.

### Incident Purpose

An incident represents an active business problem such as:

- server-room door open unexpectedly
- server-room temperature too high
- monitored device unreachable

An incident is not the same as a single sensor reading or a single ping result.

### Incident Lifecycle

Suggested lifecycle:

1. `Open`
2. `Acknowledged`
3. `Resolved`
4. `Closed`

Optional later state:

- `Suppressed`

## Duplicate Suppression

Duplicate suppression is required. Without it, each polling cycle could create a new ticket for the same problem.

Recommended correlation keys:

- door incident: `ServerRoomId + ShellyDeviceId + IncidentType`
- temperature incident: `ServerRoomId + ShellyDeviceId + IncidentType`
- connectivity incident: `MonitoredDeviceId + IncidentType`

Rules:

- if an open incident with the same correlation key exists, do not create a new ticket
- instead, append an event or update the existing incident
- if the issue is resolved and happens again later, create a new incident and a new ticket

## Severity Mapping

The following initial mapping is sensible.

| Event Type | Suggested Severity | Suggested Priority |
|---|---|---|
| Door open outside maintenance window | Critical | Urgent |
| Temperature critical threshold exceeded | Critical | Urgent |
| Temperature warning threshold exceeded | Warning | High |
| Monitored device failure threshold reached | Major | High |

This mapping should stay configurable in a later iteration.

## Ticket Payload Requirements

Every created ticket should contain at least:

- customer name
- server room name
- incident type
- severity
- event timestamp in UTC
- human-readable summary
- detailed description
- source system identifier
- correlation key

Additional fields that are strongly recommended:

- Shelly device name if relevant
- monitored device name and IP address if relevant
- measured values such as temperature, brightness, battery percent, response time, and failure count
- whether a maintenance window was active
- internal incident identifier from `SRMCore`

## Ticket System Adapter

The backend should not bind its business logic directly to one ticket product.

Recommended abstraction:

- `ITicketingService`

Recommended first operations:

- `CreateTicketAsync`
- `UpdateTicketAsync`
- `CloseTicketAsync`
- `AddCommentAsync`

The first implementation targets:

- Redmine

The abstraction should still allow later adapters for:

- MantisBT
- other on-premise products

## Endpoint Responsibilities

No public frontend or agent endpoint should create tickets directly.

Recommended responsibility split:

- `SRMAgent` sends monitoring data only
- `SRMCore` reporting services persist the data
- `SRMCore` incident evaluation logic decides whether an incident changes state
- `SRMCore` persists queued ticket work items
- a background worker processes queued ticket work items against Redmine

If a manual acknowledge or resolve workflow is added later, that should happen through dedicated `SRMCore` endpoints and not through direct calls from `SRMApp` to the external ticket system.

## Security Requirements

- ticket-system credentials must come from configuration or secret management, never from source code
- all integration calls must use TLS where supported
- outbound ticket calls must be logged with enough detail for troubleshooting, but without leaking secrets
- failed ticket creation attempts must be retried in a controlled way
- duplicate ticket creation must be prevented even if retry logic is active

## Non-Functional Requirements

- ticket creation must be idempotent for the same open incident
- temporary ticket-system outages must not block monitoring-data persistence
- ticket integration failures must be observable through logs and later through health checks
- the backend must use asynchronous queued ticket submission

## Recommended First Implementation Slice

The first implementation should stay small and deterministic.

Recommended order:

1. add backend incident evaluation for door-open outside maintenance window
2. add backend incident evaluation for monitored-device failure threshold reached
3. add backend incident evaluation for warning and critical temperature incidents
4. add a provider abstraction for Redmine
5. add queued ticket processing
6. persist a link between backend incidents and external ticket identifiers
7. add comment-only resolution handling when the underlying condition clears

## Chosen Decisions

- first on-premise ticket system: `Redmine`
- ticket creation mode: `queued`
- warning-level temperature incidents: `create tickets immediately`
- resolution behavior: `add a comment to the external ticket, do not auto-close it`
- frontend behavior: `ticket data may be viewed in SRMApp, but ticket modification remains exclusive to Redmine`

## Implementation TODOs

- add incident domain entities to `SRMShared`
- add incident persistence to `SRMCore`
- add queue-oriented ticket dispatch persistence in `SRMCore`
- add incident evaluation services in `SRMCore`
- define the exact Redmine API contract
- add a Redmine adapter and first implementation
- add a queued ticket worker in `SRMCore`
- add integration tests for incident creation and duplicate suppression
- add frontend pages for incident and ticket visibility
