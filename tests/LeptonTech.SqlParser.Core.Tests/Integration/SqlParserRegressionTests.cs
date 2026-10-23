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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_regression.rs.
// Modificado: tradução de Rust para C#. A macro `tpch_tests!` foi expandida: um `[Fact]` por
// consulta, com o nome gerado pelo Rust (`tpch_1`..`tpch_22`). O `include_str!` de
// `queries/tpch/N.sql` vira a leitura do arquivo copiado para a saída do build
// (`Queries/tpch/N.sql`, conteúdo do .csproj).

using System.Text;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core.Tests;

public class SqlParserRegressionTests
{
    // const QUERIES: &[&str] = &[include_str!(concat!("queries/tpch/", $value, ".sql")), ..];
    // QUERIES[$value - 1] → Query(value).
    private static string Query(int value) =>
        File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Queries", "tpch", $"{value}.sql"),
            Encoding.UTF8);

    // fn $name() { .. } gerado pela macro para cada consulta.
    private static void TpchTest(int value)
    {
        var dialect = new GenericDialect();
        Parser.ParseSql(dialect, Query(value));
    }

    [Fact]
    public void tpch_1() => TpchTest(1);

    [Fact]
    public void tpch_2() => TpchTest(2);

    [Fact]
    public void tpch_3() => TpchTest(3);

    [Fact]
    public void tpch_4() => TpchTest(4);

    [Fact]
    public void tpch_5() => TpchTest(5);

    [Fact]
    public void tpch_6() => TpchTest(6);

    [Fact]
    public void tpch_7() => TpchTest(7);

    [Fact]
    public void tpch_8() => TpchTest(8);

    [Fact]
    public void tpch_9() => TpchTest(9);

    [Fact]
    public void tpch_10() => TpchTest(10);

    [Fact]
    public void tpch_11() => TpchTest(11);

    [Fact]
    public void tpch_12() => TpchTest(12);

    [Fact]
    public void tpch_13() => TpchTest(13);

    [Fact]
    public void tpch_14() => TpchTest(14);

    [Fact]
    public void tpch_15() => TpchTest(15);

    [Fact]
    public void tpch_16() => TpchTest(16);

    [Fact]
    public void tpch_17() => TpchTest(17);

    [Fact]
    public void tpch_18() => TpchTest(18);

    [Fact]
    public void tpch_19() => TpchTest(19);

    [Fact]
    public void tpch_20() => TpchTest(20);

    [Fact]
    public void tpch_21() => TpchTest(21);

    [Fact]
    public void tpch_22() => TpchTest(22);
}
