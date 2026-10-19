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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/query.rs.
// Modificado: tradução de Rust para C#.

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>A single column definition in XMLTABLE</summary>
/// <remarks>
/// <code>
/// COLUMNS
///     id int PATH '@id',
///     ordinality FOR ORDINALITY,
///     "COUNTRY_NAME" text,
///     country_id text PATH 'COUNTRY_ID',
///     size_sq_km float PATH 'SIZE[@unit = "sq_km"]',
///     size_other text PATH 'concat(SIZE[@unit!="sq_km"], " ", SIZE[@unit!="sq_km"]/@unit)',
///     premier_name text PATH 'PREMIER_NAME' DEFAULT 'not specified'
/// </code>
/// </remarks>
/// <param name="Name">The name of the column.</param>
/// <param name="Option">Column options: type/path/default or FOR ORDINALITY</param>
public sealed partial record XmlTableColumn(Ident Name, XmlTableColumnOption Option) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{Name}");
        switch (Option)
        {
            case XmlTableColumnOption.NamedInfo { Type: var type, Path: var path, Default: var @default, Nullable: var nullable }:
                f.Write($" {type}");
                if (path is { } p)
                {
                    f.Write($" PATH {p}");
                }
                if (@default is { } d)
                {
                    f.Write($" DEFAULT {d}");
                }
                if (!nullable)
                {
                    f.Write($" NOT NULL");
                }
                break;
            case XmlTableColumnOption.ForOrdinality:
                f.Write($" FOR ORDINALITY");
                break;
            default:
                throw new InvalidOperationException($"Variante de XmlTableColumnOption não tratada: {Option.GetType().Name}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
