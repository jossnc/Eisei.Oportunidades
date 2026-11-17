using Eisei.Oportunidades.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eisei.Oportunidades.Api.Data.Configurations;

public class OportunidadConfiguration : IEntityTypeConfiguration<Oportunidad>
{
    public void Configure(EntityTypeBuilder<Oportunidad> builder)
    {
        builder.ToTable("Oportunidades");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Cliente)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(x => x.MontoEstimado)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2);

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Nueva");

        builder.Property(x => x.FechaCreacionUtc)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
