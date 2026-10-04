# Server configuration

## Server

| Key | Type | Presence | Default |
|-----|------|----------|---------|
| [`Server:Host`](#serverhost) | String | default | `localhost` |
| [`Server:Port`](#serverport) | Int32 (Port) | default | `8080` |
| [`Server:RequestTimeout`](#serverrequesttimeout) | TimeSpan | default | `00:00:30` |
| [`Server:MaxConnections`](#servermaxconnections) | Int32 (ConnectionLimit) | optional |  |
| [`Server:AllowedOrigins`](#serverallowedorigins) | IReadOnlyList\<Uri\> (Origins) | required |  |
| [`Server:Features`](#serverfeatures) | IReadOnlyList\<String\> (Features) | default | *empty* |
| [`Server:ApiKey`](#serverapikey) | String | optional |  |

### `Server:Host`

The host name to listen on.

- **Type:** String
- **Format:** Any text.
- **Presence:** default `localhost`

### `Server:Port`

The port to listen on.

- **Type:** Int32 (Port)
- **Format:** A 32-bit integer.
- **Validated:** must be between 1 and 65535
- **Presence:** default `8080`

### `Server:RequestTimeout`

Maximum time allowed for a request.

- **Type:** TimeSpan
- **Format:** A duration as [-][d.]hh:mm:ss[.fffffff], e.g. 00:00:30.
- **Presence:** default `00:00:30`

### `Server:MaxConnections`

Maximum number of concurrent connections. No limit when missing.

- **Type:** Int32 (ConnectionLimit)
- **Format:** A 32-bit integer.
- **Validated:** must be greater than 0
- **Presence:** optional, missing is null

### `Server:AllowedOrigins`

Origins allowed to call the server.

- **Type:** IReadOnlyList\<Uri\> (Origins)
- **Form:** indexed: `Server:AllowedOrigins:0`, `Server:AllowedOrigins:1`, …
- **Validated:** must not be empty
- **Items:** Uri
  - **Format:** An absolute URI, e.g. https://example.com.
- **Presence:** required

### `Server:Features`

Enabled features.

- **Type:** IReadOnlyList\<String\> (Features)
- **Form:** one entry, items separated by `,`
- **Items:** String
  - **Format:** Any text.
- **Presence:** default *empty*

### `Server:ApiKey`

Key for calling the payment provider. Payments are disabled when missing.

- **Type:** String
- **Format:** Any text.
- **Presence:** optional, missing is null
- **Sensitive:** the value is never shown in diagnostics or documentation.

## Admin

| Key | Type | Presence | Default |
|-----|------|----------|---------|
| [`Admin:Port`](#adminport) | Int32 (UnprivilegedPort) | default | `9090` |
| [`Admin:Email`](#adminemail) | String (Email) | required |  |

### `Admin:Port`

The port of the admin interface.

- **Type:** Int32 (UnprivilegedPort)
- **Format:** A 32-bit integer.
- **Validated:** must be between 1 and 65535; must be greater than or equal to 1024
- **Presence:** default `9090`

### `Admin:Email`

Where operational alerts are sent.

- **Type:** String (Email)
- **Format:** Any text.
- **Normalized:** trim whitespace
- **Validated:** must contain @
- **Presence:** required

## Logging

| Key | Type | Presence | Default |
|-----|------|----------|---------|
| [`Logging:Verbosity`](#loggingverbosity) | Verbosity | default | `Normal` |
| [`Logging:TraceMask`](#loggingtracemask) | Int32 (Mask) | default | `0xF` |

### `Logging:Verbosity`

How much the server logs.

- **Type:** Verbosity
- **Format:** One of the names, or its number, ignoring case.
- **Values:** `Quiet`, `Normal`, `Verbose`
- **Presence:** default `Normal`

### `Logging:TraceMask`

Which subsystems write trace messages, one bit each.

- **Type:** Int32 (Mask)
- **Format:** A 32-bit integer in hexadecimal, with or without 0x.
- **Validated:** must be less than or equal to 0xFF
- **Presence:** default `0xF`

