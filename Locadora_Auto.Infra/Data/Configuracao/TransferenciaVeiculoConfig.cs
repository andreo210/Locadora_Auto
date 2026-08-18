using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class TransferenciaVeiculoConfig : IEntityTypeConfiguration<TransferenciaVeiculo>
    {
        public void Configure(EntityTypeBuilder<TransferenciaVeiculo> builder)
        {
            builder.ToTable("tb_transferencia_veiculo");

            //chave primaria
            builder.HasKey(e => e.IdTransferenciaVeiculo);
            builder.Property(e => e.IdTransferenciaVeiculo)
                .HasColumnName("id_transferencia_veiculo");

            builder.Property(e => e.DataEnvio)
                .HasColumnName("data_envio")
                .IsRequired();

            builder.Property(e => e.DataPrevistaChegada)
                .HasColumnName("data_prevista_chegada")
                .IsRequired();

            builder.Property(e => e.DataChegada)
                .HasColumnName("data_chegada");

            builder.Property(e => e.Status)
                .HasColumnName("status_transferencia")
                .HasConversion<int>()
                .IsRequired();

            builder.Property(e => e.Observacao)
                .HasColumnName("observacao")
                .HasMaxLength(500);

            //chave estrangeira
            builder.Property(e => e.IdVeiculo)
                .HasColumnName("id_veiculo")
                .IsRequired();

            builder.Property(e => e.IdFilialOrigem)
                .HasColumnName("id_filial_origem")
                .IsRequired();

            builder.Property(e => e.IdFilialDestino)
                .HasColumnName("id_filial_destino")
                .IsRequired();

            builder.Property(e => e.IdFuncionarioResponsavel)
                .HasColumnName("id_funcionario_responsavel")
                .IsRequired();

            builder.HasOne<Veiculo>()
                   .WithMany(v => v.Transferencias)
                   .HasForeignKey(e => e.IdVeiculo)
                   .OnDelete(DeleteBehavior.Cascade);

            //filial nao e excluida com veiculo em transito, e apagar a transferencia deixaria o
            //movimento da RN-37 sem documento de origem
            builder.HasOne(e => e.FilialOrigem)
                   .WithMany()
                   .HasForeignKey(e => e.IdFilialOrigem)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.FilialDestino)
                   .WithMany()
                   .HasForeignKey(e => e.IdFilialDestino)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Responsavel)
                   .WithMany()
                   .HasForeignKey(e => e.IdFuncionarioResponsavel)
                   .OnDelete(DeleteBehavior.Restrict);

            //as duas consultas: a viagem em curso de um veiculo e as atrasadas da rede
            builder.HasIndex(e => new { e.IdVeiculo, e.Status });
            builder.HasIndex(e => e.DataPrevistaChegada);
        }
    }

}
