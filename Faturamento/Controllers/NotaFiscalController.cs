using Faturamento.DTOs;
using Faturamento.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Faturamento.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotaFiscalController : ControllerBase
{
    private readonly NotaFiscalService _service;

    public NotaFiscalController(NotaFiscalService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> ListarTodos()
    {
        var notas = await _service.ListarTodosAsync();

        return Ok(notas);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var nota = await _service.BuscarPorIdAsync(id);

        if (nota == null)
        {
            return NotFound(new
            {
                mensagem = "Nota fiscal não encontrada."
            });
        }

        return Ok(nota);
    }

    [HttpPost]
    public async Task<IActionResult> Criar(CriarNotaFiscalRequest request)
    {
        try
        {
            var nota = await _service.CriarAsync(request);

            return StatusCode(StatusCodes.Status201Created, nota);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("indisponível"))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                mensagem = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                mensagem = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                mensagem = "Não foi possível persistir a nota fiscal. Tente novamente."
            });
        }
    }

    [HttpPost("{id:int}/imprimir")]
    public async Task<IActionResult> Imprimir(int id)
    {
        try
        {
            var nota = await _service.ImprimirAsync(id);

            if (nota == null)
            {
                return NotFound(new
                {
                    mensagem = "Nota fiscal não encontrada."
                });
            }

            return Ok(nota);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("indisponível"))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                mensagem = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                mensagem = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
    }
}
