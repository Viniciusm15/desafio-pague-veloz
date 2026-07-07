using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Infrastructure.Persistence.Configurations;

public class AccountOperationConfiguration : IEntityTypeConfiguration<AccountOperation>
{
    public void Configure(EntityTypeBuilder<AccountOperation> builder)
    {
        builder.ToTable("AccountOperations");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.OccurredAt)
            .IsRequired();

        builder.HasOne<Account>()
            .WithMany(a => a.Operations)
            .HasForeignKey(o => o.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
