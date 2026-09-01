# User Manual

## Purpose

SRM shows server-room temperature, door state, network reachability, incidents, and Redmine synchronization. Available actions depend on your role.

## Roles

| Role | Monitoring access | Configuration | User administration |
|---|---|---|---|
| SystemAdmin | All customers | All | All users and Agent credentials |
| Employee | All customers | All | Customer users and Agent credentials |
| CustomerAdmin | Own customer, read-only | None | Users of own customer |
| Customer | Own customer, read-only | None | None |

Customers never receive another customer's data. Configuration buttons are hidden for customer roles, and Core rejects direct mutation requests from those roles.

## Sign in and profile

1. Open `Login` and enter your username and password.
2. If an administrator reset the password, open `Profile` and choose a new one before using normal navigation.
3. Use `Profile` to update contact details or change the password.
4. Use `Logout` to revoke the current refresh token and access token.

Passwords require at least 12 characters with uppercase, lowercase, digit, and special characters.

## Navigation

The operational hierarchy is:

1. customer
2. server room
3. Agent appliance
4. Shelly or monitored network device
5. sensor readings, ping results, and incidents

The dashboard and overview cards provide shortcuts into that hierarchy. Language switching changes visible UI text between English and German.

## Internal configuration

`SystemAdmin` and `Employee` can create, edit, and delete:

- customers
- server rooms and temperature thresholds
- Agent records
- Shelly devices and their local base URLs
- monitored network devices, ping intervals, timeouts, and failure thresholds
- maintenance windows

Disabling monitoring for a room stops Core from returning monitoring targets to its Agent. Deactivating an individual Agent or device excludes it from runtime monitoring.

Sensor readings and ping results come from the Agent and are read-only in the web UI for every role.

## Monitoring views

### Sensor readings

Each reading shows temperature, battery percentage, brightness, door state, and time. Browser-local time is used in the monitoring result views. Battery and brightness are collected but do not currently create incidents.

### Ping results

Each result shows reachability, round-trip time, consecutive failures, threshold state, error information, and browser-local recorded time. The Agent honors each target's configured interval and timeout. It refreshes configuration every 30 seconds and immediately resets the target's ping schedule and failure counter when ping-relevant settings such as its IP address change.

### Incidents

The incident overview shows the incident type, mapped Redmine priority, source, Redmine ticket status, and initial ticket-creation status. Ticket changes must be made in Redmine. Terminal Redmine statuses (`Resolved`, `Closed`, and `Rejected`) are hidden by default. Use **Show Closed Incidents** to display them as grey cards.

### Maintenance windows

Maintenance windows define planned work for a server room. A door-open reading inside an active window does not create a door incident.

## User administration

- SystemAdmin can manage all supported human roles.
- Employee can manage customer-scoped users.
- CustomerAdmin can manage `Customer` and `CustomerAdmin` users assigned to the same customer.
- Customer cannot manage users.

Administrators can activate/deactivate accounts and reset passwords. A reset forces a password change at the next login.

Agent credentials are available only to SystemAdmin and Employee. The secret is hashed by Auth; rotating it also requires updating the corresponding Agent's private configuration.

## Ticket system

Redmine is a separate on-premise web application. SRM automatically creates tickets for qualifying door, temperature, and connectivity incidents, retries temporary failures, and comments when the condition clears. SRM does not automatically close the Redmine issue. Closing and reopening a door starts a new incident and ticket. Recurring temperature warnings continue to use the same nonterminal Redmine ticket; warning/critical changes update its priority.

## Known limitations

Production-readiness and optional functionality gaps are listed in [TODO.md](TODO.md). The most user-visible omissions are optional battery/brightness alerts, pagination on large lists, and incident acknowledgement workflows.
