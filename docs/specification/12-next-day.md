# Next training day tracking

## Goal
Show a visual indicator for the user's next training day within a plan.

## Data model
- `UserNextTrainingDay` stores the next day per user + plan:
  - `UserId`
  - `TrainingPlanId`
  - `TrainingDayId`
  - `UpdatedAt`

## UI
- In the plan details view, the next day is marked with a badge:
  - Text: "next day"
  - Style: green badge, strong solid border, soft solid green background, text color matches border.

## Behavior
- If no `UserNextTrainingDay` row exists for the user+plan, the UI treats the first day in the list as the next day (client-side only).
- When a training run finishes, the backend updates `UserNextTrainingDay` for that plan to the next day in the current list order.
- The pointer is circular: if the completed day is the last in the current order, the next day becomes the first in the list.
- Reordering days does **not** automatically change the stored next-day pointer. It only changes when a run completes or a day is deleted.

## Deletion rules
- Before deleting a day, if it is the current next day for that plan:
  - Compute the next day in the list order **excluding** the day being deleted.
  - Update the `UserNextTrainingDay` entry to that day.
  - If no days remain, remove the entry.
- When deleting a plan, remove any `UserNextTrainingDay` entry for that plan.

## API changes
- The training run completion endpoint updates the next-day table after saving analytics.
