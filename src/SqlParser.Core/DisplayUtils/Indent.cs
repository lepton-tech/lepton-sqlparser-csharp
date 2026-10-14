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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/display_utils.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core;

/// <summary>A wrapper around a value that adds an indent to the value when displayed with {:#}.</summary>
/// <remarks>
/// No Rust, o mesmo tipo implementa <c>Display</c> (quando <c>T: Display</c>) e <c>Write</c> (quando
/// <c>T: Write</c>). Aqui ele implementa <see cref="IDisplay"/> e <see cref="IFmtWrite"/>; o uso como
/// destino exige que <c>T</c> seja um <see cref="IFmtWrite"/>.
/// </remarks>
internal sealed class Indent<T> : IDisplay, IFmtWrite
{
    public Indent(T value) => Value = value;

    /// <summary>Rust: campo <c>.0</c>.</summary>
    public T Value { get; }

    public void Fmt(Formatter f)
    {
        if (f.Alternate)
        {
            f.WriteStr(DisplayUtils.INDENT);
            // write!(Indent(f), "{:#}", self.0)
            new Formatter(new Indent<Formatter>(f), alternate: true).FmtInPlace(Value);
        }
        else
        {
            f.FmtInPlace(Value);
        }
    }

    /// <summary>Adds an indent to the inner writer</summary>
    public void WriteStr(string s)
    {
        var inner = (IFmtWrite)Value!;
        inner.WriteStr(s);
        // Our NewLine and SpaceOrNewline utils always print individual newlines as a single-character string.
        if (s == "\n")
        {
            inner.WriteStr(DisplayUtils.INDENT);
        }
    }

    public override string ToString() => Formatter.Format(this);
}
