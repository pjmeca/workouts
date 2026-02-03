# Workouts (working title)

Workouts is a .NET 10 Razor Pages web app for planning and running training routines. It lets each user manage their own plans, days, and exercises, and then run workouts in a focused play mode.

> ⛵ Try it out at: [workouts.pjmeca.com](https://workouts.pjmeca.com)! *(beta site — all plans will be ported to the final release)*


## Highlights
- ASP.NET Identity authentication (login by email or username).
- Training plans, days, and exercises with manual ordering.
- Play mode that runs exercises set-by-set with rest screens and timers.
- Image upload per exercise (single image, future-ready for multiple).
- PostgreSQL + Entity Framework Core (code-first).
- Serilog logging with daily rolling files.

## Tech stack
- ASP.NET Core (.NET 10) Razor Pages + Identity
- Entity Framework Core (LINQ)
- PostgreSQL
- Bootstrap + custom CSS
- Serilog

## Storage notes
- Exercise images are stored on disk under `{userId}/{planId}/{exerciseId}/{imageGuid}.webp`.
- Image metadata (original name, GUID) is stored in the database.

## Development process note
This project was **structured vibe coded** — meaning it followed a controlled vibe‑coding process using Codex with precise technical guidance and an iterative, guided workflow. The initial development journal is available in [`docs/research`](docs/research/structured-vibe-coding-journal.md) if you want to review the process (user prompts are in Spanish 🇪🇸 — my native language).
