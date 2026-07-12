# PagueVeloz — Sistema de Processamento de Transações Financeiras

Núcleo transacional de uma plataforma de adquirência, implementado em **C# .NET 9**, seguindo Clean Architecture, CQRS e um pipeline de eventos assíncronos baseado no padrão Transactional Outbox.

## Sumário

- [Decisões técnicas e arquiteturais](#decisões-técnicas-e-arquiteturais)
- [Frameworks e bibliotecas — justificativa](#frameworks-e-bibliotecas--justificativa)
- [Compilação e execução](#compilação-e-execução)
- [Execução dos testes](#execução-dos-testes)
- [Exemplos de uso da API](#exemplos-de-uso-da-api)
- [Limitações conhecidas](#limitações-conhecidas)

---

## Decisões técnicas e arquiteturais

### Clean Architecture em 5 projetos

```
src/
  PagueVeloz.Domain          → regras de negócio puras, sem dependência externa nenhuma
  PagueVeloz.Application     → casos de uso (CQRS/MediatR), orquestração
  PagueVeloz.Infrastructure  → EF Core, Outbox, interceptors, observabilidade
  PagueVeloz.API             → controllers, middlewares, composição da aplicação
workers/
  PagueVeloz.Worker          → processo em background, consome a fila de eventos
```

A regra de dependência é sempre "de fora para dentro": `API`/`Worker` dependem de `Application`, que depende de `Domain`; `Infrastructure` implementa contratos definidos no `Domain`. Isso mantém a regra de negócio testável sem precisar de banco, HTTP ou qualquer infraestrutura — e permite, no futuro, extrair `Application` + `Domain` para um serviço independente sem reescrever a lógica.

### Modelagem do domínio: `Account` como agregado raiz

`Account` concentra toda a regra financeira (`Credit`, `Debit`, `Reserve`, `Capture`, `Reversal`) e nunca lança exceção para violações de regra de negócio (saldo insuficiente, conta bloqueada, etc.). Em vez disso, cada operação sempre retorna um `AccountOperation` com `Status: Success` ou `Status: Failed`, e a tentativa é **sempre** persistida no histórico — inclusive as que falham. Essa decisão reflete diretamente o exemplo do próprio desafio, em que uma tentativa de débito com saldo insuficiente também é reportada como uma transação processada (`status: failed`), não como um erro de sistema.

Exceções continuam existindo apenas para situações que **não** são resultado de negócio (`NotFoundException` quando a conta não existe, `ConcurrencyConflictException` em conflito de concorrência) — tratadas globalmente por um middleware, não espalhadas em `try/catch` por controller.

### Transferência como orquestração, não como método do agregado

`Transfer` nunca foi modelado dentro de `Account`, porque mexe em **dois** agregados diferentes — uma Entity não deveria conhecer/manipular outra instância da mesma classe. A transferência é orquestrada no `TransferCommandHandler`: debita a origem, credita o destino, e persiste as duas mudanças num único `SaveChangesAsync()`. Se o débito falhar, o crédito nunca é sequer tentado — a checagem de `Status` acontece explicitamente entre as duas chamadas.

### Idempotência dentro do próprio agregado

Cada operação recebe um `reference_id` obrigatório. Antes de aplicar qualquer mudança, `Account` verifica se aquele `reference_id` já existe no seu próprio histórico — se existir, devolve o resultado já processado, sem reaplicar o efeito. Um índice único `(AccountId, ReferenceId)` no banco garante essa mesma proteção mesmo sob concorrência real, quando duas requisições passam pela checagem em memória ao mesmo tempo.

### Controle de concorrência otimista via `xmin`

Em vez de uma coluna `RowVersion` dedicada (padrão do SQL Server), usamos a coluna de sistema `xmin` nativa do PostgreSQL como token de concorrência otimista — o Postgres já incrementa esse valor a cada `UPDATE`, sem custo de schema adicional. Quando duas requisições tentam alterar a mesma conta simultaneamente, a segunda falha com `DbUpdateConcurrencyException`, que o `UnitOfWork` traduz para uma `ConcurrencyConflictException` de aplicação (mantendo o `Infrastructure`/EF Core como um detalhe invisível para a API) — devolvida ao cliente como `409 Conflict`.

### Eventos assíncronos: Domain Events + Transactional Outbox

Cada operação bem-sucedida levanta um Domain Event (`AccountCreditedEvent`, `AccountDebitedEvent`, etc.). Um `SaveChangesInterceptor` do EF Core (`DomainEventInterceptor`) intercepta automaticamente esses eventos em qualquer entidade rastreada e os converte em linhas na tabela `OutboxMessages`, **dentro da mesma transação** que persiste a operação de negócio — garantindo atomicidade entre "a operação aconteceu" e "existe um evento pendente de publicação" sem precisar de duas fases de commit.

Um processo separado (`PagueVeloz.Worker`, um `BackgroundService`) faz polling dessa tabela a cada 5 segundos, processa as mensagens pendentes e marca como concluídas — implementando consistência eventual sem acoplar o fluxo síncrono da API à disponibilidade de um broker externo.

### Rastreamento de transações via Correlation ID

Todo request HTTP recebe (ou propaga, se já vier no header `X-Correlation-Id`) um identificador único, guardado num `AsyncLocal<string?>` (via `ICorrelationIdProvider`) — isso permite que o valor "flua" naturalmente por toda a cadeia de chamadas assíncronas dentro daquele request, sem precisar passar `HttpContext` explicitamente para o `Infrastructure`. O `DomainEventInterceptor` grava esse Correlation ID junto de cada `OutboxMessage`, e o Worker o reinsere no contexto de log do Serilog ao processar a mensagem — permitindo rastrear, pelos logs, a jornada completa de uma transação: requisição HTTP → operação de domínio → evento gravado → evento publicado, minutos depois, em outro processo.

### Contrato de API alinhado ao especificado no desafio

- Campos JSON em `snake_case` (`account_id`, `reference_id`, `available_balance`...), configurado globalmente via `JsonNamingPolicy.SnakeCaseLower` — ainda que `camelCase` também seja comum no mercado, o enunciado do desafio especifica `snake_case` explicitamente nos exemplos, e essa especificação tem prioridade sobre convenção geral de mercado.
- `amount` trafega como **inteiro em centavos** (`long`), evitando qualquer ambiguidade de arredondamento de ponto flutuante — a conversão para valores "de exibição" fica só na borda (DTOs), nunca dentro do `Domain`.

---

## Frameworks e bibliotecas — justificativa

| Biblioteca | Por que foi escolhida |
|---|---|
| **MediatR** | Implementa CQRS sem acoplar o Controller à lógica de aplicação — cada operação vira um `Command`/`Query` isolado, testável sem precisar instanciar toda a cadeia de dependências manualmente. |
| **FluentValidation** | Validação de entrada declarativa, plugada como pipeline behavior do MediatR — roda automaticamente antes de qualquer Handler, sem `if` de validação espalhado pelo código. |
| **Entity Framework Core + Npgsql** | ORM com suporte a PostgreSQL (incluindo `xmin` como token de concorrência) e a `SaveChangesInterceptor`, peça central do padrão Outbox implementado aqui. |
| **PostgreSQL** | Escolhido em vez de SQL Server por dois motivos: (1) MVCC nativo do Postgres reduz contenção de lock em cenários de alta concorrência de escrita — justamente o cenário que o desafio descreve ("alto volume de transações"); (2) é o padrão de facto em fintechs de alto volume, sem custo de licenciamento por núcleo. |
| **Serilog** | Logging estruturado (JSON), com enrichers de Correlation ID — essencial para rastrear transações através da fronteira síncrona/assíncrona (API → Outbox → Worker). |
| **xUnit + FluentAssertions + Moq** | Padrão de mercado para testes em .NET; `FluentAssertions` deixa as asserções mais legíveis (`x.Should().Be(...)`), `Moq` isola os Handlers de Application dos repositórios reais nos testes unitários. |
| **Testcontainers.PostgreSql** | Testes de integração rodam contra um Postgres real, em container descartável — evita o risco de "passa no `InMemoryDatabase` mas quebra em produção" por causa de comportamento específico do provider (ex: `xmin`, tipos `jsonb`). |
| **Docker Compose** | Sobe API, Worker e Postgres com um único comando, sem exigir instalação manual de banco na máquina de quem for avaliar o projeto. |

---

## Compilação e execução

### Pré-requisitos

- .NET 9 SDK
- Docker e Docker Compose

### Passo a passo (via Docker — recomendado)

```bash
# 1. Clonar o repositório
git clone <url-do-repositorio>
cd PagueVeloz.TransactionEngine

# 2. Configurar variáveis de ambiente
cp .env.example .env
# edite o .env se quiser trocar usuário/senha/nome do banco (opcional)

# 3. Subir tudo (Postgres + API + Worker)
docker compose up --build
```

A API sobe em `http://localhost:5000` (ajuste conforme a porta mapeada no seu `docker-compose.yml`), com Swagger disponível em `http://localhost:5000/swagger`.

### Passo a passo (local, sem Docker)

```bash
# 1. Sobe só o banco
docker compose up -d postgres

# 2. Restaura dependências e compila
dotnet build

# 3. Aplica as migrations
dotnet ef database update --project src/PagueVeloz.Infrastructure --startup-project src/PagueVeloz.API

# 4. Roda a API
dotnet run --project src/PagueVeloz.API

# 5. Em outro terminal, roda o Worker
dotnet run --project workers/PagueVeloz.Worker
```

---

## Execução dos testes

```bash
# Todos os testes (unitários + integração)
dotnet test

# Só Domain (regras de negócio puras, sem dependência nenhuma)
dotnet test tests/PagueVeloz.Domain.Tests

# Só Application (Handlers, com repositórios mockados)
dotnet test tests/PagueVeloz.Application.Tests

# Só Integração (sobe um Postgres real via Testcontainers — exige Docker rodando)
dotnet test tests/PagueVeloz.IntegrationTests
```

**Cobertura:**
- `Domain.Tests` — todas as operações do `Account` (`Credit`, `Debit`, `Reserve`, `Capture`, `Reversal`), incluindo os cenários exatos das tabelas do desafio (saldo insuficiente, uso de limite de crédito, reserva+captura) e idempotência via `reference_id`.
- `Application.Tests` — cada `CommandHandler`, cobrindo caminho feliz, conta não encontrada, e falha de regra de negócio propagada como `status: failed` (não como exceção).
- `IntegrationTests` — fluxo HTTP completo, ponta a ponta, contra um banco Postgres real.

---

## Exemplos de uso da API

### 1. Criar um cliente

```bash
curl -X POST http://localhost:5000/api/customer \
  -H "Content-Type: application/json" \
  -d '{
    "name": "João Silva",
    "document": "123.456.789-00"
  }'
```

### 2. Abrir uma conta

```bash
curl -X POST http://localhost:5000/api/account \
  -H "Content-Type: application/json" \
  -d '{
    "customer_id": "<id-do-cliente>",
    "credit_limit": 50000
  }'
```

### 3. Executar uma transação — crédito

```bash
curl -X POST http://localhost:5000/api/account/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "operation": "credit",
    "account_id": "<id-da-conta>",
    "amount": 100000,
    "currency": "BRL",
    "reference_id": "TXN-001",
    "metadata": { "description": "Depósito inicial" }
  }'
```

**Resposta:**
```json
{
  "transaction_id": "b3f1...",
  "status": "success",
  "balance": 100000,
  "reserved_balance": 0,
  "available_balance": 100000,
  "timestamp": "2026-07-12T18:05:00Z",
  "error_message": null
}
```

### 4. Débito com saldo insuficiente

```bash
curl -X POST http://localhost:5000/api/account/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "operation": "debit",
    "account_id": "<id-da-conta>",
    "amount": 900000,
    "currency": "BRL",
    "reference_id": "TXN-002"
  }'
```

**Resposta (`400 Bad Request`, saldo permanece inalterado):**
```json
{
  "transaction_id": "a91c...",
  "status": "failed",
  "balance": 100000,
  "reserved_balance": 0,
  "available_balance": 100000,
  "timestamp": "2026-07-12T18:06:00Z",
  "error_message": "Insufficient funds to complete the debit."
}
```

### 5. Reserva seguida de captura

```bash
curl -X POST http://localhost:5000/api/account/transactions \
  -H "Content-Type: application/json" \
  -d '{"operation":"reserve","account_id":"<id>","amount":30000,"currency":"BRL","reference_id":"TXN-003"}'

# copia o transaction_id retornado acima e usa como reserve_operation_id
curl -X POST http://localhost:5000/api/account/transactions \
  -H "Content-Type: application/json" \
  -d '{"operation":"capture","account_id":"<id>","reserve_operation_id":"<transaction_id-da-reserva>","currency":"BRL","reference_id":"TXN-004"}'
```

### 6. Transferência entre contas

```bash
curl -X POST http://localhost:5000/api/account/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "operation": "transfer",
    "account_id": "<id-conta-origem>",
    "destination_account_id": "<id-conta-destino>",
    "amount": 50000,
    "currency": "BRL",
    "reference_id": "TXN-005"
  }'
```

### 7. Estorno de uma operação

```bash
curl -X POST http://localhost:5000/api/account/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "operation": "reversal",
    "account_id": "<id-da-conta>",
    "original_operation_id": "<transaction_id-da-operação-original>",
    "currency": "BRL",
    "reference_id": "TXN-006"
  }'
```

### 8. Idempotência — reenviar o mesmo `reference_id`

Repetir exatamente a chamada do exemplo 3, com o mesmo `reference_id: "TXN-001"`, devolve o **mesmo** `transaction_id` e `timestamp` da primeira vez — o saldo não é creditado duas vezes.

### 9. Consultar uma conta (saldo + histórico)

```bash
curl http://localhost:5000/api/account/<id>
```

### 10. Health check

```bash
curl http://localhost:5000/health
```

---

## Limitações conhecidas

Documentadas aqui de forma consciente, não por esquecimento:

- **Retry com backoff exponencial**: o `OutboxMessage` já modela `Attempts`/`NextAttemptAt` com backoff exponencial, e o `OutboxProcessorWorker` já possui o bloco `try/catch` para acioná-lo — mas hoje a "publicação" do evento é simulada (log estruturado), sem um ponto de falha real, então esse caminho de retry não é exercitado em condições normais de uso.
- **Fallback strategies / Circuit breaker**: não implementados nesta versão.
- **Métricas de performance** (contadores, latência, throughput): não implementadas; a observabilidade atual cobre logs estruturados, Correlation ID e health checks.
- **Publicação de eventos em um broker real** (RabbitMQ/Kafka): o Outbox está pronto para plugar um publisher real — hoje a implementação simula a publicação via log, mantendo a garantia de atomicidade e consistência eventual sem depender de infraestrutura externa adicional para a avaliação deste desafio.