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
- Plans, days, and exercises use soft delete; images remain hard-deleted.
- Training run (play mode) shows one exercise at a time, includes rest screens, and repeats sets.
- Play mode includes pause/resume.
- Show a \"next day\" badge per plan and persist next-day pointer.
- Login accepts either email or username; both must be unique.
- List ordering is manual; new items appear at the end.
- OrderIndex is always 0-based.
- Persist play mode state in session storage and include an Abandon button.
- Capture and persist analytics (per-set/rest timings) on completion. Each run uses a GUID id.

## Image storage
- Store one image per exercise now, but keep the model ready for multiple images.
- Path format: `{userId}/{planId}/{exerciseId}/{imageGuid}.webp`
- Delete files from disk when their exercise or plan is deleted.
- Convert and compress images to WebP client-side before upload if possible.
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
- Update the TODO checklist in `docs/specification/11-todo.md` whenever a task is completed or new tasks are added.
