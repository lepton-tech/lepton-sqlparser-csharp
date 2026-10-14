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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/value.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>The side on which <c>TRIM</c> should be applied.</summary>
/// <remarks>Corresponds to <c>TRIM(BOTH|LEADING|TRAILING)</c> SQL syntax.</remarks>
public abstract record TrimWhereField : IDisplay
{
    /// <summary><c>BOTH</c> (trim from both ends)</summary>
    public sealed record Both : TrimWhereField;

    /// <summary><c>LEADING</c> (trim from start)</summary>
    public sealed record Leading : TrimWhereField;

    /// <summary><c>TRAILING</c> (trim from end)</summary>
    public sealed record Trailing : TrimWhereField;

    public void Fmt(Formatter f)
    {
        f.WriteStr(this switch
        {
            Both => "BOTH",
            Leading => "LEADING",
            Trailing => "TRAILING",
            _ => throw new InvalidOperationException($"Variante de TrimWhereField não tratada: {GetType().Name}"),
        });
    }

    public sealed override string ToString() => Formatter.Format(this);
}
