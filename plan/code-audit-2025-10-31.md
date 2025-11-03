# .NET Security Audit Report: pihole-dnspropagate

## Executive Summary

This security audit examines the pihole-dnspropagate .NET application, a tool for synchronizing DNS records between Pi-hole instances. The application demonstrates generally good security practices with proper authentication, input validation, and secure communication. However, several areas require attention, particularly around credential management and potential information disclosure.

## Audit Scope

- __Application__: pihole-dnspropagate (.NET 9.0)
- __Components__: Worker service, Teleporter client library, validation framework
- __Dependencies__: 15 NuGet packages analyzed
- __Architecture__: Background worker with HTTP health endpoint

## Critical Findings

### 🔴 HIGH: Plaintext Password Storage and Transmission

__Location__: `src/PiholeDnsPropagate/Options/PrimaryPiHoleOptions.cs`, `src/PiholeDnsPropagate/Options/SecondaryPiHoleOptions.cs`

__Issue__: Passwords are stored and transmitted as plaintext strings throughout the application.

__Risk__:

- Memory dumps could expose credentials
- Logs might inadvertently capture passwords
- Network interception reveals credentials

__Evidence__:

```csharp
public string Password { get; set; } = string.Empty;
```

__Recommendation__:

- Implement secure credential storage using `System.Security.SecureString` or encrypted storage
- Use environment variables or secure vaults for credential management
- Consider implementing OAuth2 or API key authentication if supported by Pi-hole

### 🟡 MEDIUM: Example Credentials in Development Files

__Location__: `.env.dev`

__Issue__: Development environment file contains example passwords that could be committed to version control.

__Risk__: Developers might accidentally commit real credentials or use weak defaults.

__Evidence__:

```javascript
PRIMARY_PIHOLE_PASSWORD=changeme
SECONDARY_PIHOLE_PASSWORDS=changeme,changeme
```

__Recommendation__:

- Remove example credentials from `.env.dev`
- Add `.env*` to `.gitignore` (already present)
- Use placeholder values or environment variable references
- Implement credential validation to prevent weak passwords

## Security Strengths

### ✅ Secure Communication

__Finding__: All HTTP clients properly configured with certificate revocation checking.

__Evidence__:

```csharp
handler.CheckCertificateRevocationList = true;
```

### ✅ Proper Authentication Session Management

__Finding__: Pi-hole authentication uses session tokens with proper expiration and CSRF protection.

__Evidence__:

- Session tokens expire after 5 minutes
- Automatic session refresh on expiration
- Proper session invalidation on disposal

### ✅ Input Validation

__Finding__: Comprehensive validation using FluentValidation for all configuration options.

__Evidence__:

- URI validation for Pi-hole endpoints
- Required field validation for passwords
- Array bounds checking for secondary node configuration

### ✅ Secure Logging

__Finding__: Application uses structured logging with LoggerMessage.Define, preventing log injection.

__Evidence__:

```csharp
private static readonly Action<ILogger, string, Exception?> LogAuthenticationFailure =
    LoggerMessage.Define<string>(LogLevel.Error, new EventId(2001, nameof(PiHoleSessionFactory)),
        "Failed to authenticate against Pi-hole instance {InstanceName}.");
```

### ✅ No Vulnerable Dependencies

__Finding__: All NuGet packages are current with no known vulnerabilities.

__Evidence__: `dotnet list package --vulnerable` returned no vulnerable packages.

### ✅ Resource Management

__Finding__: Proper disposal of HTTP clients and streams throughout the application.

__Evidence__:

- `IDisposable` and `IAsyncDisposable` implementations
- Using statements for stream management
- Proper cleanup in exception scenarios

## Additional Security Observations

### Health Endpoint Security

__Location__: `src/PiholeDnsPropagate.Worker/Services/HealthEndpointService.cs`

__Status__: ✅ SECURE

The health endpoint (`/healthz`) only exposes operational status information and does not leak sensitive configuration details.

### Archive Processing

__Location__: `src/PiholeDnsPropagate/Teleporter/TeleporterArchiveProcessor.cs`

__Status__: ✅ SECURE

TOML parsing is safe with proper error handling and no execution of untrusted content.

### Thread Safety

__Location__: `src/PiholeDnsPropagate.Worker/Services/SyncState.cs`

__Status__: ✅ SECURE

Uses `Interlocked` operations for thread-safe state management.

## Configuration Security Assessment

### Environment Variables

- ✅ Properly ignored in `.gitignore`
- ✅ Used for sensitive configuration
- ❌ Example values in development files

### Docker Security

- ✅ Multi-stage build reduces attack surface
- ✅ Non-root execution (implicit in .NET runtime images)
- ✅ Diagnostics disabled in production

## Recommendations

### Immediate Actions (High Priority)

1. __Implement Secure Credential Storage__

   - Replace plaintext password storage with `SecureString`
   - Use environment variables exclusively for credentials
   - Remove example passwords from development files

2. __Add Credential Validation__

   - Implement password strength requirements
   - Validate credential format and length

### Medium Priority

3. __Implement Rate Limiting__

   - Add rate limiting to health endpoint
   - Implement backoff strategies for failed authentication

4. __Enhanced Monitoring__

   - Add audit logging for authentication events
   - Implement credential rotation detection

### Low Priority

5. __Code Hardening__

   - Add security headers to HTTP clients
   - Implement request timeouts consistently
   - Add input size limits for archive processing

## Compliance Considerations

- __GDPR__: Plaintext password storage may violate data protection requirements
- __SOX__: Sensitive credential handling needs review
- __NIST__: Aligns with most security controls but needs credential management improvements

## Conclusion

The pihole-dnspropagate application demonstrates solid security fundamentals with proper authentication, validation, and communication security. The primary concern is plaintext credential handling, which should be addressed immediately. The codebase shows good security awareness with modern .NET security practices throughout.

__Overall Security Rating: B (Good with critical improvements needed)__

__Risk Level__: MEDIUM - Address credential management issues before production deployment.
