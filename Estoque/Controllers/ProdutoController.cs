using Estoque.DTOs;
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

            return CreatedAtAction(
                nameof(BuscarPorId),
                new { id = produto.Id },
                produto
            );
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

    [HttpPost("{id:int}/baixa")]
    public async Task<IActionResult> BaixarEstoque(int id, BaixarEstoqueRequest request)
    {
        try
        {
            var produto = await _service.BaixarEstoqueAsync(id, request);

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

