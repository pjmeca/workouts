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
- [ ] Implement exercise create/edit modal with validation
- [ ] Upload image from modal (single image now)
- [ ] Client-side image compression before upload
- [ ] Save image metadata in DB (original name + GUID file name)
- [ ] Store image file to disk with configured base path
- [ ] Delete exercise with confirmation modal
- [ ] Delete image file from disk on exercise/plan delete
- [ ] Implement exercise ordering (drag & drop + up/down buttons)
- [ ] Persist exercise ordering to database

## Training run (Play mode)
- [x] Play page scaffold
- [ ] Build exercise sequence (days + exercises + sets + rest screens)
- [ ] Add timer + rest countdown UI
- [ ] Session storage persistence for current step
- [ ] Abandon training confirmation (modal)
- [ ] Previous/Next navigation

## UX/UI
- [x] Base layout + theme styles + animations
- [x] Confirmation modal (Bootstrap) in shared layout
- [ ] Mobile-first refinements for tables and buttons
- [ ] Empty states for day/exercise lists
- [ ] Drag-and-drop styling and accessibility

## Logging and error handling
- [x] Serilog request logging + daily file logs
- [x] Basic error page
- [ ] Log image upload/delete success + failures
- [ ] Log reorder operations

## Security & validation
- [x] Server-side validation for plan create/edit
- [x] Validation for day forms
- [ ] Validation for exercise forms
- [ ] Ensure user scoping for all queries (days/exercises)

## Documentation
- [x] Keep specification updated
- [ ] Update TODO checklist as work progresses
