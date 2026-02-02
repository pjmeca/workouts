using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Workouts.Data;
using Workouts.Models;

namespace Workouts.Pages.Plans.Days;

[Authorize]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ILogger<CreateModel> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool NotFound { get; private set; }
    public Guid PlanId { get; private set; }
    public string PlanName { get; private set; } = string.Empty;

    public class InputModel
    {
        [Required]
        public Guid PlanId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid planId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var loaded = await LoadPlanAsync(planId, userId);
        if (!loaded)
        {
            return Page();
        }

        Input.PlanId = PlanId;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await LoadPlanAsync(Input.PlanId, userId);
            return Page();
        }

        var plan = await _db.TrainingPlans
            .Include(p => p.Days)
            .FirstOrDefaultAsync(p => p.Id == Input.PlanId && p.UserId == userId);

        if (plan == null)
        {
            NotFound = true;
            return Page();
        }

        var nextId = plan.Days.Count == 0 ? 1 : plan.Days.Max(day => day.Id) + 1;
        var nextOrder = plan.Days.Count == 0 ? 0 : plan.Days.Max(day => day.OrderIndex) + 1;

        var day = new TrainingDay
        {
            TrainingPlanId = plan.Id,
            Id = nextId,
            Name = Input.Name.Trim(),
            Notes = string.IsNullOrWhiteSpace(Input.Notes) ? null : Input.Notes.Trim(),
            OrderIndex = nextOrder
        };

        plan.Days.Add(day);
        plan.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Created day {DayId} for plan {PlanId} and user {UserId}", day.Id, plan.Id, userId);

        return RedirectToPage("/Plans/Details", new { id = plan.Id });
    }

    private async Task<bool> LoadPlanAsync(Guid planId, string userId)
    {
        var plan = await _db.TrainingPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId && p.UserId == userId);

        if (plan == null)
        {
            NotFound = true;
            return false;
        }

        PlanId = plan.Id;
        PlanName = plan.Name;
        return true;
    }
}
