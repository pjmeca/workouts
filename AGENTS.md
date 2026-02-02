# Instructions for agents

## Project overview
This repository will contain a .NET 10 Razor Pages web app for managing training routines. The full functional spec is in `docs/specification/index.md`.

## Required stack
- ASP.NET Core (.NET 10) Razor Pages
- ASP.NET Identity for authentication (no admin roles)
- Entity Framework Core + LINQ for all DB access
- PostgreSQL as the database
- Bootstrap for base HTML/CSS
- Serilog for application logging

## UI and UX rules
- All UI text must be in English.
- Mobile-first responsive layout.
- Minimal JavaScript; prefer server-rendered pages and partial updates.
- Add simple, tasteful CSS animations for transitions and reorder feedback.
- Use a modal to create/edit exercises within the plan details screen.
- Implement drag and drop ordering plus up/down buttons.
- All delete actions must show confirmation dialogs.

## Functional rules
- CRUD: plans, days, exercises.
- Plan ownership is private to each user.
- Hard delete only, including image files.
- Training run (play mode) shows one exercise at a time, includes rest screens, and repeats sets.
- Login accepts either email or username; both must be unique.
- List ordering is manual; new items appear at the end.
- OrderIndex is always 0-based.
- Persist play mode state in session storage and include an Abandon button.

## Image storage
- Store one image per exercise now, but keep the model ready for multiple images.
- Path format: `{userId}/{planId}/{exerciseId}/{imageGuid}.jpg`
- Delete files from disk when their exercise or plan is deleted.
- Compress images client-side before upload if possible.
- Store the original file name in the database and use a GUID file name on disk.

## Data access
- Use EF Core and LINQ only (no raw SQL).
- Scope all queries by the current user.
- Use transactions for reorder operations.

## Logging
- Use Serilog with daily rolling files.
- Retain 31 days of log files.

## Documentation
- Keep `docs/specification` aligned with any functional changes.
