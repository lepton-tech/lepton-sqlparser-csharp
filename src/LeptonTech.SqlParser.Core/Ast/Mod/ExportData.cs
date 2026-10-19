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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>Represents an <c>EXPORT DATA</c> statement.</summary>
/// <param name="Options">Options for the export operation.</param>
/// <param name="Query">The query producing the data to export.</param>
/// <param name="Connection">Optional named connection to use for export.</param>
public sealed partial record ExportData(Sequence<SqlOption> Options, Query Query, ObjectName? Connection) : IDisplay
{
    public void Fmt(Formatter f)
    {
        if (Connection is { } connection)
        {
            f.Write($"EXPORT DATA WITH CONNECTION {connection} OPTIONS({DisplayCommaSeparated(Options)}) AS {Query}");
        }
        else
        {
            f.Write($"EXPORT DATA OPTIONS({DisplayCommaSeparated(Options)}) AS {Query}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
