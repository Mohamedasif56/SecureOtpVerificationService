# Secure OTP Verification Service

Secure email OTP assessment implementation using C# and .NET 8.

## Features

- Exact `dso.org.sg` domain validation
- Cryptographically secure six-digit OTP generation
- One-minute OTP validity
- Maximum ten verification attempts
- Timeout protection around blocking input
- Expiry re-check after input returns
- Fixed-time OTP comparison
- OTP invalidation after success, timeout, or failed attempts
- xUnit automated tests

## Run

```bash
dotnet restore
dotnet build
dotnet run --project src/SecureOtpVerificationService
```

Use `user@dso.org.sg`. The console email sender prints the simulated email and OTP.

## Test

```bash
dotnet test
```

## Assumptions

1. Only the exact `dso.org.sg` domain is accepted; subdomains are rejected.
2. The task-provided email text `You OTP Code is...` is preserved exactly.
3. The supplied `send_email` dependency is represented by `IEmailSender`.
4. One OTP is active per service instance.
5. A successfully sent new OTP replaces the previous OTP.
6. OTP validity starts at generation time.
7. Invalid OTP input consumes an attempt.
8. OTP state is cleared after success, timeout, or ten failed attempts.
9. Caller cancellation is propagated separately from OTP timeout.
10. A production distributed implementation should use centralized temporary storage such as Redis, rate limiting, audit events, and safe logging.

## Testing Strategy

Automated tests cover malformed emails, unauthorized domains, sender failure, correct OTP, ten incorrect attempts, blocking-input timeout, and correct OTP entered after expiry.

For production, add concurrency, rate-limit, load, provider integration, distributed-state, and observability tests.
