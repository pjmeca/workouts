namespace Workouts.Models;

public class TrainingPlan
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public int OrderIndex { get; set; }

    public ICollection<TrainingDay> Days { get; set; } = new List<TrainingDay>();
}
