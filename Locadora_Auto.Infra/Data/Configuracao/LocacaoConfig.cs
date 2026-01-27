using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class LocacaoConfig : IEntityTypeConfiguration<Locacao>
    {
        public void Configure(EntityTypeBuilder<Locacao> builder)
        {
            builder.ToTable("tbLocacao");

            // ------------------ Chave Primária ------------------
            builder.HasKey(e => e.IdLocacao);

            builder.Property(e => e.IdLocacao)
                   .HasColumnName("id_locacao");

            // ------------------ Propriedades ------------------
            builder.Property(e => e.ClienteId)
                   .HasColumnName("id_cliente");

            builder.Property(e => e.IdVeiculo)
                   .HasColumnName("id_veiculo");

            builder.Property(e => e.IdFuncionario)
                  .HasColumnName("id_funcionario");

            builder.Property(e => e.IdFilialRetirada)
                  .HasColumnName("id_filial_retirada");

            builder.Property(e => e.IdFilialDevolucao)
                  .HasColumnName("id_filial_devolucao");

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
                   .HasConversion<string>()  // Enum -> string
                   .HasMaxLength(20)
                   .IsRequired();

            // ------------------ Relacionamentos 1:1 / 1:N ------------------
            builder.HasOne(e => e.Cliente)
                   .WithMany(x=>x.Locacoes)
                   .HasForeignKey(e => e.ClienteId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Veiculo)
                   .WithMany(x => x.Locacoes)
                   .HasForeignKey(e => e.IdVeiculo)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Funcionario)
                   .WithMany(x => x.Locacoes)
                   .HasForeignKey(e => e.IdFuncionario)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.FilialRetirada)
                   .WithMany(x => x.LocacoesRetirada)
                   .HasForeignKey(e => e.IdFilialRetirada)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.FilialDevolucao)
                   .WithMany(x => x.LocacoesDevolucao)
                   .HasForeignKey(e => e.IdFilialDevolucao)
                   .OnDelete(DeleteBehavior.Restrict);

            // ------------------ Relacionamentos 1:N ------------------
            builder.HasMany(l => l.Pagamentos)
                   .WithOne(p => p.Locacao)
                   .HasForeignKey(p => p.IdLocacao)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(l => l.Multas)
                   .WithOne(m => m.Locacao)
                   .HasForeignKey(m => m.IdLocacao)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(l => l.Seguros)
                   .WithOne(s => s.Locacao)
                   .HasForeignKey(s => s.IdLocacao)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
