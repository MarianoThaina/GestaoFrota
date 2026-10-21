# Módulo de frota (US06, US06-b, US07, US08, US09)

Fecha o maior buraco apontado no levantamento de 20/10: não existia controller
nem tela de Veículos, Rotas ou Viagens. Cobre as Sprints 5 e 6 do roadmap.

## O que entra

**Modelo e banco** (migration única `20261021120000_ModuloFrota`, escrita à mão como a `FinanceiroCore`)
- `Veiculo.CapacidadeCargaToneladas` (US06).
- `Viagem.QuilometragemRodadaKm` persistida, no lugar da propriedade calculada em memória (US08); viagens já encerradas recebem o valor no `UPDATE` da migration.
- `Movimentacao.QuantidadeLitros` (US09).
- Tabela `Manutencoes` (US06-b): veículo, data, descrição, valor, data de conclusão, FK opcional para a despesa gerada. Check `[ValorTotal] > 0`.
- Índice `(VeiculoId, Status)` em `Viagens` para a pergunta "este veículo está em viagem?".
- Snapshot atualizado à mão para bater com o modelo.

**Domínio**
- `ValidadorPlaca`: formato antigo (AAA9999) e Mercosul (AAA9A99), normalização (maiúscula, sem hífen) e formatação para exibir.
- `RegrasViagem`: validação do encerramento (`final > inicial`) e cálculo da quilometragem rodada. Puras, sem banco, no mesmo modelo da `CalculadoraFinanceira`.

**API**
- `VeiculosController`: CRUD com placa validada e única (409 em duplicata), desativação lógica, recusa de hodômetro menor que o último registrado em viagem, recusa de desativar veículo em viagem. Leitura para todos os perfis (o Motorista precisa escolher o veículo); escrita para Administrador e Gestor de Frota; desativar só Administrador.
- `RotasController`: mesma estrutura.
- `ViagensController`: listar (Motorista só recebe as próprias, filtro no servidor), abrir (veículo precisa estar Disponível e Ativo, rota Ativa, um veículo não abre duas viagens), encerrar (validação do hodômetro, grava km rodada, atualiza `KmAtual` e devolve o veículo a Disponível, ou a Em Manutenção se houver manutenção aberta), cancelar (gestores). `GET /api/viagens/motoristas` para o gestor abrir viagem em nome de alguém.
- `ManutencoesController`: criar em transação (manutenção + despesa de saída na categoria "Manutenção" + status do veículo), listar, concluir. Recusa veículo em viagem.
- `MovimentacoesController`: `viagemId` e `quantidadeLitros` no request/response, validação da viagem (existe e não está cancelada), filtro `?viagemId=`.
- `Perfis.OperacaoViagens` (Administrador, Gestor, Motorista).

**App (MAUI Android)**
- Menu: *Viagens*, *Frota* e *Rotas*. Motorista vê Viagens e Frota (consulta) e **abre direto em Viagens** (US17). Rotas só para Administrador e Gestor.
- `FrotaPage` (lista com situação e filtro; editar, registrar manutenção, concluir manutenção, desativar) e `VeiculoFormPage`.
- `RotasPage` no desenho da tela de Categorias (criação no topo, edição inline, desativar por deslize).
- `ViagensPage` (aviso de sem conexão com a contagem da fila, filtro por situação, cartões com hodômetro inicial/final/rodado), `ViagemFormPage` (veículo disponível, rota, motorista quando gestor, hodômetro sugerido pelo último do veículo) e `EncerrarViagemPage` (prévia da quilometragem enquanto digita; erro em vermelho quando final ≤ inicial).
- `ManutencaoFormPage` (data, descrição, valor, forma de pagamento, já pago).
- `MovimentacaoFormPage`: picker "Vincular à viagem" e campo de litros quando a categoria é de combustível.
- Cache SQLite de veículos, rotas e viagens. **Fila offline só para viagens**: abrir e encerrar sem rede gravam `AberturaPendente`/`EncerramentoPendente`; `ViagemService.SincronizarPendentesAsync` reenvia ao abrir a tela com conexão e guarda a mensagem da API em `ErroSincronizacao` quando o servidor recusa (ex.: veículo já estava em viagem).

**Docs**: `README.md` (endpoints e decisões), `estado-atual.md`, `.http` com exemplos.

## Decisões

