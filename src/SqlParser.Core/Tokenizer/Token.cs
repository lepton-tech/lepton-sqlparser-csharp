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

using SqlParser.Core.Ast;

namespace SqlParser.Core;

/// <summary>SQL Token enumeration</summary>
// Enum com dados → abstract record + variantes sealed record aninhadas (convencoes.md §6).
// Dentro deste tipo, `Word`, `Whitespace` e `DollarQuotedString` são as VARIANTES; os tipos
// homônimos são escritos qualificados (SqlParser.Core.Word etc.).
public abstract partial record Token : IDisplay
{
    /// <summary>An end-of-file marker, not a real token</summary>
    public sealed record EOF : Token;

    /// <summary>A keyword (like SELECT) or an optionally quoted SQL identifier</summary>
    public sealed record Word(SqlParser.Core.Word Value) : Token;

    /// <summary>An unsigned numeric literal</summary>
    /// <param name="Value">The literal text.</param>
    /// <param name="Long">Whether the literal has the <c>L</c> suffix.</param>
    // 2+ campos: nomes registrados em docs/port/nomes.md.
    public sealed record Number(string Value, bool Long) : Token;

    /// <summary>A character that could not be tokenized</summary>
    // Rust: Char(char). `Value` contém exatamente UM scalar value Unicode (1 ou 2 unidades UTF-16),
    // porque um `char` Rust fora do BMP não cabe num `char` C#. Ver docs/port/nomes.md.
    public sealed record Char(string Value) : Token;

    /// <summary>Single quoted string: i.e: 'string'</summary>
    public sealed record SingleQuotedString(string Value) : Token;

    /// <summary>Double quoted string: i.e: "string"</summary>
    public sealed record DoubleQuotedString(string Value) : Token;

    /// <summary>
    /// Triple single quoted strings: Example '''abc'''
    /// [BigQuery](https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_literals)
    /// </summary>
    public sealed record TripleSingleQuotedString(string Value) : Token;

    /// <summary>
    /// Triple double quoted strings: Example """abc"""
    /// [BigQuery](https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_literals)
    /// </summary>
    public sealed record TripleDoubleQuotedString(string Value) : Token;

    /// <summary>Dollar quoted string: i.e: $$string$$ or $tag_name$string$tag_name$</summary>
    public sealed record DollarQuotedString(SqlParser.Core.Ast.DollarQuotedString Value) : Token;

    /// <summary>
    /// Byte string literal: i.e: b'string' or B'string' (note that some backends, such as
    /// PostgreSQL, may treat this syntax as a bit string literal instead, i.e: b'10010101')
    /// </summary>
    public sealed record SingleQuotedByteStringLiteral(string Value) : Token;

    /// <summary>Byte string literal: i.e: b"string" or B"string"</summary>
    public sealed record DoubleQuotedByteStringLiteral(string Value) : Token;

    /// <summary>
    /// Triple single quoted literal with byte string prefix. Example <c>B'''abc'''</c>
    /// [BigQuery](https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_literals)
    /// </summary>
    public sealed record TripleSingleQuotedByteStringLiteral(string Value) : Token;

    /// <summary>
    /// Triple double quoted literal with byte string prefix. Example <c>B"""abc"""</c>
    /// [BigQuery](https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_literals)
    /// </summary>
    public sealed record TripleDoubleQuotedByteStringLiteral(string Value) : Token;

    /// <summary>
    /// Single quoted literal with raw string prefix. Example <c>R'abc'</c>
    /// [BigQuery](https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_literals)
    /// </summary>
    public sealed record SingleQuotedRawStringLiteral(string Value) : Token;

    /// <summary>
    /// Double quoted literal with raw string prefix. Example <c>R"abc"</c>
    /// [BigQuery](https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_literals)
    /// </summary>
    public sealed record DoubleQuotedRawStringLiteral(string Value) : Token;

    /// <summary>
    /// Triple single quoted literal with raw string prefix. Example <c>R'''abc'''</c>
    /// [BigQuery](https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_literals)
    /// </summary>
    public sealed record TripleSingleQuotedRawStringLiteral(string Value) : Token;

