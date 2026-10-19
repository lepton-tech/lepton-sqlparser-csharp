extern alias SqlParserCs;

using System.Text;
using CoreDialects = SqlParser.Core.Dialects;
using CsDialects = SqlParserCs::SqlParser.Dialects;

namespace SqlParser.Bench;

/// <summary>Dialetos medidos (os dois parsers têm os três).</summary>
public enum DialectKind
{
    Generic,
    PostgreSql,
    MsSql,
}

/// <summary>Cargas de trabalho: o mesmo texto SQL é entregue aos dois parsers.</summary>
public static class Workloads
{
    /// <summary>Instrução OLTP curta.</summary>
    public const string Oltp = "SELECT a, b FROM t WHERE id = 1";

    /// <summary>As 22 consultas TPC-H (índice 0 = consulta 1), lidas dos recursos embutidos.</summary>
    public static readonly string[] Tpch = LoadTpch();

    /// <summary>Nomes das cargas usadas em <see cref="PipelineBenchmarks"/>.</summary>
    public static readonly string[] PipelineNames = { "Tpch22", "Oltp", "Insert1000", "Select300", "Where200" };

    /// <summary>Texto SQL (uma ou mais instruções) de cada carga de <see cref="PipelineNames"/>.</summary>
    public static string[] Get(string name) => name switch
    {
        "Tpch22" => Tpch,
        "Oltp" => new[] { Oltp },
        "Insert1000" => new[] { Insert(1000) },
        "Select300" => new[] { SelectColumns(300) },
        "Where200" => new[] { WherePredicates(200) },
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
    };

    /// <summary>INSERT com <paramref name="rows"/> linhas em VALUES (inteiro, string, decimal, NULL, negativo).</summary>
    public static string Insert(int rows)
    {
        var sb = new StringBuilder("INSERT INTO orders (id, name, price, note, delta) VALUES ");
        for (var i = 1; i <= rows; i++)
        {
            if (i > 1)
            {
                sb.Append(", ");
            }
            sb.Append('(').Append(i).Append(", 'name ").Append(i).Append("', ").Append(i).Append(".25, NULL, -").Append(i % 7).Append(')');
        }
        return sb.ToString();
    }

    /// <summary>SELECT com <paramref name="columns"/> colunas (metade qualificada, metade com alias).</summary>
    public static string SelectColumns(int columns)
    {
        var sb = new StringBuilder("SELECT ");
        for (var i = 1; i <= columns; i++)
        {
            if (i > 1)
            {
                sb.Append(", ");
            }
            if (i % 2 == 0)
            {
                sb.Append("t.col").Append(i);
            }
            else
            {
                sb.Append("col").Append(i).Append(" AS c").Append(i);
            }
        }
        sb.Append(" FROM schema1.wide_table AS t");
        return sb.ToString();
    }

    /// <summary>WHERE com <paramref name="predicates"/> predicados ligados por AND (=, &gt;, LIKE, IS NOT NULL, IN).</summary>
    public static string WherePredicates(int predicates)
    {
        var sb = new StringBuilder("SELECT * FROM t WHERE ");
        for (var i = 1; i <= predicates; i++)
        {
            if (i > 1)
            {
                sb.Append(" AND ");
            }
            switch (i % 5)
            {
                case 0: sb.Append("c").Append(i).Append(" = ").Append(i); break;
                case 1: sb.Append("c").Append(i).Append(" > ").Append(i).Append(".5"); break;
                case 2: sb.Append("c").Append(i).Append(" LIKE 'abc%'"); break;
                case 3: sb.Append("c").Append(i).Append(" IS NOT NULL"); break;
                default: sb.Append("c").Append(i).Append(" IN (1, 2, 3)"); break;
            }
        }
        return sb.ToString();
    }

    /// <summary><c>SELECT ((((1))))</c> com <paramref name="depth"/> pares de parênteses.</summary>
    public static string NestedParens(int depth) =>
        "SELECT " + new string('(', depth) + "1" + new string(')', depth);

    /// <summary>
    /// <paramref name="depth"/> tabelas derivadas aninhadas:
    /// <c>SELECT x FROM (SELECT x FROM (SELECT 1 AS x) AS t1) AS t0</c>.
    /// </summary>
    public static string NestedSubqueries(int depth)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < depth; i++)
        {
            sb.Append("SELECT x FROM (");
        }
        sb.Append("SELECT 1 AS x");
        for (var i = depth - 1; i >= 0; i--)
        {
            sb.Append(") AS t").Append(i);
        }
        return sb.ToString();
    }

    private static string[] LoadTpch()
    {
        var asm = typeof(Workloads).Assembly;
        var result = new string[22];
        for (var i = 1; i <= 22; i++)
        {
            using var stream = asm.GetManifestResourceStream($"tpch.{i}.sql")
                ?? throw new InvalidOperationException($"Recurso tpch.{i}.sql não encontrado.");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            result[i - 1] = reader.ReadToEnd();
        }
        return result;
    }
}

/// <summary>Instâncias de dialeto de cada biblioteca.</summary>
public static class DialectFactory
{
    public static CoreDialects.Dialect Ours(DialectKind kind) => kind switch
    {
        DialectKind.Generic => new CoreDialects.GenericDialect(),
        DialectKind.PostgreSql => new CoreDialects.PostgreSqlDialect(),
        DialectKind.MsSql => new CoreDialects.MsSqlDialect(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static CsDialects.Dialect Cs(DialectKind kind) => kind switch
    {
        DialectKind.Generic => new CsDialects.GenericDialect(),
        DialectKind.PostgreSql => new CsDialects.PostgreSqlDialect(),
        DialectKind.MsSql => new CsDialects.MsSqlDialect(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
