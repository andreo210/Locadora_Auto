using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.Entidades.Indentity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class UserConfigHistorico : IEntityTypeConfiguration<UserHistorico>
    {
        public void Configure(EntityTypeBuilder<UserHistorico> builder)
        {
            builder.ToTable("tbUserHistorico");

            builder.HasKey(e => e.IdHistorico);
            builder.Property(e => e.IdHistorico)
                .HasColumnName("id_historico");

            builder.Property(e => e.Id)
                .HasColumnName("id");

            builder.Property(e => e.NomeCompleto)
                .HasColumnName("nome_completo");

            builder.Property(e => e.PhoneNumber)
               .HasColumnName("phone_number");

            builder.Property(e => e.Email)
                .HasColumnName("email");

            builder.Property(e => e.DataEvento)
                .HasColumnName("data_evento");

            builder.Property(e => e.Acao)
                .HasColumnName("acao");

            builder.Property(e => e.UsuarioEvento)
                .HasColumnName("usuario_evento");
        }
    }
}
