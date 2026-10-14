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

/// <summary>
/// <c>RAISERROR</c> options
/// See <see href="https://learn.microsoft.com/en-us/sql/t-sql/language-elements/raiserror-transact-sql?view=sql-server-ver16#options"/>
/// </summary>
// Enum sem dados COM Display → records (convencoes.md §13, caso 37).
public abstract partial record RaisErrorOption : IDisplay
{
    /// <summary>Log the error.</summary>
    public sealed record Log : RaisErrorOption;

    /// <summary>Do not wait for completion.</summary>
    public sealed record NoWait : RaisErrorOption;

    /// <summary>Set the error state.</summary>
    public sealed record SetError : RaisErrorOption;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Log:
                f.Write($"LOG");
                break;
            case NoWait:
                f.Write($"NOWAIT");
                break;
            case SetError:
                f.Write($"SETERROR");
                break;
            default:
                throw new InvalidOperationException($"Variante de RaisErrorOption não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
