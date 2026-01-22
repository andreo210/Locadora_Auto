using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class CategoriaVeiculoConfig : IEntityTypeConfiguration<CategoriaVeiculo>
    {
        public void Configure(EntityTypeBuilder<CategoriaVeiculo> builder)
        {
            builder.ToTable("categoria_veiculo");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasColumnName("id_categoria");

            builder.Property(e => e.Nome)
                .HasColumnName("nome")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.ValorDiaria)
                .HasColumnName("valor_diaria")
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(e => e.LimiteKm)
                .HasColumnName("limite_km");

            builder.Property(e => e.ValorKmExcedente)
                .HasColumnName("valor_km_excedente")
                .HasPrecision(10, 2);
        }
    }

}
