using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Workouts.Models;

namespace Workouts.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<TrainingPlan> TrainingPlans => Set<TrainingPlan>();
    public DbSet<TrainingDay> TrainingDays => Set<TrainingDay>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ExerciseImage> ExerciseImages => Set<ExerciseImage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<TrainingPlan>(entity =>
        {
            entity.HasKey(plan => plan.Id);
            entity.HasIndex(plan => new { plan.UserId, plan.OrderIndex });

            entity.Property(plan => plan.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(plan => plan.Description)
                .HasMaxLength(500);

            entity.HasMany(plan => plan.Days)
                .WithOne(day => day.TrainingPlan)
                .HasForeignKey(day => day.TrainingPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TrainingDay>(entity =>
        {
            entity.HasKey(day => new { day.TrainingPlanId, day.Id });
            entity.HasIndex(day => new { day.TrainingPlanId, day.OrderIndex });

            entity.Property(day => day.Id)
                .ValueGeneratedNever();

            entity.Property(day => day.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(day => day.Notes)
                .HasMaxLength(500);

            entity.HasMany(day => day.Exercises)
                .WithOne(exercise => exercise.TrainingDay)
                .HasForeignKey(exercise => new { exercise.TrainingPlanId, exercise.TrainingDayId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Exercise>(entity =>
        {
            entity.HasKey(exercise => new { exercise.TrainingPlanId, exercise.TrainingDayId, exercise.Id });
            entity.HasIndex(exercise => new { exercise.TrainingPlanId, exercise.TrainingDayId, exercise.OrderIndex });

            entity.Property(exercise => exercise.Id)
                .ValueGeneratedNever();

            entity.Property(exercise => exercise.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(exercise => exercise.Description)
                .HasMaxLength(1000);

            entity.Property(exercise => exercise.Notes)
                .HasMaxLength(1000);
        });

        builder.Entity<ExerciseImage>(entity =>
        {
            entity.HasKey(image => image.Id);

            entity.Property(image => image.OriginalFileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(image => image.StoredFileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(image => image.RelativePath)
                .HasMaxLength(500)
                .IsRequired();

            entity.HasOne(image => image.Exercise)
                .WithMany(exercise => exercise.Images)
                .HasForeignKey(image => new { image.TrainingPlanId, image.TrainingDayId, image.ExerciseId })
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
