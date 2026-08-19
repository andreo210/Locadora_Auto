using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    /// <summary>
    /// RN-17: acessórios pelas diárias <b>efetivas</b>.
    ///
    /// <c>LocacaoAdicional.Dias</c> é congelado na inclusão com base na previsão, e por isso erra em
    /// toda devolução fora do previsto: quem devolve no segundo dia de um contrato de cinco pagaria
    /// cinco diárias de cadeirinha. O registro do adicional continua guardando o que foi
    /// <b>vendido</b> — quem responde o que foi <b>usado</b> é o fechamento.
    /// </summary>
    public class ApuracaoDeAcessoriosTests
    {
        private static readonly DateTime Retirada = new(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Acessorio_e_cobrado_por_unidade_e_por_diaria_efetiva()
        {
            var (locacao, periodo) = Contrato(diasPrevistos: 3, diasReais: 3);

            var total = locacao.ApurarAcessorios(periodo);

            var linha = locacao.Fechamento!.Linhas.Single(l => l.Tipo == TipoLinhaFechamento.Acessorio);
            Assert.Equal(6m, linha.Quantidade);      // 2 unidades × 3 diárias
            Assert.Equal(20m, linha.ValorUnitario);
            Assert.Equal(120m, linha.Total);
            Assert.Equal(120m, total);
        }

        [Fact]
        public void Devolucao_antecipada_cobra_menos_acessorio()
        {
            // é o defeito que a RN-17 existe para corrigir: o contrato previa 5 dias e o carro
            // voltou em 2. O `Dias` congelado cobraria 5 diárias de cadeirinha
            var (locacao, periodo) = Contrato(diasPrevistos: 5, diasReais: 2);

            locacao.ApurarAcessorios(periodo);

            var linha = locacao.Fechamento!.Linhas.Single(l => l.Tipo == TipoLinhaFechamento.Acessorio);
            Assert.Equal(4m, linha.Quantidade);      // 2 unidades × 2 diárias
            Assert.Equal(80m, linha.Total);

            // e o registro da venda continua dizendo o que foi contratado
            Assert.Equal(5, locacao.Adicionais.Single().Dias);
        }

        [Fact]
        public void Devolucao_atrasada_cobra_mais_acessorio()
        {
            // o outro lado do mesmo defeito: o carro ficou dois dias além do previsto e a
            // cadeirinha foi junto
            var (locacao, periodo) = Contrato(diasPrevistos: 3, diasReais: 5);

            locacao.ApurarAcessorios(periodo);

            var linha = locacao.Fechamento!.Linhas.Single(l => l.Tipo == TipoLinhaFechamento.Acessorio);
            Assert.Equal(10m, linha.Quantidade);     // 2 unidades × 5 diárias
            Assert.Equal(200m, linha.Total);
        }

        [Fact]
        public void A_base_de_calculo_mostra_o_contratado_ao_lado_do_efetivo()
        {
            // é a defesa da linha no balcão: o cliente vendeu 5 e usou 2, e a conta diz os dois
            var (locacao, periodo) = Contrato(diasPrevistos: 5, diasReais: 2);

            locacao.ApurarAcessorios(periodo);

            var baseCalculo = locacao.Fechamento!.Linhas
                .Single(l => l.Tipo == TipoLinhaFechamento.Acessorio).BaseCalculo;

            Assert.Contains("2 diária(s) efetiva(s)", baseCalculo);
            Assert.Contains("contratado para 5", baseCalculo);
        }

        [Fact]
        public void Cada_acessorio_sai_em_uma_linha()
        {
            // uma linha por item, e não uma soma: o extrato precisa dizer o que é cadeirinha e o
            // que é GPS, senão o cliente contesta o bloco inteiro
            var (locacao, periodo) = Contrato(segundoAcessorio: true);

            var total = locacao.ApurarAcessorios(periodo);

            var linhas = locacao.Fechamento!.Linhas
                .Where(l => l.Tipo == TipoLinhaFechamento.Acessorio)
                .ToList();

            Assert.Equal(2, linhas.Count);
            Assert.Equal(120m, linhas[0].Total);   // 2 un × 3 diárias × R$ 20,00
            Assert.Equal(90m, linhas[1].Total);    // 1 un × 3 diárias × R$ 30,00
            Assert.Equal(210m, total);
        }

        [Fact]
        public void Contrato_sem_acessorio_nao_escreve_linha_nenhuma()
        {
            var (locacao, periodo) = Contrato(comAcessorios: false);

            var total = locacao.ApurarAcessorios(periodo);

            Assert.Equal(0m, total);
            Assert.DoesNotContain(locacao.Fechamento!.Linhas, l => l.Tipo == TipoLinhaFechamento.Acessorio);
        }

        [Fact]
        public void Apurar_os_acessorios_duas_vezes_e_recusado()
        {
            var (locacao, periodo) = Contrato();
            locacao.ApurarAcessorios(periodo);

            Assert.Throws<DomainException>(() => locacao.ApurarAcessorios(periodo));
        }

        /// <summary>
        /// Contrato devolvido com a conta aberta e o período apurado. Por padrão leva um acessório:
        /// 2 unidades a R$ 20,00 a diária. <paramref name="segundoAcessorio"/> acrescenta 1 unidade
        /// a R$ 30,00.
        /// </summary>
        private static (Locacao locacao, ApuracaoDePeriodo periodo) Contrato(
            int diasPrevistos = 3,
            int diasReais = 3,
            bool comAcessorios = true,
            bool segundoAcessorio = false)
        {
            var locacao = Fabrica.Locacao(
                dataInicio: Retirada,
                dataFimPrevista: Retirada.AddDays(diasPrevistos));

            if (comAcessorios)
                locacao.AdicionarAdicional(idAdicional: 1, valorDiaria: 20m, quantidade: 2);

            if (segundoAcessorio)
                locacao.AdicionarAdicional(idAdicional: 2, valorDiaria: 30m, quantidade: 1);

            Fabrica.Devolver(Fabrica.Retirar(locacao), dataFimReal: Retirada.AddDays(diasReais));
            locacao.AbrirFechamento(1);

            var filial = Fabrica.Filial();
            Fabrica.DefinirId(filial, 1);

            return (locacao, locacao.ApurarPeriodo(filial));
        }
    }
}
