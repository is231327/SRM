# JWT Certificate-Based Authentication Setup

This document describes how to generate and deploy X.509 certificates for asymmetric JWT authentication in the SRM project.

## Architecture Overview

| Service       | Role            | Certificate Key |
|---------------|-----------------|-----------------|
| **SRMAuth**   | Token Issuer    | **Private key** (for signing) |
| **SRMCore**   | Token Validator | **Public key** (for validation) |

The certificates use **RS256** (RSA SHA-256) as the signing algorithm. **No symmetric key fallback exists** — both services require a valid certificate.

## Step 1: Generate the Certificate

### Option A: Self-Signed Certificate (Development / Staging)

```bash
# Generate a self-signed certificate with a private key
openssl req -x509 -newkey rsa:4096 -keyout jwt-signing-key.pem -out jwt-certificate.crt \
    -days 3650 -nodes -subj "/CN=SRM-JWT-Issuer/O=SRM/C=DE" \
    -addext "subjectKeyUsage=signing" \
    -addext "extendedKeyUsage=serverAuth"

# Convert to PKCS#12 (.pfx) for Windows / .NET
openssl pkcs12 -in jwt-certificate.crt -inkey jwt-signing-key.pem \
    -out jwt-signing.pfx -legacy -legacy
```

### Option B: Self-Signed CA + Certificate (Production-Style)

```bash
# 1. Create a CA key and self-signed CA certificate
openssl req -x509 -newkey rsa:4096 -keyout ca-key.pem -out ca-certificate.crt \
    -days 3650 -nodes -subj "/CN=SRM-CA/O=SRM/C=DE"

# 2. Create a CSR (Certificate Signing Request) for the JWT signing key
openssl req -newkey rsa:4096 -keyout jwt-signing-key.pem \
    -out jwt-certificate.csr -nodes \
    -subj "/CN=SRM-JWT-Issuer/O=SRM/C=DE" \
    -addext "subjectKeyUsage=signing" \
    -addext "extendedKeyUsage=serverAuth"

# 3. Sign the CSR with the CA
openssl x509 -req -in jwt-certificate.csr -CA ca-certificate.crt -CAkey ca-key.pem \
    -out jwt-certificate.crt -days 3650 -set_serial 01 \
    -extfile <(printf "subjectKeyUsage=signing\nextendedKeyUsage=serverAuth")

# 4. Convert to .pfx
openssl pkcs12 -in jwt-certificate.crt -inkey jwt-signing-key.pem \
    -out jwt-signing.pfx -legacy -legacy
```

### Option C: Enterprise CA (Production)

For production, obtain a certificate from your organization's internal CA or a public CA (e.g., Let's Encrypt) and use the provided `.pfx` or `.crt`/`.key` files.

## Step 2: Install the Certificate on the Server

### On a Windows Server

```powershell
# Import the .pfx certificate into the Local Machine\Personal store
$cert = Import-PfxCertificate -FilePath "C:\certs\jwt-signing.pfx" `
    -CertStoreLocation Cert:\LocalMachine\My `
    -Password (ConvertTo-SecureString "YourPfxPassword" -AsPlainText -Force)

# Verify it was installed
Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -like "*SRM-JWT-Issuer*" }
```

### On a Linux Server (ASP.NET Core with X.509 file loading)

```bash
# Copy the .pfx to a secure location
sudo mkdir -p /etc/aspnetcore/certs
sudo cp jwt-signing.pfx /etc/aspnetcore/certs/
sudo chown www-data:www-data /etc/aspnetcore/certs/jwt-signing.pfx
sudo chmod 640 /etc/aspnetcore/certs/jwt-signing.pfx

# Copy the public cert for SRMCore
sudo cp jwt-certificate.crt /etc/aspnetcore/certs/
sudo chown www-data:www-data /etc/aspnetcore/certs/jwt-certificate.crt
sudo chmod 644 /etc/aspnetcore/certs/jwt-certificate.crt
```

## Step 3: Configure SRMAuth (Token Issuer)

Set the following environment variables on the SRMAuth service:

### Using a .pfx file (recommended for Linux / containers):

```bash
SRM_JWT_SIGNING_CERTIFICATE_PATH=/etc/aspnetcore/certs/jwt-signing.pfx
SRM_JWT_SIGNING_CERTIFICATE_PASSWORD=YourPfxPassword
```

### Using a certificate store lookup (recommended for Windows):

```bash
SRM_JWT_SIGNING_CERTIFICATE_THUMBPRINT=<certificate-thumbprint>
SRM_JWT_SIGNING_CERTIFICATE_STORE=LocalMachine
SRM_JWT_SIGNING_CERTIFICATE_STORE_LOCATION=My
```

## Step 4: Configure SRMCore (Token Validator)

Set the following environment variables on the SRMCore service:

### Using a .crt file (public key only):

```bash
SRM_JWT_VALIDATION_CERTIFICATE_PATH=/etc/aspnetcore/certs/jwt-certificate.crt
```

### Using a certificate store lookup:

```bash
SRM_JWT_VALIDATION_CERTIFICATE_THUMBPRINT=<certificate-thumbprint>
SRM_JWT_VALIDATION_CERTIFICATE_STORE=Root
SRM_JWT_VALIDATION_CERTIFICATE_STORE_LOCATION=CurrentUser
```

## Step 5: Verify the Setup

1. **Check SRMAuth logs** for successful startup (no certificate loading errors).
2. **Request a token** from SRMAuth's login endpoint.
3. **Verify the token's algorithm** is `RS256`:
   ```bash
   # Decode the JWT header (first part of the token, base64url-decoded)
   echo "<TOKEN>" | cut -d. -f1 | base64 -d | python3 -m json.tool
   # Expected: {"alg":"RS256","typ":"JWT"}
   ```
4. **Test a request** to SRMCore with the token — it should succeed.

## Troubleshooting

| Problem                                      | Solution                                                                 |
|----------------------------------------------|--------------------------------------------------------------------------|
| "No certificate found with thumbprint..."    | Verify the thumbprint is correct and the store path is accurate.         |
| "Certificate does not contain a private key" | Ensure the .pfx file contains the private key; re-export if needed.      |
| "Invalid signature" on SRMCore               | Ensure SRMCore uses the **public** key from the **same** certificate.    |
| Certificate expired                          | Renew the certificate and update the thumbprint/env vars on all services.|
| .NET can't load .pfx on Linux                | Ensure `libssl` is installed; set `DOTNET_SYSTEM_GLOBALIZATION_ENABLED=1`.|

## Migration from Symmetric Key

When migrating from the previous symmetric key setup:

1. **Generate a new certificate** (see Step 1).
2. **Install it** on all servers (see Step 2).
3. **Update environment variables** on SRMAuth and SRMCore (Steps 3 & 4).
4. **Restart all services** — existing tokens signed with the old key will be rejected.
5. **Revoke old tokens** in Redis if necessary (they will expire naturally).

All users will need to re-authenticate after the switch.
