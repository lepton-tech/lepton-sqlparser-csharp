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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/tokenizer.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core;

/// <summary>Represents whitespace in the input: spaces, newlines, tabs and comments.</summary>
public abstract partial record Whitespace : IDisplay
{
    /// <summary>A single space character.</summary>
    public sealed record Space : Whitespace;

    /// <summary>A newline character.</summary>
    public sealed record Newline : Whitespace;

    /// <summary>A tab character.</summary>
    public sealed record Tab : Whitespace;

    /// <summary>
    /// A single-line comment (e.g. <c>-- comment</c> or <c># comment</c>).
    /// The <c>comment</c> field contains the text, and <c>prefix</c> contains the comment prefix.
    /// </summary>
    /// <param name="Comment">The content of the comment (without the prefix).</param>
    /// <param name="Prefix">The prefix used for the comment (for example <c>--</c> or <c>#</c>).</param>
    public sealed record SingleLineComment(string Comment, string Prefix) : Whitespace;

    /// <summary>A multi-line comment (without the <c>/* ... */</c> delimiters).</summary>
    public sealed record MultiLineComment(string Value) : Whitespace;

    /// <inheritdoc/>
    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Space:
                f.WriteStr(" ");
                break;
            case Newline:
                f.WriteStr("\n");
                break;
            case Tab:
                f.WriteStr("\t");
                break;
            case SingleLineComment { Prefix: var prefix, Comment: var comment }:
                // writeln!(f, "{prefix}{comment}")
                f.WriteStr(prefix);
                f.WriteStr(comment);
                f.WriteStr("\n");
                break;
            case MultiLineComment { Value: var s }:
                f.WriteStr("/*");
                f.WriteStr(s);
                f.WriteStr("*/");
                break;
            default:
                throw new InvalidOperationException($"Variante de Whitespace não tratada: {GetType().Name}");
        }
    }

    /// <summary>Rust: <c>impl fmt::Display for Whitespace</c>.</summary>
    public sealed override string ToString() => Formatter.Format(this);
}
