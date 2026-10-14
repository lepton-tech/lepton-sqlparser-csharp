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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>
/// Specifies the partition keys for the unload operation
/// <code>
/// PARTITION BY ( column_name [, ... ] ) [ INCLUDE ]
/// </code>
/// </summary>
/// <param name="Columns">Columns used to partition the unload output.</param>
/// <param name="Include">Whether to include the partition in the output.</param>
public sealed record UnloadPartitionBy(Sequence<Ident> Columns, bool Include) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"PARTITION BY ({DisplayCommaSeparated(Columns)}){(Include ? " INCLUDE" : "")}");
    }

    public override string ToString() => Formatter.Format(this);
}
