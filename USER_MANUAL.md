# User Manual

## Purpose

This manual explains how to use the Server Room Monitoring application from a user perspective.

The system helps you:

- manage customers
- manage server rooms
- manage monitoring devices
- review collected monitoring data
- manage your own profile and password

## What You Can Do

Depending on your role, you can use the application to:

- sign in
- switch between English and German
- open the home page and dashboard
- manage customers
- manage server rooms
- manage agents
- manage Shelly devices
- manage monitored devices
- review ping results
- review sensor readings
- manage maintenance windows
- manage users
- manage agent credentials
- update your own contact details
- change your own password

## Navigation

The application uses a hierarchical structure.

The usual path is:

1. Open `Customers`
2. Choose a customer
3. Open the related server rooms
4. Open the related agents
5. Open the related Shelly devices or monitored devices
6. Review the collected monitoring data

You can usually move from the business context to the technical details step by step.

## Login

To sign in:

1. Open the login page.
2. Enter your username and password.
3. Confirm the login.

After login, you will see only the areas that are allowed for your role.

## Language Switching

You can switch the application language between:

- English
- German

The language switch affects the visible page texts in the frontend.

## Dashboard

The dashboard gives you a quick overview of the current system.

It is intended as a starting point for navigation and a quick status check.

## Managing Customers

On the customer page, you can:

- create customers
- edit customers
- delete customers

From a customer entry, you can continue to the related server rooms.

## Managing Server Rooms

On the server room page, you can:

- create server rooms
- edit server rooms
- delete server rooms

From a server room entry, you can continue to:

- agents
- maintenance windows

## Managing Agents

On the agent page, you can:

- create agents
- edit agents
- delete agents

From an agent entry, you can continue to:

- Shelly devices
- monitored devices

## Managing Shelly Devices

On the Shelly device page, you can:

- create Shelly devices
- edit Shelly devices
- delete Shelly devices

From a Shelly device entry, you can open the related sensor readings.

## Managing Monitored Devices

On the monitored device page, you can:

- create monitored devices
- edit monitored devices
- delete monitored devices

From a monitored device entry, you can open the related ping results.

## Reviewing Sensor Readings

Sensor readings show the monitoring values reported from a Shelly device.

Typical values include:

- temperature
- battery value
- brightness
- door status
- recording time

## Reviewing Ping Results

Ping results show whether a monitored device was reachable.

Typical values include:

- reachable or not reachable
- response time
- failure count
- time of the check
- possible error message

## Maintenance Windows

Maintenance windows are used to record planned work periods.

You can:

- create maintenance windows
- edit maintenance windows
- delete maintenance windows

These entries help distinguish planned work from unexpected events.

## User Management

If your role allows it, you can manage users.

Typical actions are:

- create users
- edit users
- reset passwords
- activate or deactivate users

Customer-related user management depends on your role.

## Agent Credentials

If your role allows it, you can manage agent credentials.

These credentials are used by monitoring agents to connect to the system.

Typical actions are:

- create agent credentials
- review existing agent credentials
- update existing agent credentials

## Profile and Password

On the profile page, you can:

- update your contact details
- change your password

If an administrator resets your password, you must set a new password after your next login before you can continue normal work.

## Password Rules

Passwords currently must contain:

- at least 12 characters
- at least one uppercase letter
- at least one lowercase letter
- at least one digit
- at least one special character

## Help and Contact

The application contains:

- a help page
- a contact page

These pages provide orientation and project-related contact information.

## Current Limitations

At the current stage:

- ticket system integration is not available yet
- the application is still being expanded and improved
- some advanced security and session features are planned for a later step

## Test Reminder

When testing the application, also check:

- login
- language switching
- password change
- password reset
- profile update
- sensor readings
- ping results
