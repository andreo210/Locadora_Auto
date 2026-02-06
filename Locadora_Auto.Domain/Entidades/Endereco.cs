namespace Locadora_Auto.Domain.Entidades
{
    public class Endereco
    {
        public int IdEndereco { get; set; }

        public int? IdCliente { get; set; }
        public string Logradouro { get; set; } = null!;
        public string Numero { get; set; } = null!;
        public string? Complemento { get; set; }
        public string Bairro { get; set; } = null!;
        public string Cidade { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public string Cep { get; set; } = null!;

        //navegacao(opcional)
        public Clientes Cliente { get; set; } = null!;
        public Filial Filial { get; set; } = null!;


        public Endereco() { }

        public static Endereco Criar(
            string logradouro,
            string numero,
            string bairro,
            string cidade,
            string estado,
            string cep,
            string? complemento = null)
        {
            VerificarEndereco(logradouro, numero, bairro, cidade, estado, cep, complemento);

            return new Endereco
            {
                Logradouro = logradouro,
                Numero = numero,
                Bairro = bairro,
                Cidade = cidade,
                Estado = estado,
                Cep = cep.Replace("-","").Replace(".",""),
                Complemento = complemento
            };
        }

        public void Atualizar(
            string logradouro,
            string numero,
            string bairro,
            string cidade,
            string estado,
            string cep,
            string? complemento = null)
        {
            VerificarEndereco(logradouro, numero, bairro, cidade, estado, cep, complemento);


            Logradouro = logradouro;
            Numero = numero;
            Bairro = bairro;
            Cidade = cidade;
            Estado = estado;
            Cep = cep;
            Complemento = complemento;
            
        }

        public static void VerificarEndereco(
           string logradouro,
           string numero,
           string bairro,
           string cidade,
           string estado,
           string cep,
           string? complemento = null)
        {
            if (string.IsNullOrWhiteSpace(numero))
                throw new InvalidOperationException("Número é obrigatório");
            if (string.IsNullOrWhiteSpace(cep))
                throw new InvalidOperationException("CEP é obrigatório");
            if (string.IsNullOrWhiteSpace(logradouro))
                throw new InvalidOperationException("Logradouro é obrigatório");
            if (string.IsNullOrWhiteSpace(bairro))
                throw new InvalidOperationException("Bairro é obrigatório");
            if (string.IsNullOrWhiteSpace(cidade))
                throw new InvalidOperationException("Cidade é obrigatória");
            if (string.IsNullOrWhiteSpace(estado))
                throw new InvalidOperationException("Estado é obrigatório");           
        }
    }
}
