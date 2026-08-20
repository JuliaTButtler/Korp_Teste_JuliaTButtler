export type StatusNotaFiscal = 'ABERTA' | 'FECHADA';

export interface ItemNotaFiscal {
  id: number;
  notaFiscalId?: number;
  produtoId: number;
  quantidade: number;
}

export interface NotaFiscal {
  id: number;
  numero: number;
  status: StatusNotaFiscal;
  dataCriacao: string | Date;
  itens: ItemNotaFiscal[];
}

export interface ItemNotaFiscalRequest {
  produtoId: number;
  quantidade: number;
}

export interface CriarNotaFiscalRequest {
  itens: ItemNotaFiscalRequest[];
}
