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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/query.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Ast;

/// <summary>The mode for handling empty matches in a <c>MATCH_RECOGNIZE</c> operation.</summary>
// Enum sem dados com `Display` → records (convencoes.md §13, caso 37).
public abstract partial record EmptyMatchesMode : IDisplay
{
    /// <summary><c>SHOW EMPTY MATCHES</c></summary>
    public sealed record Show : EmptyMatchesMode;

    /// <summary><c>OMIT EMPTY MATCHES</c></summary>
    public sealed record Omit : EmptyMatchesMode;

    /// <summary><c>WITH UNMATCHED ROWS</c></summary>
    public sealed record WithUnmatched : EmptyMatchesMode;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Show:
                f.Write($"SHOW EMPTY MATCHES");
                break;
            case Omit:
                f.Write($"OMIT EMPTY MATCHES");
                break;
            case WithUnmatched:
                f.Write($"WITH UNMATCHED ROWS");
                break;
            default:
                throw new InvalidOperationException($"Variante de EmptyMatchesMode não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
