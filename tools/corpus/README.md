# Extrator de corpus SQL dos testes do Rust (protótipo)

`extract-sql-corpus.js` varre `upstream/sqlparser-rs/tests/*.rs` e extrai cada SQL passado aos helpers de teste do Rust (`verified_stmt`, `one_statement_parses_to`, `verified_expr`, `parse_sql_statements` etc.), com o conjunto de dialetos alvo. Gera um arquivo JSONL (um caso por linha).

Foi usado na análise técnica: extraiu 4.089 casos (3.853 SQLs distintos) da v0.63+, que serviram para medir a paridade do SqlParser-cs (`docs/analise-tecnica-e-plano.md` §5.3).

```powershell
node tools/corpus/extract-sql-corpus.js upstream/sqlparser-rs corpus.jsonl
```

**Status: protótipo, ferramenta de desenvolvimento** (requer Node.js; não é dependência do produto).

Limitações conhecidas:
- **Casos negativos não detectados:** `parse_sql_statements(...)` seguido de `assert_eq!(..., res.unwrap_err())` em variável é classificado como positivo. A categoria "parse-only" tem ruído.
- **Argumentos não resolvidos:** argumentos montados com `format!`, laços ou variáveis complexas não são resolvidos (~7%).
- **Dialetos não resolvidos:** seletores por capacidade (`all_dialects_where(|d| d.supports_x())`) são registrados textualmente, não resolvidos.

Plano: reescrever em C# (`tools/`) na Fase 2, junto com o oráculo Rust (ADR-0005).

Origem: escrito durante a análise técnica (2026-10-14). Os casos extraídos derivam dos testes do sqlparser-rs (Apache-2.0); ver `NOTICE`.
