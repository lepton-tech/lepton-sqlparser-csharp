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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/value.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>
/// The Unicode Standard defines four normalization forms, which are intended to eliminate
/// certain distinctions between visually or functionally identical characters.
/// </summary>
/// <remarks>
/// See <see href="https://unicode.org/reports/tr15/">Unicode Normalization Forms</see> for details.
/// </remarks>
public abstract record NormalizationForm : IDisplay
{
    /// <summary>Canonical Decomposition, followed by Canonical Composition.</summary>
    public sealed record NFC : NormalizationForm;

    /// <summary>Canonical Decomposition.</summary>
    public sealed record NFD : NormalizationForm;

    /// <summary>Compatibility Decomposition, followed by Canonical Composition.</summary>
    public sealed record NFKC : NormalizationForm;

    /// <summary>Compatibility Decomposition.</summary>
    public sealed record NFKD : NormalizationForm;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case NFC:
                f.Write($"NFC");
                break;
            case NFD:
                f.Write($"NFD");
                break;
            case NFKC:
                f.Write($"NFKC");
                break;
            case NFKD:
                f.Write($"NFKD");
                break;
            default:
                throw new InvalidOperationException($"Variante de NormalizationForm não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
