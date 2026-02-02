# Overview and scope

## Purpose
Provide a simple web app to create, organize, and run personal training routines. Each user manages their own training plans, days, and exercises. The app prioritizes a fast and smooth UX with minimal JavaScript and server-rendered Razor Pages.

## In scope
- User registration and login using ASP.NET Identity.
- CRUD for training plans.
- CRUD for days within a plan.
- CRUD for exercises within a day.
- Ordering of days and exercises using drag and drop plus up/down controls.
- Training run (play mode) that shows one exercise at a time, with series and rest handling.
- Image upload per exercise (single image now, model prepared for multiple images later).
- File storage on the server for images following the defined path convention.

## Out of scope (for this version)
- Password reset or email verification.
- Admin roles or privileged users.
- Sharing plans between users.
- Internationalization or multiple languages.
- Mobile apps or offline usage.

## Success criteria
- A user can register, log in, and manage training plans without page reload friction.
- Ordering changes are intuitive on both desktop and mobile.
- Play mode is smooth, clear, and usable during a workout.
- All data is scoped to the current user.
