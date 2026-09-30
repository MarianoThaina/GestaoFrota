create or replace function public.set_updated_at()
returns trigger
language plpgsql
as $$
begin
  new.updated_at = now();
  return new;
end;
$$;

create table if not exists public.perfil (
  id          uuid primary key default gen_random_uuid(),
  nome        text not null unique,
  descricao   text,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now()
);

drop trigger if exists trg_perfil_updated_at on public.perfil;
create trigger trg_perfil_updated_at
before update on public.perfil
for each row execute function public.set_updated_at();

alter table public.perfil enable row level security;

drop policy if exists "perfil_select_authenticated" on public.perfil;
create policy "perfil_select_authenticated"
on public.perfil
for select
to authenticated
using (true);

insert into public.perfil (nome, descricao) values
  ('Admin',      'Acesso total ao sistema'),
  ('Gestor',     'Gerencia frota, viagens, fretes e movimentações'),
  ('Motorista',  'Acessa as próprias viagens e registra despesas'),
  ('Financeiro', 'Acessa movimentações, dívidas e parcelas')
on conflict (nome) do nothing;

create type public.status_veiculo       as enum ('disponivel', 'em_viagem', 'manutencao', 'inativo');
create type public.status_viagem        as enum ('planejada', 'em_andamento', 'concluida', 'cancelada');
create type public.status_frete         as enum ('pendente', 'em_transporte', 'entregue', 'cancelado');
create type public.tipo_categoria       as enum ('receita', 'despesa');
create type public.status_movimentacao  as enum ('pendente', 'confirmada', 'cancelada');
create type public.status_divida        as enum ('ativa', 'quitada', 'cancelada');
create type public.status_parcela       as enum ('pendente', 'paga', 'atrasada', 'cancelada');

create table public.usuario (
  id          uuid primary key default gen_random_uuid(),
  nome        text not null,
  email       text not null unique,
  google_id   text not null unique,
  perfil_id   uuid not null references public.perfil (id) on delete restrict,
  ativo       boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now()
);

create table public.veiculo (
  id            uuid primary key default gen_random_uuid(),
  placa         text not null unique,
  modelo        text not null,
  marca         text not null,
  ano           integer check (ano between 1900 and 2100),
  tipo          text,
  capacidade_kg numeric(10,2) check (capacidade_kg >= 0),
  status        public.status_veiculo not null default 'disponivel',
  km_atual      numeric(10,2) not null default 0 check (km_atual >= 0),
  ativo         boolean not null default true,
  created_at    timestamptz not null default now(),
  updated_at    timestamptz not null default now()
);

create table public.rota (
  id           uuid primary key default gen_random_uuid(),
  origem       text not null,
  destino      text not null,
  distancia_km numeric(10,2) check (distancia_km >= 0),
  ativo        boolean not null default true,
  created_at   timestamptz not null default now(),
  updated_at   timestamptz not null default now()
);

create table public.cliente (
  id          uuid primary key default gen_random_uuid(),
  nome        text not null,
  documento   text not null unique,
  telefone    text,
  ativo       boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now()
);

create table public.categoria (
  id          uuid primary key default gen_random_uuid(),
  nome        text not null,
  tipo        public.tipo_categoria not null,
  ativo       boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now()
);

create table public.forma_pagamento (
  id          uuid primary key default gen_random_uuid(),
  nome        text not null unique,
  ativo       boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now()
);

create table public.viagem (
  id                    uuid primary key default gen_random_uuid(),
  veiculo_id            uuid not null references public.veiculo (id) on delete restrict,
  rota_id               uuid not null references public.rota (id) on delete restrict,
  motorista_id          uuid not null references public.usuario (id) on delete restrict,
  data_saida            timestamptz not null,
  data_chegada_prevista timestamptz,
  data_chegada_real     timestamptz,
  status                public.status_viagem not null default 'planejada',
  km_inicial            numeric(10,2) check (km_inicial >= 0),
  km_final              numeric(10,2) check (km_final >= 0),
  created_at            timestamptz not null default now(),
  updated_at            timestamptz not null default now(),
  constraint viagem_km_coerente check (km_final is null or km_inicial is null or km_final >= km_inicial)
);

