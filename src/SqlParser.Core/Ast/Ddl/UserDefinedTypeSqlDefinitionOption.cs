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

namespace SqlParser.Core.Ast;

/// <summary>
/// Options for PostgreSQL <c>CREATE TYPE ... (&lt;options&gt;)</c> statement (base type definition).
/// <para>
/// Base types are the lowest-level data types in PostgreSQL. To define a new base type,
/// you must specify functions that convert it to and from text representation, and optionally
/// binary representation and other properties.
/// </para>
/// <para>Note: This syntax uses parentheses directly after the type name, without the <c>AS</c> keyword.</para>
/// <para>PostgreSQL Documentation</para>
/// <para>See: <see href="https://www.postgresql.org/docs/current/sql-createtype.html"/></para>
/// <para>Examples</para>
/// <code>
/// CREATE TYPE complex (
///     INPUT = complex_in,
///     OUTPUT = complex_out,
///     INTERNALLENGTH = 16,
///     ALIGNMENT = double
/// );
/// </code>
/// </summary>
public abstract partial record UserDefinedTypeSqlDefinitionOption : IDisplay
{
    /// <summary>Function to convert from external text representation to internal: <c>INPUT = input_function</c></summary>
    public sealed record Input(ObjectName Value) : UserDefinedTypeSqlDefinitionOption;

    /// <summary>Function to convert from internal to external text representation: <c>OUTPUT = output_function</c></summary>
    public sealed record Output(ObjectName Value) : UserDefinedTypeSqlDefinitionOption;

    /// <summary>Function to convert from external binary representation to internal: <c>RECEIVE = receive_function</c></summary>
    public sealed record Receive(ObjectName Value) : UserDefinedTypeSqlDefinitionOption;

    /// <summary>Function to convert from internal to external binary representation: <c>SEND = send_function</c></summary>
    public sealed record Send(ObjectName Value) : UserDefinedTypeSqlDefinitionOption;

    /// <summary>Function to convert type modifiers from text array to internal form: <c>TYPMOD_IN = type_modifier_input_function</c></summary>
    public sealed record TypmodIn(ObjectName Value) : UserDefinedTypeSqlDefinitionOption;

    /// <summary>Function to convert type modifiers from internal to text form: <c>TYPMOD_OUT = type_modifier_output_function</c></summary>
    public sealed record TypmodOut(ObjectName Value) : UserDefinedTypeSqlDefinitionOption;

    /// <summary>Function to compute statistics for the data type: <c>ANALYZE = analyze_function</c></summary>
    public sealed record Analyze(ObjectName Value) : UserDefinedTypeSqlDefinitionOption;

    /// <summary>Function to handle subscripting operations: <c>SUBSCRIPT = subscript_function</c></summary>
    public sealed record Subscript(ObjectName Value) : UserDefinedTypeSqlDefinitionOption;

    /// <summary>Internal storage size in bytes, or VARIABLE for variable-length: <c>INTERNALLENGTH = { internallength | VARIABLE }</c></summary>
    public sealed record InternalLength(UserDefinedTypeInternalLength Value) : UserDefinedTypeSqlDefinitionOption;

    /// <summary>Indicates values are passed by value rather than by reference: <c>PASSEDBYVALUE</c></summary>
    public sealed record PassedByValue : UserDefinedTypeSqlDefinitionOption;

    /// <summary>Storage alignment requirement (1, 2, 4, or 8 bytes): <c>ALIGNMENT = alignment</c></summary>
    // A variante esconde o tipo de topo homônimo: tipo qualificado (convencoes.md §6 e §13, caso 38).
    public sealed record Alignment(Ast.Alignment Value) : UserDefinedTypeSqlDefinitionOption;

    /// <summary>Storage strategy for varlena types: <c>STORAGE = storage</c></summary>
    public sealed record Storage(UserDefinedTypeStorage Value) : UserDefinedTypeSqlDefinitionOption;

    /// <summary>Copy properties from an existing type: <c>LIKE = like_type</c></summary>
    public sealed record Like(ObjectName Value) : UserDefinedTypeSqlDefinitionOption;

    /// <summary>Type category for implicit casting rules (single char): <c>CATEGORY = category</c></summary>
    // Rust: `Category(char)`. O `char` do Rust é um scalar value, que pode estar fora do BMP; aqui
    // é uma `string` com exatamente um scalar value (1 ou 2 unidades UTF-16), como em `Token.Char`
    // (nomes.md).
    public sealed record Category(string Value) : UserDefinedTypeSqlDefinitionOption;

    /// <summary>Whether this type is preferred within its category: <c>PREFERRED = preferred</c></summary>
    public sealed record Preferred(bool Value) : UserDefinedTypeSqlDefinitionOption;

    /// <summary>Default value for the type: <c>DEFAULT = default</c></summary>
    public sealed record Default(Expr Value) : UserDefinedTypeSqlDefinitionOption;

    /// <summary>Element type for array types: <c>ELEMENT = element</c></summary>
    public sealed record Element(DataType Value) : UserDefinedTypeSqlDefinitionOption;

    /// <summary>Delimiter character for array value display: <c>DELIMITER = delimiter</c></summary>
    public sealed record Delimiter(string Value) : UserDefinedTypeSqlDefinitionOption;

    /// <summary>Whether the type supports collation: <c>COLLATABLE = collatable</c></summary>
    public sealed record Collatable(bool Value) : UserDefinedTypeSqlDefinitionOption;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Input { Value: var name }:
                f.Write($"INPUT = {name}");
                break;
            case Output { Value: var name }:
                f.Write($"OUTPUT = {name}");
                break;
            case Receive { Value: var name }:
                f.Write($"RECEIVE = {name}");
                break;
            case Send { Value: var name }:
                f.Write($"SEND = {name}");
                break;
            case TypmodIn { Value: var name }:
                f.Write($"TYPMOD_IN = {name}");
                break;
            case TypmodOut { Value: var name }:
                f.Write($"TYPMOD_OUT = {name}");
                break;
            case Analyze { Value: var name }:
                f.Write($"ANALYZE = {name}");
                break;
            case Subscript { Value: var name }:
                f.Write($"SUBSCRIPT = {name}");
                break;
            case InternalLength { Value: var len }:
                f.Write($"INTERNALLENGTH = {len}");
                break;
            case PassedByValue:
                f.Write($"PASSEDBYVALUE");
                break;
            case Alignment { Value: var align }:
                f.Write($"ALIGNMENT = {align}");
                break;
            case Storage { Value: var storage }:
                f.Write($"STORAGE = {storage}");
                break;
            case Like { Value: var name }:
                f.Write($"LIKE = {name}");
                break;
            case Category { Value: var c }:
                f.Write($"CATEGORY = '{c}'");
                break;
            case Preferred { Value: var b }:
                f.Write($"PREFERRED = {b}");
                break;
            case Default { Value: var expr }:
                f.Write($"DEFAULT = {expr}");
                break;
            case Element { Value: var dt }:
                f.Write($"ELEMENT = {dt}");
                break;
            case Delimiter { Value: var s }:
                f.Write($"DELIMITER = '{Ast.Value.EscapeSingleQuoteString(s)}'");
                break;
            case Collatable { Value: var b }:
                f.Write($"COLLATABLE = {b}");
                break;
            default:
                throw new InvalidOperationException($"Variante de UserDefinedTypeSqlDefinitionOption não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
