namespace Locadora_Auto.Application.Models.Dto
{
    public class CriarVeiculoDto
    {
        public string Placa { get; set; } = null!;
        public string Marca { get; set; } = null!;
        public string Modelo { get; set; } = null!;
        public int Ano { get; set; }
        public string Chassi { get; set; } = null!;
        public int KmInicial { get; set; }

        public int IdCategoria { get; set; }
        public int IdFilialAtual { get; set; }
    }


    public class VeiculoDto
    {
        public int IdVeiculo { get; set; }
        public string Placa { get; set; } = null!;
        public string Marca { get; set; } = null!;
        public string Modelo { get; set; } = null!;
        public int Ano { get; set; }
        public string Chassi { get; set; } = null!;
        public int KmAtual { get; set; }
        public int IdStatus { get; set; }
        public string Status { get; set; } = null!;
        public bool Ativo { get; set; }
        public bool Disponivel { get; set; }

        public int IdCategoria { get; set; }
        public string Categoria { get; set; } = null!;

        public int IdFilialAtual { get; set; }
        public string Filial { get; set; } = null!;

        /// <summary>RN-56: preenchidos só depois que o ativo deixou a frota.</summary>
        public string? MotivoDesmobilizacao { get; set; }
        public DateTime? DataDesmobilizacao { get; set; }
        public int? IdFuncionarioDesmobilizacao { get; set; }
    }
    /// <summary>
    /// RN-56: o ativo deixa a frota. Motivo e responsável são obrigatórios pelo mesmo motivo do
    /// bloqueio — baixa de ativo sem justificativa registrada é apontamento de auditoria na hora.
    /// </summary>
    public class DesmobilizarVeiculoDto
    {
        /// <summary>Idade, quilometragem, custo de manutenção, perda total, queda de demanda.</summary>
        public string Motivo { get; set; } = null!;

        public int IdFuncionarioResponsavel { get; set; }
    }

    public class AtualizarVeiculoDto
    {
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public int? Ano { get; set; }
        public int? KmAtual { get; set; }
        public int? IdFilialAtual { get; set; }
    }

    /// <summary>
    /// O que uma passada da varredura da RN-45 encontrou. Não é resposta de endpoint: é o que a
    /// varredura devolve para quem a disparou registrar em log.
    ///
    /// Os números existem para a liberação automática não virar caixa-preta. Uma varredura que só
    /// dissesse "liberei 4" esconderia justamente o que interessa saber — quantos carros o pátio
    /// deixou vencer.
    /// </summary>
    public class LiberacaoPreparacaoDto
    {
        /// <summary>Quantos veículos estavam em preparação no momento da varredura.</summary>
        public int Analisados { get; set; }

        /// <summary>Quantos voltaram à oferta por vencimento do prazo, sem conferência do pátio.</summary>
        public int Liberados { get; set; }

        /// <summary>Quantos continuam no pátio dentro do prazo da filial.</summary>
        public int AindaNoPrazo { get; set; }

        /// <summary>
        /// Subconjunto de <see cref="Liberados"/>: veículos que entraram em preparação antes da
        /// trilha da RN-37 existir e por isso não têm carimbo de início. Contados à parte porque
        /// são liberados por dedução — estão parados desde antes da implantação — e não por prazo
        /// medido. Tende a zero e não deve voltar a subir.
        /// </summary>
        public int SemCarimbo { get; set; }
    }


}
