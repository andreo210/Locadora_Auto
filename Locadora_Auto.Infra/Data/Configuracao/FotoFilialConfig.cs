using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class FotoFilialConfig : IEntityTypeConfiguration<FotoFilial>
    {
        public void Configure(EntityTypeBuilder<FotoFilial> builder)
        {
            builder.ToTable("tbFotoFilial");

            builder.HasKey(e => e.IdFoto);

            builder.Property(e => e.IdFoto)
                .HasColumnName("id_foto");

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

            //chave estrangeira
            builder.HasOne<Filial>()
                   .WithMany(l => l.Fotos)
                   .HasForeignKey("id_filial")// fk fantasma, não existe na classe, mas é necessário para o relacionamento
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
