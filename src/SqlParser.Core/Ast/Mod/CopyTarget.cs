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

/// <summary>Target for the <c>COPY</c> command: STDIN, STDOUT, a file, or a program.</summary>
public abstract record CopyTarget : IDisplay
{
    /// <summary>Use standard input as the source.</summary>
    public sealed record Stdin : CopyTarget;

    /// <summary>Use standard output as the target.</summary>
    public sealed record Stdout : CopyTarget;

    /// <summary>Read from or write to a file.</summary>
    /// <param name="Filename">The path name of the input or output file.</param>
    public sealed record File(string Filename) : CopyTarget;

    /// <summary>Use a program as the source or target (shell command).</summary>
    /// <param name="Command">A command to execute</param>
    public sealed record Program(string Command) : CopyTarget;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Stdin:
                f.Write($"STDIN");
                break;
            case Stdout:
                f.Write($"STDOUT");
                break;
            case File file:
                f.Write($"'{Ast.Value.EscapeSingleQuoteString(file.Filename)}'");
                break;
            case Program program:
                f.Write($"PROGRAM '{Ast.Value.EscapeSingleQuoteString(program.Command)}'");
                break;
            default:
                throw new InvalidOperationException($"Variante de CopyTarget não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
