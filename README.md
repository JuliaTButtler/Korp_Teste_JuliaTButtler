# Korp_Teste_JuliaTButtler

Sistema de emissão de notas fiscais desenvolvido para o desafio técnico da Korp.

A solução é composta por um frontend em **Angular** e dois microsserviços em **ASP.NET Core (C#)**, com persistência em **Oracle**.

Meu Linkedin: www.linkedin.com/in/júlia-t-buttler-2b842b367

---

## Tecnologias utilizadas

| Camada | Tecnologia |
|--------|------------|
| Frontend | Angular 21, TypeScript, Signals, FormsModule, CSS próprio |
| Backend | ASP.NET Core 8 (Web API), C# |
| ORM | Entity Framework Core 8 |
| Banco de dados | Oracle Database |
| Comunicação entre serviços | HTTP (`HttpClient` tipado) |
| Documentação das APIs | Swagger / Swashbuckle |
| Empacotamento backend | NuGet (`.csproj`) |
| Empacotamento frontend | npm (`package.json`) |

Não foi utilizado Golang neste projeto. O backend é exclusivamente C# / .NET.

---

## O que é necessário para abrir o projeto

- [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0) (o repositório inclui `global.json` com a versão `8.0.421`)
- [Node.js](https://nodejs.org/) (LTS recomendado) e npm
- Acesso a um **Oracle Database** (local ou remoto) com usuário/senha e string de conexão válidos
- Ferramenta opcional: [EF Core tools](https://learn.microsoft.com/ef/core/cli/dotnet) para aplicar migrations  
  (`dotnet tool install --global dotnet-ef`)
- IDE opcional: Visual Studio, Visual Studio Code ou Rider

Portas padrão usadas no projeto:

| Serviço | URL |
|---------|-----|
| Estoque | `http://localhost:5003` |
| Faturamento | `http://localhost:5051` |
| Angular | `http://localhost:4200` |
| Swagger Estoque | `http://localhost:5003/swagger` |
| Swagger Faturamento | `http://localhost:5051/swagger` |

---

## Configuração do Oracle

Edite a connection string nos arquivos:

- `Estoque/appsettings.json`
- `Faturamento/appsettings.json`

Exemplo:

```json
{
  "ConnectionStrings": {
    "OracleConnection": "User Id=seu_usuario;Password=sua_senha;Data Source=seu_host:1521/SEU_SERVICE;"
  }
}
```

Substitua:

- `seu_usuario` — usuário do Oracle  
- `sua_senha` — senha do Oracle  
- `seu_host` — host/IP do servidor  
- `SEU_SERVICE` — service name / SID 

Os dois microsserviços podem apontar para o mesmo banco Oracle; cada um possui o próprio contexto e tabelas (`PRODUTO` no Estoque; notas/itens no Faturamento).

### Aplicar as migrations (criar/atualizar tabelas)

Com a connection string configurada, execute:

```bash
# Estoque
dotnet ef database update --project Estoque/Estoque.csproj --startup-project Estoque/Estoque.csproj

# Faturamento
dotnet ef database update --project Faturamento/Faturamento.csproj --startup-project Faturamento/Faturamento.csproj
```

---

## Como rodar o programa

Abra **três terminais** (ou use a IDE para subir os dois backends).

### 1. Microsserviço de Estoque

```bash
cd Estoque
dotnet run --launch-profile http
```

Confirme em `http://localhost:5003/swagger`.

### 2. Microsserviço de Faturamento

```bash
cd Faturamento
dotnet run --launch-profile http
```

Confirme em `http://localhost:5051/swagger`.

No `Faturamento/appsettings.json`, a URL do Estoque já está configurada:

```json
"EstoqueApi": {
  "BaseUrl": "http://localhost:5003",
  "TimeoutSeconds": 3
}
```

### 3. Frontend Angular

```bash
cd frontend
npm install
npm start
```

Abra `http://localhost:4200`.

As URLs das APIs usadas pelo frontend estão em `frontend/src/app/config/api.ts` (apontando para as portas acima). CORS está liberado nos backends para `http://localhost:4200`.

**Ordem sugerida:** Estoque → Faturamento → Angular.

---

## Arquitetura e responsabilidades

```text
┌─────────────────┐
│  Angular (UI)   │
│  :4200          │
└────────┬────────┘
         │ HTTP
    ┌────┴────┐
    ▼         ▼
┌────────┐  ┌──────────────┐
│Estoque │◄─│ Faturamento  │
│ :5003  │  │ :5051        │
└───┬────┘  └──────┬───────┘
    │              │
    └──────┬───────┘
           ▼
     Oracle Database
```

### Serviço de Estoque

Responsável pelo **controle de produtos e saldos**.

- Cadastro e listagem de produtos (código, descrição, saldo)
- Campo `Reservado` (quantidade comprometida em notas ainda abertas)
- Disponibilidade efetiva: `saldo - reservado`
- Movimentos internos usados pelo Faturamento:
  - `reservar` / `liberar-reserva`
  - `baixa` / `estornar-baixa`
  - `entrada` (reforço de estoque pela UI)

### Serviço de Faturamento

Responsável pela **gestão de notas fiscais**.

- Criação de notas com numeração sequencial e status inicial `ABERTA`
- Inclusão de múltiplos itens (produto + quantidade)
- Impressão: altera status para `FECHADA` e solicita baixa no Estoque
- Orquestra chamadas HTTP ao Estoque via `EstoqueClient`
- Compensa reservas/baixas em caso de falha parcial

### Frontend Angular

- Telas: início, produtos, lista de notas, nova nota, detalhe/impressão
- Validação de formulários e feedback de erro ao usuário
- Indicador de processamento na impressão
- Detecção de indisponibilidade dos microsserviços (timeout / rede / HTTP 503)

---

## Fluxos principais

### Criar nota fiscal

1. Usuário seleciona produtos e quantidades na tela **Nova nota**.
2. Frontend envia `POST /api/NotaFiscal` ao Faturamento.
3. Faturamento valida os itens.
4. Para cada item, chama o Estoque (`reservar`).
5. Persiste a nota com número sequencial (`ultimoNumero + 1`) e status `ABERTA`.
6. Se alguma etapa falhar após reservas parciais, executa **compensação** (`liberar-reserva`) na ordem inversa.

### Imprimir nota fiscal

1. Usuário abre o detalhe de uma nota `ABERTA` e clica em **Imprimir**.
2. Frontend exibe indicador de processamento (`Imprimindo...`).
3. Faturamento confere disponibilidade no Estoque.
4. Atualiza o status para `FECHADA` de forma condicional (somente se ainda estiver `ABERTA`).
5. Para cada item, chama `baixa` no Estoque (diminui `saldo` e `reservado`).
6. Se a baixa falhar no meio do caminho:
   - estorna as baixas já feitas;
   - reabre a nota para `ABERTA`;
   - propaga o erro para a UI.

Exemplo de saldo: saldo anterior = 10; nota utiliza 2 → novo saldo = 8.

Notas com status diferente de `ABERTA` **não podem** ser impressas (backend e frontend).

---

## Tratamento de falhas e exceções (backend)

Padrão utilizado nos controllers:

| Situação | Exceção / condição | HTTP |
|----------|--------------------|------|
| Validação de entrada | `ArgumentException` | 400 Bad Request |
| Regra de negócio (saldo, status, código duplicado) | `InvalidOperationException` | 409 Conflict |
| Estoque fora do ar / timeout | mensagem contendo `indisponível` | 503 Service Unavailable |
| Recurso inexistente | retorno `null` do service | 404 Not Found |
| Falha de persistência | `DbUpdateException` | 409 Conflict (mensagem amigável) |

No Estoque, movimentos de estoque usam `UPDATE` condicional no banco. Se nenhuma linha for afetada, o serviço interpreta saldo insuficiente ou produto inexistente.

No Faturamento, o `EstoqueClient` trata falhas de rede (`HttpRequestException`, timeout, cancelamento) e converte em *“Serviço de estoque indisponível.”*

No frontend (`api-error.ts`):

- timeout obrigatório nas chamadas;
- mensagens claras por serviço (`estoque` / `faturamento`);
- formulários podem ser bloqueados quando o serviço está indisponível.

### Como demonstrar a falha de um microsserviço

1. Suba Estoque, Faturamento e Angular normalmente.
2. Cadastre um produto e confirme que criar nota funciona.
3. **Pare o serviço de Estoque** (encerre o processo / `Ctrl+C` no terminal do Estoque).
4. Tente criar uma nova nota ou imprimir uma nota aberta.
5. Observe o feedback na tela (ex.: *Serviço de estoque indisponível.*) e/ou resposta HTTP 503 no Faturamento.
6. **Suba o Estoque novamente** e repita a operação: o sistema volta a funcionar sem alteração de código.

Isso atende ao requisito de recuperação da falha com feedback apropriado ao usuário.

---

## Detalhamento técnico (itens do enunciado)

### Ciclos de vida do Angular utilizados

Foi utilizado principalmente o ciclo **`OnInit` / `ngOnInit`** nas páginas:

- `Produtos`
- `Notas`
- `NotaNova`
- `NotaDetalhe`

No `ngOnInit`, as telas disparam o carregamento inicial dos dados (produtos e/ou notas).

Também foi utilizado o modelo reativo moderno do Angular com **Signals** (`signal` / `computed`) para estado de listas e disponibilidade dos serviços.

### Uso de RxJS

A biblioteca **RxJS** está presente no projeto como dependência do Angular (`package.json`).

No código da aplicação, as chamadas HTTP **não** foram feitas com `HttpClient` + `Observable`. Em vez disso, o frontend usa **`fetch` + `async/await`** (utilitário `api-error.ts`) e estado com **Signals**.

Resumo honesto para avaliação: RxJS está disponível via Angular, mas o fluxo de dados da aplicação foi implementado com Promises/async e Signals.

### Outras bibliotecas (frontend) e finalidade

| Biblioteca | Finalidade |
|------------|------------|
| `@angular/core`, `common`, `compiler`, `platform-browser` | Runtime e base da aplicação |
| `@angular/router` | Rotas (`/`, `/produtos`, `/notas`, `/notas/nova`, `/notas/:id`) |
| `@angular/forms` (`FormsModule`) | Formulários template-driven |
| `rxjs` | Dependência do Angular (sem Observables explícitos no app) |
| `tslib` | Helpers TypeScript |
| `vitest` / `jsdom` (dev) | Testes unitários do scaffold Angular |
| `prettier` (dev) | Formatação |

### Bibliotecas para componentes visuais

**Não** foram utilizadas bibliotecas de UI (Angular Material, PrimeNG, Bootstrap etc.).  
Os componentes visuais são **HTML + CSS próprios** dos templates Angular (botões, tabelas, badges de status, alertas).

### Gerenciamento de dependências no Golang

**Não aplicável** — não há serviços em Go.

### Frameworks utilizados no C#

- **ASP.NET Core 8** (Web API)
- **Entity Framework Core 8**
- **Oracle.EntityFrameworkCore**
- **Swashbuckle.AspNetCore** (Swagger)
- **Microsoft.AspNetCore.OpenApi**

Dependências gerenciadas via **NuGet** nos arquivos `Estoque.csproj` e `Faturamento.csproj`.

### Como foram tratados erros e exceções no backend

Ver seção [Tratamento de falhas e exceções (backend)](#tratamento-de-falhas-e-exceções-backend).

Em resumo: exceções tipadas no service → mapeamento HTTP nos controllers → JSON com campo `mensagem` → frontend exibe o texto ao usuário. Há compensação (saga simples) entre Faturamento e Estoque para não deixar reserva/baixa inconsistente.

### Uso de LINQ (C#)

Sim. Exemplos de uso:

- Listagens com `Include`, `AsNoTracking`, `OrderByDescending`, `ToListAsync`
- Busca com `FirstOrDefaultAsync`
- Numeração sequencial com `MaxAsync`
- Validação de itens duplicados com `GroupBy`
- Impressão com `Where(... Status == ABERTA).ExecuteUpdateAsync(...)` (atualização condicional)
- Compensação com `Enumerable.Reverse`

---

## Requisitos opcionais

| Opcional | Status | Observação |
|----------|--------|------------|
| Tratamento de concorrência | **Implementado** | Updates condicionais no Oracle (`WHERE saldo - reservado >= quantidade`, baixa com `saldo`/`reservado` suficientes) e fechamento atômico da nota (`Status == ABERTA`). Se duas notas competem pelo último saldo, apenas uma reserva/baixa com sucesso. |
| Uso de Inteligência Artificial | **Não implementado** | — |
| Idempotência | **Parcial** | Reimprimir nota já `FECHADA` é rejeitado (não baixa estoque de novo). Não há chave de idempotência explícita na criação de nota. |

---

## Estrutura do repositório

```text
Korp_Teste_JuliaTButtler/
├── Estoque/                 # Microsserviço de produtos e saldos
├── Faturamento/             # Microsserviço de notas fiscais
├── frontend/                # Aplicação Angular
├── global.json              # Versão do SDK .NET
└── README.md                # Este documento
```

Documentação adicional do scaffold Angular: ver `frontend/README.md` (aponta de volta para este arquivo).

---

## Funcionalidades (resumo para demonstração)

1. **Cadastro de produtos** — código, descrição e saldo obrigatórios; persistência no Oracle.
2. **Cadastro de notas fiscais** — numeração sequencial, status inicial `ABERTA`, múltiplos itens.
3. **Impressão** — botão intuitivo, indicador de processamento, fecha a nota, baixa estoque; bloqueia notas que não estão `ABERTA`.
4. **Entrada de estoque** — reforço de saldo pela tela de produtos.
5. **Resiliência** — falha do Estoque tratada com mensagem clara e retomada após o serviço voltar.

---

