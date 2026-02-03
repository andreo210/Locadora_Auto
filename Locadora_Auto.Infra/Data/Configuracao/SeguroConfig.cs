using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class SeguroConfig : IEntityTypeConfiguration<Seguro>
    {
        public void Configure(EntityTypeBuilder<Seguro> builder)
        {
            builder.ToTable("tbSeguro");

            builder.HasKey(e => e.IdSeguro);

            builder.Property(e => e.IdSeguro)
                .HasColumnName("id_seguro");

            builder.Property(e => e.Descricao)
                .HasColumnName("descricao")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.Nome)
                .HasColumnName("nome")
                .HasMaxLength(45)
                .IsRequired();

            builder.Property(e => e.ValorDiaria)
                .HasColumnName("valor_diaria")
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(e => e.Cobertura)
                .HasColumnName("cobertura")
                .HasMaxLength(200);
        }
    }

}
