using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Workouts.Pages.Plans;

public class ExerciseInputModel
{
    [Required]
    public Guid PlanId { get; set; }

    [Required]
    public int DayId { get; set; }

    public int? ExerciseId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Sets { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Repetitions { get; set; }

    [Range(0, int.MaxValue)]
    public int? RestSeconds { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public IFormFile? Image { get; set; }
}
