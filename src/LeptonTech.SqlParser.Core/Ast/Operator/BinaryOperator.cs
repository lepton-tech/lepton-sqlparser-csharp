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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>Binary operators</summary>
public abstract partial record BinaryOperator : IDisplay
{
    /// <summary>Plus, e.g. <c>a + b</c></summary>
    public sealed record Plus : BinaryOperator;

    /// <summary>Minus, e.g. <c>a - b</c></summary>
    public sealed record Minus : BinaryOperator;

    /// <summary>Multiply, e.g. <c>a * b</c></summary>
    public sealed record Multiply : BinaryOperator;

    /// <summary>Divide, e.g. <c>a / b</c></summary>
    public sealed record Divide : BinaryOperator;

    /// <summary>Modulo, e.g. <c>a % b</c></summary>
    public sealed record Modulo : BinaryOperator;

    /// <summary>String/Array Concat operator, e.g. <c>a || b</c></summary>
    public sealed record StringConcat : BinaryOperator;

    /// <summary>Greater than, e.g. <c>a &gt; b</c></summary>
    public sealed record Gt : BinaryOperator;

    /// <summary>Less than, e.g. <c>a &lt; b</c></summary>
    public sealed record Lt : BinaryOperator;

    /// <summary>Greater equal, e.g. <c>a &gt;= b</c></summary>
    public sealed record GtEq : BinaryOperator;

    /// <summary>Less equal, e.g. <c>a &lt;= b</c></summary>
    public sealed record LtEq : BinaryOperator;

    /// <summary>Spaceship, e.g. <c>a &lt;=&gt; b</c></summary>
    public sealed record Spaceship : BinaryOperator;

    /// <summary>Equal, e.g. <c>a = b</c></summary>
    public sealed record Eq : BinaryOperator;

    /// <summary>Not equal, e.g. <c>a &lt;&gt; b</c></summary>
    public sealed record NotEq : BinaryOperator;

    /// <summary>And, e.g. <c>a AND b</c></summary>
    public sealed record And : BinaryOperator;

    /// <summary>Or, e.g. <c>a OR b</c></summary>
    public sealed record Or : BinaryOperator;

    /// <summary>XOR, e.g. <c>a XOR b</c></summary>
    public sealed record Xor : BinaryOperator;

    /// <summary>Bitwise or, e.g. <c>a | b</c></summary>
    public sealed record BitwiseOr : BinaryOperator;

    /// <summary>Bitwise and, e.g. <c>a &amp; b</c></summary>
    public sealed record BitwiseAnd : BinaryOperator;

    /// <summary>Bitwise XOR, e.g. <c>a ^ b</c></summary>
    public sealed record BitwiseXor : BinaryOperator;

    /// <summary>Integer division operator <c>//</c> in DuckDB</summary>
    public sealed record DuckIntegerDivide : BinaryOperator;

    /// <summary>MySQL <see href="https://dev.mysql.com/doc/refman/8.0/en/arithmetic-functions.html"><c>DIV</c></see> integer division</summary>
    public sealed record MyIntegerDivide : BinaryOperator;

    /// <summary>
    /// MATCH operator, e.g. <c>a MATCH b</c> (SQLite-specific)
    /// See <see href="https://www.sqlite.org/lang_expr.html#the_like_glob_regexp_match_and_extract_operators"/>
    /// </summary>
    public sealed record Match : BinaryOperator;

    /// <summary>REGEXP operator, e.g. <c>a REGEXP b</c> (SQLite-specific)</summary>
    public sealed record Regexp : BinaryOperator;

    /// <summary>
    /// GLOB operator, e.g. <c>a GLOB b</c> (SQLite-specific)
    /// See <see href="https://www.sqlite.org/lang_expr.html#the_like_glob_regexp_match_and_extract_operators"/>
    /// </summary>
    public sealed record Glob : BinaryOperator;

    /// <summary>Support for custom operators (such as Postgres custom operators)</summary>
    public sealed record Custom(string Value) : BinaryOperator;

    /// <summary>Bitwise XOR, e.g. <c>a # b</c> (PostgreSQL-specific)</summary>
    public sealed record PGBitwiseXor : BinaryOperator;

