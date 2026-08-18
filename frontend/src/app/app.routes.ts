import { Routes } from '@angular/router';
import { Inicio } from './pages/inicio/inicio';
import { NotaDetalhe } from './pages/nota-detalhe/nota-detalhe';
import { NotaNova } from './pages/nota-nova/nota-nova';
import { Notas } from './pages/notas/notas';
import { Produtos } from './pages/produtos/produtos';

export const routes: Routes = [
  { path: '', component: Inicio },
  { path: 'produtos', component: Produtos },
  { path: 'notas', component: Notas },
  { path: 'notas/nova', component: NotaNova },
  { path: 'notas/:id', component: NotaDetalhe },
  { path: '**', redirectTo: '' },
];
