// Testes próprios do port (não existem no sqlparser-rs): um `Result` guardado sem `?` e propagado depois de
// outro passo (convencoes.md §13, caso 83) é relançado com `StackGuard.Rethrow`, que preserva o rastro de pilha
// do ponto original do erro (`throw e;` o reiniciaria no ponto do relançamento; revisão R1, achado 5;
// docs/port/onda-d/D5.md).

using Lepton.SqlParser.Core.Dialects;

namespace Lepton.SqlParser.Core.Tests;

public class StoredErrorRethrowTests
{
    [Fact]
    public void Erro_guardado_do_corpo_do_STRUCT_mantem_o_rastro_original()
    {
        // `parse_duckdb_struct_type_def`: o `parse_comma_separated` falha em `)` (falta o tipo do campo `a`),
        // o `expect_token(&Token::RParen)?` seguinte passa e só então o erro guardado é devolvido.
        var ex = Assert.Throws<ParserException>(
            () => Parser.ParseSql(new DuckDbDialect(), "SELECT CAST(x AS STRUCT(a))"));
        Assert.Contains("Expected: a data type name, found: )", ex.Message);

        // O quadro da closure passada ao `ParseCommaSeparated` (onde o erro nasceu) só aparece no rastro se ele
        // não foi reiniciado no relançamento.
        Assert.Contains("<ParseDuckdbStructTypeDef>", ex.StackTrace);
    }
}
