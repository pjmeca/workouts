# Soft delete policy

## Scope
- TrainingPlan, TrainingDay, and Exercise use soft delete.
- ExerciseImage files remain hard-deleted from disk.

## Fields
- Each soft-deletable entity has `DeletedAt` (nullable UTC timestamp).
- A null `DeletedAt` means the entity is active.

## UI behavior
- Soft-deleted plans/days/exercises are hidden from all lists and screens.
- Ordering and "next day" logic ignore soft-deleted items.

## Delete behavior
- Deleting a plan/day/exercise sets `DeletedAt` and updates `UpdatedAt` where applicable.
- Deleting a plan/day/exercise still removes image records and files.
- When deleting a plan:
  - Soft-delete the plan, its days, and exercises.
  - Physically delete exercise images.
  - Remove the `UserNextTrainingDay` row for that plan.
- When deleting a day:
  - Soft-delete the day and its exercises.
  - Physically delete exercise images.
  - Update `UserNextTrainingDay` if needed.
- When deleting an exercise:
  - Soft-delete the exercise.
  - Physically delete the exercise image.

## Query rules
- All queries for plans/days/exercises filter `DeletedAt == null`.
- A global query filter may be used to enforce this consistently.
