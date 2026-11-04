# GestaoFrota.Database

Diagrama do desenho inicial do banco (Sprint 1), em `.brM3`, `.xml` e `.png`.
Fica como registro e para a apresentação.

**O schema real do projeto é o das migrations do EF Core em
`GestaoFrota.Infrastructure/Migrations/`, aplicadas em SQL Server** com:

```bash
dotnet ef database update -p GestaoFrota.Infrastructure -s GestaoFrota.API
```

O rascunho `create_tables.sql` da Sprint 1 foi removido: estava em PostgreSQL,
não era executado em lugar nenhum e já não correspondia ao modelo (tinha
`cliente` e `parcela`, que o código não usa, e não tinha `Manutencoes`). Ele
continua no histórico do Git para consulta.

O diagrama também é da Sprint 1: tabelas criadas depois (como `Manutencoes`)
podem não aparecer nele; em caso de dúvida, valem as migrations.
