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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>An <see cref="AlterType"/> operation</summary>
public abstract partial record AlterTypeOperation : IDisplay
{
    /// <summary>Rename the type.</summary>
    public sealed record Rename(AlterTypeRename Value) : AlterTypeOperation;

    /// <summary>Add a new value to the type (for enum-like types).</summary>
    public sealed record AddValue(AlterTypeAddValue Value) : AlterTypeOperation;

    /// <summary>Rename an existing value of the type.</summary>
    public sealed record RenameValue(AlterTypeRenameValue Value) : AlterTypeOperation;

    // Rust: impl fmt::Display for AlterTypeOperation (declarado depois de AlterTypeRenameValue em ddl.rs).
    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Rename { Value: var rename }:
                f.Write($"RENAME TO {rename.NewName}");
                break;
            case AddValue { Value: var addValue }:
            {
                f.Write($"ADD VALUE");
                if (addValue.IfNotExists)
                {
                    f.Write($" IF NOT EXISTS");
                }
                f.Write($" {addValue.Value}");
                switch (addValue.Position)
                {
                    case AlterTypeAddValuePosition.Before { Value: var neighborValue }:
                        f.Write($" BEFORE {neighborValue}");
                        break;
                    case AlterTypeAddValuePosition.After { Value: var neighborValue }:
                        f.Write($" AFTER {neighborValue}");
                        break;
                    case null:
                        break;
                    case { } other:
                        throw new InvalidOperationException(
                            $"Variante de AlterTypeAddValuePosition não tratada: {other.GetType().Name}");
                }
                break;
            }
            case RenameValue { Value: var renameValue }:
                f.Write($"RENAME VALUE {renameValue.From} TO {renameValue.To}");
                break;
            default:
                throw new InvalidOperationException($"Variante de AlterTypeOperation não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
