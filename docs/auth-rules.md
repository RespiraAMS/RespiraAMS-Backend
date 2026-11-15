# Authentication

The authentication service exposes the session and account endpoints under `/api/v1/auth`:

```http
GET  /api/v1/auth/accounts?page=1&size=10&role=MedicalRegulationReviewer&status=Active&isEmailConfirmed=true
POST /api/v1/auth/login
POST /api/v1/auth/logout
POST /api/v1/auth/refresh
POST /api/v1/auth/email-verification
POST /api/v1/auth/email-verification/confirm
```

`GET /api/v1/auth/accounts` lists the accounts page by page and accepts optional
`role` (`Doctor`, `HeadDepartmentDoctor`, `MedicalRegulationReviewer`, `Admin`),
`status` (`Active`, `Inactive`), and `isEmailConfirmed` (`true`, `false`) filters.
The endpoint requires `Admin` or `HeadDepartmentDoctor` authorization (`Admin` may
view any role, while `HeadDepartmentDoctor` is scoped to `Doctor` accounts).

A user who forgot its password resets it by email. The first endpoint issues a
single-use token and mails a link that carries it, the second one consumes the
token and stores the new password:

```http
POST /api/v1/auth/forgot-password          { "email": "doctor@respira.ams" }
POST /api/v1/auth/forgot-password/confirm  { "token": "<token from the link>",
                                             "newPassword": "<new password>",
                                             "confirmPassword": "<new password>"}
```

The link points at the `PasswordReset:ResetUrl` of the AppHost configuration and
carries the token as a `token` query parameter. Only the SHA-256 hash of the token
is stored, the raw one only travels by email. The token expires after
`PasswordReset:TokenExpiresHours` hours and only the newest one stays valid, so
a reset link can be used once. A password has to be at least 8 characters long and
contain a letter and a digit.

Changing the password revokes every refresh token of the account, so the sessions
opened with the previous password cannot be used anymore. The user signs in again
with the new one.
