namespace Faturamento.Models;

public class NotaFiscal
{
    public int Id { get; set; }

    public int Numero { get; set; }

    public StatusNotaFiscal Status { get; set; } = StatusNotaFiscal.ABERTA;

    public DateTime DataCriacao { get; set; }

    public List<ItemNotaFiscal> Itens { get; set; } = [];
}
