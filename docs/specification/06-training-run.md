# Training run (play mode)

## Entry point
- Play button on a day starts a training run for that day.
- Provide an "Abandon" button to exit the run.

## Exercise flow
- Exercises are ordered within the selected day by exercise order.
- Each exercise appears multiple times based on Sets.
- Example for exercise A with Sets = 2 and RestSeconds > 0:
  - A (set 1) -> Rest -> A (set 2) -> Rest -> next exercise

## Exercise screen
- Centered content:
  - Name
  - Description
  - Repetitions
  - Image (if present)
  - Incrementing timer showing elapsed time on the current set
- Navigation controls below:
  - Previous (left arrow)
  - Next (right arrow)
- The timer resets when moving to the next set/exercise.

## Rest screen
- Shown after each set if RestSeconds > 0.
- Large countdown timer in the center.
- Controls below the timer:
  - "-10" reduces remaining rest time by 10 seconds (min 0)
  - "+10" increases remaining rest time by 10 seconds
- When the countdown reaches 0, advance to the next set/exercise automatically.
- If RestSeconds = 0, skip rest screen.

## State handling
- The sequence (including repeated sets and rest screens) is generated from the plan data.
- Store current state in session storage to resume after a refresh.
- Minimal JS, but required for timers, session storage, and smooth transitions.
