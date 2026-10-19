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

namespace Lepton.SqlParser.Core.Ast;

/// <summary><c>FOR XML</c> or <c>FOR JSON</c> clause (MSSQL): formats the output of a query as XML or JSON.</summary>
public abstract partial record ForClause : IDisplay
{
    /// <summary><c>FOR BROWSE</c> clause.</summary>
    public sealed record Browse : ForClause;

    /// <summary><c>FOR JSON ...</c> clause and its options.</summary>
    /// <param name="ForJson">JSON mode (<c>AUTO</c> or <c>PATH</c>).</param>
    /// <param name="Root">Optional <c>ROOT('...')</c> parameter.</param>
    /// <param name="IncludeNullValues"><c>INCLUDE_NULL_VALUES</c> flag.</param>
    /// <param name="WithoutArrayWrapper"><c>WITHOUT_ARRAY_WRAPPER</c> flag.</param>
    public sealed record Json(ForJson ForJson, string? Root, bool IncludeNullValues, bool WithoutArrayWrapper) : ForClause;

    /// <summary><c>FOR XML ...</c> clause and its options.</summary>
    /// <param name="ForXml">XML mode (<c>RAW</c>, <c>AUTO</c>, <c>EXPLICIT</c>, <c>PATH</c>).</param>
    /// <param name="Elements"><c>ELEMENTS</c> flag.</param>
    /// <param name="BinaryBase64"><c>BINARY BASE64</c> flag.</param>
    /// <param name="Root">Optional <c>ROOT('...')</c> parameter.</param>
    /// <param name="Type"><c>TYPE</c> flag.</param>
    public sealed record Xml(ForXml ForXml, bool Elements, bool BinaryBase64, string? Root, bool Type) : ForClause;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Browse:
                f.Write($"FOR BROWSE");
                break;
            case Json { ForJson: var forJson, Root: var root, IncludeNullValues: var includeNullValues, WithoutArrayWrapper: var withoutArrayWrapper }:
                f.Write($"FOR JSON ");
                f.Write($"{forJson}");
                if (root is { } jsonRoot)
                {
                    f.Write($", ROOT('{jsonRoot}')");
                }
                if (includeNullValues)
                {
                    f.Write($", INCLUDE_NULL_VALUES");
                }
                if (withoutArrayWrapper)
                {
                    f.Write($", WITHOUT_ARRAY_WRAPPER");
                }
                break;
            case Xml { ForXml: var forXml, Elements: var elements, BinaryBase64: var binaryBase64, Root: var root, Type: var type }:
                f.Write($"FOR XML ");
                f.Write($"{forXml}");
                if (binaryBase64)
                {
                    f.Write($", BINARY BASE64");
                }
                if (type)
                {
                    f.Write($", TYPE");
                }
                if (root is { } xmlRoot)
                {
                    f.Write($", ROOT('{xmlRoot}')");
                }
                if (elements)
                {
                    f.Write($", ELEMENTS");
                }
                break;
            default:
                throw new InvalidOperationException($"Variante de ForClause não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
