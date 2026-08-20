import { DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NotaFiscal } from '../../models/nota-fiscal';
import { ProdutoService } from '../../services/produto.service';
import { NotaFiscalService } from '../../services/nota-fiscal.service';
import { formatarNumeroNota } from '../../utils/formatacao';

interface ItemDetalhe {
  produtoId: number;
  quantidade: number;
  codigo: string;
  descricao: string;
}

@Component({
  selector: 'app-nota-detalhe',
  imports: [DatePipe, RouterLink],
  templateUrl: './nota-detalhe.html',
  styleUrl: './nota-detalhe.css',
})
export class NotaDetalhe implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly notaFiscalService = inject(NotaFiscalService);
  private readonly produtoService = inject(ProdutoService);
  private readonly cdr = inject(ChangeDetectorRef);

  private readonly notaId = Number(this.route.snapshot.paramMap.get('id'));
  private readonly notaInicial = this.lerNotaDaNavegacao();

  readonly nota = signal<NotaFiscal | undefined>(undefined);
  readonly itens = signal<ItemDetalhe[]>([]);
  readonly formatarNumeroNota = formatarNumeroNota;

  erro = '';
  carregando = false;
  imprimindo = false;

  ngOnInit(): void {
    void this.carregar();
  }

  async carregar(): Promise<void> {
    this.erro = '';

    const notaPronta =
      this.notaInicial ?? this.notaFiscalService.obterDoCache(this.notaId);

    if (notaPronta) {
      this.aplicarNota(notaPronta);
      this.carregando = false;
      this.cdr.detectChanges();
      await this.atualizarProdutosDosItens();
      return;
    }

    this.carregando = true;
    this.cdr.detectChanges();

    try {
      const [nota] = await Promise.all([
        this.notaFiscalService.buscarPorId(this.notaId),
        this.produtoService.carregar().catch((error: unknown) => {
          this.erro =
            error instanceof Error
              ? error.message
              : 'Não foi possível carregar os produtos da nota.';
        }),
      ]);

      this.aplicarNota(nota);
    } catch (error) {
      this.erro =
        error instanceof Error ? error.message : 'Não foi possível carregar a nota.';
    } finally {
      this.carregando = false;
      this.cdr.detectChanges();
    }
  }

  async imprimir(): Promise<void> {
    this.erro = '';

    const confirmou = window.confirm(
      'Imprimir esta nota fecha o documento e baixa o estoque. Essa ação não pode ser desfeita. Deseja continuar?'
    );

    if (!confirmou) {
      return;
    }

    this.imprimindo = true;
    this.cdr.detectChanges();

    try {
      const nota = await this.notaFiscalService.imprimir(this.notaId);
      this.aplicarNota(nota);
      await this.produtoService.carregar().catch(() => undefined);
      this.aplicarNota(nota);
    } catch (error) {
      this.erro = error instanceof Error ? error.message : 'Não foi possível imprimir a nota.';

      try {
        const nota = await this.notaFiscalService.buscarPorId(this.notaId);
        this.aplicarNota(nota);
      } catch {
        // Mantém a mensagem do erro de impressão.
      }
    } finally {
      this.imprimindo = false;
      this.cdr.detectChanges();
    }
  }

  private async atualizarProdutosDosItens(): Promise<void> {
    try {
      await this.produtoService.carregar();
      this.aplicarNota(this.nota());
    } catch (error) {
      if (!this.erro) {
        this.erro =
          error instanceof Error
            ? error.message
            : 'Não foi possível atualizar os dados dos produtos.';
      }
    }
  }

  private aplicarNota(nota: NotaFiscal | undefined): void {
    this.nota.set(nota);
    this.itens.set(this.montarItens(nota));
  }

  private montarItens(nota: NotaFiscal | undefined): ItemDetalhe[] {
    if (!nota) {
      return [];
    }

    return nota.itens.map((item) => {
      const produto = this.produtoService.buscarPorId(item.produtoId);

      return {
        produtoId: item.produtoId,
        quantidade: item.quantidade,
        codigo: produto?.codigo ?? String(item.produtoId),
        descricao: produto?.descricao ?? 'Produto não encontrado',
      };
    });
  }

  private lerNotaDaNavegacao(): NotaFiscal | undefined {
    const estado = this.router.getCurrentNavigation()?.extras.state
      ?? history.state;

    const nota = estado?.['nota'];

    if (!nota || typeof nota !== 'object') {
      return undefined;
    }

    return nota as NotaFiscal;
  }
}
