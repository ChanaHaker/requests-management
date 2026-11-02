using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequestsManagement.DAL.Entities;

namespace RequestsManagement.DAL.Configurations;

public class RequestConfiguration : IEntityTypeConfiguration<Request>
{
    public void Configure(EntityTypeBuilder<Request> builder)
    {
        builder.ToTable("Requests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.OrganizationName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.AssignedTo)
            .HasMaxLength(100);

        builder.Property(r => r.RowVersion)
            .IsRowVersion();

        // Filter by status, sorted by date. Also covers "count by status" aggregation.
        builder.HasIndex(r => new { r.Status, r.CreatedAt });

        // Filter by priority, sorted by date. Also covers "count by priority" aggregation.
        builder.HasIndex(r => new { r.Priority, r.CreatedAt });

        // Exact filter by organization.
        builder.HasIndex(r => r.OrganizationName);

        // Exact filter by assignee.
        builder.HasIndex(r => r.AssignedTo);

        // Date-range filter and the default sort (newest first).
        builder.HasIndex(r => r.CreatedAt);
    }
}