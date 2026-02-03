# Functional requirements

## Training plans
- Create, read, update, delete training plans.
- Each plan belongs to a single user.
- Deleting a plan is a logical delete (soft delete) for plans/days/exercises.
- Image files are still deleted from disk on plan deletion.
- All delete actions require a confirmation dialog.
- Plans are manually ordered; new plans appear at the end.
- Plans can be rearranged by drag and drop or up/down buttons.

## Days
- Create, read, update, delete days within a plan.
- Deleting a day is a logical delete (soft delete) for the day and its exercises.
- Image files are still deleted from disk on day deletion.
- Days are ordered and can be rearranged by drag and drop or up/down buttons.
- New days are added to the end of the list.

## Exercises
- Create, read, update, delete exercises within a day.
- Deleting an exercise is a logical delete (soft delete).
- Exercises are ordered and can be rearranged using up/down buttons (no drag and drop).
- Exercise creation/editing is performed in a modal within the plan view.
- Deleting an exercise removes its image file from disk.
- New exercises are added to the end of the list.

## Training run (play mode)
- From the day list, a Play button starts a training run for that day.
- The training run displays exercises from the selected day in order and one at a time.
- Each exercise appears as many times as its Sets value.
- After each exercise set, show a rest screen if RestSeconds > 0.
- The training run state is stored in session storage to recover after refresh.
- Provide an \"Abandon\" button to exit the run.
- Provide a Pause button that stops all timers and disables navigation until resumed.
- On completion, persist the run stats to the server and update the \"next day\" pointer.

## Image upload
- One image per exercise (for now).
- Client-side compression before upload to reduce server storage (convert to WebP).
- Server stores the file using the specified folder structure and a GUID file name.
- The original file name is stored in the database.

## Data access
- All database access uses EF Core and LINQ.
- User data isolation is enforced in every query.

## Error handling
- Validation errors are shown inline on forms and in modals.
- Deletion actions require confirmation dialog.
