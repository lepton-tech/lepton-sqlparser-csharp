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

/// <summary>Parent object types usable with <c>SHOW ... IN &lt;parent&gt;</c> clauses.</summary>
public abstract record ShowStatementInParentType : IDisplay
{
    /// <summary>ACCOUNT parent type for SHOW statements.</summary>
    public sealed record Account : ShowStatementInParentType;

    /// <summary>DATABASE parent type for SHOW statements.</summary>
    public sealed record Database : ShowStatementInParentType;

    /// <summary>SCHEMA parent type for SHOW statements.</summary>
    public sealed record Schema : ShowStatementInParentType;

    /// <summary>TABLE parent type for SHOW statements.</summary>
    public sealed record Table : ShowStatementInParentType;

    /// <summary>VIEW parent type for SHOW statements.</summary>
    public sealed record View : ShowStatementInParentType;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Account:
                f.Write($"ACCOUNT");
                break;
            case Database:
                f.Write($"DATABASE");
                break;
            case Schema:
                f.Write($"SCHEMA");
                break;
            case Table:
                f.Write($"TABLE");
                break;
            case View:
                f.Write($"VIEW");
                break;
            default:
                throw new InvalidOperationException($"Variante de ShowStatementInParentType não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
