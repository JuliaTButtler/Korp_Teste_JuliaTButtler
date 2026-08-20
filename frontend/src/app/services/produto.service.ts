import { Injectable, signal } from '@angular/core';
import { API_ESTOQUE } from '../config/api';
import { Produto } from '../models/produto';
import { apiJson } from '../utils/api-error';

@Injectable({ providedIn: 'root' })
export class ProdutoService {
  private readonly baseUrl = `${API_ESTOQUE}/produto`;
  private readonly timeoutMs = 5000;

  private readonly produtos = signal<Produto[]>([]);
  private readonly servicoDisponivel = signal(true);

  readonly lista = this.produtos.asReadonly();
  readonly disponivel = this.servicoDisponivel.asReadonly();

  async carregar(): Promise<void> {
    try {
      const lista = await apiJson<Produto[]>(this.baseUrl, {
        servico: 'estoque',
        fallback: 'Não foi possível carregar os produtos.',
        timeoutMs: this.timeoutMs,
      });

      this.produtos.set(
        lista.map((produto) => ({
          ...produto,
          reservado: produto.reservado ?? 0,
        }))
      );
      this.servicoDisponivel.set(true);
    } catch (error) {
      this.servicoDisponivel.set(false);
      throw error;
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
      const produto = await apiJson<Produto>(this.baseUrl, {
        method: 'POST',
        body: {
          codigo: codigoNormalizado,
          descricao: descricaoNormalizada,
          saldo,
        },
        servico: 'estoque',
        fallback: 'Não foi possível cadastrar o produto.',
        timeoutMs: this.timeoutMs,
      });

      this.produtos.update((lista) => [
        ...lista,
        { ...produto, reservado: produto.reservado ?? 0 },
      ]);
      this.servicoDisponivel.set(true);

      return produto;
    } catch (error) {
      this.marcarIndisponivelSeAplicavel(error);
      throw error;
    }
  }

  async entrarEstoque(id: number, quantidade: number): Promise<Produto> {
    if (quantidade <= 0) {
      throw new Error('A quantidade da entrada deve ser maior que zero.');
    }

    try {
      const atualizado = await apiJson<Produto>(`${this.baseUrl}/${id}/entrada`, {
        method: 'POST',
        body: { quantidade },
        servico: 'estoque',
        fallback: 'Não foi possível registrar a entrada.',
        timeoutMs: this.timeoutMs,
      });

      this.produtos.update((lista) =>
        lista.map((item) =>
          item.id === id
            ? { ...atualizado, reservado: atualizado.reservado ?? 0 }
            : item
        )
      );
      this.servicoDisponivel.set(true);

      return atualizado;
    } catch (error) {
      this.marcarIndisponivelSeAplicavel(error);
      throw error;
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
