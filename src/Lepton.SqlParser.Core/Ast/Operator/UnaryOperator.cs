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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/operator.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Ast;

/// <summary>Unary operators</summary>
public abstract partial record UnaryOperator : IDisplay
{
    /// <summary>
    /// <c>@-@</c> Length or circumference (PostgreSQL/Redshift geometric operator)
    /// see <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record AtDashAt : UnaryOperator;

    /// <summary>Unary logical not operator: e.g. <c>! false</c> (Hive-specific)</summary>
    public sealed record BangNot : UnaryOperator;

    /// <summary>Bitwise Not, e.g. <c>~9</c></summary>
    public sealed record BitwiseNot : UnaryOperator;

    /// <summary>
    /// <c>@@</c> Center (PostgreSQL/Redshift geometric operator)
    /// see <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record DoubleAt : UnaryOperator;

    /// <summary>
    /// <c>#</c> Number of points in path or polygon (PostgreSQL/Redshift geometric operator)
    /// see <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record Hash : UnaryOperator;

    /// <summary>Plus, e.g. <c>+9</c></summary>
    public sealed record Plus : UnaryOperator;

    /// <summary>Minus, e.g. <c>-9</c></summary>
    public sealed record Minus : UnaryOperator;

    /// <summary>Not, e.g. <c>NOT(true)</c></summary>
    public sealed record Not : UnaryOperator;

    /// <summary>Absolute value, e.g. <c>@ -9</c> (PostgreSQL-specific)</summary>
    public sealed record PGAbs : UnaryOperator;

    /// <summary>Cube root, e.g. <c>||/27</c> (PostgreSQL-specific)</summary>
    public sealed record PGCubeRoot : UnaryOperator;

    /// <summary>Factorial, e.g. <c>9!</c> (PostgreSQL-specific)</summary>
    public sealed record PGPostfixFactorial : UnaryOperator;

    /// <summary>Factorial, e.g. <c>!!9</c> (PostgreSQL-specific)</summary>
    public sealed record PGPrefixFactorial : UnaryOperator;

    /// <summary>Square root, e.g. <c>|/9</c> (PostgreSQL-specific)</summary>
    public sealed record PGSquareRoot : UnaryOperator;

    /// <summary>
    /// <c>?-</c> Is horizontal? (PostgreSQL/Redshift geometric operator)
    /// see <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record QuestionDash : UnaryOperator;

    /// <summary>
    /// <c>?|</c> Is vertical? (PostgreSQL/Redshift geometric operator)
    /// see <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record QuestionPipe : UnaryOperator;

    public void Fmt(Formatter f)
    {
        f.WriteStr(this switch
        {
            AtDashAt => "@-@",
            BangNot => "!",
            BitwiseNot => "~",
            DoubleAt => "@@",
            Hash => "#",
            Minus => "-",
            Not => "NOT",
            PGAbs => "@",
            PGCubeRoot => "||/",
            PGPostfixFactorial => "!",
            PGPrefixFactorial => "!!",
            PGSquareRoot => "|/",
            Plus => "+",
            QuestionDash => "?-",
            QuestionPipe => "?|",
            _ => throw new InvalidOperationException($"Variante de UnaryOperator não tratada: {GetType().Name}"),
        });
    }

    public sealed override string ToString() => Formatter.Format(this);
}
