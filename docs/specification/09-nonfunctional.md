# Non-functional requirements

## UX and performance
- Mobile-first responsive design.
- Fast initial load; avoid full-page reloads where possible.
- Use partial updates (Razor partials + minimal JS or HTMX-style approach) to keep UX fluid.
- Provide subtle UI animations for transitions and reorder feedback.

## Accessibility
- Buttons have accessible labels and focus styles.
- Color contrast meets WCAG AA for text and controls.

## Security
- Use ASP.NET Identity for auth and password hashing.
- Authorize access to plan/day/exercise by user ownership.
- Validate all inputs server-side.
- Prevent path traversal in file uploads.

## Reliability
- Use transactions for reorder operations.
- Log failures for file upload/delete operations.

## Logging
- Use Serilog for structured logging.
- Write logs to disk, one file per day.
- Retain 31 days of log files, deleting older files.
- Use appropriate severity levels (Debug, Information, Warning, Error, Fatal).

## Maintainability
- Keep JavaScript minimal and isolated.
- Prefer server-rendered Razor Pages with clear page models.
- Use EF Core migrations for database changes.