    /// <summary>Bitwise shift left, e.g. <c>a &lt;&lt; b</c> (PostgreSQL-specific)</summary>
    public sealed record PGBitwiseShiftLeft : BinaryOperator;

    /// <summary>Bitwise shift right, e.g. <c>a &gt;&gt; b</c> (PostgreSQL-specific)</summary>
    public sealed record PGBitwiseShiftRight : BinaryOperator;

    /// <summary>Exponent, e.g. <c>a ^ b</c> (PostgreSQL-specific)</summary>
    public sealed record PGExp : BinaryOperator;

    /// <summary>Overlap operator, e.g. <c>a &amp;&amp; b</c> (PostgreSQL-specific)</summary>
    public sealed record PGOverlap : BinaryOperator;

    /// <summary>String matches regular expression (case sensitively), e.g. <c>a ~ b</c> (PostgreSQL-specific)</summary>
    public sealed record PGRegexMatch : BinaryOperator;

    /// <summary>String matches regular expression (case insensitively), e.g. <c>a ~* b</c> (PostgreSQL-specific)</summary>
    public sealed record PGRegexIMatch : BinaryOperator;

    /// <summary>String does not match regular expression (case sensitively), e.g. <c>a !~ b</c> (PostgreSQL-specific)</summary>
    public sealed record PGRegexNotMatch : BinaryOperator;

    /// <summary>String does not match regular expression (case insensitively), e.g. <c>a !~* b</c> (PostgreSQL-specific)</summary>
    public sealed record PGRegexNotIMatch : BinaryOperator;

    /// <summary>String matches pattern (case sensitively), e.g. <c>a ~~ b</c> (PostgreSQL-specific)</summary>
    public sealed record PGLikeMatch : BinaryOperator;

    /// <summary>String matches pattern (case insensitively), e.g. <c>a ~~* b</c> (PostgreSQL-specific)</summary>
    public sealed record PGILikeMatch : BinaryOperator;

    /// <summary>String does not match pattern (case sensitively), e.g. <c>a !~~ b</c> (PostgreSQL-specific)</summary>
    public sealed record PGNotLikeMatch : BinaryOperator;

    /// <summary>String does not match pattern (case insensitively), e.g. <c>a !~~* b</c> (PostgreSQL-specific)</summary>
    public sealed record PGNotILikeMatch : BinaryOperator;

    /// <summary>String "starts with", eg: <c>a ^@ b</c> (PostgreSQL-specific)</summary>
    public sealed record PGStartsWith : BinaryOperator;

    /// <summary>The <c>-&gt;</c> operator.</summary>
    /// <remarks>
    /// On PostgreSQL, this operator extracts a JSON object field or array
    /// element, for example <c>'{"a":"b"}'::json -&gt; 'a'</c> or <c>[1, 2, 3]'::json
    /// -&gt; 2</c>.
    /// <para>See <see href="https://www.postgresql.org/docs/current/functions-json.html"/>.</para>
    /// </remarks>
    public sealed record Arrow : BinaryOperator;

    /// <summary>The <c>-&gt;&gt;</c> operator.</summary>
    /// <remarks>
    /// On PostgreSQL, this operator extracts a JSON object field or JSON
    /// array element and converts it to text, for example <c>'{"a":"b"}'::json
    /// -&gt;&gt; 'a'</c> or <c>[1, 2, 3]'::json -&gt;&gt; 2</c>.
    /// <para>See <see href="https://www.postgresql.org/docs/current/functions-json.html"/>.</para>
    /// </remarks>
    public sealed record LongArrow : BinaryOperator;

    /// <summary>The <c>#&gt;</c> operator.</summary>
    /// <remarks>
    /// On PostgreSQL, this operator extracts a JSON sub-object at the specified
    /// path, for example:
    /// <code>
    ///'{"a": {"b": ["foo","bar"]}}'::json #&gt; '{a,b,1}'
    /// </code>
    /// See <see href="https://www.postgresql.org/docs/current/functions-json.html"/>.
    /// </remarks>
    public sealed record HashArrow : BinaryOperator;

