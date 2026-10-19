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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/ddl.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Ast;

/// <summary>
/// Options for PostgreSQL <c>CREATE TYPE ... AS RANGE</c> statement.
/// <para>
/// Range types are data types representing a range of values of some element type
/// (called the range's subtype). These options configure the behavior of the range type.
/// </para>
/// <para>PostgreSQL Documentation</para>
/// <para>See: <see href="https://www.postgresql.org/docs/current/sql-createtype.html"/></para>
/// <para>Examples</para>
/// <code>
/// CREATE TYPE int4range AS RANGE (
///     SUBTYPE = int4,
///     SUBTYPE_OPCLASS = int4_ops,
///     CANONICAL = int4range_canonical,
///     SUBTYPE_DIFF = int4range_subdiff
/// );
/// </code>
/// </summary>
public abstract partial record UserDefinedTypeRangeOption : IDisplay
{
    /// <summary>The element type that the range type will represent: <c>SUBTYPE = subtype</c></summary>
    public sealed record Subtype(DataType Value) : UserDefinedTypeRangeOption;

    /// <summary>The operator class for the subtype: <c>SUBTYPE_OPCLASS = subtype_operator_class</c></summary>
    public sealed record SubtypeOpClass(ObjectName Value) : UserDefinedTypeRangeOption;

    /// <summary>Collation to use for ordering the subtype: <c>COLLATION = collation</c></summary>
    public sealed record Collation(ObjectName Value) : UserDefinedTypeRangeOption;

    /// <summary>Function to convert range values to canonical form: <c>CANONICAL = canonical_function</c></summary>
    public sealed record Canonical(ObjectName Value) : UserDefinedTypeRangeOption;

    /// <summary>Function to compute the difference between two subtype values: <c>SUBTYPE_DIFF = subtype_diff_function</c></summary>
    public sealed record SubtypeDiff(ObjectName Value) : UserDefinedTypeRangeOption;

    /// <summary>Name of the corresponding multirange type: <c>MULTIRANGE_TYPE_NAME = multirange_type_name</c></summary>
    public sealed record MultirangeTypeName(ObjectName Value) : UserDefinedTypeRangeOption;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Subtype { Value: var dt }:
                f.Write($"SUBTYPE = {dt}");
                break;
            case SubtypeOpClass { Value: var name }:
                f.Write($"SUBTYPE_OPCLASS = {name}");
                break;
            case Collation { Value: var name }:
                f.Write($"COLLATION = {name}");
                break;
            case Canonical { Value: var name }:
                f.Write($"CANONICAL = {name}");
                break;
            case SubtypeDiff { Value: var name }:
                f.Write($"SUBTYPE_DIFF = {name}");
                break;
            case MultirangeTypeName { Value: var name }:
                f.Write($"MULTIRANGE_TYPE_NAME = {name}");
                break;
            default:
                throw new InvalidOperationException($"Variante de UserDefinedTypeRangeOption não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
