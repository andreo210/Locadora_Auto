using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class FilialConfig : IEntityTypeConfiguration<Filial>
    {
        public void Configure(EntityTypeBuilder<Filial> builder)
        {
            builder.ToTable("tb_filial");

            builder.HasKey(e => e.IdFilial);

            builder.Property(e => e.IdFilial)
                .HasColumnName("id_filial");

            builder.Property(e => e.Nome)
                .HasColumnName("nome")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.Cidade)
                .HasColumnName("cidade")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.Ativo)
                .HasColumnName("ativo");

            // RN-45/RN-46: minutos entre a devolução e a volta do carro à oferta. O default vale
            // para as filiais que já existiam quando a coluna entrou.
            builder.Property(e => e.TempoPreparacaoMinutos)
                .HasColumnName("tempo_preparacao_minutos")
                .HasDefaultValue(Filial.PreparacaoPadraoMinutos)
                .IsRequired();

            // RN-49: a filial participa do remanejamento programado de frota. O default true vale
            // para as filiais que já existiam quando a coluna entrou — obrigá-las a se habilitar
            // deixaria a rede inteira sem transferência até alguém perceber.
            builder.Property(e => e.PermiteTransferencia)
                .HasColumnName("permite_transferencia")
                .HasDefaultValue(true)
                .IsRequired();

            // ---- Parâmetros do fechamento (doc 07 §9, backlog A2/A3) ----
            // Todos com default, e o default é o que vale para as filiais que já existiam quando as
            // colunas entraram. Onde o padrão da casa é conhecido (tolerância, hora excedente) ele
            // é o default; onde o número é local (combustível, limpeza, one-way) o default é zero,
            // que significa "não configurado" e não "de graça" — quem trata isso é a apuração.

            // RN-21: true para não bloquear, quando o A8 entrar, um one-way que a rede aceita hoje
            builder.Property(e => e.HabilitadaOneWay)
                .HasColumnName("habilitada_one_way")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(e => e.TaxaRetornoOneWay)
                .HasColumnName("taxa_retorno_one_way")
                .HasPrecision(10, 2)
                .HasDefaultValue(0m)
                .IsRequired();

            // RN-03
            builder.Property(e => e.ToleranciaMinutos)
                .HasColumnName("tolerancia_minutos")
                .HasDefaultValue(Filial.ToleranciaPadraoMinutos)
                .IsRequired();

            // RN-04: quatro casas porque o padrão é a dízima de 1/3 truncada
            builder.Property(e => e.PercentualHoraExcedente)
                .HasColumnName("percentual_hora_excedente")
                .HasPrecision(5, 4)
                .HasDefaultValue(Filial.PercentualHoraExcedentePadrao)
                .IsRequired();

            // RN-15
            builder.Property(e => e.PrecoLitroCombustivel)
                .HasColumnName("preco_litro_combustivel")
                .HasPrecision(10, 2)
                .HasDefaultValue(0m)
                .IsRequired();

            builder.Property(e => e.TaxaServicoAbastecimento)
                .HasColumnName("taxa_servico_abastecimento")
                .HasPrecision(10, 2)
                .HasDefaultValue(0m)
                .IsRequired();

            // RN-23
            builder.Property(e => e.ValorLimpezaEspecial)
                .HasColumnName("valor_limpeza_especial")
                .HasPrecision(10, 2)
                .HasDefaultValue(0m)
                .IsRequired();

            //chave estrangeira
            builder.Property(e => e.IdEndereco)
                .HasColumnName("id_endereco")
                .IsRequired();

            builder.HasOne(u => u.Endereco)//uma filial tem um endereço
                .WithOne(f => f.Filial)//um endereço tem uma filial
                .HasForeignKey<Filial>(f => f.IdEndereco)
                .OnDelete(DeleteBehavior.Cascade);//ao deletar a filial, deletar o endereço

        }
    }

}
