# GestaoFrota.Database

Material do desenho inicial do banco (diagrama em `.brM3`, `.xml` e `.png`) e o
rascunho `create_tables.sql`, escrito em **PostgreSQL** (`plpgsql`,
`gen_random_uuid()`, `timestamptz`).

**O schema real do projeto é o das migrations do EF Core em
`GestaoFrota.Infrastructure/Migrations/`, aplicadas em SQL Server** com
`dotnet ef database update -p GestaoFrota.Infrastructure -s GestaoFrota.API`.
O script desta pasta não é executado em lugar nenhum e já não corresponde ao
modelo (ex.: tabela `perfil` separada, enquanto o código guarda o perfil como
texto em `Usuarios.Perfil`; não tem `Manutencoes`, `QuilometragemRodadaKm`
nem `QuantidadeLitros`).

Fica aqui como registro do diagrama entregue na Sprint 1. Se o time preferir
não manter os dois, o que pode ser apagado é o `.sql`; o diagrama continua útil
na apresentação.
