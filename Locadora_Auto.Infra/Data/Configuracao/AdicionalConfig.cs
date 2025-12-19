using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class AdicionalConfig : IEntityTypeConfiguration<Adicional>
    {
        public void Configure(EntityTypeBuilder<Adicional> builder)
        {
            builder.ToTable("adicional");

            builder.HasKey(e => e.IdAdicional);

            builder.Property(e => e.IdAdicional)
                .HasColumnName("id_adicional");

            builder.Property(e => e.Nome)
                .HasColumnName("nome")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.ValorDiaria)
                .HasColumnName("valor_diaria")
                .HasPrecision(10, 2)
                .IsRequired();
        }
    }

}