    /// <summary>
    /// Triple double quoted literal with raw string prefix. Example <c>R"""abc"""</c>
    /// [BigQuery](https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_literals)
    /// </summary>
    public sealed record TripleDoubleQuotedRawStringLiteral(string Value) : Token;

    /// <summary>"National" string literal: i.e: N'string'</summary>
    public sealed record NationalStringLiteral(string Value) : Token;

    /// <summary>
    /// Quote delimited literal. Examples <c>Q'{ab'c}'</c>, <c>Q'|ab'c|'</c>, <c>Q'|ab|c|'</c>
    /// [Oracle](https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/Literals.html#GUID-1824CBAA-6E16-4921-B2A6-112FB02248DA)
    /// </summary>
    public sealed record QuoteDelimitedStringLiteral(QuoteDelimitedString Value) : Token;

    /// <summary>
    /// "Nationa" quote delimited literal. Examples <c>NQ'{ab'c}'</c>, <c>NQ'|ab'c|'</c>, <c>NQ'|ab|c|'</c>
    /// [Oracle](https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/Literals.html#GUID-1824CBAA-6E16-4921-B2A6-112FB02248DA)
    /// </summary>
    public sealed record NationalQuoteDelimitedStringLiteral(QuoteDelimitedString Value) : Token;

    /// <summary>"escaped" string literal, which are an extension to the SQL standard: i.e: e'first \n second' or E 'first \n second'</summary>
    public sealed record EscapedStringLiteral(string Value) : Token;

    /// <summary>Unicode string literal: i.e: U&amp;'first \000A second'</summary>
    public sealed record UnicodeStringLiteral(string Value) : Token;

    /// <summary>Hexadecimal string literal: i.e.: X'deadbeef'</summary>
    public sealed record HexStringLiteral(string Value) : Token;

    /// <summary>Comma</summary>
    public sealed record Comma : Token;

    /// <summary>Whitespace (space, tab, etc)</summary>
    public sealed record Whitespace(SqlParser.Core.Whitespace Value) : Token;

    /// <summary>Double equals sign <c>==</c></summary>
    public sealed record DoubleEq : Token;

    /// <summary>Equality operator <c>=</c></summary>
    public sealed record Eq : Token;

    /// <summary>Not Equals operator <c>&lt;&gt;</c> (or <c>!=</c> in some dialects)</summary>
    public sealed record Neq : Token;

    /// <summary>Less Than operator <c>&lt;</c></summary>
    public sealed record Lt : Token;

    /// <summary>Greater Than operator <c>&gt;</c></summary>
    public sealed record Gt : Token;

    /// <summary>Less Than Or Equals operator <c>&lt;=</c></summary>
    public sealed record LtEq : Token;

    /// <summary>Greater Than Or Equals operator <c>&gt;=</c></summary>
    public sealed record GtEq : Token;

    /// <summary>Spaceship operator &lt;=&gt;</summary>
    public sealed record Spaceship : Token;

    /// <summary>Plus operator <c>+</c></summary>
    public sealed record Plus : Token;

    /// <summary>Minus operator <c>-</c></summary>
    public sealed record Minus : Token;

    /// <summary>Multiplication operator <c>*</c></summary>
    public sealed record Mul : Token;

    /// <summary>Division operator <c>/</c></summary>
    public sealed record Div : Token;

    /// <summary>Integer division operator <c>//</c> in DuckDB</summary>
    public sealed record DuckIntDiv : Token;

    /// <summary>Modulo Operator <c>%</c></summary>
    public sealed record Mod : Token;

    /// <summary>String concatenation <c>||</c></summary>
    public sealed record StringConcat : Token;

    /// <summary>Left parenthesis <c>(</c></summary>
    public sealed record LParen : Token;

    /// <summary>Right parenthesis <c>)</c></summary>
    public sealed record RParen : Token;

    /// <summary>Period (used for compound identifiers or projections into nested types)</summary>
    public sealed record Period : Token;

    /// <summary>Colon <c>:</c></summary>
    public sealed record Colon : Token;

