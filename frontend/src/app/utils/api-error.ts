import { HttpErrorResponse } from '@angular/common/http';

export function mensagemErroHttp(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    const mensagem = extrairMensagem(error.error);

    if (mensagem) {
      return mensagem;
    }

    if (error.status === 409) {
      return 'A operação conflitou com o estado atual do recurso.';
    }

    if (error.status === 0) {
      return 'Não foi possível conectar ao servidor. Verifique se os microsserviços estão em execução.';
    }

    if (error.status === 503) {
      return 'Serviço de estoque indisponível.';
    }
  }

  if (
    error &&
    typeof error === 'object' &&
    'name' in error &&
    (error as { name?: string }).name === 'TimeoutError'
  ) {
    return 'A requisição demorou demais. Tente novamente.';
  }

  if (error instanceof Error && error.message) {
    return error.message;
  }

  return fallback;
}

function extrairMensagem(corpo: unknown): string | null {
  if (!corpo) {
    return null;
  }

  if (typeof corpo === 'string') {
    try {
      return extrairMensagem(JSON.parse(corpo));
    } catch {
      return corpo.trim() || null;
    }
  }

  if (typeof corpo === 'object' && 'mensagem' in corpo) {
    const mensagem = (corpo as { mensagem: unknown }).mensagem;
    return typeof mensagem === 'string' && mensagem.trim() ? mensagem : null;
  }

  return null;
}
