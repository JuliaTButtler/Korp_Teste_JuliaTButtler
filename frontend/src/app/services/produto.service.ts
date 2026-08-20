import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { catchError, firstValueFrom, timeout, throwError } from 'rxjs';
import { Produto } from '../models/produto';
import { mensagemErroHttp } from '../utils/api-error';

@Injectable({ providedIn: 'root' })
export class ProdutoService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api-estoque/produto';
  private readonly timeoutMs = 15000;

  private readonly produtos = signal<Produto[]>([]);

  readonly lista = this.produtos.asReadonly();

  async carregar(): Promise<void> {
    try {
      const lista = await firstValueFrom(
        this.http.get<Produto[]>(this.baseUrl).pipe(
          timeout(this.timeoutMs),
          catchError((error) => this.rejeitar(error, 'Não foi possível carregar os produtos.'))
        )
      );
      this.produtos.set(
        lista.map((produto) => ({
          ...produto,
          reservado: produto.reservado ?? 0,
        }))
      );
    } catch (error) {
      throw this.normalizarErro(error, 'Não foi possível carregar os produtos.');
    }
  }

  buscarPorId(id: number): Produto | undefined {
    return this.produtos().find((produto) => produto.id === id);
  }

  async criar(codigo: string, descricao: string, saldo: number): Promise<Produto> {
    const codigoNormalizado = codigo.trim();
    const descricaoNormalizada = descricao.trim();

    if (!codigoNormalizado) {
      throw new Error('O código do produto é obrigatório.');
    }

    if (!descricaoNormalizada) {
      throw new Error('A descrição do produto é obrigatória.');
    }

    if (!Number.isFinite(saldo)) {
      throw new Error('O saldo do produto é obrigatório.');
    }

    if (saldo < 0) {
      throw new Error('O saldo do produto não pode ser negativo.');
    }

    const codigoExistente = this.produtos().some(
      (produto) => produto.codigo.toLowerCase() === codigoNormalizado.toLowerCase()
    );

    if (codigoExistente) {
      throw new Error('Já existe um produto cadastrado com esse código.');
    }

    try {
      const produto = await firstValueFrom(
        this.http
          .post<Produto>(this.baseUrl, {
            codigo: codigoNormalizado,
            descricao: descricaoNormalizada,
            saldo,
          })
          .pipe(
            timeout(this.timeoutMs),
            catchError((error) =>
              this.rejeitar(error, 'Não foi possível cadastrar o produto.')
            )
          )
      );

      this.produtos.update((lista) => [
        ...lista,
        { ...produto, reservado: produto.reservado ?? 0 },
      ]);

      return produto;
    } catch (error) {
      throw this.normalizarErro(error, 'Não foi possível cadastrar o produto.');
    }
  }

  async entrarEstoque(id: number, quantidade: number): Promise<Produto> {
    if (quantidade <= 0) {
      throw new Error('A quantidade da entrada deve ser maior que zero.');
    }

    try {
      const atualizado = await firstValueFrom(
        this.http
          .post<Produto>(`${this.baseUrl}/${id}/entrada`, { quantidade })
          .pipe(
            timeout(this.timeoutMs),
            catchError((error) =>
              this.rejeitar(error, 'Não foi possível registrar a entrada.')
            )
          )
      );

      this.produtos.update((lista) =>
        lista.map((item) =>
          item.id === id
            ? { ...atualizado, reservado: atualizado.reservado ?? 0 }
            : item
        )
      );

      return atualizado;
    } catch (error) {
      throw this.normalizarErro(error, 'Não foi possível registrar a entrada.');
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
