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

        // Every non-clustered index also carries the clustered key (Id),
        // so each index below is effectively (column(s), Id) - matching the sort order
        // used by the repository (sort field, then Id). No sort operation is needed.

        // Filter by status, sorted by date. Also covers "count by status" and sort by status.
        builder.HasIndex(r => new { r.Status, r.CreatedAt });

        // Filter by priority, sorted by date. Also covers "count by priority" and sort by priority.
        builder.HasIndex(r => new { r.Priority, r.CreatedAt });

        // Exact filter by organization, and sort by organization.
        builder.HasIndex(r => r.OrganizationName);

        // Exact filter by assignee.
        builder.HasIndex(r => r.AssignedTo);

        // Date-range filter and the default sort (newest first).
        builder.HasIndex(r => r.CreatedAt);

        // Sort by last update.
        builder.HasIndex(r => r.UpdatedAt);

        // Sort by title. Without it, sorting 100,000 rows by text needed a large memory grant
        // and timed out on LocalDB. (It does not help LIKE '%term%' search.)
        builder.HasIndex(r => r.Title);
    }
}