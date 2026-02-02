# Data model

## Notes
- Use Entity Framework Core with LINQ for all data access.
- Include ordering fields for draggable lists.
- Primary key types:
  - TrainingPlan: Guid
  - TrainingDay: int (composite key with TrainingPlanId)
  - Exercise: int (composite key with TrainingDayId)
  - ExerciseImage: Guid

## Entities

### ApplicationUser
- Id
- Email
- UserName
- PasswordHash (managed by Identity)
- Navigation: TrainingPlans

### TrainingPlan
- Id
- UserId (FK to ApplicationUser)
- Name (required)
- Description (optional)
- CreatedAt
- UpdatedAt
- OrderIndex (required for manual list ordering)
- Navigation: Days

### TrainingDay
- Id (int)
- TrainingPlanId (FK, part of composite PK)
- Name (required, e.g., "Day 1 - Upper Body")
- Notes (optional)
- OrderIndex (required for ordering in a plan)
- Navigation: Exercises

### Exercise
- Id (int)
- TrainingPlanId (FK, part of composite PK)
- TrainingDayId (FK, part of composite PK)
- Name (required)
- Description (optional)
- Sets (required, integer > 0)
- Repetitions (required, integer > 0)
- RestSeconds (optional, integer >= 0)
- Notes (optional)
- OrderIndex (required for ordering in a day)
- Navigation: Images

### ExerciseImage
- Id
- ExerciseId (FK)
- OriginalFileName
- StoredFileName (Guid-based file name used on disk)
- RelativePath (e.g., user/plan/exercise/{imageGuid}.jpg)
- ContentType (optional)
- SizeBytes (optional)
- IsPrimary (bool; only one true for now)
- CreatedAt

## Indexes and constraints
- Unique index on ApplicationUser.Email and UserName.
- Composite index on TrainingPlan (UserId, OrderIndex).
- Composite primary key on TrainingDay (TrainingPlanId, Id).
- Composite primary key on Exercise (TrainingPlanId, TrainingDayId, Id).
- Composite index on TrainingDay (TrainingPlanId, OrderIndex).
- Composite index on Exercise (TrainingPlanId, TrainingDayId, OrderIndex).
- Enforce single primary image per exercise at the application level (and optionally a filtered unique index).

## Ordering rules
- TrainingDay.OrderIndex defines the order within a plan.
- Exercise.OrderIndex defines the order within a day.
- When reordering, update the affected range in a single transaction.
- OrderIndex is 0-based and contiguous.
- New items append at the end of their list.
