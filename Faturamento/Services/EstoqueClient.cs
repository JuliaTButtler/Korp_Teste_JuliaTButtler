using System.Net;
using System.Net.Http.Json;
using Faturamento.DTOs;

namespace Faturamento.Services;

public class EstoqueClient
{
    private readonly HttpClient _httpClient;

    public EstoqueClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProdutoEstoqueResponse?> ObterProdutoAsync(int produtoId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/produto/{produtoId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            GarantirEstoqueDisponivel(response);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    "Não foi possível consultar o produto no estoque."
                );
            }

            return await response.Content.ReadFromJsonAsync<ProdutoEstoqueResponse>();
        }
        catch (Exception ex) when (EhFalhaDeComunicacao(ex))
        {
            throw EstoqueIndisponivel();
        }
    }

    public Task ReservarAsync(int produtoId, int quantidade)
    {
        return PostMovimentoAsync(
            produtoId,
            "reservar",
            quantidade,
            $"Saldo insuficiente para reservar o produto {produtoId}.",
            "Não foi possível reservar o produto no estoque."
        );
    }

    public Task LiberarReservaAsync(int produtoId, int quantidade)
    {
        return PostMovimentoAsync(
            produtoId,
            "liberar-reserva",
            quantidade,
            $"Não foi possível liberar a reserva do produto {produtoId}.",
            "Não foi possível liberar a reserva no estoque."
        );
    }

    public Task BaixarEstoqueAsync(int produtoId, int quantidade)
    {
        return PostMovimentoAsync(
            produtoId,
            "baixa",
            quantidade,
            $"Saldo insuficiente para o produto {produtoId}.",
            "Não foi possível realizar a baixa no estoque."
        );
    }

    public Task EstornarBaixaAsync(int produtoId, int quantidade)
    {
        return PostMovimentoAsync(
            produtoId,
            "estornar-baixa",
            quantidade,
            $"Não foi possível estornar a baixa do produto {produtoId}.",
            "Não foi possível estornar a baixa no estoque."
        );
    }

    private async Task PostMovimentoAsync(
        int produtoId,
        string acao,
        int quantidade,
        string mensagemConflito,
        string mensagemGenerica
    )
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/produto/{produtoId}/{acao}",
                new { quantidade }
            );

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            GarantirEstoqueDisponivel(response);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException(
                    $"Produto {produtoId} não encontrado no estoque."
                );
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new InvalidOperationException(mensagemConflito);
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new ArgumentException(
                    $"Quantidade inválida para o produto {produtoId}."
                );
            }

            throw new InvalidOperationException(mensagemGenerica);
        }
        catch (Exception ex) when (EhFalhaDeComunicacao(ex))
        {
            throw EstoqueIndisponivel();
        }
    }

    private static void GarantirEstoqueDisponivel(HttpResponseMessage response)
    {
        if ((int)response.StatusCode >= 500)
        {
            throw EstoqueIndisponivel();
        }
    }

    private static bool EhFalhaDeComunicacao(Exception ex)
    {
        return ex is HttpRequestException or TaskCanceledException or TimeoutException;
    }

    private static InvalidOperationException EstoqueIndisponivel()
    {
        return new InvalidOperationException("Serviço de estoque indisponível.");
    }
}
