# Play mode pause

## UI placement
- A pause icon button appears in the top-right corner of the workout card (the white rectangle).
- The button is visible during exercise and rest screens.

## Pause behavior
- When paused:
  - Show a large play button centered in the workout card.
  - Dim the workout content and disable all navigation controls (Previous/Next/Rest +/-).
  - All timers stop (exercise elapsed and rest countdown).
- When resumed:
  - The overlay/play button disappears.
  - Timers resume from the exact paused values.
- Pause/resume can be toggled any number of times in the same run.

## State persistence
- Store pause state and paused timer values in session storage so a refresh keeps the run paused.
- Resuming should continue from the saved paused state.

## Accessibility
- The pause button has an accessible label (e.g., "Pause workout").
- The resume play button has an accessible label (e.g., "Resume workout").
