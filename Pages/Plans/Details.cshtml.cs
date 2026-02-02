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

    public DetailsModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
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
}
