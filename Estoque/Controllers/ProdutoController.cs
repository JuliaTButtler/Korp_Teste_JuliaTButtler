using Estoque.DTOs;
using Estoque.Models;
using Estoque.Services;
using Microsoft.AspNetCore.Mvc;

namespace Estoque.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProdutoController : ControllerBase
{
    private readonly ProdutoService _service;

    public ProdutoController(ProdutoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> ListarTodos()
    {
        var produtos = await _service.ListarTodosAsync();

        return Ok(produtos);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var produto = await _service.BuscarPorIdAsync(id);

        if (produto == null)
        {
            return NotFound(new
            {
                mensagem = "Produto não encontrado."
            });
        }

        return Ok(produto);
    }

    [HttpPost]
    public async Task<IActionResult> Criar(CriarProdutoRequest request)
    {
        try
        {
            var produto = await _service.CriarAsync(request);

            return StatusCode(StatusCodes.Status201Created, produto);
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

    [HttpPost("{id:int}/reservar")]
    public async Task<IActionResult> Reservar(int id, BaixarEstoqueRequest request)
    {
        return await ExecutarMovimento(() => _service.ReservarAsync(id, request));
    }

    [HttpPost("{id:int}/liberar-reserva")]
    public async Task<IActionResult> LiberarReserva(int id, BaixarEstoqueRequest request)
    {
        return await ExecutarMovimento(() => _service.LiberarReservaAsync(id, request));
    }

    [HttpPost("{id:int}/baixa")]
    public async Task<IActionResult> BaixarEstoque(int id, BaixarEstoqueRequest request)
    {
        return await ExecutarMovimento(() => _service.BaixarEstoqueAsync(id, request));
    }

    [HttpPost("{id:int}/estornar-baixa")]
    public async Task<IActionResult> EstornarBaixa(int id, BaixarEstoqueRequest request)
    {
        return await ExecutarMovimento(() => _service.EstornarBaixaAsync(id, request));
    }

    [HttpPost("{id:int}/entrada")]
    public async Task<IActionResult> EntrarEstoque(int id, EntradaEstoqueRequest request)
    {
        return await ExecutarMovimento(() => _service.EntrarEstoqueAsync(id, request));
    }

    private async Task<IActionResult> ExecutarMovimento(Func<Task<Produto?>> operacao)
    {
        try
        {
            var produto = await operacao();

            if (produto == null)
            {
                return NotFound(new
                {
                    mensagem = "Produto não encontrado."
                });
            }

            return Ok(produto);
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
