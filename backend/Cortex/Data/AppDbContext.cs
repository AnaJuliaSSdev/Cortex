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
    public DbSet<Chunk> Chunks { get; set; }

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

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.FilePath)
                .HasMaxLength(1000);

            entity.Property(e => e.Source)
                .HasMaxLength(500);

            entity.HasMany(e => e.Chunks)
                .WithOne(c => c.Document)
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });


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

        modelBuilder.Entity<Chunk>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Content)
                .IsRequired()
                .HasMaxLength(10000);

            entity.Property(e => e.Embedding)
                .HasColumnType("vector(768)");

            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb");

            entity.HasOne(e => e.Document)
                .WithMany(d => d.Chunks)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => new { e.DocumentId, e.ChunkIndex })
                .HasDatabaseName("IX_Chunks_DocumentId_ChunkIndex");
        });
    }
}
