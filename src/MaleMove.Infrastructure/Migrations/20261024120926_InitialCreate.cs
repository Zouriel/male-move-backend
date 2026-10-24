using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace MaleMove.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "DatasetSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Publisher = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceUri = table.Column<string>(type: "text", nullable: true),
                    SourceYear = table.Column<int>(type: "integer", nullable: true),
                    License = table.Column<string>(type: "text", nullable: false),
                    ApprovalState = table.Column<string>(type: "text", nullable: false),
                    CoverageDescription = table.Column<string>(type: "text", nullable: false),
                    Limitations = table.Column<string>(type: "text", nullable: false),
                    LastCheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeographicAreas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentAreaId = table.Column<Guid>(type: "uuid", nullable: true),
                    OfficialCode = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AreaType = table.Column<string>(type: "text", nullable: false),
                    Geometry = table.Column<MultiPolygon>(type: "geometry(MultiPolygon,4326)", nullable: false),
                    SourceVersion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeographicAreas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermitAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PermitId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PreviousState = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    NewState = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermitAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermitDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    MediaType = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ScanStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OcrStatus = table.Column<string>(type: "text", nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermitDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoadSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OsmWayId = table.Column<long>(type: "bigint", nullable: false),
                    FromNodeId = table.Column<long>(type: "bigint", nullable: false),
                    ToNodeId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    RoadClass = table.Column<string>(type: "text", nullable: false),
                    AccessProfile = table.Column<string>(type: "text", nullable: false),
                    IsOneWay = table.Column<bool>(type: "boolean", nullable: false),
                    LengthMeters = table.Column<double>(type: "double precision", nullable: false),
                    AssumedBaseSpeedKmh = table.Column<double>(type: "double precision", nullable: true),
                    Geometry = table.Column<LineString>(type: "geometry(LineString,4326)", nullable: false),
                    NetworkVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadSegments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrafficProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DayType = table.Column<string>(type: "text", nullable: false),
                    RoadClass = table.Column<string>(type: "text", nullable: false),
                    StartsAtLocal = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndsAtLocal = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    SpeedMultiplier = table.Column<double>(type: "double precision", nullable: false),
                    IsCalibrated = table.Column<bool>(type: "boolean", nullable: false),
                    AssumptionNote = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrafficProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PopulationSeries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GeographicAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Indicator = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: true),
                    AgeGroup = table.Column<string>(type: "text", nullable: true),
                    Sex = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<long>(type: "bigint", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    IsProjection = table.Column<bool>(type: "boolean", nullable: false),
                    DatasetSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceLocator = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PopulationSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PopulationSeries_DatasetSources_DatasetSourceId",
                        column: x => x.DatasetSourceId,
                        principalTable: "DatasetSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PopulationSeries_GeographicAreas_GeographicAreaId",
                        column: x => x.GeographicAreaId,
                        principalTable: "GeographicAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Permits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PermitNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LocationDescription = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    WorkDescription = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Contractor = table.Column<string>(type: "text", nullable: true),
                    ImpactType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ApprovalState = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SiteVerificationState = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DailyStart = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    DailyEnd = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    ReviewComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    PermitDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permits_PermitDocuments_PermitDocumentId",
                        column: x => x.PermitDocumentId,
                        principalTable: "PermitDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PermitRoadSegments",
                columns: table => new
                {
                    PermitId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadSegmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestrictionType = table.Column<string>(type: "text", nullable: false),
                    DelaySeconds = table.Column<double>(type: "double precision", nullable: true),
                    SpeedMultiplier = table.Column<double>(type: "double precision", nullable: true),
                    MatchConfidence = table.Column<double>(type: "double precision", nullable: true),
                    MatchEvidence = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermitRoadSegments", x => new { x.PermitId, x.RoadSegmentId });
                    table.ForeignKey(
                        name: "FK_PermitRoadSegments_Permits_PermitId",
                        column: x => x.PermitId,
                        principalTable: "Permits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PermitRoadSegments_RoadSegments_RoadSegmentId",
                        column: x => x.RoadSegmentId,
                        principalTable: "RoadSegments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeographicAreas_AreaType_OfficialCode",
                table: "GeographicAreas",
                columns: new[] { "AreaType", "OfficialCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeographicAreas_Geometry",
                table: "GeographicAreas",
                column: "Geometry")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_PermitAuditEvents_PermitId_OccurredAt",
                table: "PermitAuditEvents",
                columns: new[] { "PermitId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PermitDocuments_Sha256",
                table: "PermitDocuments",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_PermitRoadSegments_RoadSegmentId",
                table: "PermitRoadSegments",
                column: "RoadSegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Permits_ApprovalState_StartsAt_EndsAt",
                table: "Permits",
                columns: new[] { "ApprovalState", "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Permits_PermitDocumentId",
                table: "Permits",
                column: "PermitDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Permits_PermitNumber",
                table: "Permits",
                column: "PermitNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PopulationSeries_DatasetSourceId",
                table: "PopulationSeries",
                column: "DatasetSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_PopulationSeries_GeographicAreaId_Year_Indicator_Category_A~",
                table: "PopulationSeries",
                columns: new[] { "GeographicAreaId", "Year", "Indicator", "Category", "AgeGroup", "Sex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoadSegments_FromNodeId_ToNodeId",
                table: "RoadSegments",
                columns: new[] { "FromNodeId", "ToNodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoadSegments_Geometry",
                table: "RoadSegments",
                column: "Geometry")
                .Annotation("Npgsql:IndexMethod", "gist");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PermitAuditEvents");

            migrationBuilder.DropTable(
                name: "PermitRoadSegments");

            migrationBuilder.DropTable(
                name: "PopulationSeries");

            migrationBuilder.DropTable(
                name: "TrafficProfiles");

            migrationBuilder.DropTable(
                name: "Permits");

            migrationBuilder.DropTable(
                name: "RoadSegments");

            migrationBuilder.DropTable(
                name: "DatasetSources");

            migrationBuilder.DropTable(
                name: "GeographicAreas");

            migrationBuilder.DropTable(
                name: "PermitDocuments");
        }
    }
}
