// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/spans.rs (#[cfg(test)] pub mod tests).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core.Tests;

// Notas da tradução (onda-b/B7.md):
// - `assert_eq!(a, b)` mantém a ordem do Rust, exceto quando um dos lados é literal: o literal vai na
//   posição "expected" (regra xUnit2000, como em AstModTests).
// - `assert_eq!(1, r.len())` usa `Assert.Equal(1, r.Count)`, como no Rust; o xUnit2013 (que sugere
//   Assert.Single) fica suprimido neste arquivo, como em AstModTests.
// - `panic!(msg)` → `Assert.Fail(msg)`; `let P = x else { panic!(..) };` → `if (x is not P) { Assert.Fail(..); return; }`.
// - As strings `r#"..."#` com várias linhas viram concatenações de linhas com `\n` explícito, para que
//   espaços no início e no fim (que definem as colunas dos spans) fiquem visíveis e não dependam da
//   remoção de indentação dos raw string literals do C#.
#pragma warning disable xUnit2013
public class SpansTests
{
    // struct SpanTest<'a>(Parser<'a>, &'a str);
    private sealed class SpanTest
    {
        // fn new(dialect: &'a dyn Dialect, sql: &'a str) -> Self
        public SpanTest(Dialect dialect, string sql)
        {
            Parser = new Parser(dialect).TryWithSql(sql);
            Sql = sql;
        }

        /// <summary>Rust: campo <c>.0</c>.</summary>
        public Parser Parser { get; }

        /// <summary>Rust: campo <c>.1</c>.</summary>
        public string Sql { get; }

        // get the subsection of the source string that corresponds to the span
        // only works on single-line strings
        public string GetSource(Span span)
        {
            // lines in spans are 1-indexed
            // &self.1[(span.start.column as usize - 1)..(span.end.column - 1) as usize]
            var start = (int)span.Start.Column - 1;
            var end = (int)(span.End.Column - 1);
            return Sql.Substring(start, end - start);
        }
    }

    [Fact]
    public void test_join()
    {
        var dialect = new GenericDialect();
        var test = new SpanTest(
            dialect,
            "SELECT id, name FROM users LEFT JOIN companies ON users.company_id = companies.id");

        var query = test.Parser.ParseSelect();
        var selectSpan = query.Span();

        Assert.Equal(
            "SELECT id, name FROM users LEFT JOIN companies ON users.company_id = companies.id",
            test.GetSource(selectSpan));

        var joinSpan = query.From[0].Joins[0].Span();

        // 'LEFT JOIN' missing
        Assert.Equal(
            "companies ON users.company_id = companies.id",
            test.GetSource(joinSpan));
    }

    [Fact]
    public void test_union()
    {
        var dialect = new GenericDialect();
        var test = new SpanTest(
            dialect,
            "SELECT a FROM postgres.public.source UNION SELECT a FROM postgres.public.source");

        var query = test.Parser.ParseQuery();
        var selectSpan = query.Span();

        Assert.Equal(
            "SELECT a FROM postgres.public.source UNION SELECT a FROM postgres.public.source",
            test.GetSource(selectSpan));
    }

    [Fact]
    public void test_subquery()
    {
        var dialect = new GenericDialect();
        var test = new SpanTest(
            dialect,
            "SELECT a FROM (SELECT a FROM postgres.public.source) AS b");

        var query = test.Parser.ParseSelect();
        var selectSpan = query.Span();

        Assert.Equal(
            "SELECT a FROM (SELECT a FROM postgres.public.source) AS b",
            test.GetSource(selectSpan));

        var subquerySpan = query.From[0].Span();

        // left paren missing
        Assert.Equal(
            "SELECT a FROM postgres.public.source) AS b",
            test.GetSource(subquerySpan));
    }

