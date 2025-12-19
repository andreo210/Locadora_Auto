using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Domain.Entidades
{
    public class Cliente
    {
        public int IdCliente { get; set; }
        public string Cpf { get; set; } = null!;
        public string Nome { get; set; } = null!;
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public string Status { get; set; } = "ATIVO";

        public int IdEndereco { get; set; }
        public Endereco Endereco { get; set; } = null!;


        public ICollection<Locacao> Locacoes { get; set; } = [];
        public ICollection<Reserva> Reservas { get; set; } = [];
    }

}
