# Application TODO checklist

This checklist tracks what is needed to complete the app. Update it whenever a task is completed or new tasks are added.

## Foundation
- [x] Create functional specification in `docs/specification`
- [x] Create `AGENTS.md` with project rules
- [x] Scaffold Razor Pages structure manually (no `dotnet new` in this environment)
- [x] Add EF Core + Npgsql + Identity packages
- [x] Configure Serilog with daily file logs and console output
- [x] Add appsettings placeholders for PostgreSQL and image storage
- [x] Create domain models (Plan/Day/Exercise/Image) and relationships
- [x] Configure `ApplicationDbContext` with composite keys and indexes
- [x] Add EF Core migrations (user will run locally)

## Authentication
- [x] Implement Identity login (email or username)
- [x] Implement registration with unique email and username validation
- [x] Add logout page
- [ ] Style Identity validation errors and messages (if needed)

## Plans (CRUD + ordering)
- [x] Plans list (index) with Create/Edit/Play actions
- [x] Create plan form
- [x] Edit plan form
- [x] Delete plan with confirmation modal
- [x] Implement plan ordering (drag & drop + up/down buttons)
- [x] Persist plan ordering to database

## Days (CRUD + ordering)
- [x] Create day form
- [x] Edit day form
- [x] Delete day with confirmation modal
- [x] Implement day ordering (drag & drop + up/down buttons)
- [x] Persist day ordering to database

## Exercises (CRUD + ordering)
- [x] Implement exercise create/edit modal with validation
- [x] Upload image from modal (single image now)
- [x] Client-side image compression before upload
- [x] Save image metadata in DB (original name + GUID file name)
- [x] Store image file to disk with configured base path
- [x] Delete exercise with confirmation modal
- [x] Delete image file from disk on exercise/plan delete
- [x] Implement exercise ordering (up/down buttons only)
- [x] Persist exercise ordering to database

## Training run (Play mode)
- [x] Play page scaffold
- [x] Build exercise sequence (days + exercises + sets + rest screens)
- [x] Add timer + rest countdown UI
- [x] Session storage persistence for current step
- [x] Abandon training confirmation (modal)
- [x] Previous/Next navigation

## UX/UI
- [x] Base layout + theme styles + animations
- [x] Confirmation modal (Bootstrap) in shared layout
- [x] Mobile-first refinements for tables and buttons
- [x] Empty states for day/exercise lists
- [x] Drag-and-drop styling and accessibility

## Logging and error handling
- [x] Serilog request logging + daily file logs
- [x] Basic error page
- [x] Log image upload/delete success + failures
- [x] Log reorder operations

## Security & validation
- [x] Server-side validation for plan create/edit
- [x] Validation for day forms
- [x] Validation for exercise forms
- [ ] Ensure user scoping for all queries (days/exercises)

## Documentation
- [x] Keep specification updated
- [ ] Update TODO checklist as work progresses
