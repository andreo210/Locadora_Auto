using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class MovimentoVeiculoConfig : IEntityTypeConfiguration<MovimentoVeiculo>
    {
        public void Configure(EntityTypeBuilder<MovimentoVeiculo> builder)
        {
            builder.ToTable("tb_movimento_veiculo");

            //chave primaria
            builder.HasKey(e => e.IdMovimentoVeiculo);
            builder.Property(e => e.IdMovimentoVeiculo)
                .HasColumnName("id_movimento_veiculo");

            builder.Property(e => e.StatusOrigem)
                .HasColumnName("status_origem")
                .HasConversion<int>();

            builder.Property(e => e.StatusDestino)
                .HasColumnName("status_destino")
                .HasConversion<int>()
                .IsRequired();

            builder.Property(e => e.TipoOrigem)
                .HasColumnName("tipo_origem")
                .HasConversion<int>()
                .IsRequired();

            builder.Property(e => e.DataMovimento)
                .HasColumnName("data_movimento")
                .IsRequired();

            //auditoria: é daqui que sai o autor da transição (RN-37)
            builder.Property(e => e.IdUsuarioCriacao)
                .HasColumnName("id_usuario_criacao");

            builder.Property(e => e.IdUsuarioModificacao)
                .HasColumnName("id_usuario_modificacao");

            builder.Property(e => e.DataCriacao)
                .HasColumnName("data_criacao");

            builder.Property(e => e.DataModificacao)
                .HasColumnName("data_modificacao");

            //chave estrangeira
            builder.Property(e => e.IdVeiculo)
                .HasColumnName("id_veiculo")
                .IsRequired();

            builder.Property(e => e.IdLocacaoOrigem)
                .HasColumnName("id_locacao_origem");

            builder.Property(e => e.IdManutencaoOrigem)
                .HasColumnName("id_manutencao_origem");

            builder.HasOne<Veiculo>()
                   .WithMany(v => v.Movimentos)
                   .HasForeignKey(e => e.IdVeiculo)
                   .OnDelete(DeleteBehavior.Cascade);

            //locação nunca é excluída (a FK do veículo para locação já é Restrict), então o
            //movimento não precisa de cascata e o histórico não desaparece por engano
            builder.HasOne(e => e.LocacaoOrigem)
                   .WithMany()
                   .HasForeignKey(e => e.IdLocacaoOrigem)
                   .OnDelete(DeleteBehavior.Restrict);

            //a manutenção sai junto com o veículo (cascata), e o movimento que a cita tem de sair
            //junto também — senão a exclusão do veículo esbarra na referência
            builder.HasOne(e => e.ManutencaoOrigem)
                   .WithMany()
                   .HasForeignKey(e => e.IdManutencaoOrigem)
                   .OnDelete(DeleteBehavior.Cascade);

            //a consulta natural é "o que aconteceu com este carro, em ordem" — e é dela que sai
            //tanto o último movimento quanto a fila de preparação vencida (RN-45)
            builder.HasIndex(e => new { e.IdVeiculo, e.DataMovimento });
        }
    }

}
