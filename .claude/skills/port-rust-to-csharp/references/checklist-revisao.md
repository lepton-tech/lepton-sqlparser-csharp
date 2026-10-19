# Checklist de revisão linha a linha (Rust × C#)

Use na revisão de toda unidade portada. De preferência, rode a revisão num **subagente com contexto limpo**, que recebe apenas o trecho Rust, o arquivo C# e este checklist.

**Por que existe:**
- No Lucene.NET, revisar linha a linha contra o original encontrou bugs que os testes não mostravam.
- O estudo "Lost in Translation" (ICSE 2024) catalogou os bugs típicos de tradução feita por LLM.

Os itens abaixo derivam dessa taxonomia, adaptada para Rust → C#. Ver `docs/port/referencias-e-boas-praticas.md` (P3, L4).

## 1. Estrutura e escopo
- [ ] Todo item Rust do trecho tem correspondente C# (nada esquecido), na **mesma ordem**.
- [ ] Nenhuma lógica **adicionada** ou **removida**. Cada `if`/`match`/braço/`return` antecipado do Rust existe no C#.
- [ ] Cada `match` virou um `switch`. Braços com guarda (`if`) preservados. `_ =>` corresponde ao `default` certo, sem `default` que engole casos em silêncio.
- [ ] Nenhuma edição fora da unidade (reformatação, renomeação, comentários alheios).

## 2. Tipos e números
- [ ] Larguras de inteiros conforme `convencoes.md` §5 (`u8`→`byte`, `u64`→`ulong`, `usize`→`int`). Conversões explícitas onde o Rust usa `as`.
- [ ] Overflow: o Rust entra em pânico em debug e faz *wrap* em release. Verificar se o código depende disso (raro).
- [ ] `Option<T>` ↔ `T?`. Nenhum `null` onde o Rust não tem `Option`, e nenhum valor mágico no lugar de `None`.
- [ ] Igualdade: records comparam por valor. Coleções dentro de records **não**, até a Fase 2 (`Sequence<T>`). Os `impl PartialEq` customizados foram reproduzidos.

## 3. Strings e caracteres (fonte nº 1 de bugs de tradução)
- [ ] Nenhum índice de byte UTF-8 do Rust usado como índice de `char` C#.
- [ ] `char` fora do BMP: `Location.Column` avança 1 por *scalar value*, ou então há `DIV-NNN` registrada.
- [ ] Classificação de caracteres conforme §10. Em especial `is_ascii_digit` ≠ `char.IsDigit`, e `is_alphabetic` = `char.IsLetter`.
- [ ] `to_uppercase/to_lowercase` → `RustString.ToUppercase/ToLowercase` (`char`: `RustChar.ToUppercase/ToLowercase`), nunca `*Invariant()` (§10; caso 139). Números → `CultureInfo.InvariantCulture`.
- [ ] Escapes de strings literais traduzidos corretamente (`\u{..}`, `\x..`, `r#"..."#`).

## 4. Semântica de API
- [ ] `peek`/`next` do iterador: o C# consome exatamente os mesmos caracteres/tokens.
- [ ] Métodos de coleção com a mesma semântica (`first()`, `last()`, `get(i)` → `Option`, não exceção).
- [ ] `Display`: texto **idêntico** (espaços, aspas, maiúsculas). `{:#}` → `WriteAlternate`/`Alternate`.
- [ ] Mensagens de erro **idênticas** ao `format!` do Rust.

## 5. Plataforma (`netstandard2.0`)
- [ ] Sem APIs ausentes no `netstandard2.0` (§11). Sem default interface members.
- [ ] Sem `ToString()` sintetizado de record exposto. A base usa `sealed override` (§7).
- [ ] Sem "sintaxe Rust duplicada" que compila mas muda o sentido (por exemplo, `..` de faixa, shadowing de variável com `let`).

## 6. Testes
- [ ] Os testes Rust do trecho foram portados com o **mesmo nome** e os **mesmos dados**.
- [ ] Nenhum teste foi editado para passar. Divergências estão com `Skip = "DIV-NNN"` e registradas.
- [ ] Testes verdes em net48, net8.0 e net10.0.
