extern alias SqlParserCs;

using Lepton.SqlParser.Core;
using Lepton.SqlParser.Core.Ast;
using Cs = SqlParserCs::SqlParser;
using CoreDialect = Lepton.SqlParser.Core.Dialects.Dialect;
using CsAst = SqlParserCs::SqlParser.Ast;
using CsDialect = SqlParserCs::SqlParser.Dialects.Dialect;

namespace Lepton.SqlParser.Bench;

/// <summary>
/// Operações medidas, uma por biblioteca. "Ours" = Lepton.SqlParser.Core (port fiel); "Api" = nossa API pública
/// (src/Lepton.SqlParser); "Cs" = SqlParser-cs. Cada chamada cria um parser novo, como no uso típico das duas.
/// </summary>
public static class Libs
{
    private static readonly SqlQueryParser Api = new();

    // ---- parse ----

    public static Sequence<Statement> OursParse(CoreDialect dialect, string sql) =>
        Parser.ParseSql(dialect, sql);

    public static Sequence<Statement> OursParse(CoreDialect dialect, string sql, int recursionLimit) =>
        new Parser(dialect).WithRecursionLimit(recursionLimit).TryWithSql(sql).ParseStatements();

    public static Sequence<Statement> ApiParse(CoreDialect dialect, string sql) =>
        Api.Parse(sql, dialect);

    public static Cs.Sequence<CsAst.Statement> CsParse(CsDialect dialect, string sql, Cs.ParserOptions? options = null) =>
        new Cs.Parser().ParseSql(sql.AsSpan(), dialect, options);

    // ---- tokenize ----

    public static int OursTokenize(CoreDialect dialect, string sql) =>
        new Tokenizer(dialect, sql).TokenizeWithLocation().Count;

    public static int CsTokenize(CsDialect dialect, string sql) =>
        new Cs.Tokenizer().Tokenize(sql.AsSpan(), dialect).Count;

    // ---- ToString / ToSql ----

    /// <summary>Core: <c>Display</c> de cada instrução (= <c>ToString()</c>).</summary>
    public static int OursToString(Sequence<Statement> statements)
    {
        var length = 0;
        foreach (var statement in statements)
        {
            length += statement.ToString().Length;
        }
        return length;
    }

    /// <summary>API pública: <c>ToSql()</c> de cada instrução.</summary>
    public static int ApiToSql(Sequence<Statement> statements)
    {
        var length = 0;
        foreach (var statement in statements)
        {
            length += statement.ToSql().Length;
        }
        return length;
    }

    /// <summary>SqlParser-cs: <c>ToSql()</c> de cada instrução.</summary>
    public static int CsToSql(Cs.Sequence<CsAst.Statement> statements)
    {
        var length = 0;
        foreach (var statement in statements)
        {
            length += Cs.SqlWritingExtensions.ToSql(statement).Length;
        }
        return length;
    }

    public static IEnumerable<string> OursStrings(Sequence<Statement> statements)
    {
        foreach (var statement in statements)
        {
            yield return statement.ToString();
        }
    }

    public static IEnumerable<string> CsStrings(Cs.Sequence<CsAst.Statement> statements)
    {
        foreach (var statement in statements)
        {
            yield return Cs.SqlWritingExtensions.ToSql(statement);
        }
    }
}
