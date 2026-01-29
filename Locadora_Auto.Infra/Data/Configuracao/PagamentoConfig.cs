using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PagamentoConfig : IEntityTypeConfiguration<Pagamento>
{
    public void Configure(EntityTypeBuilder<Pagamento> builder)
    {
        builder.ToTable("tbPagamento");

        builder.HasKey(p => p.IdPagamento);

        builder.Property(p => p.IdPagamento)
            .HasColumnName("id_pagamento");

        builder.Property(p => p.Valor)
            .HasColumnName("valor")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(p => p.DataPagamento)
            .HasColumnName("data_pagamento");

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        // FK sombra para Locação
        builder.Property<int>("id_locacao");

        builder.HasOne<Locacao>()
            .WithMany(l => l.Pagamentos)
            .HasForeignKey("id_locacao")
            .OnDelete(DeleteBehavior.Restrict);

        // FK sombra para FormaPagamento
        builder.Property(p => p.FormaPagamento)
            .HasConversion<int>()
            .HasColumnName("id_forma_pagamento")
            .IsRequired();
    }
}
