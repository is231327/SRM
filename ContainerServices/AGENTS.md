# Agent Setup Guide

## Overview

This document covers setting up and troubleshooting the SRM monitoring agent (`SRMAgent`) and its associated Shelly devices.

## Agent Credentials

### Default Credentials (from `.env`)

| Field | Value |
|-------|-------|
| **Client Identifier** | `agent1-client` |
| **Client Secret** | `ChangeThisAgentSecret123!` |

### Agent Login Endpoint

```
POST http://srm-auth:8080/api/auth/agent/login
```

The agent uses these credentials to authenticate and obtain an **RS256-signed JWT bearer token** for subsequent API calls.

> **Note:** SRMAuth now issues tokens signed with an **X.509 private key** (asymmetric RS256). SRMCore validates them with the corresponding **public key**. No symmetric signing key is used. See `JWT_CERTIFICATE_SETUP.md` for certificate management.

## Adding an Agent

### Step 1: Create Agent Credentials

1. Navigate to **Agent Credentials** in the frontend
2. Create new credentials (or use the default ones above)
3. Note the **Client Identifier** and **Client Secret**

### Step 2: Add Agent Entry

1. Navigate to your **Server Room** → **Agents**
2. Click **Create Agent**
3. Fill in the fields:
   - **Name**: e.g., `agent1`
   - **IP Address or Hostname**: The address where the agent is reachable (e.g., `http://srm-agent:8080` inside Docker, or `http://localhost:7032` externally)
   - **Server Room**: Assign to the appropriate server room
4. Save

### Step 3: Verify Agent is Running

```bash
docker compose logs srm-agent
```

Expected output:
- `Agent login failed` errors initially (before credentials are created)
- `200` responses from `POST /api/auth/agent/login` once credentials are valid
- `Agent monitoring cycle finished. Submitted readings: 0. Ping checks: 0.` when running

## Adding Shelly Devices

Shelly devices are sensor devices that report readings (temperature, battery, brightness, door status, etc.).

### Pre-configured Shelly Services

The `docker-compose.yml` includes three Shelly demo services:

| Service | Container Name | Port | Image |
|---------|---------------|------|-------|
| `shelly1` | shelly1 | 5000 | `srmprod.azurecr.io/shelly1-demo:latest` |
| `shelly2` | shelly2 | 5001 | `srmprod.azurecr.io/shelly2-demo:latest` |
| `shelly3` | shelly3 | 5002 | `srmprod.azurecr.io/shelly3-demo:latest` |

### Adding Shelly Devices in the Frontend

1. Navigate to your **Agent** → **Shelly Devices**
2. Click **Create Shelly Device**
3. Fill in the fields:

| Field | Value for shelly1 | Value for shelly2 | Value for shelly3 |
|-------|-------------------|-------------------|-------------------|
| **Name** | `shelly1` | `shelly2` | `shelly3` |
| **Base URL** | `http://shelly1:5000` | `http://shelly2:5001` | `http://shelly3:5002` |
| **Device Type** | (leave default or check expected format) | | |
| **MAC Address** | `AA:BB:CC:DD:EE:01` | `AA:BB:CC:DD:EE:02` | `AA:BB:CC:DD:EE:03` |
| **Firmware** | (leave default or check expected format) | | |

> **Note:** The Base URL uses the **Docker network hostname** (e.g., `shelly1`), not `localhost`. This is because the agent runs inside Docker and needs to reach Shelly devices via the Docker network.

### Adding Shelly Devices via API

You can also add Shelly devices through the Core API:

```
POST http://localhost:7030/api/shellydevices
```

## Agent Workflow

The agent follows this monitoring cycle:

