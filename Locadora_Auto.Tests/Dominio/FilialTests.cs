using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    /// <summary>
    /// A filial guarda o tempo de preparação (RN-45/RN-46) — quantos minutos o carro devolvido leva
    /// para voltar à oferta. É parâmetro comercial, então o que o domínio garante não é o valor e
    /// sim a faixa: nada de negativo, nada de tão grande que esconda frota da oferta sem que isso
    /// apareça como indisponibilidade.
    /// </summary>
    public class FilialTests
    {
        [Fact]
        public void Filial_nova_nasce_com_o_tempo_de_preparacao_padrao()
        {
            var filial = Fabrica.Filial();

            Assert.Equal(Filial.PreparacaoPadraoMinutos, filial.TempoPreparacaoMinutos);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(30)]
        [InlineData(Filial.PreparacaoMaximaMinutos)]
        public void Tempo_de_preparacao_aceita_a_faixa_valida(int minutos)
        {
            // zero é legítimo: é a filial declarando que não tem preparação
            var filial = Filial.Criar("Filial Centro", "São Paulo", Fabrica.Endereco(), minutos);

            Assert.Equal(minutos, filial.TempoPreparacaoMinutos);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(Filial.PreparacaoMaximaMinutos + 1)]
        public void Tempo_de_preparacao_fora_da_faixa_e_recusado(int minutos)
        {
            Assert.Throws<InvalidOperationException>(
                () => Filial.Criar("Filial Centro", "São Paulo", Fabrica.Endereco(), minutos));
        }

        [Fact]
        public void Atualizar_sem_informar_preparacao_mantem_a_atual()
        {
            // é o caso do cliente que não conhece o campo: não pode zerar o parâmetro sem querer
            var filial = Filial.Criar("Filial Centro", "São Paulo", Fabrica.Endereco(), 45);

            filial.Atualizar("Filial Centro Renomeada", "São Paulo", Fabrica.Endereco());

            Assert.Equal(45, filial.TempoPreparacaoMinutos);
            Assert.Equal("Filial Centro Renomeada", filial.Nome);
        }

        [Fact]
        public void Atualizar_informando_preparacao_troca_o_valor()
        {
            var filial = Filial.Criar("Filial Centro", "São Paulo", Fabrica.Endereco(), 45);

            filial.Atualizar("Filial Centro", "São Paulo", Fabrica.Endereco(), 90);

            Assert.Equal(90, filial.TempoPreparacaoMinutos);
        }

        [Fact]
        public void Atualizar_com_preparacao_invalida_nao_altera_nada()
        {
            var filial = Filial.Criar("Filial Centro", "São Paulo", Fabrica.Endereco(), 45);

            Assert.Throws<InvalidOperationException>(
                () => filial.Atualizar("Nome Novo", "Campinas", Fabrica.Endereco(), -10));

            // a recusa é atômica: nem o tempo de preparação nem os demais campos mudaram
            Assert.Equal(45, filial.TempoPreparacaoMinutos);
            Assert.Equal("Filial Centro", filial.Nome);
            Assert.Equal("São Paulo", filial.Cidade);
        }
    }
}
