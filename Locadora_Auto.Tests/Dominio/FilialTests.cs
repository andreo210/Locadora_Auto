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

        // ======================= prazo (RN-45, parte automática) =======================

        [Fact]
        public void Prazo_de_preparacao_soma_os_minutos_da_filial_ao_inicio()
        {
            var filial = Fabrica.Filial(tempoPreparacaoMinutos: 120);
            var devolucao = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

            Assert.Equal(devolucao.AddHours(2), filial.PrazoDePreparacao(devolucao));
        }

        [Theory]
        [InlineData(119, false)] // um minuto antes do prazo o carro ainda é do pátio
        [InlineData(120, true)]  // no instante do vencimento já pode sair sozinho
        [InlineData(121, true)]
        public void Preparacao_vence_no_minuto_do_prazo_e_nao_antes(int minutosDecorridos, bool vencida)
        {
            var filial = Fabrica.Filial(tempoPreparacaoMinutos: 120);
            var devolucao = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

            Assert.Equal(vencida, filial.PreparacaoVencida(devolucao, devolucao.AddMinutes(minutosDecorridos)));
        }

        [Fact]
        public void Filial_sem_preparacao_vence_no_instante_da_devolucao()
        {
            // zero é a filial declarando que não prepara carro; o prazo não tem o que esperar
            var filial = Fabrica.Filial(tempoPreparacaoMinutos: 0);
            var devolucao = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

            Assert.True(filial.PreparacaoVencida(devolucao, devolucao));
        }

        // ======================= parâmetros do fechamento (doc 07 §9) =======================

        [Fact]
        public void Filial_nova_nasce_com_os_parametros_de_fechamento_padrao()
        {
            var filial = Fabrica.Filial();

            // onde existe padrão da casa, ele vale desde o cadastro
            Assert.Equal(Filial.ToleranciaPadraoMinutos, filial.ToleranciaMinutos);
            Assert.Equal(Filial.PercentualHoraExcedentePadrao, filial.PercentualHoraExcedente);

            // RN-21: participar do one-way é o caso normal
            Assert.True(filial.HabilitadaOneWay);

            // onde o número é local, zero significa "ninguém configurou ainda"
            Assert.Equal(0m, filial.TaxaRetornoOneWay);
            Assert.Equal(0m, filial.PrecoLitroCombustivel);
            Assert.Equal(0m, filial.TaxaServicoAbastecimento);
            Assert.Equal(0m, filial.ValorLimpezaEspecial);
        }

        [Fact]
        public void Parametro_ausente_mantem_o_valor_atual()
        {
            // é a garantia que protege a praça inteira do cliente que não conhece os campos: uma
            // edição de nome de filial não pode zerar o preço do litro
            var filial = Fabrica.Filial();
            filial.DefinirParametrosFinanceiros(
                habilitadaOneWay: false,
                taxaRetornoOneWay: 250m,
                toleranciaMinutos: 45,
                percentualHoraExcedente: 0.25m,
                precoLitroCombustivel: 6.19m,
                taxaServicoAbastecimento: 35m,
                valorLimpezaEspecial: 120m);

            filial.DefinirParametrosFinanceiros();

            Assert.False(filial.HabilitadaOneWay);
            Assert.Equal(250m, filial.TaxaRetornoOneWay);
            Assert.Equal(45, filial.ToleranciaMinutos);
            Assert.Equal(0.25m, filial.PercentualHoraExcedente);
            Assert.Equal(6.19m, filial.PrecoLitroCombustivel);
            Assert.Equal(35m, filial.TaxaServicoAbastecimento);
            Assert.Equal(120m, filial.ValorLimpezaEspecial);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(30)]
        [InlineData(Filial.ToleranciaMaximaMinutos)]
        public void Tolerancia_aceita_a_faixa_valida(int minutos)
        {
            // zero é legítimo: é a filial que cobra do minuto um
            var filial = Fabrica.Filial();

            filial.DefinirParametrosFinanceiros(toleranciaMinutos: minutos);

            Assert.Equal(minutos, filial.ToleranciaMinutos);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(Filial.ToleranciaMaximaMinutos + 1)]
        public void Tolerancia_fora_da_faixa_e_recusada(int minutos)
        {
            // acima de 24h a tolerância engoliria a diária seguinte inteira (RN-01)
            var filial = Fabrica.Filial();

            Assert.Throws<InvalidOperationException>(
                () => filial.DefinirParametrosFinanceiros(toleranciaMinutos: minutos));
        }

        // o parâmetro é `double` porque atributo não aceita constante decimal; a conversão é feita
        // no corpo, e os valores são exatos em binário nas casas usadas aqui
        [Theory]
        [InlineData(0)]      // hora excedente de graça não é parâmetro, é defeito
        [InlineData(-0.5)]
        [InlineData(1.5)]    // hora custando mais que a diária é o que a RN-05 existe para impedir
        public void Percentual_de_hora_excedente_fora_da_faixa_e_recusado(double percentual)
        {
            var filial = Fabrica.Filial();

            Assert.Throws<InvalidOperationException>(
                () => filial.DefinirParametrosFinanceiros(percentualHoraExcedente: (decimal)percentual));
        }

        [Theory]
        [InlineData(0.2)]
        [InlineData(0.25)]
        [InlineData(1)]      // hora excedente valendo uma diária inteira é limite, não erro
        public void Percentual_de_hora_excedente_aceita_a_faixa_valida(double percentual)
        {
            var filial = Fabrica.Filial();

            filial.DefinirParametrosFinanceiros(percentualHoraExcedente: (decimal)percentual);

            Assert.Equal((decimal)percentual, filial.PercentualHoraExcedente);
        }

        [Fact]
        public void Parametro_de_dinheiro_negativo_e_recusado()
        {
            var filial = Fabrica.Filial();

            Assert.Throws<InvalidOperationException>(
                () => filial.DefinirParametrosFinanceiros(taxaRetornoOneWay: -1m));
            Assert.Throws<InvalidOperationException>(
                () => filial.DefinirParametrosFinanceiros(precoLitroCombustivel: -1m));
            Assert.Throws<InvalidOperationException>(
                () => filial.DefinirParametrosFinanceiros(taxaServicoAbastecimento: -1m));
            Assert.Throws<InvalidOperationException>(
                () => filial.DefinirParametrosFinanceiros(valorLimpezaEspecial: -1m));
        }

        [Fact]
        public void Parametro_recusado_nao_deixa_os_outros_gravados()
        {
            // valida tudo antes de atribuir qualquer coisa: meia configuração gravada seria pior
            // que nenhuma, porque ninguém saberia qual metade valeu
            var filial = Fabrica.Filial();

            Assert.Throws<InvalidOperationException>(
                () => filial.DefinirParametrosFinanceiros(
                    precoLitroCombustivel: 6.19m,
                    toleranciaMinutos: -1));

            Assert.Equal(0m, filial.PrecoLitroCombustivel);
            Assert.Equal(Filial.ToleranciaPadraoMinutos, filial.ToleranciaMinutos);
        }
    }
}
