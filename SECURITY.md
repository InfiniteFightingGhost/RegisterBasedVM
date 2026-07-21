# Security Policy

## Project Status & Security Updates

Raptor is in active development on the `main` branch. Security fixes, memory safety patches, and bytecode verifier updates are committed directly to `main`. Once stable releases begin, security backports will follow tagged release channels.

---

## Reporting a Vulnerability

Because **Raptor** uses high-performance C# `unsafe` code, stack allocation, raw pointers, and pinned memory buffers to achieve zero-GC execution, safety bounds in the `BytecodeVerifier` are critical.

If you discover a memory safety vulnerability, bytecode verification bypass, out-of-bounds execution exploit, or security issue, please do **NOT** open a public GitHub issue.

### Security Disclosure Process

1. **GitHub Private Security Advisory**: Report vulnerabilities privately via [GitHub Security Advisories](https://github.com/InfiniteFightingGhost/Raptor/security/advisories/new) or contact maintainer [@InfiniteFightingGhost](https://github.com/InfiniteFightingGhost).
2. **Details to Include**:
   - Description of the issue or bytecode exploit payload.
   - Proof of concept script (`.rapt` or `.rasm`) demonstrating the vulnerability.
   - Impact assessment (e.g. host memory read/write out of bounds).
3. **Response Timeline**:
   - We will acknowledge receipt of your report within 48 hours.
   - We will provide a status update and estimated timeline for a patch within 5 business days.
   - Once resolved, we will publish a security advisory and credit the reporter.
