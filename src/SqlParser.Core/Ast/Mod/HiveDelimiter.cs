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

/// <summary>Kind of delimiter used in Hive <c>ROW FORMAT</c> definitions.</summary>
public abstract record HiveDelimiter : IDisplay
{
    /// <summary>Fields terminated by a delimiter.</summary>
    public sealed record FieldsTerminatedBy : HiveDelimiter;

    /// <summary>Fields escaped by a character.</summary>
    public sealed record FieldsEscapedBy : HiveDelimiter;

    /// <summary>Collection items terminated by a delimiter.</summary>
    public sealed record CollectionItemsTerminatedBy : HiveDelimiter;

    /// <summary>Map keys terminated by a delimiter.</summary>
    public sealed record MapKeysTerminatedBy : HiveDelimiter;

    /// <summary>Lines terminated by a delimiter.</summary>
    public sealed record LinesTerminatedBy : HiveDelimiter;

    /// <summary>Null represented by a specific token.</summary>
    public sealed record NullDefinedAs : HiveDelimiter;

    public void Fmt(Formatter f)
    {
        f.WriteStr(this switch
        {
            FieldsTerminatedBy => "FIELDS TERMINATED BY",
            FieldsEscapedBy => "ESCAPED BY",
            CollectionItemsTerminatedBy => "COLLECTION ITEMS TERMINATED BY",
            MapKeysTerminatedBy => "MAP KEYS TERMINATED BY",
            LinesTerminatedBy => "LINES TERMINATED BY",
            NullDefinedAs => "NULL DEFINED AS",
            _ => throw new InvalidOperationException($"Variante de HiveDelimiter não tratada: {GetType().Name}"),
        });
    }

    public sealed override string ToString() => Formatter.Format(this);
}
