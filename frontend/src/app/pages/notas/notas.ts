import { DatePipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NotaFiscalService } from '../../services/nota-fiscal.service';
import { formatarNumeroNota } from '../../utils/formatacao';

@Component({
  selector: 'app-notas',
  imports: [RouterLink, DatePipe],
  templateUrl: './notas.html',
  styleUrl: './notas.css',
})
export class Notas {
  private readonly notaFiscalService = inject(NotaFiscalService);

  readonly notas = computed(() => this.notaFiscalService.listarTodos());
  readonly formatarNumeroNota = formatarNumeroNota;
}
