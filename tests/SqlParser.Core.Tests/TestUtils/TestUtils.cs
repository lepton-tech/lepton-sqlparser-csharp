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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/test_utils.rs.
// Modificado: tradução de Rust para C#. Funções livres do módulo `test_utils` → membros estáticos
// da classe `TestUtils` (convencoes.md §4). Só as partes que não dependem do parser; o restante
// está listado em docs/port/progresso.md (Fase 2).

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core.Tests;

/// <summary>
/// This module contains internal utilities used for testing the library.
/// While technically public, the library's users are not supposed to rely
/// on this module, as it will change without notice.
/// </summary>
public static class TestUtils
{
    /// <summary>Returns all available dialects.</summary>
    public static TestedDialects AllDialects()
    {
        return new TestedDialects(new List<Dialect>
        {
            new GenericDialect(),
            new PostgreSqlDialect(),
            new MsSqlDialect(),
            new AnsiDialect(),
            new SnowflakeDialect(),
            new HiveDialect(),
            new RedshiftSqlDialect(),
            new MySqlDialect(),
            new BigQueryDialect(),
            new SQLiteDialect(),
            new DuckDbDialect(),
            new DatabricksDialect(),
            new ClickHouseDialect(),
            new OracleDialect(),
            new TeradataDialect(),
        });
    }

    /// <summary>Returns all available dialects with the specified parser options</summary>
    public static TestedDialects AllDialectsWithOptions(ParserOptions options) =>
        TestedDialects.NewWithOptions(AllDialects().Dialects, options);

    /// <summary>Returns all dialects matching the given predicate.</summary>
    public static TestedDialects AllDialectsWhere(Func<Dialect, bool> predicate)
    {
        var dialects = AllDialects();
        dialects.Dialects.RemoveAll(d => !predicate(d)); // dialects.dialects.retain(|d| predicate(&**d))
        return dialects;
    }

    /// <summary>
    /// Returns available dialects. The <c>except</c> predicate is used
    /// to filter out specific dialects.
    /// </summary>
    public static TestedDialects AllDialectsExcept(Func<Dialect, bool> except)
    {
        return AllDialectsWhere(d => !except(d));
    }

    /// <summary>Rust: <c>assert_eq_vec&lt;T: ToString&gt;(expected: &amp;[&amp;str], actual: &amp;[T])</c>.</summary>
    public static void AssertEqVec<T>(IReadOnlyList<string> expected, IReadOnlyList<T> actual)
    {
        var actualStrings = new List<string>(actual.Count);
        foreach (var a in actual)
        {
            actualStrings.Add(a?.ToString() ?? "");
        }
        Assert.Equal(expected, actualStrings);
    }

    /// <summary>
    /// Tradução de <c>assert_eq!(expected, actual, "msg")</c>: falha com a mensagem do Rust seguida da
    /// diferença do <see cref="Assert.Equal{T}(T, T)"/> (skill <c>port-rust-tests</c>).
    /// </summary>
    public static void AssertEqualWithMessage<T>(T expected, T actual, string message)
    {
        try
        {
            Assert.Equal(expected, actual);
        }
        catch (Xunit.Sdk.XunitException e)
        {
            Assert.Fail($"{message}{Environment.NewLine}{e.Message}");
        }
    }

    /// <summary>Rust: <c>only&lt;T&gt;(v: impl IntoIterator&lt;Item = T&gt;) -&gt; T</c>.</summary>
    public static T Only<T>(IEnumerable<T> v)
    {
        using var iter = v.GetEnumerator();
        if (iter.MoveNext())
        {
            var item = iter.Current;
            if (!iter.MoveNext())
            {
                return item;
            }
        }
        throw new InvalidOperationException("only called on collection without exactly one item");
    }

    /// <summary>Rust: <c>expr_from_projection(item: &amp;SelectItem) -&gt; &amp;Expr</c>.</summary>
    public static Expr ExprFromProjection(SelectItem item) =>
        item switch
        {
            SelectItem.UnnamedExpr expr => expr.Value,
            _ => throw new InvalidOperationException("Expected UnnamedExpr"),
        };

    /// <summary>Rust: <c>alter_table_op_with_name</c>.</summary>
    public static AlterTableOperation AlterTableOpWithName(Statement stmt, string expectedName)
    {
        switch (stmt)
        {
            case Statement.AlterTable alterTableStmt:
            {
                var alterTable = alterTableStmt.Value;
                Assert.Equal(alterTable.Name.ToString(), expectedName);
                Assert.False(alterTable.IfExists);
                Assert.False(alterTable.Only);
                Assert.Null(alterTable.TableType);
                return Only(alterTable.Operations);
            }
            default:
                throw new InvalidOperationException("Expected ALTER TABLE statement");
        }
    }

    /// <summary>Rust: <c>alter_table_op</c>.</summary>
    public static AlterTableOperation AlterTableOp(Statement stmt) => AlterTableOpWithName(stmt, "tab");

    /// <summary>Rust: <c>number(n: &amp;str) -&gt; Value</c> (sem a feature <c>bigdecimal</c>, o número é um <c>String</c>).</summary>
    public static Value Number(string n) => new Value.Number(n, false);

