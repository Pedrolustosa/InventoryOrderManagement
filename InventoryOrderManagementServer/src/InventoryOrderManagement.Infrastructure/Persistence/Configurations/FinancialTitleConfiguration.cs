using InventoryOrderManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryOrderManagement.Infrastructure.Persistence.Configurations;

public class FinancialTitleConfiguration : IEntityTypeConfiguration<FinancialTitle>
{
    public void Configure(EntityTypeBuilder<FinancialTitle> builder)
    {
        builder.ToTable("FinancialTitles");
        builder.HasKey(title => title.Id);

        builder.Property(title => title.TitleNumber)
            .IsRequired()
            .HasMaxLength(FinancialTitle.TitleNumberMaxLength);
        builder.HasIndex(title => title.TitleNumber).IsUnique();

        builder.Property(title => title.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.HasIndex(title => title.Type);

        builder.Property(title => title.Description)
            .IsRequired()
            .HasMaxLength(FinancialTitle.DescriptionMaxLength);

        builder.Property(title => title.CounterpartyType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(title => title.CounterpartyName)
            .IsRequired()
            .HasMaxLength(FinancialTitle.CounterpartyNameMaxLength);
        builder.HasIndex(title => title.CounterpartyId);

        builder.Property(title => title.SourceType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.HasIndex(title => title.SourceId);

        builder.Property(title => title.SourceReference)
            .HasMaxLength(FinancialTitle.SourceReferenceMaxLength);

        builder.Property(title => title.DocumentNumber)
            .HasMaxLength(FinancialTitle.DocumentNumberMaxLength);

        builder.HasIndex(title => new { title.Type, title.CounterpartyId, title.DocumentNumber })
            .IsUnique()
            .HasFilter("\"DocumentNumber\" IS NOT NULL AND \"CounterpartyId\" IS NOT NULL");

        builder.HasIndex(title => new { title.Type, title.CounterpartyName, title.DocumentNumber })
            .IsUnique()
            .HasFilter("\"DocumentNumber\" IS NOT NULL AND \"CounterpartyId\" IS NULL");

        builder.Property(title => title.IssueDate).IsRequired();
        builder.HasIndex(title => title.IssueDate);

        builder.Property(title => title.TotalAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(title => title.Notes).HasMaxLength(FinancialTitle.NotesMaxLength);
        builder.Property(title => title.CreatedAtUtc).IsRequired();
        builder.HasIndex(title => title.CreatedAtUtc);
        builder.Property(title => title.CancellationReason).HasMaxLength(FinancialTitle.CancellationReasonMaxLength);
        builder.Property(title => title.Version).IsRequired().IsConcurrencyToken();

        builder.HasMany(title => title.Installments)
            .WithOne()
            .HasForeignKey(installment => installment.FinancialTitleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(title => title.Installments).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(title => title.Settlements)
            .WithOne()
            .HasForeignKey(settlement => settlement.FinancialTitleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(title => title.Settlements).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(title => title.SettledPrincipalAmount);
        builder.Ignore(title => title.OutstandingBalance);
        builder.Ignore(title => title.HasSettlements);
    }
}
