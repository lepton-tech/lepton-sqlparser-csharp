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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/mod.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>Hive input/output format specification used in <c>CREATE TABLE</c>.</summary>
public abstract partial record HiveIOFormat
{
    /// <summary>Generic IO format with separate input and output expressions.</summary>
    /// <param name="InputFormat">Expression for the input format.</param>
    /// <param name="OutputFormat">Expression for the output format.</param>
    public sealed record IOF(Expr InputFormat, Expr OutputFormat) : HiveIOFormat;

    /// <summary>File format wrapper referencing a <c>FileFormat</c> variant.</summary>
    /// <param name="Format">The file format used for storage.</param>
    public sealed record FileFormat(Ast.FileFormat Format) : HiveIOFormat;

    /// <summary>
    /// <c>USING &lt;format&gt;</c> syntax used by Spark SQL.
    /// <para>Example: <c>CREATE TABLE t (i INT) USING PARQUET</c></para>
    /// <para>See <see href="https://spark.apache.org/docs/latest/sql-ref-syntax-ddl-create-table-datasource.html"/></para>
    /// </summary>
    /// <param name="Format">The data source or format name, e.g. <c>parquet</c>, <c>delta</c>, <c>csv</c>.</param>
    public sealed record Using(Ident Format) : HiveIOFormat;
}
