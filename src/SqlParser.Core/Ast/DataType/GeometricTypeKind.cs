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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/data_type.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>
/// Represents different types of geometric shapes which are commonly used in
/// PostgreSQL/Redshift for spatial operations and geometry-related computations.
/// </summary>
/// <remarks><see href="https://www.postgresql.org/docs/9.5/functions-geometry.html">PostgreSQL</see></remarks>
public abstract partial record GeometricTypeKind : IDisplay
{
    /// <summary>Point geometry</summary>
    public sealed record Point : GeometricTypeKind;

    /// <summary>Line geometry</summary>
    public sealed record Line : GeometricTypeKind;

    /// <summary>Line segment geometry</summary>
    public sealed record LineSegment : GeometricTypeKind;

    /// <summary>Box geometry</summary>
    public sealed record GeometricBox : GeometricTypeKind;

    /// <summary>Path geometry</summary>
    public sealed record GeometricPath : GeometricTypeKind;

    /// <summary>Polygon geometry</summary>
    public sealed record Polygon : GeometricTypeKind;

    /// <summary>Circle geometry</summary>
    public sealed record Circle : GeometricTypeKind;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Point:
                f.Write($"point");
                break;
            case Line:
                f.Write($"line");
                break;
            case LineSegment:
                f.Write($"lseg");
                break;
            case GeometricBox:
                f.Write($"box");
                break;
            case GeometricPath:
                f.Write($"path");
                break;
            case Polygon:
                f.Write($"polygon");
                break;
            case Circle:
                f.Write($"circle");
                break;
            default:
                throw new InvalidOperationException($"Variante de GeometricTypeKind não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
