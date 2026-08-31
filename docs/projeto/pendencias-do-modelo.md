# Pendências do modelo de dados

Campos e entidades que as user stories pedem e que ainda não estão no modelo.

Cada item traz **o que a user story pede**, **o que existe hoje** e uma
**sugestão** — que é ponto de partida para a discussão, não decisão tomada.

> **Por que agora:** o banco ainda está vazio. Toda mudança aqui é uma migration
> simples. Depois que o sistema começar a registrar dados reais, cada uma delas
> passa a exigir script de migração e conversão.

---

## 1. `Movimentacao` — status e datas de pagamento

**O que a US03 pede:**

- Data de Inclusão e Data de Atualização automáticas
- Data de Vencimento, de preenchimento opcional
- Campo de controle de **Status: Pendente ou Pago/Recebido**
- Data de Pagamento/Recebimento, que se torna obrigatória quando o status é Pago

**O que existe hoje:**

```csharp
public DateTime Data { get; set; } = DateTime.UtcNow;
public bool Estornada { get; set; }
```

`DataCriacao` e `DataAtualizacao` vêm de `EntidadeBase` e cobrem as duas
primeiras exigências. Faltam o status e as outras duas datas.

`Estornada` resolve o rollback de uma operação, que é um caso diferente de um
lançamento que ainda não foi pago.

**Por que isso trava a US05:**

A US05 define dois cálculos distintos:

- **Saldo atual** — soma as entradas e subtrai as saídas *"cujo Status seja
  estritamente Pago/Recebido"*
- **Previsão futura** — consolida o saldo atual com as movimentações *"cujo
  Status seja Pendente, utilizando a Data de Vencimento para agrupar as
  previsões por mês"*

Sem um campo que separe pago de pendente, e sem a data de vencimento para
agrupar, não há como implementar nenhum dos dois. E os cards da US11 (Total a
Receber e Total a Pagar no mês) dependem do mesmo dado.

**Sugestão:**

```csharp
public StatusMovimentacao Status { get; set; } = StatusMovimentacao.Pendente;
public DateTime? DataVencimento { get; set; }
public DateTime? DataPagamento { get; set; }
```

```csharp
public enum StatusMovimentacao
{
    Pendente = 1,
    Pago = 2
}
```

Com validação na API: se `Status == Pago`, então `DataPagamento` é obrigatória.

---

## 2. `Movimentacao` — quantidade de litros

**O que a US09 pede:** na categoria de Combustível, um campo para a quantidade
em litros abastecida, *"para enriquecer os cálculos futuros"*.

**O que existe hoje:** o campo não existe.

**Por que importa:** a US10 define a média de consumo como *"(Quilometragem
Rodada) dividido pela (Soma de Litros de Combustível vinculados à viagem)"*. Sem
o campo, esse indicador não tem como ser calculado.

**Sugestão:**

```csharp
public decimal? QuantidadeLitros { get; set; }
```

Opcional, preenchido apenas em lançamentos de combustível.

---

## 3. `Divida` — sentido do cálculo e campos da US04

**O que a US04 pede** que a tela solicite:

- Descrição
- **Quantidade de Parcelas**
- **Valor da Parcela**
- Data de Vencimento da 1ª Parcela
- Categoria correspondente

E que exista *"um campo de leitura que calcula automaticamente o **Soma Total**
(Quantidade de Parcelas × Valor da Parcela)"*, mais um campo opcional de
**Valor de Quitação Antecipada**.

**O que existe hoje:**

```csharp
public decimal ValorTotal { get; set; }                    // informado
public decimal ValorParcela => ValorTotal / NumeroParcelas; // calculado
```

O sentido está invertido em relação à user story: hoje o total é informado e a
parcela é derivada; a US04 descreve o oposto.

Também não existem `ValorQuitacaoAntecipada` nem o vínculo com `Categoria`.

**Sugestão:**

```csharp
public decimal ValorParcela { get; set; }                       // informado
public decimal SomaTotal => ValorParcela * NumeroParcelas;      // calculado
public decimal? ValorQuitacaoAntecipada { get; set; }
public Guid CategoriaId { get; set; }
public Categoria? Categoria { get; set; }
```

**Ponto relacionado:** a US04 também define a regra de automação — ao salvar a
dívida, o sistema deve inserir N movimentações pendentes, com vencimentos
incrementados mês a mês, dentro de uma transação com rollback. Essa regra ainda
não foi implementada.

---

## 4. Registro de manutenção (US06-b)

**O que a US06-b pede:** uma tela para registrar manutenções, informando
Veículo, Data, Descrição do serviço e **Valor Total**. Ao salvar, o sistema deve
alterar o status do veículo para "Em Manutenção" e **gerar automaticamente uma
movimentação de saída** na categoria Manutenção.

**O que existe hoje:** dois campos em `Veiculo`:

```csharp
public DateTime? UltimaManutencao { get; set; }
public string? ObservacoesManutencao { get; set; }
```

Guardam a última manutenção, mas sem valor e sem histórico — não é possível
listar manutenções anteriores nem gerar a despesa correspondente.

**Sugestão:** uma entidade própria.

```csharp
public class Manutencao : EntidadeBase
{
    public Guid VeiculoId { get; set; }
    public Veiculo? Veiculo { get; set; }
    public DateTime Data { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
    public Guid? MovimentacaoId { get; set; }   // despesa gerada automaticamente
}
```

