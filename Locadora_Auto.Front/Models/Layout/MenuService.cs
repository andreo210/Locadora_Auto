using System;

namespace Locadora_Auto.Front.Models.Layout
{
    public class MenuService
    {
        public string? GetMenuTitleByUrl(string url)
        {
            foreach (var item in GetMenuItems())
            {
                // Verifica se a URL do item coincide
                if (string.Equals(item.Url.Trim('/'), url.Trim('/'), StringComparison.OrdinalIgnoreCase))
                    return item.Titulo;

                // Verifica subitens
                if (item.SubItens != null)
                {
                    var subItem = item.SubItens.FirstOrDefault(s =>
                        string.Equals(s.Url.Trim('/'), url.Trim('/'), StringComparison.OrdinalIgnoreCase));

                    if (subItem != null)
                        return subItem.Titulo;
                }
            }

            return null;
        }
        public List<MenuItem> GetMenuItems()
        {


            return new List<MenuItem>
            {
                new MenuItem
                {
                    Titulo = "Dashboard",
                    Icone = "bi-speedometer2",
                    Url = "/teste"
                },

                new MenuItem
                {
                    Titulo = "Clientes",
                    Icone = "bi-people",
                    Url = "/clientes",
                    SubItens = new List<MenuItem>
                    {
                        new MenuItem { Titulo = "Listar Clientes", Icone = "bi-list", Url = "/clientes" },
                        new MenuItem { Titulo = "Novo Cliente", Icone = "bi-person-plus", Url = "/clientes/novo" },
                    }
                },

                new MenuItem
                {
                    Titulo = "Funcionários",
                    Icone = "bi-briefcase",
                    Url = "/funcionarios",
                    SubItens = new List<MenuItem>
                    {
                        new MenuItem { Titulo = "Listar Funcionários", Icone = "bi-list", Url = "/funcionarios" },
                        new MenuItem { Titulo = "Novo Funcionário", Icone = "bi-person-plus", Url = "/funcionarios/novo" }
                    }
                },

                new MenuItem
                {
                    Titulo = "Veículos",
                    Icone = "bi-truck",
                    Url = "/veiculos",
                    SubItens = new List<MenuItem>
                    {
                        new MenuItem { Titulo = "Todos os Veículos", Icone = "bi-list", Url = "/veiculos" },
                        new MenuItem { Titulo = "Veículos Disponíveis", Icone = "bi-check-circle", Url = "/veiculos/disponiveis" },
                        new MenuItem { Titulo = "Novo Veículo", Icone = "bi-plus-circle", Url = "/veiculos/novo" },
                        new MenuItem { Titulo = "Manutenções", Icone = "bi-tools", Url = "/veiculos/manutencoes" }
                    }
                },

                new MenuItem
                {
                    Titulo = "Categorias",
                    Icone = "bi-tags",
                    Url = "/categorias",
                    SubItens = new List<MenuItem>
                    {
                        new MenuItem { Titulo = "Listar Categorias", Icone = "bi-list", Url = "/categorias" },
                        new MenuItem { Titulo = "Nova Categoria", Icone = "bi-plus", Url = "/categorias/nova" }
                    }
                },

                new MenuItem
                {
                    Titulo = "Locações",
                    Icone = "bi-calendar-check",
                    Url = "/locacoes",
                    SubItens = new List<MenuItem>
                    {
                        new MenuItem { Titulo = "Todas Locações", Icone = "bi-list", Url = "/locacoes" },
                        new MenuItem { Titulo = "Nova Locação", Icone = "bi-plus", Url = "/locacoes/nova" },
                        new MenuItem { Titulo = "Locações Ativas", Icone = "bi-play-circle", Url = "/locacoes/ativas" },
                        new MenuItem { Titulo = "Finalizadas", Icone = "bi-check2-circle", Url = "/locacoes/finalizadas" }
                    }
                },

                new MenuItem
                {
                    Titulo = "Reservas",
                    Icone = "bi-bookmark-check",
                    Url = "/reservas",
                    SubItens = new List<MenuItem>
                    {
                        new MenuItem { Titulo = "Todas Reservas", Icone = "bi-list", Url = "/reservas" },
                        new MenuItem { Titulo = "Nova Reserva", Icone = "bi-plus", Url = "/reservas/nova" }
                    }
                },

                new MenuItem
                {
                    Titulo = "Filiais",
                    Icone = "bi-building",
                    Url = "/filiais",
                    SubItens = new List<MenuItem>
                    {
                        new MenuItem { Titulo = "Listar Filiais", Icone = "bi-list", Url = "/filiais" },
                        new MenuItem { Titulo = "Nova Filial", Icone = "bi-plus", Url = "/filiais/nova" }
                    }
                },

                new MenuItem
                {
                    Titulo = "Seguros",
                    Icone = "bi-shield-check",
                    Url = "/seguros",
                    SubItens = new List<MenuItem>
                    {
                        new MenuItem { Titulo = "Listar Seguros", Icone = "bi-list", Url = "/seguros" },
                        new MenuItem { Titulo = "Novo Seguro", Icone = "bi-plus", Url = "/seguros/novo" }
                    }
                },

                new MenuItem
                {
                    Titulo = "Adicionais",
                    Icone = "bi-plus-square",
                    Url = "/adicionais",
                    SubItens = new List<MenuItem>
                    {
                        new MenuItem { Titulo = "Listar Adicionais", Icone = "bi-list", Url = "/adicionais" },
                        new MenuItem { Titulo = "Novo Adicional", Icone = "bi-plus", Url = "/adicionais/novo" }
                    }
                },

                new MenuItem
                {
                    Titulo = "Multas",
                    Icone = "bi-exclamation-triangle",
                    Url = "/multas",
                    SubItens = new List<MenuItem>
                    {
                        new MenuItem { Titulo = "Listar Multas", Icone = "bi-list", Url = "/multas" },
                        new MenuItem { Titulo = "Tipos de Multa", Icone = "bi-tags", Url = "/multas/tipos" }
                    }
                }

            };
        }
    }
}