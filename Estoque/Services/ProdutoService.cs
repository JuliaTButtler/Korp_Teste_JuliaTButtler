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

        var produto = await BuscarPorIdAsync(id);

        if (produto == null)
        {
            return null;
        }

        if (produto.Saldo - produto.Reservado < request.Quantidade)
        {
            throw new InvalidOperationException(
                "Saldo insuficiente para reservar a quantidade."
            );
        }

        var afetados = await ExecutarSqlAsync(
            $@"UPDATE ""PRODUTO""
               SET ""RESERVADO"" = ""RESERVADO"" + {request.Quantidade}
               WHERE ""ID"" = {id}
                 AND ""SALDO"" - ""RESERVADO"" >= {request.Quantidade}"
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

        var afetados = await ExecutarSqlAsync(
            $@"UPDATE ""PRODUTO""
               SET ""RESERVADO"" = ""RESERVADO"" - {request.Quantidade}
               WHERE ""ID"" = {id}
                 AND ""RESERVADO"" >= {request.Quantidade}"
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

        var afetados = await ExecutarSqlAsync(
            $@"UPDATE ""PRODUTO""
               SET ""SALDO"" = ""SALDO"" - {request.Quantidade},
                   ""RESERVADO"" = ""RESERVADO"" - {request.Quantidade}
               WHERE ""ID"" = {id}
                 AND ""SALDO"" >= {request.Quantidade}
                 AND ""RESERVADO"" >= {request.Quantidade}"
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

        var afetados = await ExecutarSqlAsync(
            $@"UPDATE ""PRODUTO""
               SET ""SALDO"" = ""SALDO"" + {request.Quantidade},
                   ""RESERVADO"" = ""RESERVADO"" + {request.Quantidade}
               WHERE ""ID"" = {id}"
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

        var afetados = await ExecutarSqlAsync(
            $@"UPDATE ""PRODUTO""
               SET ""SALDO"" = ""SALDO"" + {request.Quantidade}
               WHERE ""ID"" = {id}"
        );

        if (afetados == 0)
        {
            return null;
        }

        return await BuscarPorIdAsync(id);
    }

    private async Task<int> ExecutarSqlAsync(FormattableString sql)
    {
        try
        {
            return await _context.Database.ExecuteSqlInterpolatedAsync(sql);
        }
        catch (Exception ex) when (EhViolacaoDeRegra(ex))
        {
            return 0;
        }
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

    private static bool EhViolacaoDeRegra(Exception ex)
    {
        var mensagem = ex.ToString();

        return mensagem.Contains("ORA-02290", StringComparison.OrdinalIgnoreCase)
            || mensagem.Contains("ORA-00001", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidarQuantidade(int quantidade)
    {
        if (quantidade <= 0)
        {
            throw new ArgumentException(
                "A quantidade deve ser maior que zero."
            );
        }
    }
}
