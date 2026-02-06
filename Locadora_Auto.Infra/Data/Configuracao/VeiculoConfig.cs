using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class VeiculoConfig : IEntityTypeConfiguration<Veiculo>
    {
        public void Configure(EntityTypeBuilder<Veiculo> builder)
        {
            builder.ToTable("tbVeiculo");

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

            builder.Property(e => e.Ativo)
                .HasColumnName("ativo")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.Marca)
                .HasColumnName("marca");

            builder.Property(e => e.Modelo)
                .HasColumnName("modelo");

            builder.Property(e => e.Ano)
                .HasColumnName("ano");

            builder.Property(c => c.Status)
                  .HasColumnName("status")
                  .HasConversion<int>()
                  .IsRequired();

            //chave estrangeira
            builder.Property(e => e.IdCategoria)
                .HasColumnName("id_categoria")
                .IsRequired();

            builder.Property(e => e.FilialAtualId)
                .HasColumnName("id_filial_atual")
                .IsRequired();


            builder.HasOne(e => e.Categoria)//uma categoria tem muitos veículos
                .WithMany(v=>v.Veiculos)
                .HasForeignKey(e => e.IdCategoria)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.FilialAtual)
                .WithMany(v => v.Veiculos)
                .HasForeignKey(e => e.FilialAtualId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