    [Fact]
    public void test_cte()
    {
        var dialect = new GenericDialect();
        var test = new SpanTest(dialect, "WITH cte_outer AS (SELECT a FROM postgres.public.source), cte_ignored AS (SELECT a FROM cte_outer), cte_inner AS (SELECT a FROM cte_outer) SELECT a FROM cte_inner");

        var query = test.Parser.ParseQuery();

        var selectSpan = query.Span();

        Assert.Equal("WITH cte_outer AS (SELECT a FROM postgres.public.source), cte_ignored AS (SELECT a FROM cte_outer), cte_inner AS (SELECT a FROM cte_outer) SELECT a FROM cte_inner", test.GetSource(selectSpan));
    }

    [Fact]
    public void test_snowflake_lateral_flatten()
    {
        var dialect = new SnowflakeDialect();
        var test = new SpanTest(dialect, "SELECT FLATTENED.VALUE:field::TEXT AS FIELD FROM SNOWFLAKE.SCHEMA.SOURCE AS S, LATERAL FLATTEN(INPUT => S.JSON_ARRAY) AS FLATTENED");

        var query = test.Parser.ParseSelect();

        var selectSpan = query.Span();

        Assert.Equal("SELECT FLATTENED.VALUE:field::TEXT AS FIELD FROM SNOWFLAKE.SCHEMA.SOURCE AS S, LATERAL FLATTEN(INPUT => S.JSON_ARRAY) AS FLATTENED", test.GetSource(selectSpan));
    }

    [Fact]
    public void test_wildcard_from_cte()
    {
        var dialect = new GenericDialect();
        var test = new SpanTest(
            dialect,
            "WITH cte AS (SELECT a FROM postgres.public.source) SELECT cte.* FROM cte");

        var query = test.Parser.ParseQuery();
        // query.clone().with.unwrap().cte_tables[0].span()
        var cteSpan = query.With!.CteTables[0].Span();
        var cteQuerySpan = query.With!.CteTables[0].Query.Span();
        var bodySpan = query.Body.Span();

        // the WITH keyboard is part of the query
        Assert.Equal(
            "cte AS (SELECT a FROM postgres.public.source)",
            test.GetSource(cteSpan));
        Assert.Equal(
            "SELECT a FROM postgres.public.source",
            test.GetSource(cteQuerySpan));

        Assert.Equal("SELECT cte.* FROM cte", test.GetSource(bodySpan));
    }

    [Fact]
    public void test_case_expr_span()
    {
        var dialect = new GenericDialect();
        var test = new SpanTest(dialect, "CASE 1 WHEN 2 THEN 3 ELSE 4 END");
        var expr = test.Parser.ParseExpr();
        var exprSpan = expr.Span();
        Assert.Equal(
            "CASE 1 WHEN 2 THEN 3 ELSE 4 END",
            test.GetSource(exprSpan));
    }

    [Fact]
    public void test_placeholder_span()
    {
        var sql = "\nSELECT\n  :fooBar";
        var r = Parser.ParseSql(new GenericDialect(), sql);
        Assert.Equal(1, r.Count);
        switch (r[0])
        {
            case Statement.Query q:
            {
                var col = q.Value.Body.AsSelect()!.Projection[0];
                switch (col)
                {
                    // SelectItem::UnnamedExpr(Expr::Value(ValueWithSpan { value: Value::Placeholder(s), span }))
                    case SelectItem.UnnamedExpr { Value: Expr.Value { Inner: { Value: Value.Placeholder { Value: var s }, Span: var span } } }:
                        Assert.Equal(":fooBar", s);
                        Assert.Equal(new Span(Location.From((3, 3)), Location.From((3, 10))), span);
                        break;
                    default:
                        Assert.Fail($"expected unnamed expression; got {col}");
                        break;
                }
                break;
            }
            case var stmt:
                Assert.Fail($"expected query; got {stmt}");
                break;
        }
    }

    [Fact]
    public void test_alter_table_multiline_span()
    {
        var sql =
            "-- foo\n" +
            "ALTER TABLE users\n" +
            "  ADD COLUMN foo\n" +
            "  varchar; -- hi there";

        var r = Parser.ParseSql(new PostgreSqlDialect(), sql);
        Assert.Equal(1, r.Count);

        var stmtSpan = r[0].Span();

        Assert.Equal(stmtSpan.Start, Location.From((2, 13)));
        Assert.Equal(stmtSpan.End, Location.From((4, 11)));
    }

