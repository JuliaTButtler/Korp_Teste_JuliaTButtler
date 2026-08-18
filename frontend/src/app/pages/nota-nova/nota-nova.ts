import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Produto } from '../../models/produto';
import { NotaFiscalService } from '../../services/nota-fiscal.service';
import { ProdutoService } from '../../services/produto.service';

interface ItemLocal {
  produtoId: number;
  codigo: string;
  descricao: string;
  quantidade: number;
}

@Component({
  selector: 'app-nota-nova',
  imports: [FormsModule, RouterLink],
  templateUrl: './nota-nova.html',
  styleUrl: './nota-nova.css',
})
export class NotaNova {
  private readonly produtoService = inject(ProdutoService);
  private readonly notaFiscalService = inject(NotaFiscalService);
  private readonly router = inject(Router);

  readonly produtos = this.produtoService.lista;

  produtoId: number | null = null;
  quantidade: number | null = null;
  itens: ItemLocal[] = [];
  erro = '';

  produtoSelecionado(): Produto | undefined {
    if (this.produtoId === null) {
      return undefined;
    }

    return this.produtoService.buscarPorId(this.produtoId);
  }

  rotuloProduto(produto: Produto): string {
    return `${produto.codigo} - ${produto.descricao} (saldo: ${produto.saldo})`;
  }

  adicionarItem(): void {
    this.erro = '';

    const produto = this.produtoSelecionado();

    if (!produto) {
      this.erro = 'Selecione um produto.';
      return;
    }

    if (this.quantidade === null || this.quantidade <= 0) {
      this.erro = 'A quantidade do item deve ser maior que zero.';
      return;
    }

    if (this.quantidade > produto.saldo) {
      this.erro = `Saldo insuficiente para o produto ${produto.id}. Saldo disponível: ${produto.saldo}.`;
      return;
    }

    if (this.itens.some((item) => item.produtoId === produto.id)) {
      this.erro = 'Este produto já foi adicionado. Remova-o para informar outra quantidade.';
      return;
    }

    this.itens = [
      ...this.itens,
      {
        produtoId: produto.id,
        codigo: produto.codigo,
        descricao: produto.descricao,
        quantidade: this.quantidade,
      },
    ];

    this.produtoId = null;
    this.quantidade = null;
  }

  removerItem(produtoId: number): void {
    this.itens = this.itens.filter((item) => item.produtoId !== produtoId);
    this.erro = '';
  }

  criarNota(): void {
    this.erro = '';

    try {
      this.notaFiscalService.criar(
        this.itens.map((item) => ({
          produtoId: item.produtoId,
          quantidade: item.quantidade,
        }))
      );

      void this.router.navigate(['/notas']);
    } catch (error) {
      this.erro = error instanceof Error ? error.message : 'Não foi possível criar a nota.';
    }
  }
}