    /// <summary>DoubleColon <c>::</c> (used for casting in PostgreSQL)</summary>
    public sealed record DoubleColon : Token;

    /// <summary>Assignment <c>:=</c> (used for keyword argument in DuckDB macros and some functions, and for variable declarations in DuckDB and Snowflake)</summary>
    public sealed record Assignment : Token;

    /// <summary>SemiColon <c>;</c> used as separator for COPY and payload</summary>
    public sealed record SemiColon : Token;

    /// <summary>Backslash <c>\</c> used in terminating the COPY payload with <c>\.</c></summary>
    public sealed record Backslash : Token;

    /// <summary>Left bracket <c>[</c></summary>
    public sealed record LBracket : Token;

    /// <summary>Right bracket <c>]</c></summary>
    public sealed record RBracket : Token;

    /// <summary>Ampersand <c>&amp;</c></summary>
    public sealed record Ampersand : Token;

    /// <summary>Pipe <c>|</c></summary>
    public sealed record Pipe : Token;

    /// <summary>Caret <c>^</c></summary>
    public sealed record Caret : Token;

    /// <summary>Left brace <c>{</c></summary>
    public sealed record LBrace : Token;

    /// <summary>Right brace <c>}</c></summary>
    public sealed record RBrace : Token;

    /// <summary>Right Arrow <c>=&gt;</c></summary>
    public sealed record RArrow : Token;

    /// <summary>Sharp <c>#</c> used for PostgreSQL Bitwise XOR operator, also PostgreSQL/Redshift geometrical unary/binary operator (Number of points in path or polygon/Intersection)</summary>
    public sealed record Sharp : Token;

    /// <summary><c>##</c> PostgreSQL/Redshift geometrical binary operator (Point of closest proximity)</summary>
    public sealed record DoubleSharp : Token;

    /// <summary>Tilde <c>~</c> used for PostgreSQL Bitwise NOT operator or case sensitive match regular expression operator</summary>
    public sealed record Tilde : Token;

    /// <summary><c>~*</c> , a case insensitive match regular expression operator in PostgreSQL</summary>
    public sealed record TildeAsterisk : Token;

    /// <summary><c>!~</c> , a case sensitive not match regular expression operator in PostgreSQL</summary>
    public sealed record ExclamationMarkTilde : Token;

    /// <summary><c>!~*</c> , a case insensitive not match regular expression operator in PostgreSQL</summary>
    public sealed record ExclamationMarkTildeAsterisk : Token;

    /// <summary><c>~~</c>, a case sensitive match pattern operator in PostgreSQL</summary>
    public sealed record DoubleTilde : Token;

    /// <summary><c>~~*</c>, a case insensitive match pattern operator in PostgreSQL</summary>
    public sealed record DoubleTildeAsterisk : Token;

    /// <summary><c>!~~</c>, a case sensitive not match pattern operator in PostgreSQL</summary>
    public sealed record ExclamationMarkDoubleTilde : Token;

    /// <summary><c>!~~*</c>, a case insensitive not match pattern operator in PostgreSQL</summary>
    public sealed record ExclamationMarkDoubleTildeAsterisk : Token;

    /// <summary><c>&lt;&lt;</c>, a bitwise shift left operator in PostgreSQL</summary>
    public sealed record ShiftLeft : Token;

    /// <summary><c>&gt;&gt;</c>, a bitwise shift right operator in PostgreSQL</summary>
    public sealed record ShiftRight : Token;

    /// <summary><c>&amp;&amp;</c>, an overlap operator in PostgreSQL</summary>
    public sealed record Overlap : Token;

    /// <summary>Exclamation Mark <c>!</c> used for PostgreSQL factorial operator</summary>
    public sealed record ExclamationMark : Token;

    /// <summary>Double Exclamation Mark <c>!!</c> used for PostgreSQL prefix factorial operator</summary>
    public sealed record DoubleExclamationMark : Token;

    /// <summary>AtSign <c>@</c> used for PostgreSQL abs operator, also PostgreSQL/Redshift geometrical unary/binary operator (Center, Contained or on)</summary>
    public sealed record AtSign : Token;

