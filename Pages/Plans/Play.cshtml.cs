using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
    public IReadOnlyList<ExerciseRunDto> Exercises { get; private set; } = Array.Empty<ExerciseRunDto>();
    public string RunPayloadJson { get; private set; } = "{}";
    public string DetailsUrl { get; private set; } = string.Empty;

    public sealed record ExerciseRunDto(
        int Id,
        string Name,
        string? Description,
        int Repetitions,
        int Sets,
        int RestSeconds,
        string? Notes,
        string? ImageUrl);

    public sealed record RunPayload(
        Guid PlanId,
        int DayId,
        string PlanName,
        string DayName,
        string DetailsUrl,
        IReadOnlyList<ExerciseRunDto> Exercises);

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
            .Include(d => d.Exercises)
            .ThenInclude(e => e.Images)
            .FirstOrDefaultAsync(d =>
                d.TrainingPlanId == planId &&
                d.Id == dayId &&
                d.TrainingPlan!.UserId == userId);

        if (day?.TrainingPlan == null)
        {
            NotFound = true;
            return Page();
        }

        PlanId = day.TrainingPlanId;
        DayId = day.Id;
        PlanName = day.TrainingPlan.Name;
        DayName = day.Name;
        DetailsUrl = Url.Page("/Plans/Details", new { id = PlanId }) ?? $"/Plans/Details/{PlanId}";

        Exercises = day.Exercises
            .OrderBy(e => e.OrderIndex)
            .Select(e =>
            {
                var image = e.Images.FirstOrDefault(i => i.IsPrimary) ?? e.Images.FirstOrDefault();
                var imageUrl = image == null
                    ? null
                    : Url.Page("/Plans/Details", "ExerciseImage", new
                    {
                        id = PlanId,
                        planId = PlanId,
                        dayId = DayId,
                        exerciseId = e.Id
                    }) ?? $"/Plans/Details/{PlanId}?handler=ExerciseImage&planId={PlanId}&dayId={DayId}&exerciseId={e.Id}";

                return new ExerciseRunDto(
                    e.Id,
                    e.Name,
                    e.Description,
                    e.Repetitions,
                    Math.Max(1, e.Sets),
                    Math.Max(0, e.RestSeconds ?? 0),
                    e.Notes,
                    imageUrl);
            })
            .ToList();

        var payload = new RunPayload(PlanId, DayId, PlanName, DayName, DetailsUrl, Exercises);
        RunPayloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return Page();
    }
}
