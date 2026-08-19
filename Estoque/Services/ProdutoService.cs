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
            Saldo = request.Saldo,
            Reservado = 0
        };

        _context.Produtos.Add(produto);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException(
                "Já existe um produto cadastrado com esse código."
            );
        }

        return produto;
    }

    public async Task<Produto?> ReservarAsync(int id, BaixarEstoqueRequest request)
    {
        ValidarQuantidade(request.Quantidade);

        var afetados = await _context.Produtos
            .Where(p =>
                p.Id == id &&
                p.Saldo >= p.Reservado + request.Quantidade)
            .ExecuteUpdateAsync(p => p
                .SetProperty(
                    produto => produto.Reservado,
                    produto => produto.Reservado + request.Quantidade
                )
            );

        return await ResultadoMovimentoAsync(
            id,
            afetados,
            "Saldo insuficiente para reservar a quantidade."
        );
    }

    public async Task<Produto?> LiberarReservaAsync(int id, BaixarEstoqueRequest request)
    {
        ValidarQuantidade(request.Quantidade);

        var afetados = await _context.Produtos
            .Where(p =>
                p.Id == id &&
                p.Reservado >= request.Quantidade)
            .ExecuteUpdateAsync(p => p
                .SetProperty(
                    produto => produto.Reservado,
                    produto => produto.Reservado - request.Quantidade
                )
            );

        return await ResultadoMovimentoAsync(
            id,
            afetados,
            "Não há reserva suficiente para liberar a quantidade."
        );
    }

    public async Task<Produto?> BaixarEstoqueAsync(int id, BaixarEstoqueRequest request)
    {
        ValidarQuantidade(request.Quantidade);

        var afetados = await _context.Produtos
            .Where(p =>
                p.Id == id &&
                p.Saldo >= request.Quantidade &&
                p.Reservado >= request.Quantidade)
            .ExecuteUpdateAsync(p => p
                .SetProperty(
                    produto => produto.Saldo,
                    produto => produto.Saldo - request.Quantidade
                )
                .SetProperty(
                    produto => produto.Reservado,
                    produto => produto.Reservado - request.Quantidade
                )
            );

        return await ResultadoMovimentoAsync(
            id,
            afetados,
            "Saldo insuficiente para realizar a baixa."
        );
    }

    public async Task<Produto?> EstornarBaixaAsync(int id, BaixarEstoqueRequest request)
    {
        ValidarQuantidade(request.Quantidade);

        var afetados = await _context.Produtos
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(p => p
                .SetProperty(
                    produto => produto.Saldo,
                    produto => produto.Saldo + request.Quantidade
                )
                .SetProperty(
                    produto => produto.Reservado,
                    produto => produto.Reservado + request.Quantidade
                )
            );

        if (afetados == 0)
        {
            return null;
        }

        return await BuscarPorIdAsync(id);
    }

    public async Task<Produto?> EntrarEstoqueAsync(int id, EntradaEstoqueRequest request)
    {
        ValidarQuantidade(request.Quantidade);

        var afetados = await _context.Produtos
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(p => p
                .SetProperty(
                    produto => produto.Saldo,
                    produto => produto.Saldo + request.Quantidade
                )
            );

        if (afetados == 0)
        {
            return null;
        }

        return await BuscarPorIdAsync(id);
    }

    private async Task<Produto?> ResultadoMovimentoAsync(
        int id,
        int afetados,
        string mensagemSaldo
    )
    {
        if (afetados > 0)
        {
            return await BuscarPorIdAsync(id);
        }

        var existe = await _context.Produtos
            .AsNoTracking()
            .AnyAsync(p => p.Id == id);

        if (!existe)
        {
            return null;
        }

        throw new InvalidOperationException(mensagemSaldo);
    }

    private static void ValidarQuantidade(int quantidade)
    {
        if (quantidade <= 0)
        {
            throw new ArgumentException(
                $"A quantidade deve ser maior que zero."
            );
        }
    }
}
