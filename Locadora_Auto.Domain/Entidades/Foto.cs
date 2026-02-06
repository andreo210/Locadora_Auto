namespace Locadora_Auto.Domain.Entidades
{
    public class Foto
    {
        public int? IdFoto { get; private set; }
        public int? IdEntidade { get; private set; }
        public TipoFoto? Tipo { get; private set; }
        public string? NomeArquivo { get; private set; }
        public string? Raiz { get;private set; }
        public string? Diretorio { get; private set; }
        public string? Extensao { get; private set; }
        public DateTime DataUpload { get; private set; }
        public long? QuantidadeBytes { get; private set; }

        public static Foto Criar(int idEntidade, string nome,string raiz, string diretorio, string extensao, long quantidadeBytes, TipoFoto tipo)
        {
            if (idEntidade <= 0)
                throw new InvalidOperationException("idEntidade não pode ser menor que zero");

            if (quantidadeBytes <= 0)
                throw new InvalidOperationException("quantidadeBytes não pode ser menor que zero");

            if (string.IsNullOrWhiteSpace(nome))
                throw new InvalidOperationException("nome não pode ser nulo");

            if (string.IsNullOrWhiteSpace(raiz))
                throw new InvalidOperationException("raiz não pode ser nulo");

            if (string.IsNullOrWhiteSpace(diretorio))
                throw new InvalidOperationException("diretorio não pode ser nulo");

            if (string.IsNullOrWhiteSpace(extensao))
                throw new InvalidOperationException("diretorio não pode ser nulo");


            return new Foto
            {
                IdEntidade = idEntidade,
                NomeArquivo = nome,
                Raiz = raiz,
                Diretorio = diretorio,
                Extensao = extensao,
                DataUpload = DateTime.Now,
                Tipo = tipo,
                QuantidadeBytes = quantidadeBytes
            };
        }
        public enum TipoFoto
        {
            CategoriaVeiculo = 1,
            Filial= 2,
            Vistoria = 3,
        }
    }
}
