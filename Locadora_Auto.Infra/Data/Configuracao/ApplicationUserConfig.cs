using Locadora_Auto.Domain.Entidades.Indentity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class ApplicationUserConfig : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            //nomes de tabela e coluna vêm da convenção snake_case (UseSnakeCaseNamingConvention)
            builder.Property(e => e.NomeCompleto)
                .HasMaxLength(255);
        }
    }

}
