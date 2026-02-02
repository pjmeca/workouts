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
    public string PlanName { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var plan = await _db.TrainingPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (plan == null)
        {
            NotFound = true;
            return Page();
        }

        PlanId = plan.Id;
        PlanName = plan.Name;

        return Page();
    }
}
