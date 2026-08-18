using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class RecusaSobreposicaoConfig : IEntityTypeConfiguration<RecusaSobreposicao>
    {
        public void Configure(EntityTypeBuilder<RecusaSobreposicao> builder)
        {
            builder.ToTable("tb_recusa_sobreposicao");

            //chave primaria
            builder.HasKey(e => e.IdRecusaSobreposicao);
            builder.Property(e => e.IdRecusaSobreposicao)
                .HasColumnName("id_recusa_sobreposicao");

            builder.Property(e => e.InicioSolicitado)
                .HasColumnName("inicio_solicitado")
                .IsRequired();

            builder.Property(e => e.FimSolicitado)
                .HasColumnName("fim_solicitado")
                .IsRequired();

            builder.Property(e => e.DataRecusa)
                .HasColumnName("data_recusa")
                .IsRequired();

            builder.Property(e => e.Origem)
                .HasColumnName("origem")
                .HasConversion<int>()
                .IsRequired();

            //auditoria: e daqui que sai quem tentou
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

            builder.Property(e => e.IdFilialRetirada)
                .HasColumnName("id_filial_retirada")
                .IsRequired();

            builder.Property(e => e.IdLocacaoEmExtensao)
                .HasColumnName("id_locacao_em_extensao");

            //sem navegacao e sem FK: a recusa e o registro de um fato do balcao, e ela tem de
            //sobreviver ao veiculo ser excluido ou desmobilizado. Uma FK Restrict travaria a
            //exclusao do veiculo por causa de uma tentativa recusada meses atras; uma Cascade
            //apagaria a serie historica do indicador, que e justamente o que se quer acompanhar

            //o indicador e "por filial, no periodo": esta e a consulta dele
            builder.HasIndex(e => new { e.IdFilialRetirada, e.DataRecusa });
        }
    }

}