    /// <summary>The <c>#&gt;&gt;</c> operator.</summary>
    /// <remarks>
    /// A PostgreSQL-specific operator that extracts JSON sub-object at the
    /// specified path, for example
    /// <code>
    ///'{"a": {"b": ["foo","bar"]}}'::json #&gt;&gt; '{a,b,1}'
    /// </code>
    /// See <see href="https://www.postgresql.org/docs/current/functions-json.html"/>.
    /// </remarks>
    public sealed record HashLongArrow : BinaryOperator;

    /// <summary>The <c>@@</c> operator.</summary>
    /// <remarks>
    /// On PostgreSQL, this is used for JSON and text searches.
    /// <para>See <see href="https://www.postgresql.org/docs/current/functions-json.html"/>.</para>
    /// <para>See <see href="https://www.postgresql.org/docs/current/functions-textsearch.html"/>.</para>
    /// </remarks>
    public sealed record AtAt : BinaryOperator;

    /// <summary>The <c>@&gt;</c> operator.</summary>
    /// <remarks>
    /// On PostgreSQL, this is used for JSON and text searches.
    /// <para>See <see href="https://www.postgresql.org/docs/current/functions-json.html"/>.</para>
    /// <para>See <see href="https://www.postgresql.org/docs/current/functions-textsearch.html"/>.</para>
    /// </remarks>
    public sealed record AtArrow : BinaryOperator;

    /// <summary>The <c>&lt;@</c> operator.</summary>
    /// <remarks>
    /// On PostgreSQL, this is used for JSON and text searches.
    /// <para>See <see href="https://www.postgresql.org/docs/current/functions-json.html"/>.</para>
    /// <para>See <see href="https://www.postgresql.org/docs/current/functions-textsearch.html"/>.</para>
    /// </remarks>
    public sealed record ArrowAt : BinaryOperator;

    /// <summary>The <c>#-</c> operator.</summary>
    /// <remarks>
    /// On PostgreSQL, this operator is used to delete a field or array element
    /// at a specified path.
    /// <para>See <see href="https://www.postgresql.org/docs/current/functions-json.html"/>.</para>
    /// </remarks>
    public sealed record HashMinus : BinaryOperator;

    /// <summary>The <c>@?</c> operator.</summary>
    /// <remarks>
    /// On PostgreSQL, this operator is used to check the given JSON path
    /// returns an item for the JSON value.
    /// <para>See <see href="https://www.postgresql.org/docs/current/functions-json.html"/>.</para>
    /// </remarks>
    public sealed record AtQuestion : BinaryOperator;

    /// <summary>The <c>?</c> operator.</summary>
    /// <remarks>
    /// On PostgreSQL, this operator is used to check whether a string exists as a top-level key
    /// within the JSON value
    /// <para>See <see href="https://www.postgresql.org/docs/current/functions-json.html"/>.</para>
    /// </remarks>
    public sealed record Question : BinaryOperator;

    /// <summary>The <c>?&amp;</c> operator.</summary>
    /// <remarks>
    /// On PostgreSQL, this operator is used to check whether all of the the indicated array
    /// members exist as top-level keys.
    /// <para>See <see href="https://www.postgresql.org/docs/current/functions-json.html"/>.</para>
    /// </remarks>
    public sealed record QuestionAnd : BinaryOperator;

    /// <summary>The <c>?|</c> operator.</summary>
    /// <remarks>
    /// On PostgreSQL, this operator is used to check whether any of the the indicated array
    /// members exist as top-level keys.
    /// <para>See <see href="https://www.postgresql.org/docs/current/functions-json.html"/>.</para>
    /// </remarks>
    public sealed record QuestionPipe : BinaryOperator;

    /// <summary>PostgreSQL-specific custom operator.</summary>
    /// <remarks>
    /// See <see href="https://www.postgresql.org/docs/current/sql-createoperator.html">CREATE OPERATOR</see>
    /// for more information.
    /// </remarks>
    public sealed record PGCustomBinaryOperator(Sequence<string> Value) : BinaryOperator;

