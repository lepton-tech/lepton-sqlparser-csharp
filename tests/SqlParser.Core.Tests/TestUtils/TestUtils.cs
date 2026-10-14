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

    // PENDENTE (Fase 2): all_dialects_with_options(options: ParserOptions). Depende de `ParserOptions`.

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

    // PENDENTE (Fase 2): expr_from_projection, alter_table_op_with_name, alter_table_op, number,
    // single_quoted_string, table_alias, table, table_from_name, table_with_alias, join, call,
    // index_column. Dependem da AST.
}
