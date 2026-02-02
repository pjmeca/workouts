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
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<EditModel> _logger;

    public EditModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ILogger<EditModel> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool NotFound { get; private set; }
    public Guid PlanId { get; private set; }

    public class InputModel
    {
        [Required]
        public Guid PlanId { get; set; }

        [Required]
        public int DayId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid planId, int dayId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var day = await _db.TrainingDays
            .AsNoTracking()
            .Include(d => d.TrainingPlan)
            .FirstOrDefaultAsync(d => d.TrainingPlanId == planId && d.Id == dayId && d.TrainingPlan!.UserId == userId);

        if (day == null)
        {
            NotFound = true;
            return Page();
        }

        PlanId = planId;
        Input = new InputModel
        {
            PlanId = day.TrainingPlanId,
            DayId = day.Id,
            Name = day.Name,
            Notes = day.Notes
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            PlanId = Input.PlanId;
            return Page();
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var day = await _db.TrainingDays
            .Include(d => d.TrainingPlan)
            .FirstOrDefaultAsync(d => d.TrainingPlanId == Input.PlanId && d.Id == Input.DayId && d.TrainingPlan!.UserId == userId);

        if (day == null)
        {
            NotFound = true;
            PlanId = Input.PlanId;
            return Page();
        }

        day.Name = Input.Name.Trim();
        day.Notes = string.IsNullOrWhiteSpace(Input.Notes) ? null : Input.Notes.Trim();
        day.TrainingPlan!.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated day {DayId} for plan {PlanId} and user {UserId}", day.Id, day.TrainingPlanId, userId);

        return RedirectToPage("/Plans/Details", new { id = day.TrainingPlanId });
    }
}