create table public.frete (
  id              uuid primary key default gen_random_uuid(),
  viagem_id       uuid not null references public.viagem (id) on delete restrict,
  cliente_id      uuid not null references public.cliente (id) on delete restrict,
  descricao       text,
  peso            numeric(10,2) check (peso >= 0),
  valor_combinado numeric(12,2) not null check (valor_combinado >= 0),
  status          public.status_frete not null default 'pendente',
  created_at      timestamptz not null default now(),
  updated_at      timestamptz not null default now()
);

create table public.divida (
  id                  uuid primary key default gen_random_uuid(),
  descricao           text not null,
  credor_nome         text not null,
  valor_total         numeric(12,2) not null check (valor_total > 0),
  quantidade_parcelas integer not null check (quantidade_parcelas > 0),
  data_contratacao    date not null,
  status              public.status_divida not null default 'ativa',
  created_at          timestamptz not null default now(),
  updated_at          timestamptz not null default now()
);

create table public.movimentacao (
  id                  uuid primary key default gen_random_uuid(),
  categoria_id        uuid not null references public.categoria (id) on delete restrict,
  forma_pagamento_id  uuid not null references public.forma_pagamento (id) on delete restrict,
  usuario_id          uuid not null references public.usuario (id) on delete restrict,
  viagem_id           uuid references public.viagem (id) on delete restrict,
  frete_id            uuid references public.frete (id) on delete restrict,
  veiculo_id          uuid references public.veiculo (id) on delete restrict,
  valor               numeric(12,2) not null check (valor > 0),
  descricao           text,
  data_movimentacao   date not null default current_date,
  status              public.status_movimentacao not null default 'confirmada',
  created_at          timestamptz not null default now(),
  updated_at          timestamptz not null default now()
);

create table public.parcela (
  id              uuid primary key default gen_random_uuid(),
  divida_id       uuid not null references public.divida (id) on delete restrict,
  movimentacao_id uuid unique references public.movimentacao (id) on delete restrict,
  numero_parcela  integer not null check (numero_parcela > 0),
  valor_parcela   numeric(12,2) not null check (valor_parcela > 0),
  data_vencimento date not null,
  data_pagamento  date,
  status          public.status_parcela not null default 'pendente',
  created_at      timestamptz not null default now(),
  updated_at      timestamptz not null default now(),
  constraint parcela_numero_unico_por_divida unique (divida_id, numero_parcela)
);

create index idx_usuario_perfil_id            on public.usuario (perfil_id);

create index idx_viagem_veiculo_id            on public.viagem (veiculo_id);
create index idx_viagem_rota_id               on public.viagem (rota_id);
create index idx_viagem_motorista_id          on public.viagem (motorista_id);
create index idx_viagem_status                on public.viagem (status);

create index idx_frete_viagem_id              on public.frete (viagem_id);
create index idx_frete_cliente_id             on public.frete (cliente_id);

create index idx_movimentacao_data            on public.movimentacao (data_movimentacao);
create index idx_movimentacao_categoria_id    on public.movimentacao (categoria_id);
create index idx_movimentacao_veiculo_id      on public.movimentacao (veiculo_id);
create index idx_movimentacao_forma_pgto_id   on public.movimentacao (forma_pagamento_id);
create index idx_movimentacao_usuario_id      on public.movimentacao (usuario_id);
create index idx_movimentacao_viagem_id       on public.movimentacao (viagem_id);
create index idx_movimentacao_frete_id        on public.movimentacao (frete_id);

create index idx_parcela_divida_id            on public.parcela (divida_id);
create index idx_parcela_vencimento           on public.parcela (data_vencimento);
create index idx_parcela_status               on public.parcela (status);

do $$
declare
  t text;
begin
  foreach t in array array[
    'usuario', 'veiculo', 'rota', 'cliente', 'categoria', 'forma_pagamento',
    'viagem', 'frete', 'divida', 'movimentacao', 'parcela'
  ]
  loop
    execute format(
      'create trigger trg_%1$s_updated_at before update on public.%1$s
         for each row execute function public.set_updated_at()', t);

    execute format('alter table public.%1$s enable row level security', t);

    execute format(
      'create policy "%1$s_select_authenticated" on public.%1$s
         for select to authenticated using (true)', t);
  end loop;
end;
$$;
