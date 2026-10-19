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

/// <summary>Alterations that can be applied to an index.</summary>
public abstract partial record AlterIndexOperation : IDisplay
{
    /// <summary>Rename the index to <c>index_name</c>.</summary>
    /// <param name="IndexName">The new name for the index.</param>
    public sealed record RenameIndex(ObjectName IndexName) : AlterIndexOperation;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case RenameIndex renameIndex:
                f.Write($"RENAME TO {renameIndex.IndexName}");
                break;
            default:
                throw new InvalidOperationException($"Variante de AlterIndexOperation não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
