import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Produto, saldoDisponivel } from '../../models/produto';
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
export class NotaNova implements OnInit {
  private readonly produtoService = inject(ProdutoService);
  private readonly notaFiscalService = inject(NotaFiscalService);
  private readonly router = inject(Router);

  readonly produtos = this.produtoService.lista;
  readonly saldoDisponivel = saldoDisponivel;

  produtoId: number | null = null;
  quantidade: number | null = null;
  itens: ItemLocal[] = [];
  erro = '';
  carregando = false;
  salvando = false;

  ngOnInit(): void {
    void this.carregarProdutos();
  }

  async carregarProdutos(): Promise<void> {
    this.erro = '';
    this.carregando = true;

    try {
      await this.produtoService.carregar();
    } catch (error) {
      this.erro =
        error instanceof Error ? error.message : 'Não foi possível carregar os produtos.';
    } finally {
      this.carregando = false;
    }
  }

  produtoSelecionado(): Produto | undefined {
    if (this.produtoId === null) {
      return undefined;
    }

    return this.produtoService.buscarPorId(this.produtoId);
  }

  rotuloProduto(produto: Produto): string {
    return `${produto.codigo} - ${produto.descricao} (disponível: ${saldoDisponivel(produto)})`;
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

    const disponivel = saldoDisponivel(produto);

    if (this.quantidade > disponivel) {
      this.erro = `Saldo insuficiente para o produto ${produto.id}. Saldo disponível: ${disponivel}.`;
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

  async criarNota(): Promise<void> {
    this.erro = '';
    this.salvando = true;

    try {
      const nota = await this.notaFiscalService.criar(
        this.itens.map((item) => ({
          produtoId: item.produtoId,
          quantidade: item.quantidade,
        }))
      );

      await this.router.navigate(['/notas', nota.id], {
        state: { nota },
      });
    } catch (error) {
      this.erro = error instanceof Error ? error.message : 'Não foi possível criar a nota.';
    } finally {
      this.salvando = false;
    }
  }
}
