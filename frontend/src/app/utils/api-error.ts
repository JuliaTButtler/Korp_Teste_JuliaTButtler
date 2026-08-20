export type ServicoApi = 'estoque' | 'faturamento';

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export function mensagemIndisponivel(servico?: ServicoApi): string {
  if (servico === 'faturamento') {
    return 'Serviço de faturamento indisponível.';
  }

  if (servico === 'estoque') {
    return 'Serviço de estoque indisponível.';
  }

  return 'Serviço indisponível. Verifique se os microsserviços estão em execução.';
}

export async function apiJson<T>(
  url: string,
  options: {
    method?: string;
    body?: unknown;
    timeoutMs?: number;
    servico: ServicoApi;
    fallback: string;
  }
): Promise<T> {
  const timeoutMs = options.timeoutMs ?? 5000;
  const method = options.method ?? 'GET';
  const controller = new AbortController();

  const trabalho = (async () => {
    const response = await fetch(url, {
      method,
      headers: {
        Accept: 'application/json',
        ...(options.body !== undefined
          ? { 'Content-Type': 'application/json' }
          : {}),
      },
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
      signal: controller.signal,
    });

    if (!response.ok) {
      let corpo: unknown = null;

      try {
        corpo = await response.json();
      } catch {
        corpo = null;
      }

      const mensagem =
        extrairMensagem(corpo) ??
        (response.status === 503
          ? 'Serviço de estoque indisponível.'
          : response.status === 0
            ? mensagemIndisponivel(options.servico)
            : options.fallback);

      throw new ApiError(mensagem, response.status);
    }

    if (response.status === 204) {
      return undefined as T;
    }

    const texto = await response.text();

    if (!texto.trim()) {
      return undefined as T;
    }

    return JSON.parse(texto) as T;
  })();

  try {
    return await comTimeoutObrigatorio(
      trabalho,
      timeoutMs,
      () => {
        controller.abort();
        return new ApiError(mensagemIndisponivel(options.servico), 0);
      }
    );
  } catch (error) {
    if (error instanceof ApiError) {
      throw error;
    }

    if (ehFalhaDeRedeOuTimeout(error)) {
      throw new ApiError(mensagemIndisponivel(options.servico), 0);
    }

    throw new ApiError(
      error instanceof Error && error.message.trim()
        ? error.message
        : options.fallback,
      0
    );
  }
}

function comTimeoutObrigatorio<T>(
  promise: Promise<T>,
  timeoutMs: number,
  aoEstourar: () => Error
): Promise<T> {
  return new Promise<T>((resolve, reject) => {
    const id = setTimeout(() => {
      reject(aoEstourar());
    }, timeoutMs);

    promise.then(
      (valor) => {
        clearTimeout(id);
        resolve(valor);
      },
      (erro) => {
        clearTimeout(id);
        reject(erro);
      }
    );
  });
}

function ehFalhaDeRedeOuTimeout(error: unknown): boolean {
  if (!error || typeof error !== 'object') {
    return false;
  }

  const nome = 'name' in error ? String((error as { name?: unknown }).name) : '';
  const mensagem =
    'message' in error
      ? String((error as { message?: unknown }).message).toLowerCase()
      : '';

  return (
    nome === 'TimeoutError' ||
    nome === 'AbortError' ||
    mensagem.includes('timeout') ||
    mensagem.includes('aborted') ||
    mensagem.includes('failed to fetch') ||
    mensagem.includes('network') ||
    mensagem.includes('load failed')
  );
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
