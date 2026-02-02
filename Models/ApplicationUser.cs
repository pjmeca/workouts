using Microsoft.AspNetCore.Identity;

namespace Workouts.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<TrainingPlan> TrainingPlans { get; set; } = new List<TrainingPlan>();
}
