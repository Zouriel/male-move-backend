using MaleMove.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MaleMove.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Permit> Permits => Set<Permit>();
    public DbSet<PermitDocument> PermitDocuments => Set<PermitDocument>();
    public DbSet<PermitRoadSegment> PermitRoadSegments => Set<PermitRoadSegment>();
    public DbSet<PermitAuditEvent> PermitAuditEvents => Set<PermitAuditEvent>();
    public DbSet<RoadSegment> RoadSegments => Set<RoadSegment>();
    public DbSet<GeographicArea> GeographicAreas => Set<GeographicArea>();
    public DbSet<DatasetSource> DatasetSources => Set<DatasetSource>();
    public DbSet<PopulationSeries> PopulationSeries => Set<PopulationSeries>();
    public DbSet<TrafficProfile> TrafficProfiles => Set<TrafficProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<Permit>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.PermitNumber);
            entity.HasIndex(x => new { x.ApprovalState, x.StartsAt, x.EndsAt });
            entity.Property(x => x.PermitNumber).HasMaxLength(120).IsRequired();
            entity.Property(x => x.LocationDescription).HasMaxLength(1500).IsRequired();
            entity.Property(x => x.WorkDescription).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.ApprovalState).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.ImpactType).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.SiteVerificationState).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Revision).IsConcurrencyToken();
            entity.Property(x => x.ReviewComment).HasMaxLength(2000);
            entity.HasOne(x => x.PermitDocument).WithMany().HasForeignKey(x => x.PermitDocumentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PermitDocument>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Sha256);
            entity.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
            entity.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ScanStatus).HasMaxLength(32).IsRequired();
        });

        modelBuilder.Entity<PermitAuditEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.PermitId, x.OccurredAt });
            entity.Property(x => x.Action).HasMaxLength(80).IsRequired();
            entity.Property(x => x.PreviousState).HasMaxLength(32);
            entity.Property(x => x.NewState).HasMaxLength(32);
        });

        modelBuilder.Entity<RoadSegment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.FromNodeId, x.ToNodeId });
            entity.HasIndex(x => x.Geometry).HasMethod("gist");
            entity.Property(x => x.Geometry).HasColumnType("geometry(LineString,4326)");
            entity.Property(x => x.NetworkVersion).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<PermitRoadSegment>(entity =>
        {
            entity.HasKey(x => new { x.PermitId, x.RoadSegmentId });
            entity.HasOne(x => x.Permit).WithMany(x => x.RoadSegments).HasForeignKey(x => x.PermitId);
            entity.HasOne(x => x.RoadSegment).WithMany().HasForeignKey(x => x.RoadSegmentId);
        });

        modelBuilder.Entity<GeographicArea>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Geometry).HasMethod("gist");
            entity.HasIndex(x => new { x.AreaType, x.OfficialCode }).IsUnique();
            entity.Property(x => x.Geometry).HasColumnType("geometry(MultiPolygon,4326)");
        });

        modelBuilder.Entity<PopulationSeries>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.GeographicAreaId, x.Year, x.Indicator, x.Category, x.AgeGroup, x.Sex }).IsUnique();
            entity.HasOne(x => x.GeographicArea).WithMany().HasForeignKey(x => x.GeographicAreaId);
            entity.HasOne(x => x.DatasetSource).WithMany().HasForeignKey(x => x.DatasetSourceId);
            entity.Property(x => x.SourceLocator).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<DatasetSource>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Publisher).HasMaxLength(300).IsRequired();
        });

        modelBuilder.Entity<TrafficProfile>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.AssumptionNote).HasMaxLength(1200).IsRequired();
        });
    }
}

