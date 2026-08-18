export function formatarNumeroNota(numero: number): string {
  return numero.toString().padStart(6, '0');
}
