# Validation rules

## Training plan
- Name: required, 1-100 chars.
- Description: optional, 0-500 chars.

## Day
- Name: required, 1-100 chars.
- Notes: optional, 0-500 chars.

## Exercise
- Name: required, 1-100 chars.
- Description: optional, 0-1000 chars.
- Sets: required, integer >= 1.
- Repetitions: required, integer >= 1.
- RestSeconds: optional, integer >= 0.
- Notes: optional, 0-1000 chars.
- Image: optional (single file).

## Ordering
- OrderIndex must be unique within its parent scope (day or exercise list).
- Reordering must maintain a contiguous, 0-based index.
