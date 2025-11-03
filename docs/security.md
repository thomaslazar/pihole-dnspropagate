# Security Guidance

This project interacts with the Pi-hole HTTP API, which currently requires plaintext passwords. While plaintext credentials are unavoidable for the final HTTP request, we can still minimise exposure by following these practices.

## Why Plaintext Passwords Are Required
- Pi-hole `/api/auth` only accepts raw password strings.
- `.NET` `SecureString` is deprecated and still requires conversion back to `string` before sending requests.
- Consequently, hashing or encrypting the password inside the worker would not protect against a process-memory attacker and would break compatibility with Pi-hole.

## Recommendations
1. **Use environment variables or orchestrator secrets** to provide the Pi-hole credentials. Avoid storing them in source-controlled files.
2. **Never log credentials.** The worker already avoids logging password values; contributors must keep it that way.
3. **Minimise lifetime.** Sessions are short-lived (the worker logs in, performs the sync, and calls `DELETE /api/auth`). Avoid caching passwords beyond configuration.
4. **Rotate credentials regularly.** Change Pi-hole password/application tokens periodically and after any suspected compromise.
5. **Consider external secret stores.** For production deployments, consume credentials from services like HashiCorp Vault, Azure Key Vault, Kubernetes secrets, etc.

## Development Tips
- `.env.dev` contains placeholder values only; always override with real credentials via environment variables when testing.
- When sharing debug logs, scrub any URLs that might contain credentials.
- Ensure Compose files used in real environments reference secrets instead of inline passwords.

Refer back to this guidance when reviewing pull requests that touch authentication or configuration handling.
