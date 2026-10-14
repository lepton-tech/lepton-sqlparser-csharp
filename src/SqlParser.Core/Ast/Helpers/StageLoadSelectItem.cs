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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/helpers/stmt_data_loading.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>A single item in the <c>SELECT</c> list for data loading from staged files.</summary>
/// <param name="Alias">Optional alias for the input source.</param>
/// <param name="FileColNum">Column number within the staged file (1-based).</param>
/// <param name="Element">Optional element identifier following the column reference.</param>
/// <param name="ItemAs">Optional alias for the item (AS clause).</param>
public sealed partial record StageLoadSelectItem(Ident? Alias, int FileColNum, Ident? Element, Ident? ItemAs) : IDisplay
{
    public void Fmt(Formatter f)
    {
        if (Alias is { } alias)
        {
            f.Write($"{alias}.");
        }
        f.Write($"${FileColNum}");
        if (Element is { } element)
        {
            f.Write($":{element}");
        }
        if (ItemAs is { } itemAs)
        {
            f.Write($" AS {itemAs}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
