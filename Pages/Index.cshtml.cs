using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Workouts.Data;
using Workouts.Models;

namespace Workouts.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ILogger<IndexModel> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
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
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (plan == null)
        {
            return RedirectToPage();
        }

        _db.TrainingPlans.Remove(plan);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted training plan {PlanId} for user {UserId}", plan.Id, userId);

        return RedirectToPage();
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
