using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class LinhaFechamentoConfig : IEntityTypeConfiguration<LinhaFechamento>
    {
        public void Configure(EntityTypeBuilder<LinhaFechamento> builder)
        {
            builder.ToTable("tb_linha_fechamento");

            //chave primaria
            builder.HasKey(e => e.IdLinhaFechamento);
            builder.Property(e => e.IdLinhaFechamento)
                .HasColumnName("id_linha_fechamento");

            builder.Property(e => e.Tipo)
                .HasColumnName("tipo")
                .HasConversion<int>()
                .IsRequired();

            //RN-31: o que sustenta a cobrança quando ela é contestada. 300 caracteres cabem a
            //medição declarada — "franquia de 600 km sobre 3 diárias, rodados 750 km" — sem virar
            //campo de observação livre
            builder.Property(e => e.BaseCalculo)
                .HasColumnName("base_calculo")
                .HasMaxLength(300)
                .IsRequired();

            //quatro casas porque a RN-19 cobra proteção pró-rata: 1,5 diária é quantidade legítima
            builder.Property(e => e.Quantidade)
                .HasColumnName("quantidade")
                .HasPrecision(12, 4)
                .IsRequired();

            builder.Property(e => e.ValorUnitario)
                .HasColumnName("valor_unitario")
                .HasPrecision(10, 2)
                .IsRequired();

            //RN-33: já arredondado a 2 casas na origem, por linha
            builder.Property(e => e.Total)
                .HasColumnName("total")
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(e => e.DataLancamento)
                .HasColumnName("data_lancamento")
                .IsRequired();

            builder.Property(e => e.EhCorrecao)
                .HasColumnName("eh_correcao")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(e => e.Motivo)
                .HasColumnName("motivo")
                .HasMaxLength(500);

            //`Natureza` não tem coluna de propósito: é derivada do tipo, e guardá-la criaria um
            //segundo lugar onde o sinal da linha pode divergir do que ela cobra

            //chave estrangeira
            builder.Property(e => e.IdFechamento)
                .HasColumnName("id_fechamento")
                .IsRequired();

            //nulo nas linhas que a apuração calculou sozinha; obrigatório em correção e isenção
            builder.Property(e => e.IdFuncionarioLancamento)
                .HasColumnName("id_funcionario_lancamento");

            builder.HasOne<FechamentoLocacao>()
                   .WithMany(f => f.Linhas)
                   .HasForeignKey(e => e.IdFechamento)
                   .OnDelete(DeleteBehavior.Cascade);

            //RN-34: quem isentou continua respondendo pela isenção depois de sair da empresa — é o
            //indicador "isenções por alçada" da seção 12 que depende disso
            builder.HasOne<Funcionario>()
                   .WithMany()
                   .HasForeignKey(e => e.IdFuncionarioLancamento)
                   .OnDelete(DeleteBehavior.Restrict);

            //a leitura que existe é sempre "as linhas deste fechamento, na ordem em que foram
            //lançadas" — que é a ordem em que o extrato se lê, com as correções no fim
            builder.HasIndex(e => new { e.IdFechamento, e.IdLinhaFechamento });
        }
    }
}
