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

/// <summary>A name of a table, view, custom type, etc., possibly multi-part, i.e. db.schema.obj</summary>
// Value: Rust: campo `.0` do tuple struct (convencoes.md §13, caso 35).
public sealed partial record ObjectName(Sequence<ObjectNamePart> Value) : IDisplay
{
    // Rust: `impl From<Vec<Ident>> for ObjectName`.
    public static ObjectName From(Sequence<Ident> idents) =>
        new ObjectName(idents.Select(ident => (ObjectNamePart)new ObjectNamePart.Identifier(ident)).ToSequence());

    // Rust: `impl From<Ident> for ObjectName`.
    public static ObjectName From(Ident ident) =>
        new ObjectName(Sequence.Of<ObjectNamePart>(new ObjectNamePart.Identifier(ident)));

    public void Fmt(Formatter f)
    {
        f.Write($"{DisplaySeparated(Value, ".")}");
    }

    public override string ToString() => Formatter.Format(this);
}

/// <summary>A single part of an ObjectName</summary>
public abstract partial record ObjectNamePart : IDisplay
{
    /// <summary>A single identifier part, e.g. <c>schema</c> or <c>table</c>.</summary>
    public sealed record Identifier(Ident Value) : ObjectNamePart;

    /// <summary>A function that returns an identifier (dialect-specific).</summary>
    public sealed record Function(ObjectNamePartFunction Value) : ObjectNamePart;

    /// <summary>Return the identifier if this is an <c>Identifier</c> variant.</summary>
    public Ident? AsIdent() =>
        this switch
        {
            Identifier ident => ident.Value,
            Function => null,
            _ => throw new InvalidOperationException($"Variante de ObjectNamePart não tratada: {GetType().Name}"),
        };

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Identifier ident:
                f.Write($"{ident.Value}");
                break;
            case Function func:
                f.Write($"{func.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de ObjectNamePart não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}

/// <summary>
/// An object name part that consists of a function that dynamically
/// constructs identifiers.
/// </summary>
/// <remarks>- <see href="https://docs.snowflake.com/en/sql-reference/identifier-literal">Snowflake</see></remarks>
/// <param name="Name">The function name that produces the object name part.</param>
/// <param name="Args">Function arguments used to compute the identifier.</param>
public sealed partial record ObjectNamePartFunction(Ident Name, Sequence<FunctionArg> Args) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{Name}(");
        f.Write($"{DisplayCommaSeparated(Args)})");
    }

    public override string ToString() => Formatter.Format(this);
}