    [Fact]
    public void test_update_statement_span()
    {
        var sql =
            "-- foo\n" +
            "      UPDATE foo\n" +
            "   /* bar */\n" +
            "   SET bar = 3\n" +
            " WHERE quux > 42 ;\n";

        var r = Parser.ParseSql(new GenericDialect(), sql);
        Assert.Equal(1, r.Count);

        var stmtSpan = r[0].Span();

        Assert.Equal(stmtSpan.Start, Location.From((2, 7)));
        Assert.Equal(stmtSpan.End, Location.From((5, 17)));
    }

    [Fact]
    public void test_insert_statement_span()
    {
        var sql =
            "\n" +
            "/* foo */ INSERT  INTO  FOO  (X, Y, Z)\n" +
            "  SELECT 1, 2, 3\n" +
            "  FROM DUAL\n" +
            ";";

        var r = Parser.ParseSql(new GenericDialect(), sql);
        Assert.Equal(1, r.Count);

        var stmtSpan = r[0].Span();

        Assert.Equal(stmtSpan.Start, Location.From((2, 11)));
        Assert.Equal(stmtSpan.End, Location.From((4, 12)));
    }

    [Fact]
    public void test_replace_statement_span()
    {
        var sql =
            "\n" +
            "/* foo */ REPLACE INTO\n" +
            "    cities(name,population)\n" +
            "SELECT\n" +
            "    name,\n" +
            "    population\n" +
            "FROM\n" +
            "   cities\n" +
            "WHERE id = 1\n" +
            ";";

        var r = Parser.ParseSql(new GenericDialect(), sql);
        Assert.Equal(1, r.Count);

        // dbg!(&r[0]); (saída de depuração: não traduzida)

        var stmtSpan = r[0].Span();

        Assert.Equal(stmtSpan.Start, Location.From((2, 11)));
        Assert.Equal(stmtSpan.End, Location.From((9, 13)));
    }

    [Fact]
    public void test_delete_statement_span()
    {
        var sql =
            "-- foo\n" +
            "      DELETE /* quux */\n" +
            "        FROM foo\n" +
            "       WHERE foo.x = 42\n" +
            ";";

        var r = Parser.ParseSql(new GenericDialect(), sql);
        Assert.Equal(1, r.Count);

        var stmtSpan = r[0].Span();

        Assert.Equal(stmtSpan.Start, Location.From((2, 7)));
        Assert.Equal(stmtSpan.End, Location.From((4, 24)));
    }

