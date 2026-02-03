using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Workouts.Data;
using Workouts.Models;

namespace Workouts.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<IndexModel> _logger;
    private readonly Workouts.Options.ImageStorageOptions _imageOptions;
    private readonly IWebHostEnvironment _environment;

    public IndexModel(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger<IndexModel> logger,
        IOptions<Workouts.Options.ImageStorageOptions> imageOptions,
        IWebHostEnvironment environment)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
        _imageOptions = imageOptions.Value;
        _environment = environment;
    }

    public List<TrainingPlan> Plans { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            Plans = new List<TrainingPlan>();
            return;
        }

        Plans = await _db.TrainingPlans
            .Where(plan => plan.UserId == userId)
            .OrderBy(plan => plan.OrderIndex)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
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
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (plan == null)
        {
            return RedirectToPage();
        }

        var dayCount = plan.Days.Count;
        var exerciseCount = plan.Days.Sum(d => d.Exercises.Count);
        var imageCount = plan.Days.Sum(d => d.Exercises.Sum(e => e.Images.Count));

        _logger.LogInformation(
            "Deleting plan {PlanId} for user {UserId} with {DayCount} days, {ExerciseCount} exercises, {ImageCount} images",
            plan.Id,
            userId,
            dayCount,
            exerciseCount,
            imageCount);

        foreach (var day in plan.Days.ToList())
        {
            foreach (var exercise in day.Exercises.ToList())
            {
                await DeleteExerciseImagesAsync(exercise);
            }
        }

        _db.TrainingPlans.Remove(plan);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted training plan {PlanId} for user {UserId}", plan.Id, userId);

        return RedirectToPage();
    }

    private async Task DeleteExerciseImagesAsync(Exercise exercise)
    {
        if (exercise.Images.Count == 0)
        {
            return;
        }

        foreach (var image in exercise.Images.ToList())
        {
            _db.ExerciseImages.Remove(image);
            var absolutePath = GetAbsoluteImagePath(image.RelativePath);
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                continue;
            }

            if (System.IO.File.Exists(absolutePath))
            {
                try
                {
                    System.IO.File.Delete(absolutePath);
                    _logger.LogInformation(
                        "Deleted image file {ImagePath} for exercise {ExerciseId} (plan {PlanId}, day {DayId})",
                        absolutePath,
                        exercise.Id,
                        exercise.TrainingPlanId,
                        exercise.TrainingDayId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete image {ImagePath}", absolutePath);
                }
            }
            else
            {
                _logger.LogWarning(
                    "Expected image file missing at {ImagePath} for exercise {ExerciseId} (plan {PlanId}, day {DayId})",
                    absolutePath,
                    exercise.Id,
                    exercise.TrainingPlanId,
                    exercise.TrainingDayId);
            }

            CleanupEmptyDirectories(Path.GetDirectoryName(absolutePath));
        }

        await Task.CompletedTask;
    }

    private string GetAbsoluteImagePath(string relativePath)
    {
        var basePath = _imageOptions.BasePath;
        if (string.IsNullOrWhiteSpace(basePath))
        {
            return string.Empty;
        }

        var root = Path.IsPathRooted(basePath)
            ? basePath
            : Path.Combine(_environment.ContentRootPath, basePath);

        return Path.GetFullPath(Path.Combine(root, relativePath));
    }

    private void CleanupEmptyDirectories(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        var root = GetAbsoluteImagePath(string.Empty);
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

    public async Task<IActionResult> OnPostMoveAsync(Guid id, int delta)
    {
        if (delta == 0)
        {
            return RedirectToPage();
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var plans = await _db.TrainingPlans
            .Where(plan => plan.UserId == userId)
            .OrderBy(plan => plan.OrderIndex)
            .ToListAsync();

        var currentIndex = plans.FindIndex(plan => plan.Id == id);
        if (currentIndex < 0)
        {
            return RedirectToPage();
        }

        var newIndex = currentIndex + delta;
        if (newIndex < 0 || newIndex >= plans.Count)
        {
            return RedirectToPage();
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var current = plans[currentIndex];
        var target = plans[newIndex];
        (current.OrderIndex, target.OrderIndex) = (target.OrderIndex, current.OrderIndex);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation("Moved training plan {PlanId} for user {UserId} by {Delta}", current.Id, userId, delta);

        return RedirectToPage();
    }

    public class PlanOrderRequest
    {
        public List<Guid> OrderedIds { get; set; } = new();
    }

    public async Task<IActionResult> OnPostReorderAsync([FromBody] PlanOrderRequest request)
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

        var plans = await _db.TrainingPlans
            .Where(plan => plan.UserId == userId && request.OrderedIds.Contains(plan.Id))
            .ToListAsync();

        if (plans.Count != request.OrderedIds.Count)
        {
            return BadRequest();
        }

        var planMap = plans.ToDictionary(plan => plan.Id, plan => plan);

        await using var transaction = await _db.Database.BeginTransactionAsync();
        for (var index = 0; index < request.OrderedIds.Count; index++)
        {
            var planId = request.OrderedIds[index];
            if (planMap.TryGetValue(planId, out var plan))
            {
                plan.OrderIndex = index;
            }
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation("Reordered {PlanCount} plans for user {UserId}", plans.Count, userId);

        return new JsonResult(new { success = true });
    }
}
