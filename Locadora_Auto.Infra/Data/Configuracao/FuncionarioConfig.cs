using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class FuncionarioConfig : IEntityTypeConfiguration<Funcionario>
    {
        public void Configure(EntityTypeBuilder<Funcionario> builder)
        {
            builder.ToTable("funcionario");

            builder.HasKey(e => e.IdFuncionario);

            builder.Property(e => e.IdFuncionario)
                .HasColumnName("id_funcionario");

            builder.Property(e => e.Matricula)
                .HasColumnName("matricula")
                .HasMaxLength(20)
                .IsRequired();

            builder.HasIndex(e => e.Matricula)
                .IsUnique();

            builder.Property(e => e.Nome)
                .HasColumnName("nome")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(e => e.Cargo)
                .HasColumnName("cargo")
                .HasMaxLength(50);

            builder.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();
        }
    }

}
