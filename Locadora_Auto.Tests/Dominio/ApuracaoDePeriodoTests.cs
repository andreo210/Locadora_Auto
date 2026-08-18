using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    /// <summary>
    /// RN-01 a RN-07: quanto o tempo do contrato custa. Os quatro primeiros cenários do doc 07 §10
    /// estão aqui literais — são os critérios de aceite da especificação, e é neles que a regra
    /// deixa de ser texto.
    ///
    /// A diária padrão destes testes é R$ 150,00 e o percentual de hora excedente é o da casa
    /// (0,3333): 150 × 0,3333 = 49,995, que arredondado a 2 casas dá exatamente os R$ 50,00 de
    /// "1/3 da diária" que o doc promete.
    /// </summary>
    public class ApuracaoDePeriodoTests
    {
        private static readonly DateTime Retirada = new(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

        private const decimal Diaria = 150m;

        private static ApuracaoDePeriodo Apurar(
            DateTime devolucao,
            DateTime? retirada = null,
            decimal diaria = Diaria,
            int toleranciaMinutos = 30)
            => ApuracaoDePeriodo.Calcular(
                retirada ?? Retirada,
                devolucao,
                diaria,
                toleranciaMinutos,
                Filial.PercentualHoraExcedentePadrao);

        // ======================= doc 07 §10, critérios de aceite =======================

        [Fact]
        public void Diaria_e_ciclo_de_24h_e_nao_data_de_calendario()
        {
            // retirada 10/03 às 22:00, devolução 11/03 às 20:00 — dois dias no calendário, 22h de
            // contrato. Contar por calendário cobraria 2 diárias e geraria contestação garantida
            var apuracao = Apurar(
                retirada: new DateTime(2026, 3, 10, 22, 0, 0, DateTimeKind.Utc),
                devolucao: new DateTime(2026, 3, 11, 20, 0, 0, DateTimeKind.Utc));

            Assert.Equal(1, apuracao.DiariasCobradas);
            Assert.Equal(0, apuracao.HorasExcedentes);
            Assert.Equal(150m, apuracao.Total);
        }

        [Fact]
        public void Tolerancia_de_30_minutos_nao_gera_cobranca()
        {
            // devolução às 09:25 de um contrato que vencia às 09:00: 25 min dentro da tolerância
            var apuracao = Apurar(new DateTime(2026, 3, 12, 9, 25, 0, DateTimeKind.Utc));

            Assert.Equal(2, apuracao.DiariasCobradas);
            Assert.Equal(0, apuracao.HorasExcedentes);
            Assert.Equal(300m, apuracao.Total);
        }

        [Fact]
        public void Hora_excedente_e_por_hora_iniciada_depois_da_tolerancia()
        {
            // 2h30 de sobra, menos 30 min de tolerância, dão 2 horas — e não 3. A tolerância é
            // tempo livre: as horas contam a partir dela, não do fim do ciclo
            var apuracao = Apurar(new DateTime(2026, 3, 12, 11, 30, 0, DateTimeKind.Utc));

            Assert.Equal(2, apuracao.DiariasCobradas);
            Assert.Equal(2, apuracao.HorasExcedentes);
            Assert.Equal(50m, apuracao.ValorHoraExcedente);
            Assert.Equal(400m, apuracao.Total);
        }

        [Fact]
        public void Teto_de_uma_diaria_substitui_as_horas_excedentes()
        {
            // 4h de sobra: sem o teto seriam 4 horas a R$ 50,00 = R$ 200,00, mais que prorrogar o
            // contrato por um dia — que é o resultado indefensável que a RN-05 existe para impedir
            var apuracao = Apurar(new DateTime(2026, 3, 12, 13, 0, 0, DateTimeKind.Utc));

            Assert.Equal(1, apuracao.DiariasPorTeto);
            Assert.Equal(0, apuracao.HorasExcedentes);
            Assert.Equal(3, apuracao.DiariasCobradas);
            Assert.Equal(450m, apuracao.Total);
        }

        // ======================= bordas do cálculo =======================

        [Fact]
        public void Contrato_de_minutos_cobra_uma_diaria()
        {
            // RN-02: não existe meia diária em locadora
            var apuracao = Apurar(Retirada.AddMinutes(10));

            Assert.Equal(1, apuracao.DiariasCobradas);
            Assert.Equal(0, apuracao.HorasExcedentes);
            Assert.Equal(150m, apuracao.Total);
        }

        [Fact]
        public void A_diaria_minima_cobre_o_primeiro_ciclo_inteiro()
        {
            // 23h30 de contrato: a diária mínima já paga esse ciclo, e cobrar hora excedente sobre
            // ele seria cobrar o mesmo período duas vezes
            var apuracao = Apurar(Retirada.AddHours(23).AddMinutes(30));

            Assert.Equal(1, apuracao.DiariasCobradas);
            Assert.Equal(0, apuracao.HorasExcedentes);
        }

        [Fact]
        public void Devolucao_antecipada_cobra_o_periodo_usado_e_nenhuma_taxa()
        {
            // RN-07: a data prevista não é sequer entrada do cálculo — o que se cobra é o período
            // que o carro ficou fora, e devolver antes não gera taxa nenhuma
            var apuracao = Apurar(Retirada.AddDays(2));

            Assert.Equal(2, apuracao.DiariasCobradas);
            Assert.Equal(0, apuracao.HorasExcedentes);
            Assert.Equal(300m, apuracao.Total);
        }

        [Fact]
        public void Periodo_de_48h_exatas_da_duas_diarias_e_nenhuma_hora()
        {
            // a divisão é sobre ticks e não sobre TotalHours: em double, 48h pode sair como
            // 47,999999999 e virar uma diária a menos na conta do cliente
            var apuracao = Apurar(Retirada.AddHours(48));

            Assert.Equal(2, apuracao.DiariasCobradas);
            Assert.Equal(0, apuracao.HorasExcedentes);
        }

        [Fact]
        public void Filial_sem_tolerancia_cobra_do_primeiro_minuto()
        {
            // zero é escolha legítima da praça, e aí um minuto de sobra já é uma hora iniciada.
            // 24h01 é um ciclo cheio mais um minuto: 1 diária e 1 hora, não 2 diárias
            var apuracao = Apurar(Retirada.AddHours(24).AddMinutes(1), toleranciaMinutos: 0);

            Assert.Equal(1, apuracao.DiariasCobradas);
            Assert.Equal(1, apuracao.HorasExcedentes);
            Assert.Equal(200m, apuracao.Total);
        }

        [Fact]
        public void Teto_entra_no_empate_exato_com_uma_diaria()
        {
            // 3h30 de sobra menos 30 min dão 3 horas a R$ 50,00 = R$ 150,00, exatamente uma diária.
            // "Atingir o valor de 1 diária" (RN-05) inclui o empate
            var apuracao = Apurar(Retirada.AddHours(48).AddMinutes(210));

            Assert.Equal(3, apuracao.HorasApuradas);
            Assert.Equal(1, apuracao.DiariasPorTeto);
            Assert.Equal(0, apuracao.HorasExcedentes);
            Assert.Equal(450m, apuracao.Total);
        }

        [Fact]
        public void Valor_da_hora_excedente_e_arredondado_a_duas_casas()
        {
            // a coluna é numeric(10,2): um unitário com mais casas seria arredondado pelo banco, e
            // a linha gravada passaria a discordar do total que ela mesma declara
            var apuracao = Apurar(Retirada.AddHours(26));

            Assert.Equal(50m, apuracao.ValorHoraExcedente);
            Assert.Equal(LinhaFechamento.Arredondar(Diaria * Filial.PercentualHoraExcedentePadrao),
                         apuracao.ValorHoraExcedente);
        }

        [Fact]
        public void Devolucao_anterior_a_retirada_e_recusada()
        {
            Assert.Throws<DomainException>(() => Apurar(Retirada.AddHours(-1)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Contrato_sem_diaria_contratada_nao_e_apurado(int diaria)
        {
            Assert.Throws<DomainException>(() => Apurar(Retirada.AddDays(1), diaria: diaria));
        }

        // ======================= as linhas que a apuração escreve =======================

        [Fact]
        public void Apuracao_escreve_a_linha_de_diaria_no_fechamento()
        {
            var (locacao, filial) = ContratoDevolvido(Retirada.AddDays(3));

            var apuracao = locacao.ApurarPeriodo(filial);

            var linha = Assert.Single(locacao.Fechamento!.Linhas);
            Assert.Equal(TipoLinhaFechamento.Diaria, linha.Tipo);
            Assert.Equal(3m, linha.Quantidade);
            Assert.Equal(150m, linha.ValorUnitario);
            Assert.Equal(450m, linha.Total);
            Assert.NotEmpty(linha.BaseCalculo);
            Assert.Equal(apuracao.Total, locacao.Fechamento.Saldo);
        }

        [Fact]
        public void Apuracao_com_atraso_escreve_diaria_e_hora_excedente()
        {
            var (locacao, filial) = ContratoDevolvido(new DateTime(2026, 3, 12, 11, 30, 0, DateTimeKind.Utc));

            locacao.ApurarPeriodo(filial);

            var linhas = locacao.Fechamento!.Linhas;
            Assert.Equal(2, linhas.Count);
            Assert.Equal(300m, linhas.Single(l => l.Tipo == TipoLinhaFechamento.Diaria).Total);
            Assert.Equal(100m, linhas.Single(l => l.Tipo == TipoLinhaFechamento.HoraExcedente).Total);
            Assert.Equal(400m, locacao.Fechamento.Saldo);
        }

        [Fact]
        public void Apuracao_com_teto_escreve_a_diaria_substituta_e_nenhuma_hora()
        {
            var (locacao, filial) = ContratoDevolvido(new DateTime(2026, 3, 12, 13, 0, 0, DateTimeKind.Utc));

            locacao.ApurarPeriodo(filial);

            var linhas = locacao.Fechamento!.Linhas;
            Assert.Equal(2, linhas.Count);
            Assert.DoesNotContain(linhas, l => l.Tipo == TipoLinhaFechamento.HoraExcedente);

            var teto = linhas.Single(l => l.Tipo == TipoLinhaFechamento.DiariaPorTetoDeHoras);
            Assert.Equal(1m, teto.Quantidade);
            Assert.Equal(150m, teto.Total);

            // e o extrato conta a história: as horas que viraram diária aparecem na base de cálculo
            Assert.Contains("teto da RN-05", teto.BaseCalculo);
            Assert.Equal(450m, locacao.Fechamento.Saldo);
        }

        [Fact]
        public void Apurar_o_periodo_duas_vezes_e_recusado()
        {
            // relançar o período dobraria a conta do cliente
            var (locacao, filial) = ContratoDevolvido(Retirada.AddDays(3));
            locacao.ApurarPeriodo(filial);

            Assert.Throws<DomainException>(() => locacao.ApurarPeriodo(filial));
            Assert.Single(locacao.Fechamento!.Linhas);
        }

        [Fact]
        public void Apurar_com_a_filial_errada_e_recusado()
        {
            // a política é da praça que vendeu; aplicar a de outra filial mudaria a conta em
            // silêncio, e ninguém releria o contrato para descobrir
            var (locacao, _) = ContratoDevolvido(Retirada.AddDays(3));

            var outra = Fabrica.Filial("Filial Aeroporto");
            Fabrica.DefinirId(outra, 99);

            Assert.Throws<InvalidOperationException>(() => locacao.ApurarPeriodo(outra));
        }

        [Fact]
        public void Apurar_sem_a_conta_aberta_e_recusado()
        {
            var locacao = Fabrica.Devolver(
                Fabrica.Retirar(Fabrica.Locacao(
                    dataInicio: Retirada,
                    dataFimPrevista: Retirada.AddDays(2),
                    valorDiariaContratada: Diaria)),
                dataFimReal: Retirada.AddDays(3));

            Assert.Throws<InvalidOperationException>(() => locacao.ApurarPeriodo(FilialDeRetirada()));
        }

        /// <summary>Contrato devolvido em <paramref name="devolucao"/>, com a conta já aberta.</summary>
        private static (Locacao locacao, Filial filial) ContratoDevolvido(DateTime devolucao)
        {
            var locacao = Fabrica.Devolver(
                Fabrica.Retirar(Fabrica.Locacao(
                    dataInicio: Retirada,
                    dataFimPrevista: Retirada.AddDays(2),
                    valorDiariaContratada: Diaria)),
                dataFimReal: devolucao);

            locacao.AbrirFechamento(idFuncionarioApuracao: 1);

            return (locacao, FilialDeRetirada());
        }

        /// <summary>
        /// A filial 1, que é a de retirada padrão da fábrica, com os parâmetros da casa: 30 min de
        /// tolerância e 1/3 da diária na hora excedente.
        /// </summary>
        private static Filial FilialDeRetirada()
        {
            var filial = Fabrica.Filial();
            Fabrica.DefinirId(filial, 1);

            return filial;
        }
    }
}
