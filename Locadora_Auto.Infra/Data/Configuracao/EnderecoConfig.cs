using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class EnderecoConfig : IEntityTypeConfiguration<Endereco>
    {
        public void Configure(EntityTypeBuilder<Endereco> builder)
        {
            builder.ToTable("tbEndereco");

            builder.HasKey(e => e.IdEndereco);

            builder.Property(e => e.IdEndereco)
                .HasColumnName("id_endereco");

            builder.Property(e => e.IdCliente)
                .HasColumnName("id_cliente");
 
            builder.HasIndex(e => e.IdCliente)
                .IsUnique();

            builder.Property(e => e.Logradouro)
                .HasColumnName("logradouro");

            builder.Property(e => e.Numero)
                .HasColumnName("numero");

            builder.Property(e => e.Complemento)
                .HasColumnName("complemento");

            builder.Property(e => e.Bairro)
                .HasColumnName("bairro");

            builder.Property(e => e.Cidade)
                .HasColumnName("cidade");

            builder.Property(e => e.Estado)
                .HasColumnName("estado");

            builder.Property(e => e.Cep)
                .HasColumnName("cep");

            builder.Property(e => e.DataCriacao)
                .HasColumnName("data_criacao")
                .IsRequired();
        }
    }

}