1. **Authenticate**: `POST /api/auth/agent/login` with Client Identifier and Client Secret → receives **RS256-signed JWT token** (signed with SRMAuth's private certificate key)
2. **Fetch Configuration**: `GET /api/agent-runtime/configuration` with JWT token → receives configured Shelly devices and monitored devices
3. **Poll Shelly Devices**: `GET {BaseUrl}/status` for each configured Shelly device
4. **Report Sensor Readings**: `POST /api/agent-reporting/sensor-readings` with collected data
5. **Ping Monitored Devices**: ICMP checks for configured monitored devices
6. **Report Ping Results**: `POST /api/agent-reporting/ping-results` with results

## Ports and URLs

| Service | Port | URL (localhost) | URL (Docker network) |
|---------|------|-----------------|-----------------------|
| Auth API | 7031 | `http://localhost:7031` | `http://srm-auth:8080` |
| Core API | 7030 | `http://localhost:7030` | `http://srm-core:8080` |
| Agent API | 7032 | `http://localhost:7032` | `http://srm-agent:8080` |
| Frontend App | 7001 | `http://localhost:7001` | `http://srm-app:8080` |
| Shelly 1 | 5000 | `http://localhost:5000` | `http://shelly1:5000` |
| Shelly 2 | 5001 | `http://localhost:5001` | `http://shelly2:5001` |
| Shelly 3 | 5002 | `http://localhost:5002` | `http://shelly3:5002` |

## Troubleshooting

### Agent Shows "Submitted readings: 0"

If the agent reports `0 readings` after adding Shelly devices:

1. **Restart the agent** to force it to reload the configuration:
   ```bash
   docker compose restart srm-agent
   ```

2. **Verify the agent can reach the Shelly device**:
   ```bash
   docker compose logs srm-agent | grep shelly1
   ```
   You should see `GET http://shelly1:5000/status` with a `200` response.

3. **Check Core API logs** for incoming sensor readings:
   ```bash
   docker compose logs srm-core | grep sensor-readings
   ```

### Agent Login Fails (401 Unauthorized)

1. Verify the agent credentials exist in the database (check Agent Credentials page)
2. Ensure the **Client Identifier** and **Client Secret** in the agent configuration match exactly
3. Check that `SRM_BOOTSTRAP_ADMIN_*` credentials in `.env` are set correctly for initial setup

### No Sensor Data in Frontend

1. Verify the agent is successfully posting to `/api/agent-reporting/sensor-readings`
2. Check that the Shelly device is assigned to the correct agent
3. Ensure the agent is assigned to a server room that you have access to
4. Check the **Sensor Readings** page in the frontend for the specific Shelly device

### JWT Token Validation Errors (SRMCore rejects agent requests)

If the agent gets `401 Unauthorized` or `403 Forbidden` from SRMCore even after successful login:

1. **Verify the certificate thumbprint** on SRMCore matches the one used by SRMAuth:
   ```bash
   # On SRMCore host — verify the public cert thumbprint
   SRM_JWT_VALIDATION_CERTIFICATE_THUMBPRINT=<thumbprint>
   
   # On SRMAuth host — verify the private cert thumbprint
   SRM_JWT_SIGNING_CERTIFICATE_THUMBPRINT=<thumbprint>
   
   # Both must reference the SAME certificate pair
   ```

2. **Verify the token algorithm** is `RS256`:
   ```bash
   # Decode the JWT header from the agent's token
   echo "<TOKEN>" | cut -d. -f1 | base64 -d | python3 -m json.tool
   # Expected: {"alg":"RS256","typ":"JWT"}
   # If it shows "HS256", SRMAuth is still using a symmetric key — check its certificate config
   ```

3. **Check SRMCore logs** for certificate loading errors:
   ```bash
   docker compose logs srm-core | grep -i "certificate\|thumbprint\|x509"
   ```

4. **Restart both services** after any certificate change:
   ```bash
   docker compose restart srm-auth srm-core
   ```

## Agent vs. Monitored Devices

| Feature | Shelly Device | Monitored Device |
|---------|---------------|------------------|
| **Purpose** | Sensor readings (temperature, battery, etc.) | Network connectivity (ICMP ping) |
| **Data Source** | Polls device API (`/status`) | ICMP ping to IP address |
| **Reported To** | `/api/agent-reporting/sensor-readings` | `/api/agent-reporting/ping-results` |
| **Frontend View** | Sensor readings page | Ping results page |

## Key Files

- **`.env`**: Agent credentials (`SRM_AGENT_CLIENT_IDENTIFIER`, `SRM_AGENT_CLIENT_SECRET`)
- **`docker-compose.yml`**: Agent service definition and Shelly services
- **`SRMAgent/`**: Agent source code
- **`USER_MANUAL.md`**: User-facing documentation for managing agents and credentials
- **`TECHNICAL_DOCUMENTATION.md`**: Technical details on agent architecture and API endpoints
- **`JWT_CERTIFICATE_SETUP.md`**: Certificate generation and deployment guide (RS256 asymmetric auth)
