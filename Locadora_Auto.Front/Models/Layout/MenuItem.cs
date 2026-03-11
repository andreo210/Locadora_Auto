namespace Locadora_Auto.Front.Models.Layout
{
    public class MenuItem
    {
        public string Titulo { get; set; }
        public string Icone { get; set; }
        public string Url { get; set; }
        public List<MenuItem> SubItens { get; set; } = new();
        public bool Visible { get; set; } = true;
    }
}
