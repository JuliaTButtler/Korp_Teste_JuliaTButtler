import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ProdutoService } from '../../services/produto.service';
import { saldoDisponivel } from '../../models/produto';

@Component({
  selector: 'app-produtos',
  imports: [FormsModule],
  templateUrl: './produtos.html',
  styleUrl: './produtos.css',
})
export class Produtos implements OnInit {
  private readonly produtoService = inject(ProdutoService);

  readonly produtos = this.produtoService.lista;
  readonly saldoDisponivel = saldoDisponivel;

  codigo = '';
  descricao = '';
  saldo: number | null = null;
  quantidadesEntrada: Record<number, number | null> = {};
  erro = '';
  sucesso = '';
  carregando = false;
  salvando = false;

  get formularioBloqueado(): boolean {
    return this.salvando || (this.carregando && this.produtos().length === 0);
  }

  ngOnInit(): void {
    void this.carregar();
  }

  async carregar(): Promise<void> {
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

  async cadastrar(): Promise<void> {
    if (this.salvando) {
      return;
    }

    this.erro = '';
    this.sucesso = '';

    const codigoNormalizado = this.codigo.trim();

    if (
      this.produtos().some(
        (produto) => produto.codigo.toLowerCase() === codigoNormalizado.toLowerCase()
      )
    ) {
      this.erro = 'Já existe um produto cadastrado com esse código.';
      return;
    }

    this.salvando = true;

    try {
      if (this.saldo === null) {
        throw new Error('O saldo do produto é obrigatório.');
      }

      await this.produtoService.criar(this.codigo, this.descricao, this.saldo);
      this.sucesso = 'Produto cadastrado com sucesso.';
      this.codigo = '';
      this.descricao = '';
      this.saldo = null;
    } catch (error) {
      this.erro = error instanceof Error ? error.message : 'Não foi possível cadastrar o produto.';
    } finally {
      this.salvando = false;
    }
  }

  async entrarEstoque(produtoId: number): Promise<void> {
    if (this.salvando) {
      return;
    }

    this.erro = '';
    this.sucesso = '';
    this.salvando = true;

    try {
      const quantidade = this.quantidadesEntrada[produtoId];

      if (quantidade === null || quantidade === undefined || !Number.isFinite(quantidade)) {
        throw new Error('A quantidade da entrada é obrigatória.');
      }

      await this.produtoService.entrarEstoque(produtoId, quantidade);
      this.sucesso = 'Entrada de estoque registrada com sucesso.';
      this.quantidadesEntrada[produtoId] = null;
    } catch (error) {
      this.erro = error instanceof Error ? error.message : 'Não foi possível registrar a entrada.';
    } finally {
      this.salvando = false;
    }
  }
}
