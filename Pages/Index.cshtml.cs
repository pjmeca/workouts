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
}
