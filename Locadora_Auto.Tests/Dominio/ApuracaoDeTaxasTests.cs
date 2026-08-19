using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    /// <summary>
    /// RN-21 a RN-23: as duas taxas do fechamento. Nenhuma delas é cálculo — o valor sai pronto da
    /// filial de devolução —, e por isso não têm tipo de apuração próprio como o período ou o
    /// combustível. O que existe aqui é <b>decisão</b>: quando cobrar, e o que fazer quando a regra
    /// diz não.
    /// </summary>
    public class ApuracaoDeTaxasTests
    {
        private static readonly DateTime Retirada = new(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

        private const int FilialDeRetirada = 1;
        private const decimal TaxaOneWay = 250m;
        private const decimal ValorLimpeza = 120m;

        // ======================= one-way (RN-21, RN-22) =======================

        [Fact]
        public void Devolucao_na_propria_filial_nao_cobra_nem_escreve_linha()
        {
            // é o caso da maioria dos contratos: "taxa one-way: R$ 0,00" em todo extrato seria ruído
            var (locacao, filial) = Cenario(filialDevolucao: FilialDeRetirada);

            var total = locacao.ApurarTaxaOneWay(filial);

            Assert.Equal(0m, total);
            Assert.DoesNotContain(locacao.Fechamento!.Linhas, l => l.Tipo == TipoLinhaFechamento.TaxaRetornoOneWay);
        }

        [Fact]
        public void Devolucao_em_outra_filial_habilitada_cobra_a_taxa_de_retorno()
        {
            var (locacao, filial) = Cenario(filialDevolucao: 3);

            var total = locacao.ApurarTaxaOneWay(filial);

            var linha = locacao.Fechamento!.Linhas.Single(l => l.Tipo == TipoLinhaFechamento.TaxaRetornoOneWay);
            Assert.Equal(250m, linha.Total);
            Assert.Equal(250m, total);
            Assert.Contains("devolução na filial 3", linha.BaseCalculo);
        }

        [Fact]
        public void One_way_de_cortesia_ainda_escreve_a_linha()
        {
            // taxa zerada é decisão comercial, não ausência de evento: o carro foi devolvido longe
            // de onde saiu, e o extrato registra isso mesmo custando nada
            var (locacao, filial) = Cenario(filialDevolucao: 3, taxaOneWay: 0m);

            var total = locacao.ApurarTaxaOneWay(filial);

            var linha = locacao.Fechamento!.Linhas.Single(l => l.Tipo == TipoLinhaFechamento.TaxaRetornoOneWay);
            Assert.Equal(0m, linha.Total);
            Assert.Equal(0m, total);
        }

        [Fact]
        public void Filial_nao_habilitada_bloqueia_o_fechamento()
        {
            var (locacao, filial) = Cenario(filialDevolucao: 3, habilitadaOneWay: false);

            Assert.Throws<DomainException>(() => locacao.ApurarTaxaOneWay(filial));
        }

        [Fact]
        public void Alcada_libera_a_filial_nao_habilitada_e_fica_assinada()
        {
            // o carro já está no pátio dela: recusar para sempre não é opção, liberar sem quem
            // responda também não
            var (locacao, filial) = Cenario(filialDevolucao: 3, habilitadaOneWay: false);

            var total = locacao.ApurarTaxaOneWay(
                filial,
                idFuncionarioAlcada: 9,
                motivoAlcada: "cliente deixou o carro sem aviso; gerente autorizou o recebimento");

            var linha = locacao.Fechamento!.Linhas.Single(l => l.Tipo == TipoLinhaFechamento.TaxaRetornoOneWay);
            Assert.Equal(250m, total);
            Assert.Equal(9, linha.IdFuncionarioLancamento);
            Assert.StartsWith("cliente deixou o carro sem aviso", linha.Motivo);
            Assert.Contains("liberada por alçada", linha.BaseCalculo);

            // não é correção: a conta nem foi selada ainda
            Assert.False(linha.EhCorrecao);
        }

        [Fact]
        public void Alcada_pela_metade_continua_bloqueando()
        {
            var (locacao, filial) = Cenario(filialDevolucao: 3, habilitadaOneWay: false);

            Assert.Throws<DomainException>(() => locacao.ApurarTaxaOneWay(filial, idFuncionarioAlcada: 9));
            Assert.Throws<DomainException>(() => locacao.ApurarTaxaOneWay(filial, motivoAlcada: "autorizado"));
        }

        [Fact]
        public void Apurar_o_one_way_duas_vezes_e_recusado()
        {
            var (locacao, filial) = Cenario(filialDevolucao: 3);
            locacao.ApurarTaxaOneWay(filial);

            Assert.Throws<DomainException>(() => locacao.ApurarTaxaOneWay(filial));
        }

        // ======================= limpeza especial (RN-23) =======================

        [Fact]
        public void Limpeza_declarada_com_foto_cobra_o_valor_fixo()
        {
            var (locacao, filial) = Cenario(limpezaEspecial: true, comFoto: true);

            var total = locacao.ApurarLimpezaEspecial(filial);

            var linha = locacao.Fechamento!.Linhas.Single(l => l.Tipo == TipoLinhaFechamento.LimpezaEspecial);
            Assert.Equal(120m, linha.Total);
            Assert.Equal(120m, total);
            Assert.Contains("1 foto(s) de suporte", linha.BaseCalculo);
        }

        [Fact]
        public void Limpeza_declarada_sem_foto_nao_cobra()
        {
            // a foto é a defesa da cobrança: sem ela é a palavra do vistoriador contra a do cliente
            var (locacao, filial) = Cenario(limpezaEspecial: true, comFoto: false);

            var total = locacao.ApurarLimpezaEspecial(filial);

            Assert.Equal(0m, total);
            Assert.DoesNotContain(locacao.Fechamento!.Linhas, l => l.Tipo == TipoLinhaFechamento.LimpezaEspecial);
        }

        [Fact]
        public void Foto_sem_declaracao_nao_cobra()
        {
            // toda vistoria tem foto; foto não é declaração de sujeira especial
            var (locacao, filial) = Cenario(limpezaEspecial: false, comFoto: true);

            Assert.Equal(0m, locacao.ApurarLimpezaEspecial(filial));
        }

        [Fact]
        public void Sujeira_comum_nao_gera_cobranca()
        {
            var (locacao, filial) = Cenario();

            Assert.Equal(0m, locacao.ApurarLimpezaEspecial(filial));
        }

        [Fact]
        public void Filial_sem_valor_de_limpeza_configurado_nao_cobra()
        {
            // zero é "ninguém parametrizou", como o preço do litro do A6 — e não adianta lançar
            // linha para uma cobrança que não existe
            var (locacao, filial) = Cenario(limpezaEspecial: true, comFoto: true, valorLimpeza: 0m);

            Assert.Equal(0m, locacao.ApurarLimpezaEspecial(filial));
        }

        [Fact]
        public void Limpeza_especial_so_se_declara_na_vistoria_de_devolucao()
        {
            // na retirada o carro sai limpo: um sinalizador ali não teria o que cobrar de ninguém
            var (locacao, _) = Cenario();
            var retirada = locacao.Vistorias.First(v => v.Tipo == TipoVistoria.Retirada);

            Assert.Throws<DomainException>(() => retirada.MarcarLimpezaEspecial(true));
        }

        [Fact]
        public void Apurar_a_limpeza_duas_vezes_e_recusado()
        {
            var (locacao, filial) = Cenario(limpezaEspecial: true, comFoto: true);
            locacao.ApurarLimpezaEspecial(filial);

            Assert.Throws<DomainException>(() => locacao.ApurarLimpezaEspecial(filial));
        }

        [Fact]
        public void Apurar_taxa_com_a_filial_errada_e_recusado()
        {
            var (locacao, _) = Cenario(filialDevolucao: 3);

            var outra = Fabrica.Filial("Filial Centro");
            Fabrica.DefinirId(outra, FilialDeRetirada);

            Assert.Throws<InvalidOperationException>(() => locacao.ApurarTaxaOneWay(outra));
            Assert.Throws<InvalidOperationException>(() => locacao.ApurarLimpezaEspecial(outra));
        }

        /// <summary>
        /// Contrato devolvido com a conta aberta, e a filial de devolução com taxa de one-way de
        /// R$ 250,00 e limpeza especial de R$ 120,00.
        /// </summary>
        private static (Locacao locacao, Filial filialDevolucao) Cenario(
            int filialDevolucao = FilialDeRetirada,
            bool habilitadaOneWay = true,
            decimal taxaOneWay = TaxaOneWay,
            decimal valorLimpeza = ValorLimpeza,
            bool limpezaEspecial = false,
            bool comFoto = false)
        {
            var locacao = Fabrica.Locacao(
                dataInicio: Retirada,
                dataFimPrevista: Retirada.AddDays(3),
                idFilialRetirada: FilialDeRetirada);

            locacao.RegistrarVistoria(1, TipoVistoria.Retirada, NivelCombustivel.Cheio, 15_000, null);
            locacao.RegistrarVistoria(
                1, TipoVistoria.Devolucao, NivelCombustivel.Cheio, 15_400, null, limpezaEspecial);

            if (comFoto)
            {
                var vistoria = locacao.Vistorias.First(v => v.Tipo == TipoVistoria.Devolucao);
                Fabrica.DefinirId(vistoria, 5);
                locacao.RegistrarFoto(
                    new List<FotoVistoria>
                    {
                        FotoVistoria.Criar("areia.jpg", "wwwroot", "vistorias", ".jpg", 2048)
                    },
                    idVistoria: 5);
            }

            locacao.RegistrarDevolucao(Retirada.AddDays(3), 15_400, filialDevolucao);
            locacao.AbrirFechamento(1);

            var filial = Fabrica.Filial();
            Fabrica.DefinirId(filial, filialDevolucao);
            filial.DefinirParametrosFinanceiros(
                habilitadaOneWay: habilitadaOneWay,
                taxaRetornoOneWay: taxaOneWay,
                valorLimpezaEspecial: valorLimpeza);

            return (locacao, filial);
        }
    }
}
