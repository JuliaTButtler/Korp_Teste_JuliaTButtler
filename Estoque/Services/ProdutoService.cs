using Estoque.Data;
using Estoque.DTOs;
using Estoque.Models;
using Microsoft.EntityFrameworkCore;

namespace Estoque.Services;

public class ProdutoService
{
    private readonly AppDbContext _context;

    public ProdutoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Produto>> ListarTodosAsync()
    {
        return await _context.Produtos
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Produto?> BuscarPorIdAsync(int id)
    {
        return await _context.Produtos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Produto> CriarAsync(CriarProdutoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Codigo))
        {
            throw new ArgumentException(
                "O código do produto é obrigatório."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Descricao))
        {
            throw new ArgumentException(
                "A descrição do produto é obrigatória."
            );
        }

        if (request.Saldo < 0)
        {
            throw new ArgumentException(
                "O saldo do produto não pode ser negativo."
            );
        }

        var produtoExistente = await _context.Produtos
            .FirstOrDefaultAsync(p => p.Codigo == request.Codigo);

        if (produtoExistente != null)
        {
            throw new InvalidOperationException(
                "Já existe um produto cadastrado com esse código."
            );
        }

        var produto = new Produto
        {
            Codigo = request.Codigo,
            Descricao = request.Descricao,
            Saldo = request.Saldo
        };

        _context.Produtos.Add(produto);

        await _context.SaveChangesAsync();

        return produto;
    }
}