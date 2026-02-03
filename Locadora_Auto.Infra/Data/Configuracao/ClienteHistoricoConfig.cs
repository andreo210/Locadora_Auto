using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class ClienteHistoricoConfig : IEntityTypeConfiguration<ClienteHistorico>
    {
        public void Configure(EntityTypeBuilder<ClienteHistorico> builder)
        {
            builder.ToTable("tbClienteHistorico");

            builder.HasKey(e => e.IdHistorico);
            builder.Property(e => e.IdHistorico)
                .HasColumnName("idHistorico");

            builder.Property(e => e.IdCliente)
                .HasColumnName("id_cliente");

            builder.Property(e => e.NumeroHabilitacao)
               .HasColumnName("numero_habilitacao");

            builder.Property(e => e.ValidadeHabilitacao)
                .HasColumnName("validade_habilitacao");

            builder.Property(e => e.TotalLocacoes)
               .HasColumnName("total_locacoes");

            builder.Property(e => e.DataEvento)
                .HasColumnName("data_evento");

            builder.Property(e => e.Acao)
                .HasColumnName("acao");

            builder.Property(e => e.UsuarioEvento)
                .HasColumnName("usuario_evento");

            builder.Property(e => e.IdUsuarioModificacao)
                .HasColumnName("Id_usuario_modificacao");
        }
    }
}
