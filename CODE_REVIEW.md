# Code Review Findings

## Scope
- Reviewed authentication/navigation flow in frontend scripts and pages.
- Reviewed middleware and startup behavior in backend API.
- Performed static analysis only (build tooling unavailable in this environment).

## Findings

### 1) Broken login redirect path from nested pages (High)
**Where**
- `Frontend/js/auth.js`
- `Frontend/js/api.js`

**Issue**
- Auth failure redirects use `window.location.href = 'login.html'`.
- Most protected pages live under `Frontend/pages/`, so this resolves to `Frontend/pages/login.html` (non-existent) instead of `Frontend/login.html`.

**Impact**
- Users on nested pages can land on a 404 or remain on a broken route after token expiration/401.

**Recommendation**
- Centralize redirect target via a helper (for example, `CONFIG.LOGIN_PATH`) and use root-relative paths (`/login.html`) or computed base path logic.

---

### 2) CSP blocks existing inline event handlers (High)
**Where**
- `Backend/Program.cs` sets `Content-Security-Policy` with `script-src 'self'` (no `'unsafe-inline'`).
- Multiple HTML pages use inline handlers like `onclick="..."`.

**Issue**
- Inline handlers are blocked by current CSP policy.

**Impact**
- Buttons/actions implemented via inline `onclick` may silently fail in browsers enforcing CSP.

**Recommendation**
- Prefer removing inline handlers and attaching events in JS modules.
- If temporary compatibility is needed, use nonce/hash-based CSP rather than enabling broad `'unsafe-inline'`.

---

### 3) Root endpoint redirects to Swagger in non-development environments (Medium)
**Where**
- `Backend/Program.cs`

**Issue**
- Swagger is enabled only in development, but `/` always redirects to `/swagger`.

**Impact**
- In production/staging, `/` likely redirects users to a 404.

**Recommendation**
- Gate root redirect by environment, or return a lightweight API info payload/health response when Swagger is disabled.

## Notes
- Attempted to run `dotnet build`, but .NET SDK is not available in this container.
