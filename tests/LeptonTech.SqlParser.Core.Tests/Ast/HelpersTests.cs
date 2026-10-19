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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/helpers/stmt_create_table.rs e
// src/ast/helpers/stmt_create_database.rs (mod tests).
// Modificado: tradução de Rust para C#.

using LeptonTech.SqlParser.Core.Ast;

namespace LeptonTech.SqlParser.Core.Tests;

/// <summary>Rust: <c>mod tests</c> de <c>stmt_create_table.rs</c>.</summary>
public class CreateTableBuilderTests
{
    [Fact]
    public void test_from_valid_statement()
    {
        var builder = new CreateTableBuilder(ObjectName.From(Sequence.Of(new Ident("table_name"))));

        var createTable = (builder with { }).Build();
        var stmt = Statement.From(createTable);

        Assert.Equal(builder, CreateTableBuilder.TryFrom(stmt));
    }

    [Fact]
    public void test_from_invalid_statement()
    {
        var stmt = new Statement.Commit(Chain: false, End: false, Modifier: null);

        var ex = Assert.Throws<ParserException>(() => CreateTableBuilder.TryFrom(stmt));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected create table statement, but received: COMMIT"),
            ex.Error);
    }
}

/// <summary>Rust: <c>mod tests</c> de <c>stmt_create_database.rs</c>.</summary>
public class CreateDatabaseBuilderTests
{
    [Fact]
    public void test_from_valid_statement()
    {
        var builder = new CreateDatabaseBuilder(ObjectName.From(Sequence.Of(new Ident("db_name"))));

        var stmt = (builder with { }).Build();

        Assert.Equal(builder, CreateDatabaseBuilder.TryFrom(stmt));
    }

    [Fact]
    public void test_from_invalid_statement()
    {
        var stmt = new Statement.Commit(Chain: false, End: false, Modifier: null);

        var ex = Assert.Throws<ParserException>(() => CreateDatabaseBuilder.TryFrom(stmt));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected create database statement, but received: COMMIT"),
            ex.Error);
    }
}