    /// <summary><c>^@</c>, a "starts with" string operator in PostgreSQL</summary>
    public sealed record CaretAt : Token;

    /// <summary><c>|/</c>, a square root math operator in PostgreSQL</summary>
    public sealed record PGSquareRoot : Token;

    /// <summary><c>||/</c>, a cube root math operator in PostgreSQL</summary>
    public sealed record PGCubeRoot : Token;

    /// <summary><c>?</c> or <c>$</c> , a prepared statement arg placeholder</summary>
    public sealed record Placeholder(string Value) : Token;

    /// <summary><c>-&gt;</c>, used as a operator to extract json field in PostgreSQL</summary>
    public sealed record Arrow : Token;

    /// <summary><c>-&gt;&gt;</c>, used as a operator to extract json field as text in PostgreSQL</summary>
    public sealed record LongArrow : Token;

    /// <summary><c>#&gt;</c>, extracts JSON sub-object at the specified path</summary>
    public sealed record HashArrow : Token;

    /// <summary><c>@-@</c> PostgreSQL/Redshift geometrical unary operator (Length or circumference)</summary>
    public sealed record AtDashAt : Token;

    /// <summary><c>?-</c> PostgreSQL/Redshift geometrical unary/binary operator (Is horizontal?/Are horizontally aligned?)</summary>
    public sealed record QuestionMarkDash : Token;

    /// <summary><c>&amp;&lt;</c> PostgreSQL/Redshift geometrical binary operator (Overlaps to left?)</summary>
    public sealed record AmpersandLeftAngleBracket : Token;

    /// <summary><c>&amp;&gt;</c> PostgreSQL/Redshift geometrical binary operator (Overlaps to right?)`</summary>
    public sealed record AmpersandRightAngleBracket : Token;

    /// <summary><c>&amp;&lt;|</c> PostgreSQL/Redshift geometrical binary operator (Does not extend above?)`</summary>
    public sealed record AmpersandLeftAngleBracketVerticalBar : Token;

    /// <summary><c>|&amp;&gt;</c> PostgreSQL/Redshift geometrical binary operator (Does not extend below?)`</summary>
    public sealed record VerticalBarAmpersandRightAngleBracket : Token;

    /// <summary><c>&lt;-&gt;</c> PostgreSQL/Redshift geometrical binary operator (Distance between)</summary>
    public sealed record TwoWayArrow : Token;

    /// <summary><c>&lt;^</c> PostgreSQL/Redshift geometrical binary operator (Is below?)</summary>
    public sealed record LeftAngleBracketCaret : Token;

    /// <summary><c>&gt;^</c> PostgreSQL/Redshift geometrical binary operator (Is above?)</summary>
    public sealed record RightAngleBracketCaret : Token;

    /// <summary><c>?#</c> PostgreSQL/Redshift geometrical binary operator (Intersects or overlaps)</summary>
    public sealed record QuestionMarkSharp : Token;

    /// <summary><c>?-|</c> PostgreSQL/Redshift geometrical binary operator (Is perpendicular?)</summary>
    public sealed record QuestionMarkDashVerticalBar : Token;

    /// <summary><c>?||</c> PostgreSQL/Redshift geometrical binary operator (Are parallel?)</summary>
    public sealed record QuestionMarkDoubleVerticalBar : Token;

    /// <summary><c>~=</c> PostgreSQL/Redshift geometrical binary operator (Same as)</summary>
    public sealed record TildeEqual : Token;

    /// <summary>`&lt;&lt;| PostgreSQL/Redshift geometrical binary operator (Is strictly below?)</summary>
    public sealed record ShiftLeftVerticalBar : Token;

    /// <summary>`|&gt;&gt; PostgreSQL/Redshift geometrical binary operator (Is strictly above?)</summary>
    public sealed record VerticalBarShiftRight : Token;

    /// <summary>`|&gt; BigQuery pipe operator</summary>
    public sealed record VerticalBarRightAngleBracket : Token;

    /// <summary><c>#&gt;&gt;</c>, extracts JSON sub-object at the specified path as text</summary>
    public sealed record HashLongArrow : Token;

