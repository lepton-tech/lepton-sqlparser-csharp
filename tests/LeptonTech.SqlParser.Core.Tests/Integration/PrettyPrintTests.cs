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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/pretty_print.rs.
// Modificado: tradução de Rust para C#. As strings `r#"..."#` com várias linhas viraram
// concatenações com "\n" explícito (o arquivo C# pode estar em CRLF; um raw string literal de
// várias linhas carregaria "\r\n"). `#[ignore = ".."]` → `[Fact(Skip = "..")]`.

using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core.Tests;

public class PrettyPrintTests
{
    private static string Prettify(string sql)
    {
        var ast = Parser.ParseSql(new GenericDialect(), sql);
        return Formatter.FormatAlternate(ast[0]); // format!("{:#}", ast[0])
    }

    [Fact]
    public void test_pretty_print_select()
    {
        Assert.Equal(
            Prettify("SELECT a, b, c FROM my_table WHERE x = 1 AND y = 2"),
            ("\n" +
             "SELECT\n" +
             "  a,\n" +
             "  b,\n" +
             "  c\n" +
             "FROM\n" +
             "  my_table\n" +
             "WHERE\n" +
             "  x = 1 AND y = 2\n")
            .Trim());
    }

    [Fact]
    public void test_pretty_print_join()
    {
        Assert.Equal(
            Prettify("SELECT a FROM table1 JOIN table2 ON table1.id = table2.id"),
            ("\n" +
             "SELECT\n" +
             "  a\n" +
             "FROM\n" +
             "  table1\n" +
             "  JOIN table2 ON table1.id = table2.id\n")
            .Trim());
    }

    [Fact]
    public void test_pretty_print_subquery()
    {
        Assert.Equal(
            Prettify("SELECT * FROM (SELECT a, b FROM my_table) AS subquery"),
            ("\n" +
             "SELECT\n" +
             "  *\n" +
             "FROM\n" +
             "  (\n" +
             "    SELECT\n" +
             "      a,\n" +
             "      b\n" +
             "    FROM\n" +
             "      my_table\n" +
             "  ) AS subquery\n")
            .Trim());
    }

    [Fact]
    public void test_pretty_print_union()
    {
        Assert.Equal(
            Prettify("SELECT a FROM table1 UNION SELECT b FROM table2"),
            ("\n" +
             "SELECT\n" +
             "  a\n" +
             "FROM\n" +
             "  table1\n" +
             "UNION\n" +
             "SELECT\n" +
             "  b\n" +
             "FROM\n" +
             "  table2\n")
            .Trim());
    }

    [Fact]
    public void test_pretty_print_group_by()
    {
        Assert.Equal(
            Prettify("SELECT a, COUNT(*) FROM my_table GROUP BY a HAVING COUNT(*) > 1"),
            ("\n" +
             "SELECT\n" +
             "  a,\n" +
             "  COUNT(*)\n" +
             "FROM\n" +
             "  my_table\n" +
             "GROUP BY\n" +
             "  a\n" +
             "HAVING\n" +
             "  COUNT(*) > 1\n")
            .Trim());
    }

    [Fact]
    public void test_pretty_print_cte()
    {
        Assert.Equal(
            Prettify("WITH cte AS (SELECT a, b FROM my_table) SELECT * FROM cte"),
            ("\n" +
             "WITH cte AS (\n" +
             "  SELECT\n" +
             "    a,\n" +
             "    b\n" +
             "  FROM\n" +
             "    my_table\n" +
             ")\n" +
             "SELECT\n" +
             "  *\n" +
             "FROM\n" +
             "  cte\n")
            .Trim());
    }

    [Fact]
    public void test_pretty_print_case_when()
    {
        Assert.Equal(
            Prettify("SELECT CASE WHEN x > 0 THEN 'positive' WHEN x < 0 THEN 'negative' ELSE 'zero' END FROM my_table"),
            ("\n" +
             "SELECT\n" +
             "  CASE\n" +
             "    WHEN x > 0 THEN\n" +
             "      'positive'\n" +
             "    WHEN x < 0 THEN\n" +
             "      'negative'\n" +
             "    ELSE\n" +
             "      'zero'\n" +
             "  END\n" +
             "FROM\n" +
             "  my_table\n").Trim());
    }

