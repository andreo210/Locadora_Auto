namespace Locadora_Auto.Front.Models
{
    public class EstadoOrdenacao
    {
        public string Propriedade { get; set; } = "Matricula";
        public bool Ascendente { get; set; } = true;

        public string Direcao => Ascendente ? "asc" : "desc";
        public string Icone => Ascendente ? "bi-arrow-up" : "bi-arrow-down";

        public void Alternar(string propriedade)
        {
            if (Propriedade == propriedade)
            {
                Ascendente = !Ascendente; // Alterna direção
            }
            else
            {
                Propriedade = propriedade;
                Ascendente = true; // Nova coluna, começa ascendente
            }
        }
    }
}