- **Offline-first de verdade só onde a US pede.** US08 exige a viagem do motorista sem internet; movimentação e manutenção mexem no caixa e continuam exigindo conexão (mesmo critério do núcleo financeiro).
- **Reenvio idempotente em vez de fila genérica.** O `Id` nasce no aparelho; `POST /api/viagens` com `id` já existente devolve a viagem em vez de duplicar; `encerrar` com o mesmo hodômetro final devolve 200. Isso é o que permite reenviar sem medo.
- **Quilometragem gravada** (item 6 das pendências), para a US10 somar no banco.
- **Manutenção e despesa no mesmo commit**, como a US06-b descreve; a categoria padrão é a semeada (`00000000-0000-0000-0001-000000000004`), com busca por nome como reserva e `categoriaId` opcional no request.
- **Situação do usuário com três estados (item 7) ficou de fora**: muda o comportamento do login e pertence à frente de autenticação. A migration não cria a coluna para não deixar modelo pela metade.
- **Frete × Movimentação (item 8) não decidido aqui**: `Frete.ValorFrete` continua existindo; a receita vinculada pela US09 usa a movimentação. O time precisa escolher antes da US10.

## Como testar

1. `dotnet build GestaoFrota.API`, `dotnet build GestaoFrota.App -f net10.0-android` e `dotnet ef database update -p GestaoFrota.Infrastructure -s GestaoFrota.API`. Conferir no banco a tabela `Manutencoes` e as colunas novas.
2. Swagger como Administrador: `POST /api/veiculos` com `"placa": "rta-9f12"` → 201 com `placa: "RTA9F12"` e `placaFormatada: "RTA-9F12"`. Repetir com `"RTA9F12"` → 409. `"placa": "RTA-9F1"` → 400.
3. `POST /api/rotas`, depois `POST /api/viagens` com o veículo e a rota → 201, `status: 1`; `GET /api/veiculos/{id}` mostra `status: 2` (EmViagem). Abrir outra viagem com o mesmo veículo → 409.
4. `PATCH /api/viagens/{id}/encerrar` com `hodometroFinal` menor que o inicial → 400 com a mensagem do hodômetro. Com valor maior → 200, `quilometragemRodadaKm` = final − inicial, veículo volta a `status: 1` e `kmAtual` = final. Repetir o mesmo PATCH → 200 (idempotente).
5. `POST /api/manutencoes` → 201; `GET /api/movimentacoes?viagemId=` vazio mas `GET /api/movimentacoes` mostra a despesa "Manutenção RTA-9F12 - ...", veículo em `status: 3`. `POST /api/viagens` com esse veículo → 409. `PATCH /api/manutencoes/{id}/concluir` → veículo `status: 1`.
6. JWT de Motorista: `GET /api/viagens` só devolve as dele; `POST /api/viagens` com `motoristaId` de outra pessoa → 403; `POST /api/veiculos` → 403.
7. Emulador como Administrador: menu com *Viagens*, *Frota*, *Rotas*. Cadastrar veículo e rota, abrir viagem (o hodômetro inicial vem preenchido com o último do veículo), encerrar com valor menor (erro em vermelho) e depois maior (prévia verde, depois cartão com "Rodado").
8. Emulador como Motorista: o app abre na tela *Viagens*; *Frota* aparece sem botões de gestão; *Rotas* e módulos financeiros não aparecem.
9. Offline (modo avião no emulador) como Motorista: abrir viagem → alerta "Viagem aberta no aparelho", cartão "Aberta · aguardando envio". Encerrar → "Encerrada · aguardando envio". Voltar a rede, puxar para atualizar → os dois somem da fila e o cartão passa a "Concluída" com o rodado do servidor.
10. *Movimentações* > *Nova*: escolher categoria "Combustível" → aparece o campo de litros; escolher uma viagem no picker; salvar; na lista o detalhe mostra "372,5 L · vinculada a viagem".

## O que não foi conferido pelo autor

- Compilação e execução (sem SDK .NET e sem emulador na máquina de autoria). Pontos que merecem atenção na primeira build: `GeneratedRegex` em `ValidadorPlaca` e `Helpers/Placa.cs` (exige .NET 7+, o projeto é net10); `tran.Table<Viagem>().Delete(predicado)` do sqlite-net no `SubstituirViagensAsync`; `Shell.CurrentItem = MenuViagens` dentro de `ConfigurarMenuPorPerfil` antes de o Shell ser exibido.
- A migration e o snapshot foram escritos à mão: rodar `dotnet ef migrations add Teste -p GestaoFrota.Infrastructure -s GestaoFrota.API` depois do `database update` deve gerar uma migration vazia; se gerar diferenças, o snapshot precisa de ajuste (e a migration de teste deve ser apagada).
- `FlexLayout` com botões `IsVisible` dinâmico na `FrotaPage` (se sobrar espaço estranho, trocar por `HorizontalStackLayout` com quebra manual).
