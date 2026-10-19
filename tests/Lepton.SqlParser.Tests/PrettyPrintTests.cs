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
// Casos derivados de sqlparser-rs v0.63.0, tests/pretty_print.rs (SQL de entrada e saída esperada).
// Modificado: os casos exercitam ToSql(pretty: true) da API pública, e não Parser::parse_sql + {:#}.
// Não substitui o port fiel de tests/pretty_print.rs, que pertence a Lepton.SqlParser.Core.Tests.
// As saídas esperadas usam "\n" explícito, para não depender do fim de linha do arquivo-fonte.

namespace Lepton.SqlParser.Tests;

public class PrettyPrintTests
{
    private static string Prettify(string sql) => new SqlQueryParser().Parse(sql)[0].ToSql(pretty: true);

    private static string Lines(params string[] lines) => string.Join("\n", lines);

    [Fact]
    public void Pretty_select()
    {
        Assert.Equal(
            Lines("SELECT", "  a,", "  b,", "  c", "FROM", "  my_table", "WHERE", "  x = 1 AND y = 2"),
            Prettify("SELECT a, b, c FROM my_table WHERE x = 1 AND y = 2"));
    }

    [Fact]
    public void Pretty_join()
    {
        Assert.Equal(
            Lines("SELECT", "  a", "FROM", "  table1", "  JOIN table2 ON table1.id = table2.id"),
            Prettify("SELECT a FROM table1 JOIN table2 ON table1.id = table2.id"));
    }

    [Fact]
    public void Pretty_subquery()
    {
        Assert.Equal(
            Lines(
                "SELECT",
                "  *",
                "FROM",
                "  (",
                "    SELECT",
                "      a,",
                "      b",
                "    FROM",
                "      my_table",
                "  ) AS subquery"),
            Prettify("SELECT * FROM (SELECT a, b FROM my_table) AS subquery"));
    }

    [Fact]
    public void Pretty_group_by()
    {
        Assert.Equal(
            Lines("SELECT", "  a,", "  COUNT(*)", "FROM", "  my_table", "GROUP BY", "  a", "HAVING", "  COUNT(*) > 1"),
            Prettify("SELECT a, COUNT(*) FROM my_table GROUP BY a HAVING COUNT(*) > 1"));
    }

    [Fact]
    public void Pretty_cte()
    {
        Assert.Equal(
            Lines(
                "WITH cte AS (",
                "  SELECT",
                "    a,",
                "    b",
                "  FROM",
                "    my_table",
                ")",
                "SELECT",
                "  *",
                "FROM",
                "  cte"),
            Prettify("WITH cte AS (SELECT a, b FROM my_table) SELECT * FROM cte"));
    }

    [Fact]
    public void Pretty_case_when()
    {
        Assert.Equal(
            Lines(
                "SELECT",
                "  CASE",
                "    WHEN x > 0 THEN",
                "      'positive'",
                "    WHEN x < 0 THEN",
                "      'negative'",
                "    ELSE",
                "      'zero'",
                "  END",
                "FROM",
                "  my_table"),
            Prettify("SELECT CASE WHEN x > 0 THEN 'positive' WHEN x < 0 THEN 'negative' ELSE 'zero' END FROM my_table"));
    }

    [Fact]
    public void Pretty_window_function()
    {
        Assert.Equal(
            Lines(
                "SELECT",
                "  id,",
                "  value,",
                "  ROW_NUMBER() OVER (",
                "    PARTITION BY category",
                "    ORDER BY value DESC",
                "  ) AS rank",
                "FROM",
                "  my_table"),
            Prettify("SELECT id, value, ROW_NUMBER() OVER (PARTITION BY category ORDER BY value DESC) as rank FROM my_table"));
    }

    [Fact]
    public void Pretty_multiline_string()
    {
        // A literal string with a newline should be kept as is. The contents of the string should not be indented.
        Assert.Equal(
            Lines("SELECT", "  'multiline", "string' AS str"),
            Prettify("SELECT 'multiline\nstring' AS str"));
    }

    [Fact]
    public void Pretty_insert_values()
    {
        Assert.Equal(
            Lines("INSERT INTO my_table (a, b, c)", "VALUES", "  (1, 2, 3),", "  (4, 5, 6)"),
            Prettify("INSERT INTO my_table (a, b, c) VALUES (1, 2, 3), (4, 5, 6)"));
    }

    [Fact]
    public void Pretty_update()
    {
        Assert.Equal(
            Lines("UPDATE my_table", "SET", "  a = 1,", "  b = 2", "WHERE", "  x > 0", "RETURNING", "  id,", "  name"),
            Prettify("UPDATE my_table SET a = 1, b = 2 WHERE x > 0 RETURNING id, name"));
    }

    [Fact]
    public void Pretty_create_table()
    {
        Assert.Equal(
            Lines(
                "CREATE TABLE my_table (",
                "  id INT PRIMARY KEY,",
                "  name VARCHAR(255) NOT NULL,",
                "  CONSTRAINT fk_other FOREIGN KEY (id) REFERENCES other_table(id)",
                ")"),
            Prettify("CREATE TABLE my_table (id INT PRIMARY KEY, name VARCHAR(255) NOT NULL, CONSTRAINT fk_other FOREIGN KEY (id) REFERENCES other_table(id))"));
    }

    [Fact]
    public void Pretty_create_view()
    {
        Assert.Equal(
            Lines("CREATE VIEW my_view AS", "SELECT", "  a,", "  b", "FROM", "  my_table", "WHERE", "  x > 0"),
            Prettify("CREATE VIEW my_view AS SELECT a, b FROM my_table WHERE x > 0"));
    }
}
