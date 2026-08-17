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

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    "Não foi possível consultar o produto no estoque."
                );
            }

            return await response.Content.ReadFromJsonAsync<ProdutoEstoqueResponse>();
        }
        catch (HttpRequestException)
        {
            throw new InvalidOperationException(
                "Serviço de estoque indisponível."
            );
        }
    }

    public async Task BaixarEstoqueAsync(int produtoId, int quantidade)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/produto/{produtoId}/baixa",
                new { quantidade }
            );

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException(
                    $"Produto {produtoId} não encontrado no estoque."
                );
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new InvalidOperationException(
                    $"Saldo insuficiente para o produto {produtoId}."
                );
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new ArgumentException(
                    $"Quantidade inválida para o produto {produtoId}."
                );
            }

            throw new InvalidOperationException(
                "Não foi possível realizar a baixa no estoque."
            );
        }
        catch (HttpRequestException)
        {
            throw new InvalidOperationException(
                "Serviço de estoque indisponível."
            );
        }
    }
}
