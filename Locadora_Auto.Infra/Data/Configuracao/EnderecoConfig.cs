using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class EnderecoConfig : IEntityTypeConfiguration<Endereco>
    {
        public void Configure(EntityTypeBuilder<Endereco> builder)
        {
            builder.ToTable("endereco");

            builder.HasKey(e => e.IdEndereco);

            builder.Property(e => e.IdEndereco)
                .HasColumnName("id_endereco");

            builder.Property(e => e.Logradouro)
                .HasColumnName("logradouro")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(e => e.Numero)
                .HasColumnName("numero")
                .HasMaxLength(20);

            builder.Property(e => e.Bairro)
                .HasColumnName("bairro")
                .HasMaxLength(100);

            builder.Property(e => e.Cidade)
                .HasColumnName("cidade")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.Estado)
                .HasColumnName("uf")
                .HasMaxLength(2)
                .IsRequired();

            builder.Property(e => e.Cep)
                .HasColumnName("cep")
                .HasMaxLength(8)
                .IsRequired();
        }
    }

}
