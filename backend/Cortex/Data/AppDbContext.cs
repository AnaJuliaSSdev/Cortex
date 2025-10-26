using Cortex.Models;
using Microsoft.EntityFrameworkCore;
using Index = Cortex.Models.Index;

namespace Cortex.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Analysis> Analyses { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<Stage> Stages { get; set; }
    public DbSet<Chunk> Chunks { get; set; }
    public DbSet<PreAnalysisStage> PreAnalysisStages { get; set; }
    public DbSet<ExplorationOfMaterialStage> ExplorationOfMaterialStages { get; set; }
    public DbSet<Index> Indexes { get; set; }
    public DbSet<Indicator> Indicators { get; set; }
    public DbSet<IndexReference> IndexReferences { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<RegisterUnit> RegisterUnits { get; set; }


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

        modelBuilder.Entity<Stage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasDiscriminator<string>("StageType")
                .HasValue<PreAnalysisStage>("FloatingReading")
                .HasValue<ExplorationOfMaterialStage>("Codification")
                .HasValue<InferenceConclusionStage>("InferenceConclusion");
            entity.HasIndex(e => e.AnalysisId);
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

        modelBuilder.Entity<PreAnalysisStage>(entity =>
        {
            // Uma PreAnalysisStage TEM MUITOS Indexes
            entity.HasMany(pas => pas.Indexes)
                .WithOne(i => i.PreAnalysisStage)
                .HasForeignKey(i => i.PreAnalysisStageId)
                .OnDelete(DeleteBehavior.Cascade); // Se deletar a etapa, deleta os índices
        });

        modelBuilder.Entity<Indicator>(entity =>
        {
            entity.HasKey(e => e.Id);

            // CRÍTICO: Garante que não haja dois indicadores com o mesmo nome
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Index>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Garante que um índice seja único dentro de sua etapa
            entity.HasIndex(e => new { e.PreAnalysisStageId, e.Name }).IsUnique();

            // Define o relacionamento Muitos-para-1
            // Muitos Indexes PODEM USAR UM Indicator
            entity.HasOne(i => i.Indicator)
                .WithMany() // Um Indicator pode ser usado por muitos Indexes
                .HasForeignKey(i => i.IndicatorId)
                .OnDelete(DeleteBehavior.Restrict); // NÃO DEIXE deletar um Indicator se ele estiver em uso

            // Define o relacionamento 1-para-Muitos
            // Um Index TEM MUITAS References
            entity.HasMany(i => i.References)
                .WithOne(r => r.Index)
                .HasForeignKey(r => r.IndexId)
                .OnDelete(DeleteBehavior.Cascade); // Se deletar o Index, deleta suas referências
           
            entity.HasMany(i => i.RegisterUnits)
                .WithMany(ru => ru.FoundIndices);
        });

        // Configuração para IndexReference
        modelBuilder.Entity<IndexReference>(entity =>
        {
            entity.HasKey(e => e.Id);

            // O relacionamento (Muitos-para-1 com Index)
            // já foi definido na configuração de Index.

            // Apenas adiciona um índice para performance de consulta
            entity.HasIndex(e => e.IndexId);
        });

        // Configuração para ExplorationOfMaterialStage
        modelBuilder.Entity<ExplorationOfMaterialStage>(entity =>
        {
            entity.HasMany(eos => eos.Categories)
                .WithOne(c => c.ExplorationOfMaterialStage)
                .HasForeignKey(c => c.ExplorationOfMaterialStageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuração para Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ExplorationOfMaterialStageId, e.Name }).IsUnique();
            entity.HasMany(c => c.RegisterUnits)
                .WithOne(ru => ru.Category)
                .HasForeignKey(ru => ru.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuração para RegisterUnit
        modelBuilder.Entity<RegisterUnit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CategoryId);
        });

    }
}
