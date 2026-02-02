using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Workouts.Data;
using Workouts.Models;

namespace Workouts.Pages.Plans;

[Authorize]
public class PlayModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public PlayModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public bool NotFound { get; private set; }
    public Guid PlanId { get; private set; }
    public int DayId { get; private set; }
    public string PlanName { get; private set; } = string.Empty;
    public string DayName { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(Guid planId, int dayId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var plan = await _db.TrainingPlans
            .AsNoTracking()
            .Include(p => p.Days)
            .FirstOrDefaultAsync(p => p.Id == planId && p.UserId == userId);

        if (plan == null)
        {
            NotFound = true;
            return Page();
        }

        var day = plan.Days.FirstOrDefault(d => d.Id == dayId);
        if (day == null)
        {
            NotFound = true;
            return Page();
        }

        PlanId = plan.Id;
        DayId = day.Id;
        PlanName = plan.Name;
        DayName = day.Name;

        return Page();
    }
}
