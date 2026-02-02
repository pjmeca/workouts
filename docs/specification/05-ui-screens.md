# UI screens and flows

## Global UI
- Mobile-first layout.
- Bootstrap for base styles, custom CSS for branding and subtle animations.
- Minimal JavaScript, but enough to avoid unnecessary full-page reloads.
- All UI text in English.

## Screen: Register / Login
- Register form: Email, Username, Password.
- Login form: Username or Email + Password.
- Links between login and register.

## Screen: Plans list (Home)
- List of user training plans.
- Primary CTA: "Create plan".
- Each plan card/row shows:
  - Plan name and short description.
  - Actions on the right: Edit, Delete.
- Delete action shows confirmation dialog.
- Drag and drop ordering for plans, plus Up/Down buttons.

## Screen: Plan details (Days + Exercises)
- Shows plan name and description.
- Sections for each day in order.
- Each day contains a table (or list) of exercises in order.
- Each exercise row shows key fields and actions: Edit, Delete.
- Add Day and Add Exercise actions are clearly visible.
- Exercise creation/editing happens in a modal (no navigation away).
- Drag and drop ordering for days and exercises, plus Up/Down buttons.
- Delete actions show confirmation dialogs.
- Each day has a Play button to start a training run for that day.

## Modal: Exercise create/edit
- Fields: Name, Description, Sets, Repetitions, RestSeconds, Image upload, Notes.
- Image preview (if available).
- Client-side compression before upload.
- Save and Cancel buttons.

## Confirmation dialogs
- Delete plan, day, or exercise requires user confirmation.
- Show impact summary (e.g., "This will remove all exercises and images").

## Training run (Play)
- Fullscreen-focused flow for workouts.
- Abandon button to exit the run.
