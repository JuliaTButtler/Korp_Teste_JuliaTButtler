import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ProdutoService } from '../../services/produto.service';

@Component({
  selector: 'app-produtos',
  imports: [FormsModule],
  templateUrl: './produtos.html',
  styleUrl: './produtos.css',
})
export class Produtos {
  private readonly produtoService = inject(ProdutoService);

  readonly produtos = this.produtoService.lista;

  codigo = '';
  descricao = '';
  saldo: number | null = null;
  quantidadesEntrada: Record<number, number | null> = {};
  erro = '';
  sucesso = '';

  cadastrar(): void {
    this.erro = '';
    this.sucesso = '';

    try {
      if (this.saldo === null) {
        throw new Error('O saldo do produto é obrigatório.');
      }

      this.produtoService.criar(this.codigo, this.descricao, this.saldo);
      this.sucesso = 'Produto cadastrado com sucesso.';
      this.codigo = '';
      this.descricao = '';
      this.saldo = null;
    } catch (error) {
      this.erro = error instanceof Error ? error.message : 'Não foi possível cadastrar o produto.';
    }
  }

  entrarEstoque(produtoId: number): void {
    this.erro = '';
    this.sucesso = '';

    try {
      const quantidade = this.quantidadesEntrada[produtoId];

      if (quantidade === null || quantidade === undefined || !Number.isFinite(quantidade)) {
        throw new Error('A quantidade da entrada é obrigatória.');
      }

      this.produtoService.entrarEstoque(produtoId, quantidade);
      this.sucesso = 'Entrada de estoque registrada com sucesso.';
      this.quantidadesEntrada[produtoId] = null;
    } catch (error) {
      this.erro = error instanceof Error ? error.message : 'Não foi possível registrar a entrada.';
    }
  }
}
