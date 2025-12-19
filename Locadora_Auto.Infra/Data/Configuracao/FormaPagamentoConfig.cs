using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class FormaPagamentoConfig : IEntityTypeConfiguration<FormaPagamento>
    {
        public void Configure(EntityTypeBuilder<FormaPagamento> builder)
        {
            builder.ToTable("forma_pagamento");

            builder.HasKey(e => e.IdFormaPagamento);

            builder.Property(e => e.IdFormaPagamento)
                .HasColumnName("id_forma_pagamento");

            builder.Property(e => e.Descricao)
                .HasColumnName("descricao")
                .HasMaxLength(50)
                .IsRequired();
        }
    }

}
