namespace Workouts.Models;

public class ExerciseImage
{
    public Guid Id { get; set; }

    public Guid TrainingPlanId { get; set; }
    public int TrainingDayId { get; set; }
    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;

    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public bool IsPrimary { get; set; }

    public DateTime CreatedAt { get; set; }
}
