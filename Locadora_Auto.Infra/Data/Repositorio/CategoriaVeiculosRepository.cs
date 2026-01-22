using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class CategoriaVeiculosRepository : RepositorioGlobal<CategoriaVeiculo>, ICategoriaVeiculosRepository
    {
        public CategoriaVeiculosRepository(LocadoraDbContext dbContext) : base(dbContext) { }
        
    }
}
