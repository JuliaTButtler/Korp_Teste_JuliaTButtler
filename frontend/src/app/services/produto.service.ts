import { Injectable, signal } from '@angular/core';
import { Produto } from '../models/produto';

@Injectable({ providedIn: 'root' })
export class ProdutoService {
  private nextId = 4;

  private readonly produtos = signal<Produto[]>([
    { id: 1, codigo: '001', descricao: 'Arroz', saldo: 10 },
    { id: 2, codigo: '002', descricao: 'Feijão', saldo: 20 },
    { id: 3, codigo: '003', descricao: 'Macarrão', saldo: 14 },
  ]);

  readonly lista = this.produtos.asReadonly();

  buscarPorId(id: number): Produto | undefined {
    return this.produtos().find((produto) => produto.id === id);
  }

  criar(codigo: string, descricao: string, saldo: number): Produto {
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

    const produto: Produto = {
      id: this.nextId++,
      codigo: codigoNormalizado,
      descricao: descricaoNormalizada,
      saldo,
    };

    this.produtos.update((lista) => [...lista, produto]);

    return produto;
  }

  baixarEstoque(id: number, quantidade: number): Produto {
    if (quantidade <= 0) {
      throw new Error('A quantidade da baixa deve ser maior que zero.');
    }

    const produto = this.buscarPorId(id);

    if (!produto) {
      throw new Error('Produto não encontrado.');
    }

    if (produto.saldo < quantidade) {
      throw new Error('Saldo insuficiente para realizar a baixa.');
    }

    const atualizado: Produto = {
      ...produto,
      saldo: produto.saldo - quantidade,
    };

    this.produtos.update((lista) =>
      lista.map((item) => (item.id === id ? atualizado : item))
    );

    return atualizado;
  }
}
