# Users and authentication

## Identity provider
- Use ASP.NET Identity.
- Store users in PostgreSQL via Entity Framework Core.
- No privileged roles or admin accounts.

## Registration
- Required fields: Email, Username, Password.
- Email and Username must be unique across the application.
- Registration form validates uniqueness and rejects duplicates.
- Email is used for contact/login; no verification flow in this version.
- Password reset is not implemented in this version.

## Login
- Users can log in with either Email or Username.
- Remember-me can be offered as a standard Identity option.

## Authorization
- All training data is owned by the user who created it.
- Each query must be scoped to the current user (server-side enforcement).
- Hard delete only; no soft-delete.