    /// <summary>jsonb @&gt; jsonb -&gt; boolean: Test whether left json contains the right json</summary>
    public sealed record AtArrow : Token;

    /// <summary>jsonb &lt;@ jsonb -&gt; boolean: Test whether right json contains the left json</summary>
    public sealed record ArrowAt : Token;

    /// <summary>
    /// jsonb #- text[] -&gt; jsonb: Deletes the field or array element at the specified
    /// path, where path elements can be either field keys or array indexes.
    /// </summary>
    public sealed record HashMinus : Token;

    /// <summary>
    /// jsonb @? jsonpath -&gt; boolean: Does JSON path return any item for the specified
    /// JSON value?
    /// </summary>
    public sealed record AtQuestion : Token;

    /// <summary>
    /// jsonb @@ jsonpath → boolean: Returns the result of a JSON path predicate check
    /// for the specified JSON value. Only the first item of the result is taken into
    /// account. If the result is not Boolean, then NULL is returned.
    /// </summary>
    public sealed record AtAt : Token;

    /// <summary>
    /// jsonb ? text -&gt; boolean: Checks whether the string exists as a top-level key within the
    /// jsonb object
    /// </summary>
    public sealed record Question : Token;

    /// <summary>
    /// jsonb ?&amp; text[] -&gt; boolean: Check whether all members of the text array exist as top-level
    /// keys within the jsonb object
    /// </summary>
    public sealed record QuestionAnd : Token;

    /// <summary>
    /// jsonb ?| text[] -&gt; boolean: Check whether any member of the text array exists as top-level
    /// keys within the jsonb object
    /// </summary>
    public sealed record QuestionPipe : Token;

    /// <summary>
    /// Custom binary operator
    /// This is used to represent any custom binary operator that is not part of the SQL standard.
    /// PostgreSQL allows defining custom binary operators using CREATE OPERATOR.
    /// </summary>
    public sealed record CustomBinaryOperator(string Value) : Token;

