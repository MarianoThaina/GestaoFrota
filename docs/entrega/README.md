# Entrega final

- `relatorio-executivo.md`: fonte do Relatório Executivo. Edite aqui; os trechos `[PREENCHER]`,
  `[CONFERIR]` e `[CONFIRMAR]` dependem do grupo.
- O PDF final é gerado só no fim, quando o texto estiver fechado (comando abaixo).

Para regerar o PDF depois de editar (precisa de [Pandoc](https://pandoc.org) e Chrome):

```bash
cd docs/entrega
pandoc relatorio-executivo.md -t html5 --standalone --embed-resources --css relatorio.css --metadata lang=pt-BR -o relatorio.html
chrome --headless=new --no-pdf-header-footer --print-to-pdf=relatorio-executivo.pdf relatorio.html
```

Sem Pandoc, dá para abrir o `.md` no GitHub, copiar para o Word ou Google Docs e exportar em PDF.