    [Fact]
    public void test_pretty_print_window_function()
    {
        Assert.Equal(
            Prettify("SELECT id, value, ROW_NUMBER() OVER (PARTITION BY category ORDER BY value DESC) as rank FROM my_table"),
            ("\n" +
             "SELECT\n" +
             "  id,\n" +
             "  value,\n" +
             "  ROW_NUMBER() OVER (\n" +
             "    PARTITION BY category\n" +
             "    ORDER BY value DESC\n" +
             "  ) AS rank\n" +
             "FROM\n" +
             "  my_table\n").Trim());
    }

    [Fact]
    public void test_pretty_print_multiline_string()
    {
        // assert_eq!(a, b, "A  literal string with a newline should be kept as is. The contents of the string should not be indented.")
        Assert.Equal(
            Prettify("SELECT 'multiline\nstring' AS str"),
            ("\n" +
             "SELECT\n" +
             "  'multiline\n" +
             "string' AS str\n")
            .Trim());
    }

    [Fact]
    public void test_pretty_print_insert_values()
    {
        Assert.Equal(
            Prettify("INSERT INTO my_table (a, b, c) VALUES (1, 2, 3), (4, 5, 6)"),
            ("\n" +
             "INSERT INTO my_table (a, b, c)\n" +
             "VALUES\n" +
             "  (1, 2, 3),\n" +
             "  (4, 5, 6)\n")
            .Trim());
    }

    [Fact]
    public void test_pretty_print_insert_select()
    {
        Assert.Equal(
            Prettify("INSERT INTO my_table (a, b) SELECT x, y FROM source_table RETURNING a AS id"),
            ("\n" +
             "INSERT INTO my_table (a, b)\n" +
             "SELECT\n" +
             "  x,\n" +
             "  y\n" +
             "FROM\n" +
             "  source_table\n" +
             "RETURNING\n" +
             "  a AS id\n")
            .Trim());
    }

    [Fact]
    public void test_pretty_print_update()
    {
        Assert.Equal(
            Prettify("UPDATE my_table SET a = 1, b = 2 WHERE x > 0 RETURNING id, name"),
            ("\n" +
             "UPDATE my_table\n" +
             "SET\n" +
             "  a = 1,\n" +
             "  b = 2\n" +
             "WHERE\n" +
             "  x > 0\n" +
             "RETURNING\n" +
             "  id,\n" +
             "  name\n")
            .Trim());
    }

    [Fact]
    public void test_pretty_print_delete()
    {
        Assert.Equal(
            Prettify("DELETE FROM my_table WHERE x > 0 RETURNING id, name"),
            ("\n" +
             "DELETE FROM\n" +
             "  my_table\n" +
             "WHERE\n" +
             "  x > 0\n" +
             "RETURNING\n" +
             "  id,\n" +
             "  name\n")
            .Trim());

        Assert.Equal(
            Prettify("DELETE table1, table2"),
            ("\n" +
             "DELETE\n" +
             "  table1,\n" +
             "  table2\n")
            .Trim());
    }

    [Fact]
    public void test_pretty_print_create_table()
    {
        Assert.Equal(
            Prettify("CREATE TABLE my_table (id INT PRIMARY KEY, name VARCHAR(255) NOT NULL, CONSTRAINT fk_other FOREIGN KEY (id) REFERENCES other_table(id))"),
            ("\n" +
             "CREATE TABLE my_table (\n" +
             "  id INT PRIMARY KEY,\n" +
             "  name VARCHAR(255) NOT NULL,\n" +
             "  CONSTRAINT fk_other FOREIGN KEY (id) REFERENCES other_table(id)\n" +
             ")\n")
            .Trim());
    }

    [Fact]
    public void test_pretty_print_create_view()
    {
        Assert.Equal(
            Prettify("CREATE VIEW my_view AS SELECT a, b FROM my_table WHERE x > 0"),
            ("\n" +
             "CREATE VIEW my_view AS\n" +
             "SELECT\n" +
             "  a,\n" +
             "  b\n" +
             "FROM\n" +
             "  my_table\n" +
             "WHERE\n" +
             "  x > 0\n")
            .Trim());
    }

