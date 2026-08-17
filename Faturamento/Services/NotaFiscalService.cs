using Faturamento.Data;
using Faturamento.DTOs;
using Faturamento.Models;
using Microsoft.EntityFrameworkCore;

namespace Faturamento.Services;

public class NotaFiscalService
{
    private readonly AppDbContext _context;
    private readonly EstoqueClient _estoqueClient;

    public NotaFiscalService(AppDbContext context, EstoqueClient estoqueClient)
    {
        _context = context;
        _estoqueClient = estoqueClient;
    }

    public async Task<List<NotaFiscal>> ListarTodosAsync()
    {
        return await _context.NotasFiscais
            .AsNoTracking()
            .Include(n => n.Itens)
            .OrderByDescending(n => n.Numero)
            .ToListAsync();
    }

    public async Task<NotaFiscal?> BuscarPorIdAsync(int id)
    {
        return await _context.NotasFiscais
            .AsNoTracking()
            .Include(n => n.Itens)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<NotaFiscal> CriarAsync(CriarNotaFiscalRequest request)
    {
        ValidarItens(request.Itens);

        foreach (var item in request.Itens)
        {
            var produto = await _estoqueClient.ObterProdutoAsync(item.ProdutoId);

            if (produto == null)
            {
                throw new InvalidOperationException(
                    $"Produto {item.ProdutoId} não encontrado no estoque."
                );
            }

            if (item.Quantidade > produto.Saldo)
            {
                throw new InvalidOperationException(
                    $"Saldo insuficiente para o produto {item.ProdutoId}. Saldo disponível: {produto.Saldo}."
                );
            }
        }

        var ultimoNumero = await _context.NotasFiscais
            .MaxAsync(n => (int?)n.Numero) ?? 0;

        var nota = new NotaFiscal
        {
            Numero = ultimoNumero + 1,
            Status = StatusNotaFiscal.ABERTA,
            DataCriacao = DateTime.Now,
            Itens = request.Itens
                .Select(item => new ItemNotaFiscal
                {
                    ProdutoId = item.ProdutoId,
                    Quantidade = item.Quantidade
                })
                .ToList()
        };

        _context.NotasFiscais.Add(nota);

        await _context.SaveChangesAsync();

        return nota;
    }

    public async Task<NotaFiscal?> ImprimirAsync(int id)
    {
        var nota = await _context.NotasFiscais
            .Include(n => n.Itens)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (nota == null)
        {
            return null;
        }

        if (nota.Status != StatusNotaFiscal.ABERTA)
        {
            throw new InvalidOperationException(
                "Somente notas com status ABERTA podem ser impressas."
            );
        }

        if (nota.Itens.Count == 0)
        {
            throw new ArgumentException(
                "A nota fiscal deve possuir ao menos um item para impressão."
            );
        }

        foreach (var item in nota.Itens)
        {
            await _estoqueClient.BaixarEstoqueAsync(item.ProdutoId, item.Quantidade);
        }

        nota.Status = StatusNotaFiscal.FECHADA;

        await _context.SaveChangesAsync();

        return nota;
    }

    private static void ValidarItens(List<ItemNotaFiscalRequest> itens)
    {
        if (itens == null || itens.Count == 0)
        {
            throw new ArgumentException(
                "A nota fiscal deve possuir ao menos um item."
            );
        }

        foreach (var item in itens)
        {
            if (item.ProdutoId <= 0)
            {
                throw new ArgumentException(
                    "O identificador do produto é obrigatório."
                );
            }

            if (item.Quantidade <= 0)
            {
                throw new ArgumentException(
                    "A quantidade do item deve ser maior que zero."
                );
            }
        }
    }
}
