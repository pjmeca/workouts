namespace Workouts.Models;

public class Exercise
{
    public Guid TrainingPlanId { get; set; }
    public int TrainingDayId { get; set; }
    public TrainingDay? TrainingDay { get; set; }

    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int Sets { get; set; }
    public int Repetitions { get; set; }
    public int? RestSeconds { get; set; }

    public string? Notes { get; set; }

    public int OrderIndex { get; set; }

    public ICollection<ExerciseImage> Images { get; set; } = new List<ExerciseImage>();
}