    // impl fmt::Display for Token: o match inteiro vira UM switch na base (convencoes.md §6/§7).
    /// <inheritdoc/>
    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case EOF:
                f.WriteStr("EOF");
                break;
            case Word w:
                f.Write(w.Value);
                break;
            case Number n:
                f.WriteStr(n.Value);
                f.WriteStr(n.Long ? "L" : "");
                break;
            case Char c:
                f.WriteStr(c.Value);
                break;
            case SingleQuotedString s:
                f.WriteStr("'");
                f.WriteStr(s.Value);
                f.WriteStr("'");
                break;
            case TripleSingleQuotedString s:
                f.WriteStr("'''");
                f.WriteStr(s.Value);
                f.WriteStr("'''");
                break;
            case DoubleQuotedString s:
                f.WriteStr("\"");
                f.WriteStr(s.Value);
                f.WriteStr("\"");
                break;
            case TripleDoubleQuotedString s:
                f.WriteStr("\"\"\"");
                f.WriteStr(s.Value);
                f.WriteStr("\"\"\"");
                break;
            case DollarQuotedString s:
                f.Write(s.Value);
                break;
            case NationalStringLiteral s:
                f.WriteStr("N'");
                f.WriteStr(s.Value);
                f.WriteStr("'");
                break;
            case QuoteDelimitedStringLiteral s:
                s.Value.Fmt(f);
                break;
            case NationalQuoteDelimitedStringLiteral s:
                f.WriteStr("N");
                f.Write(s.Value);
                break;
            case EscapedStringLiteral s:
                f.WriteStr("E'");
                f.WriteStr(s.Value);
                f.WriteStr("'");
                break;
            case UnicodeStringLiteral s:
                f.WriteStr("U&'");
                f.WriteStr(s.Value);
                f.WriteStr("'");
                break;
            case HexStringLiteral s:
                f.WriteStr("X'");
                f.WriteStr(s.Value);
                f.WriteStr("'");
                break;
            case SingleQuotedByteStringLiteral s:
                f.WriteStr("B'");
                f.WriteStr(s.Value);
                f.WriteStr("'");
                break;
            case TripleSingleQuotedByteStringLiteral s:
                f.WriteStr("B'''");
                f.WriteStr(s.Value);
                f.WriteStr("'''");
                break;
            case DoubleQuotedByteStringLiteral s:
                f.WriteStr("B\"");
                f.WriteStr(s.Value);
                f.WriteStr("\"");
                break;
            case TripleDoubleQuotedByteStringLiteral s:
                f.WriteStr("B\"\"\"");
                f.WriteStr(s.Value);
                f.WriteStr("\"\"\"");
                break;
            case SingleQuotedRawStringLiteral s:
                f.WriteStr("R'");
                f.WriteStr(s.Value);
                f.WriteStr("'");
                break;
            case DoubleQuotedRawStringLiteral s:
                f.WriteStr("R\"");
                f.WriteStr(s.Value);
                f.WriteStr("\"");
                break;
            case TripleSingleQuotedRawStringLiteral s:
                f.WriteStr("R'''");
                f.WriteStr(s.Value);
                f.WriteStr("'''");
                break;
            case TripleDoubleQuotedRawStringLiteral s:
                f.WriteStr("R\"\"\"");
                f.WriteStr(s.Value);
                f.WriteStr("\"\"\"");
                break;
            case Comma:
                f.WriteStr(",");
                break;
            case Whitespace ws:
                f.Write(ws.Value);
                break;
            case DoubleEq:
                f.WriteStr("==");
                break;
            case Spaceship:
                f.WriteStr("<=>");
                break;
            case Eq:
                f.WriteStr("=");
                break;
            case Neq:
                f.WriteStr("<>");
                break;
            case Lt:
                f.WriteStr("<");
                break;
            case Gt:
                f.WriteStr(">");
                break;
            case LtEq:
                f.WriteStr("<=");
                break;
            case GtEq:
                f.WriteStr(">=");
                break;
            case Plus:
                f.WriteStr("+");
                break;
            case Minus:
                f.WriteStr("-");
                break;
            case Mul:
                f.WriteStr("*");
                break;
            case Div:
                f.WriteStr("/");
                break;
            case DuckIntDiv:
                f.WriteStr("//");
                break;
            case StringConcat:
                f.WriteStr("||");
                break;
            case Mod:
                f.WriteStr("%");
                break;
            case LParen:
                f.WriteStr("(");
                break;
            case RParen:
                f.WriteStr(")");
                break;
            case Period:
                f.WriteStr(".");
                break;
            case Colon:
                f.WriteStr(":");
                break;
            case DoubleColon:
                f.WriteStr("::");
                break;
            case Assignment:
                f.WriteStr(":=");
                break;
            case SemiColon:
                f.WriteStr(";");
                break;
            case Backslash:
                f.WriteStr("\\");
                break;
            case LBracket:
                f.WriteStr("[");
                break;
            case RBracket:
                f.WriteStr("]");
                break;
            case Ampersand:
                f.WriteStr("&");
                break;
            case Caret:
                f.WriteStr("^");
                break;
            case Pipe:
                f.WriteStr("|");
                break;
            case LBrace:
                f.WriteStr("{");
                break;
            case RBrace:
                f.WriteStr("}");
                break;
            case RArrow:
                f.WriteStr("=>");
                break;
            case Sharp:
                f.WriteStr("#");
                break;
            case DoubleSharp:
                f.WriteStr("##");
                break;
            case ExclamationMark:
                f.WriteStr("!");
                break;
            case DoubleExclamationMark:
                f.WriteStr("!!");
                break;
            case Tilde:
                f.WriteStr("~");
                break;
            case TildeAsterisk:
                f.WriteStr("~*");
                break;
            case ExclamationMarkTilde:
                f.WriteStr("!~");
                break;
            case ExclamationMarkTildeAsterisk:
                f.WriteStr("!~*");
                break;
            case DoubleTilde:
                f.WriteStr("~~");
                break;
            case DoubleTildeAsterisk:
                f.WriteStr("~~*");
                break;
            case ExclamationMarkDoubleTilde:
                f.WriteStr("!~~");
                break;
            case ExclamationMarkDoubleTildeAsterisk:
                f.WriteStr("!~~*");
                break;
            case AtSign:
                f.WriteStr("@");
                break;
            case CaretAt:
                f.WriteStr("^@");
                break;
            case ShiftLeft:
                f.WriteStr("<<");
                break;
            case ShiftRight:
                f.WriteStr(">>");
                break;
            case Overlap:
                f.WriteStr("&&");
                break;
            case PGSquareRoot:
                f.WriteStr("|/");
                break;
            case PGCubeRoot:
                f.WriteStr("||/");
                break;
            case AtDashAt:
                f.WriteStr("@-@");
                break;
            case QuestionMarkDash:
                f.WriteStr("?-");
                break;
            case AmpersandLeftAngleBracket:
                f.WriteStr("&<");
                break;
            case AmpersandRightAngleBracket:
                f.WriteStr("&>");
                break;
            case AmpersandLeftAngleBracketVerticalBar:
                f.WriteStr("&<|");
                break;
            case VerticalBarAmpersandRightAngleBracket:
                f.WriteStr("|&>");
                break;
            case VerticalBarRightAngleBracket:
                f.WriteStr("|>");
                break;
            case TwoWayArrow:
                f.WriteStr("<->");
                break;
            case LeftAngleBracketCaret:
                f.WriteStr("<^");
                break;
            case RightAngleBracketCaret:
                f.WriteStr(">^");
                break;
            case QuestionMarkSharp:
                f.WriteStr("?#");
                break;
            case QuestionMarkDashVerticalBar:
                f.WriteStr("?-|");
                break;
            case QuestionMarkDoubleVerticalBar:
                f.WriteStr("?||");
                break;
            case TildeEqual:
                f.WriteStr("~=");
                break;
            case ShiftLeftVerticalBar:
                f.WriteStr("<<|");
                break;
            case VerticalBarShiftRight:
                f.WriteStr("|>>");
                break;
            case Placeholder s:
                f.WriteStr(s.Value);
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
            case AtAt:
                f.WriteStr("@@");
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
            case CustomBinaryOperator s:
                f.WriteStr(s.Value);
                break;
            default:
                throw new InvalidOperationException($"Variante de Token não tratada: {GetType().Name}");
        }
    }

    // Base de hierarquia de records: "sealed" impede o ToString() sintetizado por variante (convencoes.md §7).
    // Rust: `impl fmt::Display for Token`.
    public sealed override string ToString() => Formatter.Format(this);

    /// <summary>
    /// Create a <c>Token::Word</c> from an unquoted <c>keyword</c>.
    /// The lookup is case-insensitive; unknown values become <c>Keyword::NoKeyword</c>.
    /// </summary>
    public static Token MakeKeyword(string keyword) => MakeWord(keyword, null);

    /// <summary>
    /// Create a <c>Token::Word</c> from <c>word</c> with an optional <c>quote_style</c>.
    /// When <c>quote_style</c> is <c>None</c>, the parser attempts a case-insensitive keyword
    /// lookup and sets the <c>Word::keyword</c> accordingly.
    /// </summary>
    public static Token MakeWord(string word, char? quoteStyle) =>
        new Word(new SqlParser.Core.Word(word, quoteStyle, Tokenizer.KeywordLookup(word, quoteStyle)));

    /// <summary>
    /// Like <see cref="MakeWord"/> but takes ownership of the word <c>String</c>,
    /// avoiding an extra allocation when the caller already has an owned value.
    /// </summary>
    // Rust: `fn make_word_owned` é privado do módulo `tokenizer` → internal (convencoes.md §13).
    internal static Token MakeWordOwned(string word, char? quoteStyle) =>
        new Word(new SqlParser.Core.Word(word, quoteStyle, Tokenizer.KeywordLookup(word, quoteStyle)));

    // Rust: `impl PartialEq<TokenWithSpan> for Token` (compara só o token).
    public bool Equals(TokenWithSpan? other) => other is not null && this == other.Token;
}