    /// <summary>Rust: <c>single_quoted_string</c>.</summary>
    public static Value SingleQuotedString(string s) => new Value.SingleQuotedString(s);

    /// <summary>Rust: <c>table_alias(explicit: bool, name) -&gt; Option&lt;TableAlias&gt;</c>.</summary>
    public static Ast.TableAlias? TableAlias(bool @explicit, string name) =>
        new Ast.TableAlias(Explicit: @explicit, Name: new Ident(name), Columns: Sequence<TableAliasColumnDef>.Empty, At: null);

    /// <summary>Rust: <c>table(name) -&gt; TableFactor</c>.</summary>
    public static TableFactor Table(string name) =>
        new TableFactor.Table(
            Name: ObjectName.From(Sequence.Of(new Ident(name))),
            Alias: null,
            Args: null,
            WithHints: Sequence<Expr>.Empty,
            Version: null,
            WithOrdinality: false,
            Partitions: Sequence<Ident>.Empty,
            JsonPath: null,
            Sample: null,
            IndexHints: Sequence<TableIndexHints>.Empty);

    /// <summary>Rust: <c>table_from_name(name: ObjectName) -&gt; TableFactor</c>.</summary>
    public static TableFactor TableFromName(ObjectName name) =>
        new TableFactor.Table(
            Name: name,
            Alias: null,
            Args: null,
            WithHints: Sequence<Expr>.Empty,
            Version: null,
            WithOrdinality: false,
            Partitions: Sequence<Ident>.Empty,
            JsonPath: null,
            Sample: null,
            IndexHints: Sequence<TableIndexHints>.Empty);

    /// <summary>Rust: <c>table_with_alias(name, with_as_keyword, alias) -&gt; TableFactor</c>.</summary>
    public static TableFactor TableWithAlias(string name, bool withAsKeyword, string alias) =>
        new TableFactor.Table(
            Name: ObjectName.From(Sequence.Of(new Ident(name))),
            Alias: TableAlias(withAsKeyword, alias),
            Args: null,
            WithHints: Sequence<Expr>.Empty,
            Version: null,
            WithOrdinality: false,
            Partitions: Sequence<Ident>.Empty,
            JsonPath: null,
            Sample: null,
            IndexHints: Sequence<TableIndexHints>.Empty);

    /// <summary>Rust: <c>join(relation: TableFactor) -&gt; Join</c>.</summary>
    public static Ast.Join Join(TableFactor relation) =>
        new Ast.Join(
            Relation: relation,
            Global: false,
            JoinOperator: new JoinOperator.Join(new JoinConstraint.Natural()));

    /// <summary>Rust: <c>call(function: &amp;str, args: impl IntoIterator&lt;Item = Expr&gt;) -&gt; Expr</c>.</summary>
    public static Expr Call(string function, IEnumerable<Expr> args) =>
        new Expr.Function(new Function(
            Name: ObjectName.From(Sequence.Of(new Ident(function))),
            UsesOdbcSyntax: false,
            Parameters: new FunctionArguments.None(),
            Args: new FunctionArguments.List(new FunctionArgumentList(
                DuplicateTreatment: null,
                Args: args.Select(arg => (FunctionArg)new FunctionArg.Unnamed(new FunctionArgExpr.Expr(arg))).ToSequence(),
                Clauses: Sequence<FunctionArgumentClause>.Empty)),
            WithinGroup: Sequence<OrderByExpr>.Empty,
            Filter: null,
            NullTreatment: null,
            Over: null));

    private const string IndexConstraintPanic =
        "Expected an index, unique, primary, full text, or spatial constraint (foreign key does not support general key part expressions)";

    /// <summary>Rust: <c>index_column(stmt: Statement) -&gt; Expr</c>.</summary>
    public static Expr IndexColumn(Statement stmt)
    {
        switch (stmt)
        {
            case Statement.CreateIndex createIndex:
                return createIndex.Value.Columns.First!.Column.Expr;
            case Statement.CreateTable createTable:
                return ConstraintFirstColumnExpr(createTable.Value.Constraints.First!);
            case Statement.AlterTable alterTable:
                switch (alterTable.Value.Operations.First)
                {
                    case AlterTableOperation.AddConstraint addConstraint:
                        return ConstraintFirstColumnExpr(addConstraint.Constraint);
                    default:
                        throw new InvalidOperationException("Expected a constraint");
                }
            default:
                throw new InvalidOperationException($"Expected CREATE INDEX, ALTER TABLE, or CREATE TABLE, got: {stmt}");
        }
    }

    private static Expr ConstraintFirstColumnExpr(TableConstraint constraint) =>
        constraint switch
        {
            TableConstraint.Index c => c.Value.Columns.First!.Column.Expr,
            TableConstraint.Unique c => c.Value.Columns.First!.Column.Expr,
            TableConstraint.PrimaryKey c => c.Value.Columns.First!.Column.Expr,
            TableConstraint.FulltextOrSpatial c => c.Value.Columns.First!.Column.Expr,
            _ => throw new InvalidOperationException(IndexConstraintPanic),
        };
}
