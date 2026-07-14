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

## Testing Strategy

Automated tests cover malformed emails, unauthorized domains, sender failure, correct OTP, ten incorrect attempts, blocking-input timeout, and correct OTP entered after expiry.
