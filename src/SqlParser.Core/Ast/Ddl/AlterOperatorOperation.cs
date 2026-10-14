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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/ddl.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>An <see cref="AlterOperator"/> operation</summary>
public abstract partial record AlterOperatorOperation : IDisplay
{
    /// <summary><c>OWNER TO { new_owner | CURRENT_ROLE | CURRENT_USER | SESSION_USER }</c></summary>
    public sealed record OwnerTo(Owner Value) : AlterOperatorOperation;

    /// <summary>
    /// <c>SET SCHEMA new_schema</c>
    /// Set the operator's schema name.
    /// </summary>
    /// <param name="SchemaName">New schema name for the operator</param>
    public sealed record SetSchema(ObjectName SchemaName) : AlterOperatorOperation;

    /// <summary><c>SET ( options )</c></summary>
    /// <param name="Options">List of operator options to set</param>
    public sealed record Set(Sequence<OperatorOption> Options) : AlterOperatorOperation;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case OwnerTo { Value: var owner }:
                f.Write($"OWNER TO {owner}");
                break;
            case SetSchema setSchema:
                f.Write($"SET SCHEMA {setSchema.SchemaName}");
                break;
            case Set set:
            {
                f.Write($"SET (");
                for (var i = 0; i < set.Options.Count; i++)
                {
                    var option = set.Options[i];
                    if (i > 0)
                    {
                        f.Write($", ");
                    }
                    f.Write($"{option}");
                }
                f.Write($")");
                break;
            }
            default:
                throw new InvalidOperationException($"Variante de AlterOperatorOperation não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
