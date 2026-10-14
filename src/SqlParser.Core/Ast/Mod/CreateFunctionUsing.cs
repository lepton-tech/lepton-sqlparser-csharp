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

/// <summary><c>USING</c> clause options for <c>CREATE FUNCTION</c> (e.g., JAR, FILE, ARCHIVE).</summary>
public abstract partial record CreateFunctionUsing : IDisplay
{
    /// <summary>Use a JAR file located at the given URI.</summary>
    public sealed record Jar(string Value) : CreateFunctionUsing;

    /// <summary>Use a file located at the given URI.</summary>
    public sealed record File(string Value) : CreateFunctionUsing;

    /// <summary>Use an archive located at the given URI.</summary>
    public sealed record Archive(string Value) : CreateFunctionUsing;

    public void Fmt(Formatter f)
    {
        f.Write($"USING ");
        switch (this)
        {
            case Jar uri:
                f.Write($"JAR '{uri.Value}'");
                break;
            case File uri:
                f.Write($"FILE '{uri.Value}'");
                break;
            case Archive uri:
                f.Write($"ARCHIVE '{uri.Value}'");
                break;
            default:
                throw new InvalidOperationException($"Variante de CreateFunctionUsing não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
