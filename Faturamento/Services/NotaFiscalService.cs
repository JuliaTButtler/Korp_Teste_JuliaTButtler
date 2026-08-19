using Faturamento.Data;
using Faturamento.DTOs;
using Faturamento.Models;
using Microsoft.EntityFrameworkCore;

namespace Faturamento.Services;

public class NotaFiscalService
{
    private const int MaximoTentativasNumero = 5;

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

        var reservados = new List<ItemNotaFiscalRequest>();

        try
        {
            foreach (var item in request.Itens)
            {
                await _estoqueClient.ReservarAsync(item.ProdutoId, item.Quantidade);
                reservados.Add(item);
            }

            return await PersistirNotaComNumeroAsync(request.Itens);
        }
        catch
        {
            await CompensarAsync(reservados, _estoqueClient.LiberarReservaAsync);
            throw;
        }
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

        if (nota.Itens.Count == 0)
        {
            throw new ArgumentException(
                "A nota fiscal deve possuir ao menos um item para impressão."
            );
        }

        await GarantirItensDisponiveisParaImpressaoAsync(nota.Itens);

        var reivindicada = await _context.NotasFiscais
            .Where(n => n.Id == id && n.Status == StatusNotaFiscal.ABERTA)
            .ExecuteUpdateAsync(n => n
                .SetProperty(
                    notaFiscal => notaFiscal.Status,
                    StatusNotaFiscal.FECHADA
                )
            );

        if (reivindicada == 0)
        {
            var atual = await BuscarPorIdAsync(id);

            if (atual == null)
            {
                return null;
            }

            throw new InvalidOperationException(
                "Somente notas com status ABERTA podem ser impressas."
            );
        }

        var baixados = new List<ItemNotaFiscalRequest>();

        try
        {
            foreach (var item in nota.Itens)
            {
                await _estoqueClient.BaixarEstoqueAsync(item.ProdutoId, item.Quantidade);
                baixados.Add(new ItemNotaFiscalRequest
                {
                    ProdutoId = item.ProdutoId,
                    Quantidade = item.Quantidade
                });
            }
        }
        catch
        {
            await CompensarAsync(baixados, _estoqueClient.EstornarBaixaAsync);

            await _context.NotasFiscais
                .Where(n => n.Id == id && n.Status == StatusNotaFiscal.FECHADA)
                .ExecuteUpdateAsync(n => n
                    .SetProperty(
                        notaFiscal => notaFiscal.Status,
                        StatusNotaFiscal.ABERTA
                    )
                );

            throw;
        }

        _context.ChangeTracker.Clear();

        return await BuscarPorIdAsync(id);
    }

    private async Task GarantirItensDisponiveisParaImpressaoAsync(
        List<ItemNotaFiscal> itens
    )
    {
        foreach (var item in itens)
        {
            var produto = await _estoqueClient.ObterProdutoAsync(item.ProdutoId);

            if (produto == null)
            {
                throw new InvalidOperationException(
                    $"Produto {item.ProdutoId} não encontrado no estoque."
                );
            }

            if (produto.Saldo < item.Quantidade || produto.Reservado < item.Quantidade)
            {
                throw new InvalidOperationException(
                    $"Saldo insuficiente para o produto {item.ProdutoId}."
                );
            }
        }
    }

    private async Task<NotaFiscal> PersistirNotaComNumeroAsync(
        List<ItemNotaFiscalRequest> itens
    )
    {
        for (var tentativa = 1; tentativa <= MaximoTentativasNumero; tentativa++)
        {
            var ultimoNumero = await _context.NotasFiscais
                .MaxAsync(n => (int?)n.Numero) ?? 0;

            var nota = new NotaFiscal
            {
                Numero = ultimoNumero + 1,
                Status = StatusNotaFiscal.ABERTA,
                DataCriacao = DateTime.Now,
                Itens = itens
                    .Select(item => new ItemNotaFiscal
                    {
                        ProdutoId = item.ProdutoId,
                        Quantidade = item.Quantidade
                    })
                    .ToList()
            };

            _context.NotasFiscais.Add(nota);

            try
            {
                await _context.SaveChangesAsync();
                return nota;
            }
            catch (DbUpdateException ex) when (
                EhViolacaoDeUnicidade(ex) &&
                tentativa < MaximoTentativasNumero
            )
            {
                _context.ChangeTracker.Clear();
            }
        }

        throw new InvalidOperationException(
            "Não foi possível gerar o número da nota fiscal. Tente novamente."
        );
    }

    private static async Task CompensarAsync(
        List<ItemNotaFiscalRequest> itens,
        Func<int, int, Task> compensar
    )
    {
        foreach (var item in Enumerable.Reverse(itens))
        {
            try
            {
                await compensar(item.ProdutoId, item.Quantidade);
            }
            catch
            {
                // Continua as demais compensações para não deixar o estoque pela metade.
            }
        }
    }

    private static void ValidarItens(List<ItemNotaFiscalRequest> itens)
    {
        if (itens == null || itens.Count == 0)
        {
            throw new ArgumentException(
                "A nota fiscal deve possuir ao menos um item."
            );
        }

        if (itens.GroupBy(item => item.ProdutoId).Any(grupo => grupo.Count() > 1))
        {
            throw new ArgumentException(
                "A nota fiscal não pode conter o mesmo produto mais de uma vez."
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

    private static bool EhViolacaoDeUnicidade(DbUpdateException ex)
    {
        var mensagem = ex.InnerException?.Message ?? ex.Message;

        return mensagem.Contains("ORA-00001", StringComparison.OrdinalIgnoreCase);
    }
}
