using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class FechamentoLocacaoConfig : IEntityTypeConfiguration<FechamentoLocacao>
    {
        public void Configure(EntityTypeBuilder<FechamentoLocacao> builder)
        {
            builder.ToTable("tb_fechamento_locacao");

            //chave primaria
            builder.HasKey(e => e.IdFechamento);
            builder.Property(e => e.IdFechamento)
                .HasColumnName("id_fechamento");

            builder.Property(e => e.DataApuracao)
                .HasColumnName("data_apuracao")
                .IsRequired();

            //nula enquanto a apuração corre; é ela que responde `Selado`
            builder.Property(e => e.DataSelagem)
                .HasColumnName("data_selagem");

            builder.Property(e => e.TotalDebitos)
                .HasColumnName("total_debitos")
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(e => e.TotalCreditos)
                .HasColumnName("total_creditos")
                .HasPrecision(10, 2)
                .IsRequired();

            //RN-29: assinado de propósito — saldo negativo é crédito a devolver, e truncar para
            //zero seria a casa ficando com dinheiro que não é dela
            builder.Property(e => e.Saldo)
                .HasColumnName("saldo")
                .HasPrecision(10, 2)
                .IsRequired();

            //chave estrangeira
            builder.Property(e => e.IdLocacao)
                .HasColumnName("id_locacao")
                .IsRequired();

            builder.Property(e => e.IdFuncionarioApuracao)
                .HasColumnName("id_funcionario_apuracao")
                .IsRequired();

            //RN-32: um fechamento por contrato. O 1:1 já cria o índice único sobre id_locacao —
            //é ele a garantia de que apurar duas vezes não produz duas contas, com a idempotência
            //de `Locacao.AbrirFechamento` sendo a recusa amigável do mesmo invariante
            builder.HasOne<Locacao>()
                   .WithOne(l => l.Fechamento)
                   .HasForeignKey<FechamentoLocacao>(e => e.IdLocacao)
                   .OnDelete(DeleteBehavior.Cascade);

            //quem apurou não some da conta quando sai da empresa: o indicador de vazamento de
            //receita da seção 12 abre por atendente, e apagar o vínculo apagaria a resposta
            builder.HasOne<Funcionario>()
                   .WithMany()
                   .HasForeignKey(e => e.IdFuncionarioApuracao)
                   .OnDelete(DeleteBehavior.Restrict);

            //a coleção é append-only e só é lida por Include; o acesso é pelo campo, que é onde a
            //entidade guarda as linhas
            builder.Metadata
                   .FindNavigation(nameof(FechamentoLocacao.Linhas))!
                   .SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
