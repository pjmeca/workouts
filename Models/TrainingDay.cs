namespace Workouts.Models;

public class TrainingDay
{
    public Guid TrainingPlanId { get; set; }
    public TrainingPlan? TrainingPlan { get; set; }

    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public int OrderIndex { get; set; }

    public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
}
