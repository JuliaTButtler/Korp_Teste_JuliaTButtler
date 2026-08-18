import { inject, Injectable, signal } from '@angular/core';
import { ItemNotaFiscalRequest, NotaFiscal } from '../models/nota-fiscal';
import { ProdutoService } from './produto.service';

@Injectable({ providedIn: 'root' })
export class NotaFiscalService {
  private readonly produtoService = inject(ProdutoService);

  private nextId = 3;
  private nextItemId = 4;
  private nextNumero = 3;

  private readonly notas = signal<NotaFiscal[]>([
    {
      id: 1,
      numero: 1,
      status: 'ABERTA',
      dataCriacao: new Date(2026, 7, 18),
      itens: [
        { id: 1, produtoId: 1, quantidade: 2 },
        { id: 2, produtoId: 2, quantidade: 5 },
      ],
    },
    {
      id: 2,
      numero: 2,
      status: 'FECHADA',
      dataCriacao: new Date(2026, 7, 18),
      itens: [{ id: 3, produtoId: 3, quantidade: 1 }],
    },
  ]);

  readonly lista = this.notas.asReadonly();

  listarTodos(): NotaFiscal[] {
    return [...this.notas()].sort((a, b) => b.numero - a.numero);
  }

  buscarPorId(id: number): NotaFiscal | undefined {
    return this.notas().find((nota) => nota.id === id);
  }

  criar(itens: ItemNotaFiscalRequest[]): NotaFiscal {
    this.validarItens(itens);

    for (const item of itens) {
      const produto = this.produtoService.buscarPorId(item.produtoId);

      if (!produto) {
        throw new Error(`Produto ${item.produtoId} não encontrado no estoque.`);
      }

      if (item.quantidade > produto.saldo) {
        throw new Error(
          `Saldo insuficiente para o produto ${item.produtoId}. Saldo disponível: ${produto.saldo}.`
        );
      }
    }

    const nota: NotaFiscal = {
      id: this.nextId++,
      numero: this.nextNumero++,
      status: 'ABERTA',
      dataCriacao: new Date(),
      itens: itens.map((item) => ({
        id: this.nextItemId++,
        produtoId: item.produtoId,
        quantidade: item.quantidade,
      })),
    };

    this.notas.update((lista) => [nota, ...lista]);

    return nota;
  }

  imprimir(id: number): NotaFiscal {
    const nota = this.buscarPorId(id);

    if (!nota) {
      throw new Error('Nota fiscal não encontrada.');
    }

    if (nota.status !== 'ABERTA') {
      throw new Error('Somente notas com status ABERTA podem ser impressas.');
    }

    if (nota.itens.length === 0) {
      throw new Error('A nota fiscal deve possuir ao menos um item para impressão.');
    }

    for (const item of nota.itens) {
      this.produtoService.baixarEstoque(item.produtoId, item.quantidade);
    }

    const fechada: NotaFiscal = {
      ...nota,
      status: 'FECHADA',
    };

    this.notas.update((lista) =>
      lista.map((item) => (item.id === id ? fechada : item))
    );

    return fechada;
  }

  private validarItens(itens: ItemNotaFiscalRequest[]): void {
    if (!itens || itens.length === 0) {
      throw new Error('A nota fiscal deve possuir ao menos um item.');
    }

    for (const item of itens) {
      if (item.produtoId <= 0) {
        throw new Error('O identificador do produto é obrigatório.');
      }

      if (item.quantidade <= 0) {
        throw new Error('A quantidade do item deve ser maior que zero.');
      }
    }
  }
}