---

## 5. `Veiculo` — capacidade de carga

**O que a US06 pede** no formulário de cadastro: Placa, Marca, Modelo, Ano de
Fabricação e **Capacidade de Carga** (ex.: toneladas).

**O que existe hoje:** todos os campos, menos a capacidade de carga.

**Sugestão:**

```csharp
public decimal CapacidadeCargaToneladas { get; set; }
```

---

## 6. `Viagem` — quilometragem rodada persistida

**O que a US08 pede:** *"o sistema deve calcular e **salvar** automaticamente a
Quilometragem Rodada (Hodômetro Final − Hodômetro Inicial)"*.

**O que existe hoje:**

```csharp
public decimal? DistanciaPercorridaKm =>
    HodometroFinal.HasValue ? HodometroFinal.Value - HodometroInicial : null;
```

O cálculo está correto, mas como propriedade derivada em memória — não vai para
o banco.

**Por que considerar persistir:** os relatórios da US10 agregam quilometragem
por rota e por veículo. Uma coluna real pode ser indexada e somada em consulta;
uma propriedade calculada obriga a trazer os registros para a memória antes.

**Sugestão:** manter a propriedade para uso imediato e gravar o valor numa
coluna no momento em que a viagem é concluída.

---

## 7. `Usuario` — situação com três estados

**O que a US16 pede:** *"quando um novo usuário faz o login pela primeira vez,
ele deve ficar com status Aguardando Aprovação"*, e o administrador então libera
o acesso e define o perfil.

**O que existe hoje:**

```csharp
public bool Ativo { get; set; } = true;
```

Um booleano representa dois estados. A user story descreve três: aguardando
aprovação, ativo e inativo.

Como consequência, no `AuthController` o usuário criado no primeiro login já
recebe o token na mesma requisição e entra no aplicativo.

**Sugestão:**

```csharp
public SituacaoUsuario Situacao { get; set; } = SituacaoUsuario.AguardandoAprovacao;
```

```csharp
public enum SituacaoUsuario
{
    AguardandoAprovacao = 1,
    Ativo = 2,
    Inativo = 3
}
```

Com o `AuthController` recusando o login enquanto a situação for
`AguardandoAprovacao`, e uma mensagem explicando que o cadastro aguarda
liberação.

---

## 8. `Frete` × `Movimentacao` — qual é a fonte da receita

**Situação:** existem hoje dois caminhos possíveis para a receita de um frete.

```csharp
public class Frete : EntidadeBase
{
    public decimal ValorFrete { get; set; }
    // ...
}
```

E a US09 define que a receita de frete é uma **movimentação de entrada vinculada
à viagem**.

**Por que decidir:** a US10 calcula a rentabilidade como *"(Soma das Entradas
vinculadas à Viagem) − (Soma das Saídas vinculadas à Viagem)"*. Se o valor do
frete for registrado nos dois lugares, ele pode ser contado duas vezes.

**Opções:**

- **A** — `Frete` guarda apenas os dados da carga (cliente, descrição, peso), e
  o valor fica só na movimentação vinculada. Uma fonte de verdade
- **B** — `Frete.ValorFrete` é a fonte, e a movimentação é gerada a partir dele
  automaticamente, como acontece com a manutenção da US06-b

Qualquer uma resolve. O que não convém é manter as duas abertas sem uma regra.

---

## 9. Categorias padrão pré-cadastradas

**O que a US01 pede:** *"o sistema deve vir com categorias padrão
pré-cadastradas para facilitar o setup inicial"*.

**O que existe hoje:** não há `HasData` nem rotina de seed. A lista aparece
vazia no primeiro acesso.

**Sugestão:** seed no `AppDbContext` com as categorias citadas ao longo do
documento — Frete (entrada), Combustível, Pedágio, Manutenção, Salários e
Financiamento (saídas). O mesmo vale para as quatro formas de pagamento fixas da
US02: Pix, Boleto, Transferência TED e Cartão Corporativo.

---

## 10. Nomenclatura de `Categoria`

**O que a US01 pede:** *"obrigatório informar o Título"*.

**O que existe hoje:** a propriedade se chama `Nome`.

Diferença apenas de nomenclatura, sem efeito funcional. Vale alinhar se a
entrega for comparada campo a campo com o documento.

---

## Resumo

| # | Item | Bloqueia |
|---|---|---|
| 1 | `Status`, `DataVencimento`, `DataPagamento` em `Movimentacao` | **US03, US05 e US11** |
| 2 | `QuantidadeLitros` | US10 (km/l) |
| 3 | Sentido do cálculo e campos de `Divida` | US04 |
| 4 | Entidade `Manutencao` | US06-b |
| 5 | Capacidade de carga | US06 |
| 6 | Quilometragem persistida | US10 (desempenho) |
| 7 | Situação do usuário com três estados | US16 |
| 8 | Fonte da receita de frete | US10 (rentabilidade) |
| 9 | Categorias e formas de pagamento padrão | US01 e US02 |
| 10 | Nome do campo Título | Cosmético |

O item 1 é o de maior alcance: três user stories dependem dele, incluindo o
motor de saldo e previsão.
