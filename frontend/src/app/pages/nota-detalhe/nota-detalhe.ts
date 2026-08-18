import { DatePipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { NotaFiscalService } from '../../services/nota-fiscal.service';
import { ProdutoService } from '../../services/produto.service';
import { formatarNumeroNota } from '../../utils/formatacao';

@Component({
  selector: 'app-nota-detalhe',
  imports: [DatePipe, RouterLink],
  templateUrl: './nota-detalhe.html',
  styleUrl: './nota-detalhe.css',
})
export class NotaDetalhe {
  private readonly route = inject(ActivatedRoute);
  private readonly notaFiscalService = inject(NotaFiscalService);
  private readonly produtoService = inject(ProdutoService);

  private readonly notaId = Number(this.route.snapshot.paramMap.get('id'));

  readonly nota = computed(() => this.notaFiscalService.buscarPorId(this.notaId));
  readonly formatarNumeroNota = formatarNumeroNota;
  erro = '';

  readonly itens = computed(() => {
    const nota = this.nota();

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
  });

  imprimir(): void {
    this.erro = '';

    const confirmou = window.confirm(
      'Imprimir esta nota fecha o documento e baixa o estoque. Essa ação não pode ser desfeita. Deseja continuar?'
    );

    if (!confirmou) {
      return;
    }

    try {
      this.notaFiscalService.imprimir(this.notaId);
    } catch (error) {
      this.erro = error instanceof Error ? error.message : 'Não foi possível imprimir a nota.';
    }
  }
}
