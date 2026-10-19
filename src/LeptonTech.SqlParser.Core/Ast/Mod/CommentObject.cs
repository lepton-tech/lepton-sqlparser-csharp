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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>Objects that can be targeted by a <c>COMMENT</c> statement.</summary>
public abstract partial record CommentObject : IDisplay
{
    /// <summary>A collation.</summary>
    public sealed record Collation : CommentObject;

    /// <summary>A table column.</summary>
    public sealed record Column : CommentObject;

    /// <summary>A database.</summary>
    public sealed record Database : CommentObject;

    /// <summary>A domain.</summary>
    public sealed record Domain : CommentObject;

    /// <summary>An extension.</summary>
    public sealed record Extension : CommentObject;

    /// <summary>A function.</summary>
    public sealed record Function : CommentObject;

    /// <summary>An index.</summary>
    public sealed record Index : CommentObject;

    /// <summary>A materialized view.</summary>
    public sealed record MaterializedView : CommentObject;

    /// <summary>A procedure.</summary>
    public sealed record Procedure : CommentObject;

    /// <summary>A role.</summary>
    public sealed record Role : CommentObject;

    /// <summary>A schema.</summary>
    public sealed record Schema : CommentObject;

    /// <summary>A sequence.</summary>
    public sealed record Sequence : CommentObject;

    /// <summary>A table.</summary>
    public sealed record Table : CommentObject;

    /// <summary>A type.</summary>
    public sealed record Type : CommentObject;

    /// <summary>A user.</summary>
    public sealed record User : CommentObject;

    /// <summary>A view.</summary>
    public sealed record View : CommentObject;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Collation:
                f.WriteStr("COLLATION");
                break;
            case Column:
                f.WriteStr("COLUMN");
                break;
            case Database:
                f.WriteStr("DATABASE");
                break;
            case Domain:
                f.WriteStr("DOMAIN");
                break;
            case Extension:
                f.WriteStr("EXTENSION");
                break;
            case Function:
                f.WriteStr("FUNCTION");
                break;
            case Index:
                f.WriteStr("INDEX");
                break;
            case MaterializedView:
                f.WriteStr("MATERIALIZED VIEW");
                break;
            case Procedure:
                f.WriteStr("PROCEDURE");
                break;
            case Role:
                f.WriteStr("ROLE");
                break;
            case Schema:
                f.WriteStr("SCHEMA");
                break;
            case Sequence:
                f.WriteStr("SEQUENCE");
                break;
            case Table:
                f.WriteStr("TABLE");
                break;
            case Type:
                f.WriteStr("TYPE");
                break;
            case User:
                f.WriteStr("USER");
                break;
            case View:
                f.WriteStr("VIEW");
                break;
            default:
                throw new InvalidOperationException($"Variante de CommentObject não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
