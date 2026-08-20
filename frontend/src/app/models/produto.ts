export interface Produto {
  id: number;
  codigo: string;
  descricao: string;
  saldo: number;
  reservado: number;
}

export function saldoDisponivel(produto: Produto): number {
  return produto.saldo - produto.reservado;
}
