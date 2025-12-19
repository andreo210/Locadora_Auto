using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class LocacaoConfig : IEntityTypeConfiguration<Locacao>
    {
        public void Configure(EntityTypeBuilder<Locacao> builder)
        {
            builder.ToTable("locacao");

            builder.HasKey(e => e.IdLocacao);

            builder.Property(e => e.IdLocacao)
                .HasColumnName("id_locacao");

            builder.Property(e => e.DataInicio)
                .HasColumnName("data_inicio")
                .IsRequired();

            builder.Property(e => e.DataFimPrevista)
                .HasColumnName("data_fim_prevista")
                .IsRequired();

            builder.Property(e => e.DataFimReal)
                .HasColumnName("data_fim_real");

            builder.Property(e => e.KmInicial)
                .HasColumnName("km_inicial")
                .IsRequired();

            builder.Property(e => e.KmFinal)
                .HasColumnName("km_final");

            builder.Property(e => e.ValorPrevisto)
                .HasColumnName("valor_previsto")
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(e => e.ValorFinal)
                .HasColumnName("valor_final")
                .HasPrecision(10, 2);

            builder.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();

            builder.HasOne(e => e.Cliente)
                .WithMany()
                .HasForeignKey(e => e.IdCliente);

            builder.HasOne(e => e.Veiculo)
                .WithMany()
                .HasForeignKey(e => e.IdVeiculo);

            builder.HasOne(e => e.Funcionario)
                .WithMany()
                .HasForeignKey(e => e.IdFuncionario);
        }
    }

}
