using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class ManutencaoConfig : IEntityTypeConfiguration<Manutencao>
    {
        public void Configure(EntityTypeBuilder<Manutencao> builder)
        {
            builder.ToTable("manutencao");

            builder.HasKey(e => e.IdManutencao);

            builder.Property(e => e.IdManutencao)
                .HasColumnName("id_manutencao");

            builder.Property(e => e.Tipo)
                .HasColumnName("tipo")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.Custo)
                .HasColumnName("custo")
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();

            builder.HasOne(e => e.Veiculo)
                .WithMany()
                .HasForeignKey(e => e.IdVeiculo);
        }
    }

}
