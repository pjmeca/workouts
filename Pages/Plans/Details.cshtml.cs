using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Workouts.Data;
using Workouts.Models;

namespace Workouts.Pages.Plans;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ILogger<DetailsModel> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
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
}
