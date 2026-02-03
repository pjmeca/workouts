# Image storage and uploads

## Storage path
Store uploaded exercise images under the server root using this structure:

```
{userId}/{planId}/{exerciseId}/{imageGuid}.webp
```

Notes:
- Use safe file names and validate extension if needed.
- Use a single image per exercise for now.
- Keep the model ready for multiple images by using a separate ExerciseImage entity.
- Store the original file name in the database.
- Store the GUID-based file name on disk.
- Assume DB is the source of truth for image existence.

## Uploads
- Upload is done via the exercise modal.
- Client-side compression converts images to WebP before upload to reduce size and preserve transparency.
- No explicit limits on file size or format in this version.

## Deletion
- Hard delete images when the exercise (or plan) is deleted.
- Ensure file system cleanup in the delete workflow.
