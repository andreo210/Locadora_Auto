using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class FotoConfig : IEntityTypeConfiguration<Foto>
    {
        public void Configure(EntityTypeBuilder<Foto> builder)
        {
            builder.ToTable("tbFotos");

            builder.HasKey(e => e.IdFoto);

            builder.Property(e => e.IdFoto)
                .HasColumnName("id_foto");

            builder.Property(c => c.Tipo)
                   .HasColumnName("tipo")
                   .HasConversion<string>();

            builder.Property(e => e.IdEntidade)
                .HasColumnName("id_entidade");

            builder.Property(e => e.NomeArquivo)
                .HasColumnName("nome_arquivo");

            builder.Property(e => e.Raiz)
               .HasColumnName("raiz");

            builder.Property(e => e.QuantidadeBytes)
              .HasColumnName("quantidadeBytes");

            builder.Property(e => e.DataUpload)
               .HasColumnName("data_upload");

            builder.Property(e => e.Diretorio)
               .HasColumnName("diretorio");

            builder.Property(e => e.Extensao)
               .HasColumnName("extensao");
        }
    }
}
