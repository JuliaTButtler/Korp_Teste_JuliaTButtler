import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { catchError, firstValueFrom, timeout, throwError } from 'rxjs';
import {
  CriarNotaFiscalRequest,
  ItemNotaFiscalRequest,
  NotaFiscal,
} from '../models/nota-fiscal';
import { mensagemErroHttp } from '../utils/api-error';

@Injectable({ providedIn: 'root' })
export class NotaFiscalService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api-faturamento/NotaFiscal';
  private readonly timeoutMs = 15000;

  private readonly notas = signal<NotaFiscal[]>([]);

  readonly lista = this.notas.asReadonly();

  async carregar(): Promise<void> {
    try {
      const lista = await firstValueFrom(
        this.http.get<NotaFiscal[]>(this.baseUrl).pipe(
          timeout(this.timeoutMs),
          catchError((error) =>
            this.rejeitar(error, 'Não foi possível carregar as notas fiscais.')
          )
        )
      );
      this.notas.set(lista);
    } catch (error) {
      throw this.normalizarErro(error, 'Não foi possível carregar as notas fiscais.');
    }
  }

  async buscarPorId(id: number): Promise<NotaFiscal | undefined> {
    const emCache = this.obterDoCache(id);

    if (emCache) {
      return emCache;
    }

    try {
      return await firstValueFrom(
        this.http.get<NotaFiscal>(`${this.baseUrl}/${id}`).pipe(
          timeout(this.timeoutMs),
          catchError((error) => {
            if (error instanceof HttpErrorResponse && error.status === 404) {
              return throwError(() => error);
            }

            return this.rejeitar(error, 'Não foi possível carregar a nota fiscal.');
          })
        )
      );
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 404) {
        return undefined;
      }

      throw this.normalizarErro(error, 'Não foi possível carregar a nota fiscal.');
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
      const nota = await firstValueFrom(
        this.http.post<NotaFiscal>(this.baseUrl, body).pipe(
          timeout(this.timeoutMs),
          catchError((error) => this.rejeitar(error, 'Não foi possível criar a nota.'))
        )
      );

      this.notas.update((lista) => [nota, ...lista]);

      return nota;
    } catch (error) {
      throw this.normalizarErro(error, 'Não foi possível criar a nota.');
    }
  }

  async imprimir(id: number): Promise<NotaFiscal> {
    try {
      const nota = await firstValueFrom(
        this.http.post<NotaFiscal>(`${this.baseUrl}/${id}/imprimir`, {}).pipe(
          timeout(this.timeoutMs),
          catchError((error) =>
            this.rejeitar(error, 'Não foi possível imprimir a nota.')
          )
        )
      );

      this.notas.update((lista) =>
        lista.map((item) => (item.id === id ? nota : item))
      );

      return nota;
    } catch (error) {
      throw this.normalizarErro(error, 'Não foi possível imprimir a nota.');
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

  private rejeitar(error: unknown, fallback: string) {
    return throwError(() => this.normalizarErro(error, fallback));
  }

  private normalizarErro(error: unknown, fallback: string): Error {
    if (error instanceof Error && !(error instanceof HttpErrorResponse)) {
      return error;
    }

    return new Error(mensagemErroHttp(error, fallback));
  }
}