    /// <summary>The <c>OVERLAPS</c> operator</summary>
    /// <remarks>
    /// Specifies a test for an overlap between two datetime periods:
    /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#overlaps-predicate"/>
    /// </remarks>
    public sealed record Overlaps : BinaryOperator;

    /// <summary>
    /// <c>##</c> Point of closest proximity (PostgreSQL/Redshift geometric operator)
    /// See <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record DoubleHash : BinaryOperator;

    /// <summary>
    /// <c>&lt;-&gt;</c> Distance between (PostgreSQL/Redshift geometric operator)
    /// See <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record LtDashGt : BinaryOperator;

    /// <summary>
    /// <c>&amp;&lt;</c> Overlaps to left? (PostgreSQL/Redshift geometric operator)
    /// See <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record AndLt : BinaryOperator;

    /// <summary>
    /// <c>&amp;&gt;</c> Overlaps to right? (PostgreSQL/Redshift geometric operator)
    /// See <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record AndGt : BinaryOperator;

    /// <summary>
    /// <c>&lt;&lt;|</c> Is strictly below? (PostgreSQL/Redshift geometric operator)
    /// See <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record LtLtPipe : BinaryOperator;

    /// <summary>
    /// <c>|&gt;&gt;</c> Is strictly above? (PostgreSQL/Redshift geometric operator)
    /// See <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record PipeGtGt : BinaryOperator;

    /// <summary>
    /// <c>&amp;&lt;|</c> Does not extend above? (PostgreSQL/Redshift geometric operator)
    /// See <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record AndLtPipe : BinaryOperator;

    /// <summary>
    /// <c>|&amp;&gt;</c> Does not extend below? (PostgreSQL/Redshift geometric operator)
    /// See <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record PipeAndGt : BinaryOperator;

    /// <summary>
    /// <c>&lt;^</c> Is below? (PostgreSQL/Redshift geometric operator)
    /// See <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record LtCaret : BinaryOperator;

    /// <summary>
    /// <c>&gt;^</c> Is above? (PostgreSQL/Redshift geometric operator)
    /// See <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record GtCaret : BinaryOperator;

    /// <summary>
    /// <c>?#</c> Intersects? (PostgreSQL/Redshift geometric operator)
    /// See <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record QuestionHash : BinaryOperator;

    /// <summary>
    /// <c>?-</c> Is horizontal? (PostgreSQL/Redshift geometric operator)
    /// See <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record QuestionDash : BinaryOperator;

    /// <summary>
    /// <c>?-|</c> Is perpendicular? (PostgreSQL/Redshift geometric operator)
    /// See <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record QuestionDashPipe : BinaryOperator;

    /// <summary>
    /// <c>?||</c> Are Parallel? (PostgreSQL/Redshift geometric operator)
    /// See <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record QuestionDoublePipe : BinaryOperator;

    /// <summary>
    /// <c>@</c> Contained or on? (PostgreSQL/Redshift geometric operator)
    /// See <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record At : BinaryOperator;

    /// <summary>
    /// <c>~=</c> Same as? (PostgreSQL/Redshift geometric operator)
    /// See <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// </summary>
    public sealed record TildeEq : BinaryOperator;

