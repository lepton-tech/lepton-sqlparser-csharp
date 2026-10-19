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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/helpers/stmt_data_loading.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Ast;

/// <summary>A command to stage files to a named stage.</summary>
/// <param name="Stage">The stage to which files are being staged.</param>
/// <param name="Pattern">Optional file matching <c>PATTERN</c> expression.</param>
public sealed partial record FileStagingCommand(
    [property: VisitWith("visit_relation")] ObjectName Stage,
    string? Pattern) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{Stage}");
        if (Pattern is { } pattern)
        {
            f.Write($" PATTERN='{pattern}'");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
