export type StatusNotaFiscal = 'ABERTA' | 'FECHADA';

export interface ItemNotaFiscal {
  id: number;
  produtoId: number;
  quantidade: number;
}

export interface NotaFiscal {
  id: number;
  numero: number;
  status: StatusNotaFiscal;
  dataCriacao: Date;
  itens: ItemNotaFiscal[];
}

export interface ItemNotaFiscalRequest {
  produtoId: number;
  quantidade: number;
}
