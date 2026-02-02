using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Workouts.Data;
using Workouts.Models;

namespace Workouts.Pages.Plans;

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

    public class InputModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var lastOrder = await _db.TrainingPlans
            .AsNoTracking()
            .Where(plan => plan.UserId == userId)
            .MaxAsync(plan => (int?)plan.OrderIndex) ?? -1;

        var plan = new TrainingPlan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = Input.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            OrderIndex = lastOrder + 1
        };

        _db.TrainingPlans.Add(plan);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created training plan {PlanId} for user {UserId}", plan.Id, userId);

        return RedirectToPage("/Index");
    }
}
