using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Workouts.Data;
using Workouts.Models;
using Workouts.Options;
using Microsoft.Extensions.Options;

namespace Workouts.Pages.Plans;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DetailsModel> _logger;
    private readonly ImageStorageOptions _imageOptions;
    private readonly IWebHostEnvironment _environment;

    public DetailsModel(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger<DetailsModel> logger,
        IOptions<ImageStorageOptions> imageOptions,
        IWebHostEnvironment environment)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
        _imageOptions = imageOptions.Value;
        _environment = environment;
    }

    public TrainingPlan Plan { get; private set; } = new();
    public bool NotFound { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var plan = await _db.TrainingPlans
            .AsNoTracking()
            .Include(p => p.Days)
            .ThenInclude(d => d.Exercises)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (plan == null)
        {
            NotFound = true;
            return Page();
        }

        plan.Days = plan.Days.OrderBy(d => d.OrderIndex).ToList();
        foreach (var day in plan.Days)
        {
            day.Exercises = day.Exercises.OrderBy(e => e.OrderIndex).ToList();
        }

        Plan = plan;
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteDayAsync(Guid planId, int dayId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var plan = await _db.TrainingPlans
            .Include(p => p.Days)
            .ThenInclude(d => d.Exercises)
            .ThenInclude(e => e.Images)
            .FirstOrDefaultAsync(p => p.Id == planId && p.UserId == userId);

        if (plan == null)
        {
            return RedirectToPage(new { id = planId });
        }

        var day = plan.Days.FirstOrDefault(d => d.Id == dayId);
        if (day == null)
        {
            return RedirectToPage(new { id = planId });
        }

        foreach (var exercise in day.Exercises.ToList())
        {
            await DeleteExerciseImagesAsync(exercise);
        }

        _db.TrainingDays.Remove(day);

        var remaining = plan.Days
            .Where(d => d.Id != dayId)
            .OrderBy(d => d.OrderIndex)
            .ToList();

        for (var index = 0; index < remaining.Count; index++)
        {
            remaining[index].OrderIndex = index;
        }

        plan.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted day {DayId} from plan {PlanId} for user {UserId}", day.Id, plan.Id, userId);

        return RedirectToPage(new { id = plan.Id });
    }

    public async Task<IActionResult> OnPostMoveDayAsync(Guid planId, int dayId, int delta)
    {
        if (delta == 0)
        {
            return RedirectToPage(new { id = planId });
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var plan = await _db.TrainingPlans
            .Include(p => p.Days)
            .FirstOrDefaultAsync(p => p.Id == planId && p.UserId == userId);

        if (plan == null)
        {
            return RedirectToPage(new { id = planId });
        }

        var orderedDays = plan.Days.OrderBy(d => d.OrderIndex).ToList();
        var currentIndex = orderedDays.FindIndex(d => d.Id == dayId);
        if (currentIndex < 0)
        {
            return RedirectToPage(new { id = planId });
        }

        var newIndex = currentIndex + delta;
        if (newIndex < 0 || newIndex >= orderedDays.Count)
        {
            return RedirectToPage(new { id = planId });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var current = orderedDays[currentIndex];
        var target = orderedDays[newIndex];
        (current.OrderIndex, target.OrderIndex) = (target.OrderIndex, current.OrderIndex);

        plan.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation("Moved day {DayId} in plan {PlanId} for user {UserId} by {Delta}", dayId, planId, userId, delta);

        return RedirectToPage(new { id = planId });
    }

    public class DayOrderRequest
    {
        public Guid PlanId { get; set; }
        public List<int> OrderedIds { get; set; } = new();
    }

    public async Task<IActionResult> OnPostReorderDaysAsync([FromBody] DayOrderRequest request)
    {
        if (request.OrderedIds.Count == 0)
        {
            return BadRequest();
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var plan = await _db.TrainingPlans
            .Include(p => p.Days)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && p.UserId == userId);

        if (plan == null)
        {
            return NotFound();
        }

        if (plan.Days.Count != request.OrderedIds.Count)
        {
            return BadRequest();
        }

        var dayMap = plan.Days.ToDictionary(day => day.Id, day => day);

        await using var transaction = await _db.Database.BeginTransactionAsync();
        for (var index = 0; index < request.OrderedIds.Count; index++)
        {
            var dayId = request.OrderedIds[index];
            if (dayMap.TryGetValue(dayId, out var day))
            {
                day.OrderIndex = index;
            }
        }

        plan.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation("Reordered {DayCount} days for plan {PlanId} and user {UserId}", plan.Days.Count, plan.Id, userId);

        return new JsonResult(new { success = true });
    }

    public class ExerciseOrderRequest
    {
        public Guid PlanId { get; set; }
        public int DayId { get; set; }
        public List<int> OrderedIds { get; set; } = new();
    }

    public async Task<IActionResult> OnPostReorderExercisesAsync([FromBody] ExerciseOrderRequest request)
    {
        if (request.OrderedIds.Count == 0)
        {
            return BadRequest();
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var day = await _db.TrainingDays
            .Include(d => d.TrainingPlan)
            .Include(d => d.Exercises)
            .FirstOrDefaultAsync(d =>
                d.TrainingPlanId == request.PlanId &&
                d.Id == request.DayId &&
                d.TrainingPlan!.UserId == userId);

        if (day == null)
        {
            return NotFound();
        }

        if (day.Exercises.Count != request.OrderedIds.Count)
        {
            return BadRequest();
        }

        var exerciseMap = day.Exercises.ToDictionary(exercise => exercise.Id, exercise => exercise);

        await using var transaction = await _db.Database.BeginTransactionAsync();
        for (var index = 0; index < request.OrderedIds.Count; index++)
        {
            var exerciseId = request.OrderedIds[index];
            if (exerciseMap.TryGetValue(exerciseId, out var exercise))
            {
                exercise.OrderIndex = index;
            }
        }

        day.TrainingPlan!.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation("Reordered {ExerciseCount} exercises for day {DayId} in plan {PlanId} (user {UserId})", day.Exercises.Count, day.Id, day.TrainingPlanId, userId);

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnGetExerciseFormAsync(Guid planId, int dayId, int? exerciseId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var day = await _db.TrainingDays
            .AsNoTracking()
            .Include(d => d.TrainingPlan)
            .Include(d => d.Exercises)
            .ThenInclude(e => e.Images)
            .FirstOrDefaultAsync(d => d.TrainingPlanId == planId && d.Id == dayId && d.TrainingPlan!.UserId == userId);

        if (day == null)
        {
            return NotFound();
        }

        var input = new ExerciseInputModel
        {
            PlanId = planId,
            DayId = dayId,
            Sets = 1,
            Repetitions = 1,
            RestSeconds = 0
        };

        if (exerciseId.HasValue)
        {
            var exercise = day.Exercises.FirstOrDefault(e => e.Id == exerciseId.Value);
            if (exercise == null)
            {
                return NotFound();
            }

            input.ExerciseId = exercise.Id;
            input.Name = exercise.Name;
            input.Description = exercise.Description;
            input.Sets = exercise.Sets;
            input.Repetitions = exercise.Repetitions;
            input.RestSeconds = exercise.RestSeconds ?? 0;
            input.Notes = exercise.Notes;

            var image = exercise.Images.FirstOrDefault(i => i.IsPrimary) ?? exercise.Images.FirstOrDefault();
            if (image != null)
            {
                ViewData["ExistingImageName"] = image.OriginalFileName;
                ViewData["ExistingImageUrl"] = Url.Page("/Plans/Details", "ExerciseImage", new
                {
                    id = planId,
                    planId,
                    dayId,
                    exerciseId = exercise.Id
                }) ?? $"/Plans/Details/{planId}?handler=ExerciseImage&planId={planId}&dayId={dayId}&exerciseId={exercise.Id}";
            }

            ViewData["FormTitle"] = "Edit exercise";
        }
        else
        {
            ViewData["FormTitle"] = "Add exercise";
        }

        return BuildExerciseFormResult(input);
    }

    public async Task<IActionResult> OnPostSaveExerciseAsync([FromForm] ExerciseInputModel input)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var day = await _db.TrainingDays
            .Include(d => d.TrainingPlan)
            .Include(d => d.Exercises)
            .ThenInclude(e => e.Images)
            .FirstOrDefaultAsync(d => d.TrainingPlanId == input.PlanId && d.Id == input.DayId && d.TrainingPlan!.UserId == userId);

        if (day == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewData["FormTitle"] = input.ExerciseId.HasValue ? "Edit exercise" : "Add exercise";
            if (input.ExerciseId.HasValue)
            {
                var existing = day.Exercises.FirstOrDefault(e => e.Id == input.ExerciseId.Value);
                var image = existing?.Images.FirstOrDefault(i => i.IsPrimary) ?? existing?.Images.FirstOrDefault();
                if (image != null)
                {
                    ViewData["ExistingImageName"] = image.OriginalFileName;
                    ViewData["ExistingImageUrl"] = Url.Page("/Plans/Details", "ExerciseImage", new
                    {
                        id = input.PlanId,
                        planId = input.PlanId,
                        dayId = input.DayId,
                        exerciseId = existing!.Id
                    }) ?? $"/Plans/Details/{input.PlanId}?handler=ExerciseImage&planId={input.PlanId}&dayId={input.DayId}&exerciseId={existing!.Id}";
                }
            }

            return BuildExerciseFormResult(input);
        }

        Exercise exercise;
        if (input.ExerciseId.HasValue)
        {
            exercise = day.Exercises.FirstOrDefault(e => e.Id == input.ExerciseId.Value)
                       ?? throw new InvalidOperationException("Exercise not found.");
        }
        else
        {
            var nextId = day.Exercises.Count == 0 ? 1 : day.Exercises.Max(e => e.Id) + 1;
            var nextOrder = day.Exercises.Count == 0 ? 0 : day.Exercises.Max(e => e.OrderIndex) + 1;

            exercise = new Exercise
            {
                TrainingPlanId = input.PlanId,
                TrainingDayId = input.DayId,
                Id = nextId,
                OrderIndex = nextOrder
            };

            day.Exercises.Add(exercise);
            _db.Exercises.Add(exercise);
        }

        exercise.Name = input.Name.Trim();
        exercise.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        exercise.Sets = input.Sets;
        exercise.Repetitions = input.Repetitions;
        exercise.RestSeconds = input.RestSeconds;
        exercise.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();

        if (input.Image != null && input.Image.Length > 0)
        {
            await ReplaceExerciseImageAsync(userId, input, exercise);
        }

        day.TrainingPlan!.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Saved exercise {ExerciseId} for day {DayId} in plan {PlanId} (user {UserId})", exercise.Id, day.Id, day.TrainingPlanId, userId);

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostDeleteExerciseAsync(Guid planId, int dayId, int exerciseId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var day = await _db.TrainingDays
            .Include(d => d.TrainingPlan)
            .Include(d => d.Exercises)
            .ThenInclude(e => e.Images)
            .FirstOrDefaultAsync(d => d.TrainingPlanId == planId && d.Id == dayId && d.TrainingPlan!.UserId == userId);

        if (day == null)
        {
            return RedirectToPage(new { id = planId });
        }

        var exercise = day.Exercises.FirstOrDefault(e => e.Id == exerciseId);
        if (exercise == null)
        {
            return RedirectToPage(new { id = planId });
        }

        await DeleteExerciseImagesAsync(exercise);

        _db.Exercises.Remove(exercise);

        var remaining = day.Exercises
            .Where(e => e.Id != exerciseId)
            .OrderBy(e => e.OrderIndex)
            .ToList();

        for (var index = 0; index < remaining.Count; index++)
        {
            remaining[index].OrderIndex = index;
        }

        day.TrainingPlan!.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted exercise {ExerciseId} from day {DayId} in plan {PlanId} (user {UserId})", exercise.Id, day.Id, day.TrainingPlanId, userId);

        return RedirectToPage(new { id = planId });
    }

    public async Task<IActionResult> OnGetExerciseImageAsync(Guid planId, int dayId, int exerciseId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var exercise = await _db.Exercises
            .AsNoTracking()
            .Include(e => e.TrainingDay)
            .ThenInclude(d => d.TrainingPlan)
            .Include(e => e.Images)
            .FirstOrDefaultAsync(e =>
                e.TrainingPlanId == planId &&
                e.TrainingDayId == dayId &&
                e.Id == exerciseId &&
                e.TrainingDay!.TrainingPlan!.UserId == userId);

        if (exercise == null)
        {
            return NotFound();
        }

        var image = exercise.Images.FirstOrDefault(i => i.IsPrimary) ?? exercise.Images.FirstOrDefault();
        if (image == null)
        {
            return NotFound();
        }

        var absolutePath = GetAbsoluteImagePath(image.RelativePath, allowMissingBasePath: true);
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            return NotFound();
        }
        if (!System.IO.File.Exists(absolutePath))
        {
            return NotFound();
        }

        var contentType = string.IsNullOrWhiteSpace(image.ContentType) ? "application/octet-stream" : image.ContentType;
        return PhysicalFile(absolutePath, contentType);
    }

    private PartialViewResult BuildExerciseFormResult(ExerciseInputModel input)
    {
        return new PartialViewResult
        {
            ViewName = "/Pages/Plans/_ExerciseFormPartial.cshtml",
            ViewData = new ViewDataDictionary<ExerciseInputModel>(ViewData, input)
        };
    }


    private async Task ReplaceExerciseImageAsync(string userId, ExerciseInputModel input, Exercise exercise)
    {
        await DeleteExerciseImagesAsync(exercise);

        var imageId = Guid.NewGuid();
        var extension = Path.GetExtension(input.Image!.FileName);
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".img" : extension.ToLowerInvariant();
        var storedFileName = $"{imageId}{safeExtension}";

        var relativePath = Path.Combine(userId, input.PlanId.ToString(), input.DayId.ToString(), storedFileName)
            .Replace('\\', '/');

        var absolutePath = GetAbsoluteImagePath(relativePath);
        var directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = new FileStream(absolutePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await input.Image.CopyToAsync(stream);

        var exerciseImage = new ExerciseImage
        {
            Id = imageId,
            TrainingPlanId = input.PlanId,
            TrainingDayId = input.DayId,
            ExerciseId = exercise.Id,
            OriginalFileName = input.Image.FileName,
            StoredFileName = storedFileName,
            RelativePath = relativePath,
            ContentType = input.Image.ContentType,
            SizeBytes = input.Image.Length,
            IsPrimary = true,
            CreatedAt = DateTime.UtcNow
        };

        exercise.Images.Clear();
        exercise.Images.Add(exerciseImage);
        _db.ExerciseImages.Add(exerciseImage);
    }

    private Task DeleteExerciseImagesAsync(Exercise exercise)
    {
        if (exercise.Images.Count == 0)
        {
            return Task.CompletedTask;
        }

        foreach (var image in exercise.Images.ToList())
        {
            _db.ExerciseImages.Remove(image);
            var absolutePath = GetAbsoluteImagePath(image.RelativePath, allowMissingBasePath: true);
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                continue;
            }
            if (System.IO.File.Exists(absolutePath))
            {
                try
                {
                    System.IO.File.Delete(absolutePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete image {ImagePath}", absolutePath);
                }
            }

            CleanupEmptyDirectories(Path.GetDirectoryName(absolutePath));
        }

        return Task.CompletedTask;
    }

    private void CleanupEmptyDirectories(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        var root = GetAbsoluteImagePath(string.Empty, allowMissingBasePath: true);
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        while (!string.IsNullOrWhiteSpace(directory) && directory.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
            {
                break;
            }

            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete empty directory {Directory}", directory);
                break;
            }

            directory = Path.GetDirectoryName(directory);
        }
    }

    private string GetAbsoluteImagePath(string relativePath, bool allowMissingBasePath = false)
    {
        var basePath = _imageOptions.BasePath;
        if (string.IsNullOrWhiteSpace(basePath))
        {
            if (allowMissingBasePath)
            {
                return string.Empty;
            }

            throw new InvalidOperationException("Image storage base path is not configured.");
        }

        var root = Path.IsPathRooted(basePath)
            ? basePath
            : Path.Combine(_environment.ContentRootPath, basePath);

        return Path.GetFullPath(Path.Combine(root, relativePath));
    }
}