    /// <summary>
    /// ':=' Assignment Operator
    /// See <see href="https://dev.mysql.com/doc/refman/8.4/en/assignment-operators.html#operator_assign-value"/>
    /// </summary>
    public sealed record Assignment : BinaryOperator;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Plus:
                f.WriteStr("+");
                break;
            case Minus:
                f.WriteStr("-");
                break;
            case Multiply:
                f.WriteStr("*");
                break;
            case Divide:
                f.WriteStr("/");
                break;
            case Modulo:
                f.WriteStr("%");
                break;
            case StringConcat:
                f.WriteStr("||");
                break;
            case Gt:
                f.WriteStr(">");
                break;
            case Lt:
                f.WriteStr("<");
                break;
            case GtEq:
                f.WriteStr(">=");
                break;
            case LtEq:
                f.WriteStr("<=");
                break;
            case Spaceship:
                f.WriteStr("<=>");
                break;
            case Eq:
                f.WriteStr("=");
                break;
            case NotEq:
                f.WriteStr("<>");
                break;
            case And:
                f.WriteStr("AND");
                break;
            case Or:
                f.WriteStr("OR");
                break;
            case Xor:
                f.WriteStr("XOR");
                break;
            case BitwiseOr:
                f.WriteStr("|");
                break;
            case BitwiseAnd:
                f.WriteStr("&");
                break;
            case BitwiseXor:
                f.WriteStr("^");
                break;
            case DuckIntegerDivide:
                f.WriteStr("//");
                break;
            case MyIntegerDivide:
                f.WriteStr("DIV");
                break;
            case Match:
                f.WriteStr("MATCH");
                break;
            case Regexp:
                f.WriteStr("REGEXP");
                break;
            case Glob:
                f.WriteStr("GLOB");
                break;
            case Custom s:
                f.WriteStr(s.Value);
                break;
            case PGBitwiseXor:
                f.WriteStr("#");
                break;
            case PGBitwiseShiftLeft:
                f.WriteStr("<<");
                break;
            case PGBitwiseShiftRight:
                f.WriteStr(">>");
                break;
            case PGExp:
                f.WriteStr("^");
                break;
            case PGOverlap:
                f.WriteStr("&&");
                break;
            case PGRegexMatch:
                f.WriteStr("~");
                break;
            case PGRegexIMatch:
                f.WriteStr("~*");
                break;
            case PGRegexNotMatch:
                f.WriteStr("!~");
                break;
            case PGRegexNotIMatch:
                f.WriteStr("!~*");
                break;
            case PGLikeMatch:
                f.WriteStr("~~");
                break;
            case PGILikeMatch:
                f.WriteStr("~~*");
                break;
            case PGNotLikeMatch:
                f.WriteStr("!~~");
                break;
            case PGNotILikeMatch:
                f.WriteStr("!~~*");
                break;
            case PGStartsWith:
                f.WriteStr("^@");
                break;
            case Arrow:
                f.WriteStr("->");
                break;
            case LongArrow:
                f.WriteStr("->>");
                break;
            case HashArrow:
                f.WriteStr("#>");
                break;
            case HashLongArrow:
                f.WriteStr("#>>");
                break;
            case AtAt:
                f.WriteStr("@@");
                break;
            case AtArrow:
                f.WriteStr("@>");
                break;
            case ArrowAt:
                f.WriteStr("<@");
                break;
            case HashMinus:
                f.WriteStr("#-");
                break;
            case AtQuestion:
                f.WriteStr("@?");
                break;
            case Question:
                f.WriteStr("?");
                break;
            case QuestionAnd:
                f.WriteStr("?&");
                break;
            case QuestionPipe:
                f.WriteStr("?|");
                break;
            case PGCustomBinaryOperator idents:
                f.Write($"OPERATOR({DisplaySeparated(idents.Value, ".")})");
                break;
            case Overlaps:
                f.WriteStr("OVERLAPS");
                break;
            case DoubleHash:
                f.WriteStr("##");
                break;
            case LtDashGt:
                f.WriteStr("<->");
                break;
            case AndLt:
                f.WriteStr("&<");
                break;
            case AndGt:
                f.WriteStr("&>");
                break;
            case LtLtPipe:
                f.WriteStr("<<|");
                break;
            case PipeGtGt:
                f.WriteStr("|>>");
                break;
            case AndLtPipe:
                f.WriteStr("&<|");
                break;
            case PipeAndGt:
                f.WriteStr("|&>");
                break;
            case LtCaret:
                f.WriteStr("<^");
                break;
            case GtCaret:
                f.WriteStr(">^");
                break;
            case QuestionHash:
                f.WriteStr("?#");
                break;
            case QuestionDash:
                f.WriteStr("?-");
                break;
            case QuestionDashPipe:
                f.WriteStr("?-|");
                break;
            case QuestionDoublePipe:
                f.WriteStr("?||");
                break;
            case At:
                f.WriteStr("@");
                break;
            case TildeEq:
                f.WriteStr("~=");
                break;
            case Assignment:
                f.WriteStr(":=");
                break;
            default:
                throw new InvalidOperationException($"Variante de BinaryOperator não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
