using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class VeiculoConfig : IEntityTypeConfiguration<Veiculo>
    {
        public void Configure(EntityTypeBuilder<Veiculo> builder)
        {
            builder.ToTable("veiculo");

            builder.HasKey(e => e.IdVeiculo);

            builder.Property(e => e.IdVeiculo)
                .HasColumnName("id_veiculo");

            builder.Property(e => e.Placa)
                .HasColumnName("placa")
                .HasMaxLength(10)
                .IsRequired();

            builder.HasIndex(e => e.Placa).IsUnique();

            builder.Property(e => e.Chassi)
                .HasColumnName("chassi")
                .HasMaxLength(30)
                .IsRequired();

            builder.HasIndex(e => e.Chassi).IsUnique();

            builder.Property(e => e.KmAtual)
                .HasColumnName("km_atual")
                .IsRequired();

            builder.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();

            builder.HasOne(e => e.Categoria)
                .WithMany()
                .HasForeignKey(e => e.IdCategoria)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.FilialAtual)
                .WithMany()
                .HasForeignKey(e => e.IdFilialAtual)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
