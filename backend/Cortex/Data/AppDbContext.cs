using Cortex.Models;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Analysis> Analyses { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Stage> Stages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {           
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Analysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(a => a.Documents)
                .WithOne(d => d.Analysis)
                .HasForeignKey(d => d.AnalysisId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(a => a.Questions)
                .WithOne(q => q.Analysis)
                .HasForeignKey(q => q.AnalysisId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(a => a.Stages)
                .WithOne(s => s.Analysis)
                .HasForeignKey(s => s.AnalysisId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Document>()
            .Property(d => d.FileType)
            .HasConversion<string>();

        modelBuilder.Entity<Document>()
            .Property(d => d.Purpose)
            .HasConversion<string>();


        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AnalysisId);
        });

        modelBuilder.Entity<Stage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasDiscriminator<string>("StageType")
                .HasValue<FloatingReadingStage>("FloatingReading")
                .HasValue<CodificationStage>("Codification")
                .HasValue<InferenceConclusionStage>("InferenceConclusion");
            entity.HasIndex(e => e.AnalysisId);
            entity.HasIndex(e => new { e.AnalysisId, e.Order });
        });
    }
}
