using Locadora_Auto.Api.Controllers;
using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.FilialServices;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Locadora_Auto.Api.V1.Controllers
{
    //[ApiController]
    //[Route("api/v1/fotos")]
    //public class FotoController : MainController
    //{
    //    private readonly IFotoService _fotoService;

    //    public FotoController(IFotoService fotoService,INotificadorService notificador): base(notificador)
    //    {
    //        _fotoService = fotoService;
    //    }

    //    // ========================= CONSULTAS =========================

    //    [HttpGet]
    //    public async Task<IActionResult> ObterTodas(CancellationToken ct)
    //    {
    //        var fotos = await _fotoService.ObterTodasAsync(ct);
    //        return CustomResponse(fotos);
    //    }

    //    //[HttpGet("{id:int}")]
    //    //public async Task<IActionResult> ObterPorId(int id, CancellationToken ct)
    //    //{
    //    //    var filial = await _filialService.ObterPorIdAsync(id, ct);
    //    //    return CustomResponse(filial);
    //    //}

    //    // ========================= CRIAÇÃO =========================

    //    [HttpPost]
    //    public async Task<IActionResult> Criar([FromForm] EnviarFotoDto dto, CancellationToken ct)
    //    {
    //        if (!ModelState.IsValid)
    //            return ValidationResponse(ModelState);

    //        var fotos = await _fotoService.UploadFotolAsync(dto, ct);
    //        return CustomResponse(fotos, HttpStatusCode.Created);
    //    }



    //    // ========================= EXCLUSÃO =========================

    //    //[HttpDelete("{id:int}")]
    //    //public async Task<IActionResult> Excluir(int id, CancellationToken ct)
    //    //{
    //    //    var sucesso = await _filialService.ExcluirFilialAsync(id, ct);
    //    //    return CustomResponse(sucesso, HttpStatusCode.NoContent);
    //    //}


    //}

}