    [Fact(Skip = "#[ignore] no Rust: https://github.com/apache/datafusion-sqlparser-rs/issues/1850")]
    public void test_pretty_print_create_function()
    {
        Assert.Equal(
            Prettify("CREATE FUNCTION my_func() RETURNS INT BEGIN SELECT COUNT(*) INTO @count FROM my_table; RETURN @count; END"),
            ("\n" +
             "CREATE FUNCTION my_func() RETURNS INT\n" +
             "BEGIN\n" +
             "  SELECT COUNT(*) INTO @count FROM my_table;\n" +
             "  RETURN @count;\n" +
             "END\n")
            .Trim());
    }

    [Fact(Skip = "#[ignore] no Rust: https://github.com/apache/datafusion-sqlparser-rs/issues/1850")]
    public void test_pretty_print_json_table()
    {
        Assert.Equal(
            Prettify("SELECT * FROM JSON_TABLE(@json, '$[*]' COLUMNS (id INT PATH '$.id', name VARCHAR(255) PATH '$.name')) AS jt"),
            ("\n" +
             "SELECT\n" +
             "  *\n" +
             "FROM\n" +
             "  JSON_TABLE(\n" +
             "    @json,\n" +
             "    '$[*]' COLUMNS (\n" +
             "      id INT PATH '$.id',\n" +
             "      name VARCHAR(255) PATH '$.name'\n" +
             "    )\n" +
             "  ) AS jt\n")
            .Trim());
    }

    [Fact(Skip = "#[ignore] no Rust: https://github.com/apache/datafusion-sqlparser-rs/issues/1850")]
    public void test_pretty_print_transaction_blocks()
    {
        Assert.Equal(
            Prettify("BEGIN; UPDATE my_table SET x = 1; COMMIT;"),
            ("\n" +
             "BEGIN;\n" +
             "UPDATE my_table SET x = 1;\n" +
             "COMMIT;\n")
            .Trim());
    }

    [Fact(Skip = "#[ignore] no Rust: https://github.com/apache/datafusion-sqlparser-rs/issues/1850")]
    public void test_pretty_print_control_flow()
    {
        Assert.Equal(
            Prettify("IF x > 0 THEN SELECT 'positive'; ELSE SELECT 'negative'; END IF;"),
            ("\n" +
             "IF x > 0 THEN\n" +
             "  SELECT 'positive';\n" +
             "ELSE\n" +
             "  SELECT 'negative';\n" +
             "END IF;\n")
            .Trim());
    }

    [Fact(Skip = "#[ignore] no Rust: https://github.com/apache/datafusion-sqlparser-rs/issues/1850")]
    public void test_pretty_print_merge()
    {
        Assert.Equal(
            Prettify("MERGE INTO target_table t USING source_table s ON t.id = s.id WHEN MATCHED THEN UPDATE SET t.value = s.value WHEN NOT MATCHED THEN INSERT (id, value) VALUES (s.id, s.value)"),
            ("\n" +
             "MERGE INTO target_table t\n" +
             "USING source_table s ON t.id = s.id\n" +
             "WHEN MATCHED THEN\n" +
             "  UPDATE SET t.value = s.value\n" +
             "WHEN NOT MATCHED THEN\n" +
             "  INSERT (id, value) VALUES (s.id, s.value)\n")
            .Trim());
    }

    [Fact(Skip = "#[ignore] no Rust: https://github.com/apache/datafusion-sqlparser-rs/issues/1850")]
    public void test_pretty_print_create_index()
    {
        Assert.Equal(
            Prettify("CREATE INDEX idx_name ON my_table (column1, column2)"),
            ("\n" +
             "CREATE INDEX idx_name\n" +
             "ON my_table (column1, column2)\n")
            .Trim());
    }

    [Fact(Skip = "#[ignore] no Rust: https://github.com/apache/datafusion-sqlparser-rs/issues/1850")]
    public void test_pretty_print_explain()
    {
        Assert.Equal(
            Prettify("EXPLAIN ANALYZE SELECT * FROM my_table WHERE x > 0"),
            ("\n" +
             "EXPLAIN ANALYZE\n" +
             "SELECT\n" +
             "  *\n" +
             "FROM\n" +
             "  my_table\n" +
             "WHERE\n" +
             "  x > 0\n")
            .Trim());
    }
}
