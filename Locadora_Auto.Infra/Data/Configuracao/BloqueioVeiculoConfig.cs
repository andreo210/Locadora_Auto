using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class BloqueioVeiculoConfig : IEntityTypeConfiguration<BloqueioVeiculo>
    {
        public void Configure(EntityTypeBuilder<BloqueioVeiculo> builder)
        {
            builder.ToTable("tb_bloqueio_veiculo");

            //chave primaria
            builder.HasKey(e => e.IdBloqueioVeiculo);
            builder.Property(e => e.IdBloqueioVeiculo)
                .HasColumnName("id_bloqueio_veiculo");

            builder.Property(e => e.Motivo)
                .HasColumnName("motivo")
                .HasConversion<int>()
                .IsRequired();

            builder.Property(e => e.Observacao)
                .HasColumnName("observacao")
                .HasMaxLength(500);

            builder.Property(e => e.DataBloqueio)
                .HasColumnName("data_bloqueio")
                .IsRequired();

            builder.Property(e => e.DataPrevistaLiberacao)
                .HasColumnName("data_prevista_liberacao")
                .IsRequired();

            builder.Property(e => e.DataLiberacao)
                .HasColumnName("data_liberacao");

            builder.Property(e => e.StatusAnterior)
                .HasColumnName("status_anterior")
                .HasConversion<int>()
                .IsRequired();

            //chave estrangeira
            builder.Property(e => e.IdVeiculo)
                .HasColumnName("id_veiculo")
                .IsRequired();

            builder.Property(e => e.IdFuncionarioResponsavel)
                .HasColumnName("id_funcionario_responsavel")
                .IsRequired();

            builder.HasOne<Veiculo>()
                   .WithMany(v => v.Bloqueios)
                   .HasForeignKey(e => e.IdVeiculo)
                   .OnDelete(DeleteBehavior.Cascade);

            //o responsável não some do bloqueio quando ele sai da empresa: a RN-52 quer saber a
            //quem cobrar o carro de volta, e apagar o vínculo apagaria justamente essa resposta
            builder.HasOne(e => e.Responsavel)
                   .WithMany()
                   .HasForeignKey(e => e.IdFuncionarioResponsavel)
                   .OnDelete(DeleteBehavior.Restrict);

            //as duas consultas que existem: o bloqueio em aberto de um veículo e a lista de
            //vencidos da seção 12, que varre por data com data_liberacao nula
            builder.HasIndex(e => new { e.IdVeiculo, e.DataLiberacao });
            builder.HasIndex(e => e.DataPrevistaLiberacao);
        }
    }

}
