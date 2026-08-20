import { Injectable, signal } from '@angular/core';
import { API_FATURAMENTO } from '../config/api';
import {
  CriarNotaFiscalRequest,
  ItemNotaFiscalRequest,
  NotaFiscal,
} from '../models/nota-fiscal';
import { ApiError, apiJson } from '../utils/api-error';

@Injectable({ providedIn: 'root' })
export class NotaFiscalService {
  private readonly baseUrl = `${API_FATURAMENTO}/NotaFiscal`;
  private readonly timeoutMs = 5000;

  private readonly notas = signal<NotaFiscal[]>([]);
  private readonly servicoDisponivel = signal(true);

  readonly lista = this.notas.asReadonly();
  readonly disponivel = this.servicoDisponivel.asReadonly();

  async carregar(): Promise<void> {
    try {
      const lista = await apiJson<NotaFiscal[]>(this.baseUrl, {
        servico: 'faturamento',
        fallback: 'Não foi possível carregar as notas fiscais.',
        timeoutMs: this.timeoutMs,
      });

      this.notas.set(lista);
      this.servicoDisponivel.set(true);
    } catch (error) {
      this.servicoDisponivel.set(false);
      throw error;
    }
  }

  async buscarPorId(id: number): Promise<NotaFiscal | undefined> {
    const emCache = this.obterDoCache(id);

    if (emCache) {
      return emCache;
    }

    try {
      const nota = await apiJson<NotaFiscal>(`${this.baseUrl}/${id}`, {
        servico: 'faturamento',
        fallback: 'Não foi possível carregar a nota fiscal.',
        timeoutMs: this.timeoutMs,
      });
      this.servicoDisponivel.set(true);
      return nota;
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        return undefined;
      }

      this.marcarIndisponivelSeAplicavel(error);
      throw error instanceof Error
        ? error
        : new Error('Não foi possível carregar a nota fiscal.');
    }
  }

  listarTodos(): NotaFiscal[] {
    return [...this.notas()].sort((a, b) => b.numero - a.numero);
  }

  obterDoCache(id: number): NotaFiscal | undefined {
    return this.notas().find((nota) => nota.id === id);
  }

  async criar(itens: ItemNotaFiscalRequest[]): Promise<NotaFiscal> {
    this.validarItens(itens);

    const body: CriarNotaFiscalRequest = { itens };

    try {
      const nota = await apiJson<NotaFiscal>(this.baseUrl, {
        method: 'POST',
        body,
        servico: 'faturamento',
        fallback: 'Não foi possível criar a nota.',
        timeoutMs: 12000,
      });

      this.notas.update((lista) => [nota, ...lista]);
      this.servicoDisponivel.set(true);

      return nota;
    } catch (error) {
      this.marcarIndisponivelSeAplicavel(error);
      throw error;
    }
  }

  async imprimir(id: number): Promise<NotaFiscal> {
    try {
      const nota = await apiJson<NotaFiscal>(`${this.baseUrl}/${id}/imprimir`, {
        method: 'POST',
        body: {},
        servico: 'faturamento',
        fallback: 'Não foi possível imprimir a nota.',
        timeoutMs: 20000,
      });

      this.notas.update((lista) =>
        lista.map((item) => (item.id === id ? nota : item))
      );
      this.servicoDisponivel.set(true);

      return nota;
    } catch (error) {
      this.marcarIndisponivelSeAplicavel(error);
      throw error;
    }
  }

  private validarItens(itens: ItemNotaFiscalRequest[]): void {
    if (!itens || itens.length === 0) {
      throw new Error('A nota fiscal deve possuir ao menos um item.');
    }

    if (new Set(itens.map((item) => item.produtoId)).size !== itens.length) {
      throw new Error(
        'A nota fiscal não pode conter o mesmo produto mais de uma vez.'
      );
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

  private marcarIndisponivelSeAplicavel(error: unknown): void {
    const mensagem =
      error instanceof Error ? error.message.toLowerCase() : '';

    if (mensagem.includes('indisponível') || mensagem.includes('indisponivel')) {
      this.servicoDisponivel.set(false);
    }
  }
}