    [Fact]
    public void test_merge_statement_spans()
    {
        var sql =
            "\n" +
            "        -- plain merge statement; no RETURNING, no OUTPUT\n" +
            "\n" +
            "        MERGE INTO target_table USING source_table\n" +
            "                ON target_table.id = source_table.oooid\n" +
            "\n" +
            "        /* an inline comment */ WHEN NOT MATCHED THEN\n" +
            "            INSERT (ID, description)\n" +
            "               VALUES (source_table.id, source_table.description)\n" +
            "\n" +
            "            -- another one\n" +
            "                WHEN MATCHED AND target_table.x = 'X' THEN\n" +
            "            UPDATE SET target_table.description = source_table.description\n" +
            "\n" +
            "              WHEN MATCHED AND target_table.x != 'X' THEN   DELETE\n" +
            "        WHEN NOT MATCHED AND 1 THEN INSERT (product, quantity) ROW\n" +
            "        WHEN MATCHED THEN DO NOTHING\n" +
            "        ";

        var r = Parser.ParseSql(new GenericDialect(), sql);
        Assert.Equal(1, r.Count);

        // ~ assert the span of the whole statement
        var stmtSpan = r[0].Span();
        Assert.Equal(stmtSpan.Start, Location.From((4, 9)));
        Assert.Equal(stmtSpan.End, Location.From((17, 37)));

        // ~ individual tokens within the statement
        // let Statement::Merge(Merge { merge_token, optimizer_hints: _, into: _, table: _, source: _,
        //     on: _, clauses, output }) = &r[0] else { panic!("not a MERGE statement"); };
        if (r[0] is not Statement.Merge { Value: { MergeToken: var mergeToken, Clauses: var clauses, Output: var output } })
        {
            Assert.Fail("not a MERGE statement");
            return;
        }
        Assert.Equal(
            mergeToken.Value.Span,
            new Span(new Location(4, 9), new Location(4, 14)));
        Assert.Equal(5, clauses.Count);

        // ~ the INSERT clause's TOKENs
        Assert.Equal(
            clauses[0].WhenToken.Value.Span,
            new Span(new Location(7, 33), new Location(7, 37)));
        if (clauses[0].Action is MergeAction.Insert { Value: { InsertToken: var insertToken, KindToken: var kindToken } })
        {
            Assert.Equal(
                insertToken.Value.Span,
                new Span(new Location(8, 13), new Location(8, 19)));
            Assert.Equal(
                kindToken.Value.Span,
                new Span(new Location(9, 16), new Location(9, 22)));
        }
        else
        {
            Assert.Fail("not a MERGE INSERT clause");
        }

        // ~ the UPDATE token(s)
        Assert.Equal(
            clauses[1].WhenToken.Value.Span,
            new Span(new Location(12, 17), new Location(12, 21)));
        // MergeAction::Update(MergeUpdateExpr { update_token, kind: _, update_predicate: _, delete_predicate: _ })
        if (clauses[1].Action is MergeAction.Update { Value: { UpdateToken: var updateToken } })
        {
            Assert.Equal(
                updateToken.Value.Span,
                new Span(new Location(13, 13), new Location(13, 19)));
        }
        else
        {
            Assert.Fail("not a MERGE UPDATE clause");
        }

        // the DELETE token(s)
        Assert.Equal(
            clauses[2].WhenToken.Value.Span,
            new Span(new Location(15, 15), new Location(15, 19)));
        if (clauses[2].Action is MergeAction.Delete { DeleteToken: var deleteToken })
        {
            Assert.Equal(
                deleteToken.Value.Span,
                new Span(new Location(15, 61), new Location(15, 67)));
        }
        else
        {
            Assert.Fail("not a MERGE DELETE clause");
        }

        // ~ an INSERT clause's ROW token
        Assert.Equal(
            clauses[3].WhenToken.Value.Span,
            new Span(new Location(16, 9), new Location(16, 13)));
        // Os `insert_token`/`kind_token` do Rust repetem os nomes do primeiro `if let`; em C# as
        // variáveis de padrão de um `if` valem no bloco inteiro (caso 72): sufixo `Row`.
        if (clauses[3].Action is MergeAction.Insert { Value: { InsertToken: var insertTokenRow, KindToken: var kindTokenRow } })
        {
            Assert.Equal(
                insertTokenRow.Value.Span,
                new Span(new Location(16, 37), new Location(16, 43)));
            Assert.Equal(
                kindTokenRow.Value.Span,
                new Span(new Location(16, 64), new Location(16, 67)));
        }
        else
        {
            Assert.Fail("not a MERGE INSERT clause");
        }

        Assert.Equal(
            clauses[4].WhenToken.Value.Span,
            new Span(new Location(17, 9), new Location(17, 13)));
        if (clauses[4].Action is MergeAction.DoNothing { DoToken: var doToken, NothingToken: var nothingToken })
        {
            Assert.Equal(
                doToken.Value.Span,
                new Span(new Location(17, 27), new Location(17, 29)));
            Assert.Equal(
                nothingToken.Value.Span,
                new Span(new Location(17, 30), new Location(17, 37)));
            Assert.Equal(
                clauses[4].Action.Span(),
                new Span(new Location(17, 27), new Location(17, 37)));
        }
        else
        {
            Assert.Fail("not a MERGE DO NOTHING clause");
        }

        Assert.True(output is null);
    }

