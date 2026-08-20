import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NotaFiscalService } from '../../services/nota-fiscal.service';
import { formatarNumeroNota } from '../../utils/formatacao';

@Component({
  selector: 'app-notas',
  imports: [RouterLink, DatePipe],
  templateUrl: './notas.html',
  styleUrl: './notas.css',
})
export class Notas implements OnInit {
  private readonly notaFiscalService = inject(NotaFiscalService);

  readonly notas = computed(() => this.notaFiscalService.listarTodos());
  readonly formatarNumeroNota = formatarNumeroNota;

  erro = '';
  carregando = false;

  ngOnInit(): void {
    void this.carregar();
  }

  async carregar(): Promise<void> {
    this.erro = '';
    this.carregando = true;

    try {
      await this.notaFiscalService.carregar();
    } catch (error) {
      this.erro =
        error instanceof Error
          ? error.message
          : 'Não foi possível carregar as notas fiscais.';
    } finally {
      this.carregando = false;
    }
  }
}