    [Fact]
    public void test_merge_statement_spans_with_returning()
    {
        var sql =
            "\n" +
            "    MERGE INTO wines AS w\n" +
            "    USING wine_stock_changes AS s\n" +
            "        ON s.winename = w.winename\n" +
            "    WHEN NOT MATCHED AND s.stock_delta > 0 THEN INSERT VALUES (s.winename, s.stock_delta)\n" +
            "    WHEN MATCHED AND w.stock + s.stock_delta > 0 THEN UPDATE SET stock = w.stock + s.stock_delta\n" +
            "    WHEN MATCHED THEN DELETE\n" +
            "    RETURNING merge_action(), w.*\n" +
            "        ";

        var r = Parser.ParseSql(new GenericDialect(), sql);
        Assert.Equal(1, r.Count);

        // ~ assert the span of the whole statement
        var stmtSpan = r[0].Span();
        Assert.Equal(
            stmtSpan,
            new Span(new Location(2, 5), new Location(8, 34)));

        // ~ individual tokens within the statement
        if (r[0] is Statement.Merge { Value.Output: var output })
        {
            if (output is OutputClause.Returning { ReturningToken: var returningToken })
            {
                Assert.Equal(
                    returningToken.Value.Span,
                    new Span(new Location(8, 5), new Location(8, 14)));
            }
            else
            {
                Assert.Fail("unexpected MERGE output clause");
            }
        }
        else
        {
            Assert.Fail("not a MERGE statement");
        }
    }

    [Fact]
    public void test_merge_statement_spans_with_output()
    {
        var sql =
            "MERGE INTO a USING b ON a.id = b.id\n" +
            "        WHEN MATCHED THEN DELETE\n" +
            "              OUTPUT inserted.*";

        var r = Parser.ParseSql(new GenericDialect(), sql);
        Assert.Equal(1, r.Count);

        // ~ assert the span of the whole statement
        var stmtSpan = r[0].Span();
        Assert.Equal(
            stmtSpan,
            new Span(new Location(1, 1), new Location(3, 32)));

        // ~ individual tokens within the statement
        if (r[0] is Statement.Merge { Value.Output: var output })
        {
            if (output is OutputClause.Output { OutputToken: var outputToken })
            {
                Assert.Equal(
                    outputToken.Value.Span,
                    new Span(new Location(3, 15), new Location(3, 21)));
            }
            else
            {
                Assert.Fail("unexpected MERGE output clause");
            }
        }
        else
        {
            Assert.Fail("not a MERGE statement");
        }
    }

    [Fact]
    public void test_merge_statement_spans_with_update_predicates()
    {
        var sql =
            "\n" +
            "       MERGE INTO a USING b ON a.id = b.id\n" +
            "        WHEN MATCHED THEN\n" +
            "              UPDATE set a.x = a.x + b.x\n" +
            "               WHERE b.x != 2\n" +
            "              DELETE WHERE a.x <> 3";

        var r = Parser.ParseSql(new GenericDialect(), sql);
        Assert.Equal(1, r.Count);

        // ~ assert the span of the whole statement
        var stmtSpan = r[0].Span();
        Assert.Equal(
            stmtSpan,
            new Span(new Location(2, 8), new Location(6, 36)));
    }

    [Fact]
    public void test_merge_statement_spans_with_insert_predicate()
    {
        var sql =
            "\n" +
            "       MERGE INTO a USING b ON a.id = b.id\n" +
            "        WHEN NOT MATCHED THEN\n" +
            "            INSERT VALUES (b.x, b.y) WHERE b.x != 2\n" +
            "-- qed\n";

        var r = Parser.ParseSql(new GenericDialect(), sql);
        Assert.Equal(1, r.Count);

        // ~ assert the span of the whole statement
        var stmtSpan = r[0].Span();
        Assert.Equal(
            stmtSpan,
            new Span(new Location(2, 8), new Location(4, 52)));
    }
}
