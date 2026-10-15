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

using System.Globalization;
using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>SQL data types</summary>
// As funções livres privadas de `data_type.rs` (`format_type_with_optional_length` etc.) são
// membros estáticos privados deste tipo homônimo (convencoes.md §13, caso 5).
public abstract partial record DataType : IDisplay
{
    /// <summary>
    /// Table type in <see href="https://www.postgresql.org/docs/15/sql-createfunction.html">PostgreSQL</see>,
    /// e.g. CREATE FUNCTION RETURNS TABLE(...).
    /// </summary>
    /// <remarks>
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-function-transact-sql?view=sql-server-ver16#c-create-a-multi-statement-table-valued-function">MsSQL</see>
    /// </remarks>
    public sealed record Table(Sequence<ColumnDef>? Value) : DataType;

    /// <summary>Table type with a name, e.g. CREATE FUNCTION RETURNS @result TABLE(...).</summary>
    /// <remarks>
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-function-transact-sql?view=sql-server-ver16#table">MsSQl</see>
    /// </remarks>
    /// <param name="Name">Table name.</param>
    /// <param name="Columns">Table columns.</param>
    public sealed record NamedTable(ObjectName Name, Sequence<ColumnDef> Columns) : DataType;

    /// <summary>Fixed-length character type, e.g. CHARACTER(10).</summary>
    public sealed record Character(CharacterLength? Value) : DataType;

    /// <summary>Fixed-length char type, e.g. CHAR(10).</summary>
    public sealed record Char(CharacterLength? Value) : DataType;

    /// <summary>Character varying type, e.g. CHARACTER VARYING(10).</summary>
    public sealed record CharacterVarying(CharacterLength? Value) : DataType;

    /// <summary>Char varying type, e.g. CHAR VARYING(10).</summary>
    public sealed record CharVarying(CharacterLength? Value) : DataType;

    /// <summary>Variable-length character type, e.g. VARCHAR(10).</summary>
    public sealed record Varchar(CharacterLength? Value) : DataType;

    /// <summary>Variable-length character type, e.g. NVARCHAR(10).</summary>
    public sealed record Nvarchar(CharacterLength? Value) : DataType;

    /// <summary>Uuid type.</summary>
    public sealed record Uuid : DataType;

    /// <summary>
    /// Large character object with optional length,
    /// e.g. CHARACTER LARGE OBJECT, CHARACTER LARGE OBJECT(1000),
    /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#character-large-object-type">SQL Standard</see>.
    /// </summary>
    public sealed record CharacterLargeObject(ulong? Value) : DataType;

    /// <summary>
    /// Large character object with optional length,
    /// e.g. CHAR LARGE OBJECT, CHAR LARGE OBJECT(1000),
    /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#character-large-object-type">SQL Standard</see>.
    /// </summary>
    public sealed record CharLargeObject(ulong? Value) : DataType;

    /// <summary>
    /// Large character object with optional length,
    /// e.g. CLOB, CLOB(1000),
    /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#character-large-object-type">SQL Standard</see>.
    /// </summary>
    /// <remarks><see href="https://docs.oracle.com/javadb/10.10.1.2/ref/rrefclob.html">Oracle</see></remarks>
    public sealed record Clob(ulong? Value) : DataType;

    /// <summary>
    /// Fixed-length binary type with optional length,
    /// see <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#binary-string-type">SQL Standard</see>,
    /// <see href="https://learn.microsoft.com/pt-br/sql/t-sql/data-types/binary-and-varbinary-transact-sql?view=sql-server-ver16">MS SQL Server</see>.
    /// </summary>
    public sealed record Binary(ulong? Value) : DataType;

    /// <summary>
    /// Variable-length binary with optional length type,
    /// see <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#binary-string-type">SQL Standard</see>,
    /// <see href="https://learn.microsoft.com/pt-br/sql/t-sql/data-types/binary-and-varbinary-transact-sql?view=sql-server-ver16">MS SQL Server</see>.
    /// </summary>
    public sealed record Varbinary(BinaryLength? Value) : DataType;

    /// <summary>
    /// Large binary object with optional length,
    /// see <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#binary-large-object-string-type">SQL Standard</see>,
    /// <see href="https://docs.oracle.com/javadb/10.8.3.0/ref/rrefblob.html">Oracle</see>.
    /// </summary>
    public sealed record Blob(ulong? Value) : DataType;

    /// <summary><see href="https://dev.mysql.com/doc/refman/9.1/en/blob.html">MySQL</see> blob with up to 2**8 bytes.</summary>
    public sealed record TinyBlob : DataType;

    /// <summary><see href="https://dev.mysql.com/doc/refman/9.1/en/blob.html">MySQL</see> blob with up to 2**24 bytes.</summary>
    public sealed record MediumBlob : DataType;

    /// <summary><see href="https://dev.mysql.com/doc/refman/9.1/en/blob.html">MySQL</see> blob with up to 2**32 bytes.</summary>
    public sealed record LongBlob : DataType;

    /// <summary>Variable-length binary data with optional length.</summary>
    /// <remarks><see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-types#bytes_type">BigQuery</see></remarks>
    public sealed record Bytes(ulong? Value) : DataType;

    /// <summary>
    /// Numeric type with optional precision and scale, e.g. NUMERIC(10,2),
    /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#exact-numeric-type">SQL Standard</see>.
    /// </summary>
    public sealed record Numeric(ExactNumberInfo Value) : DataType;

    /// <summary>
    /// Decimal type with optional precision and scale, e.g. DECIMAL(10,2),
    /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#exact-numeric-type">SQL Standard</see>.
    /// </summary>
    public sealed record Decimal(ExactNumberInfo Value) : DataType;

    /// <summary>
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/numeric-type-syntax.html">MySQL</see> unsigned decimal with optional
    /// precision and scale, e.g. DECIMAL UNSIGNED or DECIMAL(10,2) UNSIGNED.
    /// Note: Using UNSIGNED with DECIMAL is deprecated in recent versions of MySQL.
    /// </summary>
    public sealed record DecimalUnsigned(ExactNumberInfo Value) : DataType;

    /// <summary>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#bignumeric_literals">BigNumeric</see> type used in BigQuery.
    /// </summary>
    public sealed record BigNumeric(ExactNumberInfo Value) : DataType;

    /// <summary>This is alias for <c>BigNumeric</c> type used in BigQuery.</summary>
    /// <remarks><see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-types#decimal_types">BigDecimal</see></remarks>
    public sealed record BigDecimal(ExactNumberInfo Value) : DataType;

    /// <summary>
    /// Dec type with optional precision and scale, e.g. DEC(10,2),
    /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#exact-numeric-type">SQL Standard</see>.
    /// </summary>
    public sealed record Dec(ExactNumberInfo Value) : DataType;

    /// <summary>
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/numeric-type-syntax.html">MySQL</see> unsigned decimal (DEC alias) with
    /// optional precision and scale, e.g. DEC UNSIGNED or DEC(10,2) UNSIGNED.
    /// Note: Using UNSIGNED with DEC is deprecated in recent versions of MySQL.
    /// </summary>
    public sealed record DecUnsigned(ExactNumberInfo Value) : DataType;

    /// <summary>Floating point with optional precision and scale, e.g. FLOAT, FLOAT(8), or FLOAT(8,2).</summary>
    public sealed record Float(ExactNumberInfo Value) : DataType;

    /// <summary>
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/numeric-type-syntax.html">MySQL</see> unsigned floating point with optional
    /// precision and scale, e.g. FLOAT UNSIGNED, FLOAT(10) UNSIGNED or FLOAT(10,2) UNSIGNED.
    /// Note: Using UNSIGNED with FLOAT is deprecated in recent versions of MySQL.
    /// </summary>
    public sealed record FloatUnsigned(ExactNumberInfo Value) : DataType;

    /// <summary>Tiny integer with optional display width, e.g. TINYINT or TINYINT(3).</summary>
    public sealed record TinyInt(ulong? Value) : DataType;

    /// <summary>
    /// Unsigned tiny integer with optional display width,
    /// e.g. TINYINT UNSIGNED or TINYINT(3) UNSIGNED.
    /// </summary>
    public sealed record TinyIntUnsigned(ulong? Value) : DataType;

    /// <summary>Unsigned tiny integer, e.g. UTINYINT</summary>
    public sealed record UTinyInt : DataType;

    /// <summary>
    /// Int2 is an alias for SmallInt in <see href="https://www.postgresql.org/docs/current/datatype.html">PostgreSQL</see>.
    /// Note: Int2 means 2 bytes in PostgreSQL (not 2 bits).
    /// Int2 with optional display width, e.g. INT2 or INT2(5).
    /// </summary>
    public sealed record Int2(ulong? Value) : DataType;

    /// <summary>Unsigned Int2 with optional display width, e.g. INT2 UNSIGNED or INT2(5) UNSIGNED.</summary>
    public sealed record Int2Unsigned(ulong? Value) : DataType;

    /// <summary>Small integer with optional display width, e.g. SMALLINT or SMALLINT(5).</summary>
    public sealed record SmallInt(ulong? Value) : DataType;

    /// <summary>
    /// Unsigned small integer with optional display width,
    /// e.g. SMALLINT UNSIGNED or SMALLINT(5) UNSIGNED.
    /// </summary>
    public sealed record SmallIntUnsigned(ulong? Value) : DataType;

    /// <summary>Unsigned small integer, e.g. USMALLINT.</summary>
    public sealed record USmallInt : DataType;

    /// <summary>
    /// MySQL medium integer (<see href="https://dev.mysql.com/doc/refman/8.0/en/integer-types.html">1</see>) with optional
    /// display width, e.g. MEDIUMINT or MEDIUMINT(5).
    /// </summary>
    public sealed record MediumInt(ulong? Value) : DataType;

    /// <summary>
    /// Unsigned medium integer (<see href="https://dev.mysql.com/doc/refman/8.0/en/integer-types.html">1</see>) with optional
    /// display width, e.g. MEDIUMINT UNSIGNED or MEDIUMINT(5) UNSIGNED.
    /// </summary>
    public sealed record MediumIntUnsigned(ulong? Value) : DataType;

    /// <summary>Int with optional display width, e.g. INT or INT(11).</summary>
    public sealed record Int(ulong? Value) : DataType;

    /// <summary>
    /// Int4 is an alias for Integer in <see href="https://www.postgresql.org/docs/current/datatype.html">PostgreSQL</see>.
    /// Note: Int4 means 4 bytes in PostgreSQL (not 4 bits).
    /// Int4 with optional display width, e.g. Int4 or Int4(11).
    /// </summary>
    public sealed record Int4(ulong? Value) : DataType;

    /// <summary>
    /// Int8 is an alias for BigInt in <see href="https://www.postgresql.org/docs/current/datatype.html">PostgreSQL</see>
    /// and Integer type in <see href="https://clickhouse.com/docs/en/sql-reference/data-types/int-uint">ClickHouse</see>.
    /// Int8 with optional display width, e.g. INT8 or INT8(11).
    /// Note: Int8 means 8 bytes in PostgreSQL, but 8 bits in ClickHouse.
    /// </summary>
    public sealed record Int8(ulong? Value) : DataType;

    /// <summary>
    /// Integer type in <see href="https://clickhouse.com/docs/en/sql-reference/data-types/int-uint">ClickHouse</see>.
    /// Note: Int16 means 16 bits in ClickHouse.
    /// </summary>
    public sealed record Int16 : DataType;

    /// <summary>
    /// Integer type in <see href="https://clickhouse.com/docs/en/sql-reference/data-types/int-uint">ClickHouse</see>.
    /// Note: Int32 means 32 bits in ClickHouse.
    /// </summary>
    public sealed record Int32 : DataType;

    /// <summary>
    /// Integer type in <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-types#integer_types">BigQuery</see>,
    /// <see href="https://clickhouse.com/docs/en/sql-reference/data-types/int-uint">ClickHouse</see>.
    /// </summary>
    public sealed record Int64 : DataType;

    /// <summary>
    /// Integer type in <see href="https://clickhouse.com/docs/en/sql-reference/data-types/int-uint">ClickHouse</see>.
    /// Note: Int128 means 128 bits in ClickHouse.
    /// </summary>
    public sealed record Int128 : DataType;

    /// <summary>
    /// Integer type in <see href="https://clickhouse.com/docs/en/sql-reference/data-types/int-uint">ClickHouse</see>.
    /// Note: Int256 means 256 bits in ClickHouse.
    /// </summary>
    public sealed record Int256 : DataType;

    /// <summary>Integer with optional display width, e.g. INTEGER or INTEGER(11).</summary>
    public sealed record Integer(ulong? Value) : DataType;

    /// <summary>Unsigned int with optional display width, e.g. INT UNSIGNED or INT(11) UNSIGNED.</summary>
    public sealed record IntUnsigned(ulong? Value) : DataType;

    /// <summary>Unsigned int4 with optional display width, e.g. INT4 UNSIGNED or INT4(11) UNSIGNED.</summary>
    public sealed record Int4Unsigned(ulong? Value) : DataType;

    /// <summary>Unsigned integer with optional display width, e.g. INTEGER UNSIGNED or INTEGER(11) UNSIGNED.</summary>
    public sealed record IntegerUnsigned(ulong? Value) : DataType;

    /// <summary>128-bit integer type, e.g. HUGEINT.</summary>
    public sealed record HugeInt : DataType;

    /// <summary>Unsigned 128-bit integer type, e.g. UHUGEINT.</summary>
    public sealed record UHugeInt : DataType;

    /// <summary>
    /// Unsigned integer type in <see href="https://clickhouse.com/docs/en/sql-reference/data-types/int-uint">ClickHouse</see>.
    /// Note: UInt8 means 8 bits in ClickHouse.
    /// </summary>
    public sealed record UInt8 : DataType;

    /// <summary>
    /// Unsigned integer type in <see href="https://clickhouse.com/docs/en/sql-reference/data-types/int-uint">ClickHouse</see>.
    /// Note: UInt16 means 16 bits in ClickHouse.
    /// </summary>
    public sealed record UInt16 : DataType;

    /// <summary>
    /// Unsigned integer type in <see href="https://clickhouse.com/docs/en/sql-reference/data-types/int-uint">ClickHouse</see>.
    /// Note: UInt32 means 32 bits in ClickHouse.
    /// </summary>
    public sealed record UInt32 : DataType;

    /// <summary>
    /// Unsigned integer type in <see href="https://clickhouse.com/docs/en/sql-reference/data-types/int-uint">ClickHouse</see>.
    /// Note: UInt64 means 64 bits in ClickHouse.
    /// </summary>
    public sealed record UInt64 : DataType;

    /// <summary>
    /// Unsigned integer type in <see href="https://clickhouse.com/docs/en/sql-reference/data-types/int-uint">ClickHouse</see>.
    /// Note: UInt128 means 128 bits in ClickHouse.
    /// </summary>
    public sealed record UInt128 : DataType;

    /// <summary>
    /// Unsigned integer type in <see href="https://clickhouse.com/docs/en/sql-reference/data-types/int-uint">ClickHouse</see>.
    /// Note: UInt256 means 256 bits in ClickHouse.
    /// </summary>
    public sealed record UInt256 : DataType;

    /// <summary>Big integer with optional display width, e.g. BIGINT or BIGINT(20).</summary>
    public sealed record BigInt(ulong? Value) : DataType;

    /// <summary>Unsigned big integer with optional display width, e.g. BIGINT UNSIGNED or BIGINT(20) UNSIGNED.</summary>
    public sealed record BigIntUnsigned(ulong? Value) : DataType;

    /// <summary>Unsigned big integer, e.g. UBIGINT.</summary>
    public sealed record UBigInt : DataType;

    /// <summary>Unsigned Int8 with optional display width, e.g. INT8 UNSIGNED or INT8(11) UNSIGNED.</summary>
    public sealed record Int8Unsigned(ulong? Value) : DataType;

    /// <summary>
    /// Signed integer as used in <see href="https://dev.mysql.com/doc/refman/8.4/en/cast-functions.html">MySQL CAST</see> target
    /// types, without optional <c>INTEGER</c> suffix, e.g. <c>SIGNED</c>
    /// </summary>
    public sealed record Signed : DataType;

    /// <summary>
    /// Signed integer as used in <see href="https://dev.mysql.com/doc/refman/8.4/en/cast-functions.html">MySQL CAST</see> target
    /// types, with optional <c>INTEGER</c> suffix, e.g. <c>SIGNED INTEGER</c>
    /// </summary>
    public sealed record SignedInteger : DataType;

    /// <summary>
    /// Signed integer as used in <see href="https://dev.mysql.com/doc/refman/8.4/en/cast-functions.html">MySQL CAST</see> target
    /// types, without optional <c>INTEGER</c> suffix, e.g. <c>SIGNED</c>
    /// </summary>
    public sealed record Unsigned : DataType;

    /// <summary>
    /// Unsigned integer as used in <see href="https://dev.mysql.com/doc/refman/8.4/en/cast-functions.html">MySQL CAST</see> target
    /// types, with optional <c>INTEGER</c> suffix, e.g. <c>UNSIGNED INTEGER</c>.
    /// </summary>
    public sealed record UnsignedInteger : DataType;

    /// <summary>Float4 is an alias for Real in <see href="https://www.postgresql.org/docs/current/datatype.html">PostgreSQL</see>.</summary>
    public sealed record Float4 : DataType;

    /// <summary>Floating point in <see href="https://clickhouse.com/docs/en/sql-reference/data-types/float">ClickHouse</see>.</summary>
    public sealed record Float32 : DataType;

    /// <summary>
    /// Floating point in <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-types#floating_point_types">BigQuery</see>.
    /// </summary>
    /// <remarks><see href="https://clickhouse.com/docs/en/sql-reference/data-types/float">ClickHouse</see></remarks>
    public sealed record Float64 : DataType;

    /// <summary>Floating point, e.g. REAL.</summary>
    public sealed record Real : DataType;

    /// <summary>
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/numeric-type-syntax.html">MySQL</see> unsigned real, e.g. REAL UNSIGNED.
    /// Note: Using UNSIGNED with REAL is deprecated in recent versions of MySQL.
    /// </summary>
    public sealed record RealUnsigned : DataType;

    /// <summary>Float8 is an alias for Double in <see href="https://www.postgresql.org/docs/current/datatype.html">PostgreSQL</see>.</summary>
    public sealed record Float8 : DataType;

    /// <summary>Double</summary>
    public sealed record Double(ExactNumberInfo Value) : DataType;

    /// <summary>
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/numeric-type-syntax.html">MySQL</see> unsigned double precision with
    /// optional precision, e.g. DOUBLE UNSIGNED or DOUBLE(10,2) UNSIGNED.
    /// Note: Using UNSIGNED with DOUBLE is deprecated in recent versions of MySQL.
    /// </summary>
    public sealed record DoubleUnsigned(ExactNumberInfo Value) : DataType;

    /// <summary>
    /// Double Precision, see
    /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#approximate-numeric-type">SQL Standard</see>,
    /// <see href="https://www.postgresql.org/docs/current/datatype-numeric.html">PostgreSQL</see>.
    /// </summary>
    public sealed record DoublePrecision : DataType;

    /// <summary>
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/numeric-type-syntax.html">MySQL</see> unsigned double precision,
    /// e.g. DOUBLE PRECISION UNSIGNED.
    /// Note: Using UNSIGNED with DOUBLE PRECISION is deprecated in recent versions of MySQL.
    /// </summary>
    public sealed record DoublePrecisionUnsigned : DataType;

    /// <summary>Bool is an alias for Boolean, see <see href="https://www.postgresql.org/docs/current/datatype.html">PostgreSQL</see>.</summary>
    public sealed record Bool : DataType;

    /// <summary>Boolean type.</summary>
    public sealed record Boolean : DataType;

    /// <summary>Date type.</summary>
    public sealed record Date : DataType;

    /// <summary>Date32 with the same range as Datetime64.</summary>
    /// <remarks><see href="https://clickhouse.com/docs/en/sql-reference/data-types/date32">1</see></remarks>
    public sealed record Date32 : DataType;

    /// <summary>
    /// Time with optional time precision and time zone information, see
    /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#datetime-type">SQL Standard</see>.
    /// </summary>
    // Rust: `Time(Option<u64>, TimezoneInfo)`; nomes dos campos em nomes.md.
    public sealed record Time(ulong? Precision, TimezoneInfo TimezoneInfo) : DataType;

    /// <summary>Datetime with optional time precision, see <see href="https://dev.mysql.com/doc/refman/8.0/en/datetime.html">MySQL</see>.</summary>
    public sealed record Datetime(ulong? Value) : DataType;

    /// <summary>
    /// Datetime with time precision and optional timezone, see
    /// <see href="https://clickhouse.com/docs/en/sql-reference/data-types/datetime64">ClickHouse</see>.
    /// </summary>
    // Rust: `Datetime64(u64, Option<String>)`; nomes dos campos em nomes.md.
    public sealed record Datetime64(ulong Precision, string? Timezone) : DataType;

    /// <summary>
    /// Timestamp with optional time precision and time zone information, see
    /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#datetime-type">SQL Standard</see>.
    /// </summary>
    // Rust: `Timestamp(Option<u64>, TimezoneInfo)`; nomes dos campos em nomes.md.
    public sealed record Timestamp(ulong? Precision, TimezoneInfo TimezoneInfo) : DataType;

    /// <summary>
    /// Databricks timestamp without time zone. See
    /// <see href="https://docs.databricks.com/aws/en/sql/language-manual/data-types/timestamp-ntz-type">1</see>.
    /// </summary>
    public sealed record TimestampNtz(ulong? Value) : DataType;

    /// <summary>Interval type.</summary>
    /// <param name="Fields">
    /// <see href="https://www.postgresql.org/docs/17/datatype-datetime.html">PostgreSQL</see> fields specification like
    /// <c>INTERVAL YEAR TO MONTH</c>.
    /// </param>
    /// <param name="Precision">
    /// <see href="https://www.postgresql.org/docs/17/datatype-datetime.html">PostgreSQL</see> subsecond precision like
    /// <c>INTERVAL HOUR TO SECOND(3)</c>
    /// </param>
    public sealed record Interval(IntervalFields? Fields, ulong? Precision) : DataType;

    /// <summary>JSON type.</summary>
    public sealed record JSON : DataType;

    /// <summary>Binary JSON type.</summary>
    public sealed record JSONB : DataType;

    /// <summary>Regclass used in <see href="https://www.postgresql.org/docs/current/datatype.html">PostgreSQL</see> serial.</summary>
    public sealed record Regclass : DataType;

    /// <summary>Text type.</summary>
    public sealed record Text : DataType;

    /// <summary><see href="https://dev.mysql.com/doc/refman/9.1/en/blob.html">MySQL</see> text with up to 2**8 bytes.</summary>
    public sealed record TinyText : DataType;

    /// <summary><see href="https://dev.mysql.com/doc/refman/9.1/en/blob.html">MySQL</see> text with up to 2**24 bytes.</summary>
    public sealed record MediumText : DataType;

    /// <summary><see href="https://dev.mysql.com/doc/refman/9.1/en/blob.html">MySQL</see> text with up to 2**32 bytes.</summary>
    public sealed record LongText : DataType;

    /// <summary>String with optional length.</summary>
    public sealed record String(ulong? Value) : DataType;

    /// <summary>A fixed-length string e.g <see href="https://clickhouse.com/docs/en/sql-reference/data-types/fixedstring">ClickHouse</see>.</summary>
    public sealed record FixedString(ulong Value) : DataType;

    /// <summary>Bytea type, see <see href="https://www.postgresql.org/docs/current/datatype-bit.html">PostgreSQL</see>.</summary>
    public sealed record Bytea : DataType;

    /// <summary>
    /// Bit string, see <see href="https://www.postgresql.org/docs/current/datatype-bit.html">PostgreSQL</see>,
    /// <see href="https://dev.mysql.com/doc/refman/9.1/en/bit-type.html">MySQL</see>, or
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/bit-transact-sql?view=sql-server-ver16">MSSQL</see>.
    /// </summary>
    public sealed record Bit(ulong? Value) : DataType;

    /// <summary>
    /// <c>BIT VARYING(n)</c>: Variable-length bit string, see
    /// <see href="https://www.postgresql.org/docs/current/datatype-bit.html">PostgreSQL</see>.
    /// </summary>
    public sealed record BitVarying(ulong? Value) : DataType;

    /// <summary>
    /// <c>VARBIT(n)</c>: Variable-length bit string.
    /// <see href="https://www.postgresql.org/docs/current/datatype.html">PostgreSQL</see> alias for <c>BIT VARYING</c>.
    /// </summary>
    public sealed record VarBit(ulong? Value) : DataType;

    /// <summary>Custom types.</summary>
    // Rust: `Custom(ObjectName, Vec<String>)`; nomes dos campos em nomes.md.
    public sealed record Custom(ObjectName Name, Sequence<string> Modifiers) : DataType;

    /// <summary>Arrays.</summary>
    public sealed record Array(ArrayElemTypeDef Value) : DataType;

    /// <summary>
    /// Map, see <see href="https://clickhouse.com/docs/en/sql-reference/data-types/map">ClickHouse</see>,
    /// <see href="https://hive.apache.org/docs/latest/language/languagemanual-types/">Hive</see>.
    /// </summary>
    // Rust: `Map(Box<DataType>, Box<DataType>, MapBracketKind)`; nomes dos campos em nomes.md.
    public sealed record Map(DataType KeyDataType, DataType ValueDataType, MapBracketKind Bracket) : DataType;

    /// <summary>Tuple, see <see href="https://clickhouse.com/docs/en/sql-reference/data-types/tuple">ClickHouse</see>.</summary>
    public sealed record Tuple(Sequence<StructField> Value) : DataType;

    /// <summary>
    /// Nested type, see <see href="https://clickhouse.com/docs/en/sql-reference/data-types/nested-data-structures/nested">ClickHouse</see>.
    /// </summary>
    public sealed record Nested(Sequence<ColumnDef> Value) : DataType;

    /// <summary>Enum type.</summary>
    // Rust: `Enum(Vec<EnumMember>, Option<u8>)`; nomes dos campos em nomes.md.
    public sealed record Enum(Sequence<EnumMember> Values, byte? Bits) : DataType;

    /// <summary>Set type.</summary>
    public sealed record Set(Sequence<string> Value) : DataType;

    /// <summary>
    /// Struct type, see <see href="https://docs.cloudera.com/cdw-runtime/cloud/impala-sql-reference/topics/impala-struct.html">Hive</see>,
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-types#struct_type">BigQuery</see>.
    /// </summary>
    // Rust: `Struct(Vec<StructField>, StructBracketKind)`; nomes dos campos em nomes.md.
    public sealed record Struct(Sequence<StructField> Fields, StructBracketKind Bracket) : DataType;

    /// <summary>Union type, see <see href="https://duckdb.org/docs/sql/data_types/union.html">DuckDB</see>.</summary>
    public sealed record Union(Sequence<UnionField> Value) : DataType;

    /// <summary>Nullable - special marker NULL represents in ClickHouse as a data type.</summary>
    /// <remarks><see href="https://clickhouse.com/docs/en/sql-reference/data-types/nullable">ClickHouse</see></remarks>
    public sealed record Nullable(DataType Value) : DataType;

    /// <summary>LowCardinality - changes the internal representation of other data types to be dictionary-encoded.</summary>
    /// <remarks><see href="https://clickhouse.com/docs/en/sql-reference/data-types/lowcardinality">ClickHouse</see></remarks>
    public sealed record LowCardinality(DataType Value) : DataType;

    /// <summary>
    /// No type specified - only used with
    /// <c>SQLiteDialect</c>, from statements such
    /// as <c>CREATE TABLE t1 (a)</c>.
    /// </summary>
    public sealed record Unspecified : DataType;

    /// <summary>
    /// Trigger data type, returned by functions associated with triggers, see
    /// <see href="https://www.postgresql.org/docs/current/plpgsql-trigger.html">PostgreSQL</see>.
    /// </summary>
    public sealed record Trigger : DataType;

    /// <summary>
    /// Any data type, used in BigQuery UDF definitions for templated parameters, see
    /// <see href="https://cloud.google.com/bigquery/docs/user-defined-functions#templated-sql-udf-parameters">BigQuery</see>.
    /// </summary>
    public sealed record AnyType : DataType;

    /// <summary>Geometric type, see <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html">PostgreSQL</see>.</summary>
    public sealed record GeometricType(GeometricTypeKind Value) : DataType;

    /// <summary>PostgreSQL text search vectors, see <see href="https://www.postgresql.org/docs/17/datatype-textsearch.html">PostgreSQL</see>.</summary>
    public sealed record TsVector : DataType;

    /// <summary>PostgreSQL text search query, see <see href="https://www.postgresql.org/docs/17/datatype-textsearch.html">PostgreSQL</see>.</summary>
    public sealed record TsQuery : DataType;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Character d:
                FormatCharacterStringType(f, "CHARACTER", d.Value);
                break;
            case Char d:
                FormatCharacterStringType(f, "CHAR", d.Value);
                break;
            case CharacterVarying d:
                FormatCharacterStringType(f, "CHARACTER VARYING", d.Value);
                break;
            case CharVarying d:
                FormatCharacterStringType(f, "CHAR VARYING", d.Value);
                break;
            case Varchar d:
                FormatCharacterStringType(f, "VARCHAR", d.Value);
                break;
            case Nvarchar d:
                FormatCharacterStringType(f, "NVARCHAR", d.Value);
                break;
            case Uuid:
                f.Write($"UUID");
                break;
            case CharacterLargeObject d:
                FormatTypeWithOptionalLength(f, "CHARACTER LARGE OBJECT", d.Value, false);
                break;
            case CharLargeObject d:
                FormatTypeWithOptionalLength(f, "CHAR LARGE OBJECT", d.Value, false);
                break;
            case Clob d:
                FormatTypeWithOptionalLength(f, "CLOB", d.Value, false);
                break;
            case Binary d:
                FormatTypeWithOptionalLength(f, "BINARY", d.Value, false);
                break;
            case Varbinary d:
                FormatVarbinaryType(f, "VARBINARY", d.Value);
                break;
            case Blob d:
                FormatTypeWithOptionalLength(f, "BLOB", d.Value, false);
                break;
            case TinyBlob:
                f.Write($"TINYBLOB");
                break;
            case MediumBlob:
                f.Write($"MEDIUMBLOB");
                break;
            case LongBlob:
                f.Write($"LONGBLOB");
                break;
            case Bytes d:
                FormatTypeWithOptionalLength(f, "BYTES", d.Value, false);
                break;
            case Numeric d:
                f.Write($"NUMERIC{d.Value}");
                break;
            case Decimal d:
                f.Write($"DECIMAL{d.Value}");
                break;
            case DecimalUnsigned d:
                f.Write($"DECIMAL{d.Value} UNSIGNED");
                break;
            case Dec d:
                f.Write($"DEC{d.Value}");
                break;
            case DecUnsigned d:
                f.Write($"DEC{d.Value} UNSIGNED");
                break;
            case BigNumeric d:
                f.Write($"BIGNUMERIC{d.Value}");
                break;
            case BigDecimal d:
                f.Write($"BIGDECIMAL{d.Value}");
                break;
            case Float d:
                f.Write($"FLOAT{d.Value}");
                break;
            case FloatUnsigned d:
                f.Write($"FLOAT{d.Value} UNSIGNED");
                break;
            case TinyInt d:
                FormatTypeWithOptionalLength(f, "TINYINT", d.Value, false);
                break;
            case TinyIntUnsigned d:
                FormatTypeWithOptionalLength(f, "TINYINT", d.Value, true);
                break;
            case Int2 d:
                FormatTypeWithOptionalLength(f, "INT2", d.Value, false);
                break;
            case Int2Unsigned d:
                FormatTypeWithOptionalLength(f, "INT2", d.Value, true);
                break;
            case SmallInt d:
                FormatTypeWithOptionalLength(f, "SMALLINT", d.Value, false);
                break;
            case SmallIntUnsigned d:
                FormatTypeWithOptionalLength(f, "SMALLINT", d.Value, true);
                break;
            case MediumInt d:
                FormatTypeWithOptionalLength(f, "MEDIUMINT", d.Value, false);
                break;
            case MediumIntUnsigned d:
                FormatTypeWithOptionalLength(f, "MEDIUMINT", d.Value, true);
                break;
            case Int d:
                FormatTypeWithOptionalLength(f, "INT", d.Value, false);
                break;
            case IntUnsigned d:
                FormatTypeWithOptionalLength(f, "INT", d.Value, true);
                break;
            case Int4 d:
                FormatTypeWithOptionalLength(f, "INT4", d.Value, false);
                break;
            case Int8 d:
                FormatTypeWithOptionalLength(f, "INT8", d.Value, false);
                break;
            case Int16:
                f.Write($"Int16");
                break;
            case Int32:
                f.Write($"Int32");
                break;
            case Int64:
                f.Write($"INT64");
                break;
            case Int128:
                f.Write($"Int128");
                break;
            case Int256:
                f.Write($"Int256");
                break;
            case HugeInt:
                f.Write($"HUGEINT");
                break;
            case Int4Unsigned d:
                FormatTypeWithOptionalLength(f, "INT4", d.Value, true);
                break;
            case Integer d:
                FormatTypeWithOptionalLength(f, "INTEGER", d.Value, false);
                break;
            case IntegerUnsigned d:
                FormatTypeWithOptionalLength(f, "INTEGER", d.Value, true);
                break;
            case BigInt d:
                FormatTypeWithOptionalLength(f, "BIGINT", d.Value, false);
                break;
            case BigIntUnsigned d:
                FormatTypeWithOptionalLength(f, "BIGINT", d.Value, true);
                break;
            case Int8Unsigned d:
                FormatTypeWithOptionalLength(f, "INT8", d.Value, true);
                break;
            case UTinyInt:
                f.Write($"UTINYINT");
                break;
            case USmallInt:
                f.Write($"USMALLINT");
                break;
            case UBigInt:
                f.Write($"UBIGINT");
                break;
            case UHugeInt:
                f.Write($"UHUGEINT");
                break;
            case UInt8:
                f.Write($"UInt8");
                break;
            case UInt16:
                f.Write($"UInt16");
                break;
            case UInt32:
                f.Write($"UInt32");
                break;
            case UInt64:
                f.Write($"UInt64");
                break;
            case UInt128:
                f.Write($"UInt128");
                break;
            case UInt256:
                f.Write($"UInt256");
                break;
            case Signed:
                f.Write($"SIGNED");
                break;
            case SignedInteger:
                f.Write($"SIGNED INTEGER");
                break;
            case Unsigned:
                f.Write($"UNSIGNED");
                break;
            case UnsignedInteger:
                f.Write($"UNSIGNED INTEGER");
                break;
            case Real:
                f.Write($"REAL");
                break;
            case RealUnsigned:
                f.Write($"REAL UNSIGNED");
                break;
            case Float4:
                f.Write($"FLOAT4");
                break;
            case Float32:
                f.Write($"Float32");
                break;
            case Float64:
                f.Write($"FLOAT64");
                break;
            case Double d:
                f.Write($"DOUBLE{d.Value}");
                break;
            case DoubleUnsigned d:
                f.Write($"DOUBLE{d.Value} UNSIGNED");
                break;
            case Float8:
                f.Write($"FLOAT8");
                break;
            case DoublePrecision:
                f.Write($"DOUBLE PRECISION");
                break;
            case DoublePrecisionUnsigned:
                f.Write($"DOUBLE PRECISION UNSIGNED");
                break;
            case Bool:
                f.Write($"BOOL");
                break;
            case Boolean:
                f.Write($"BOOLEAN");
                break;
            case Date:
                f.Write($"DATE");
                break;
            case Date32:
                f.Write($"Date32");
                break;
            case Time d:
                FormatDatetimePrecisionAndTz(f, "TIME", d.Precision, d.TimezoneInfo);
                break;
            case Datetime d:
                FormatTypeWithOptionalLength(f, "DATETIME", d.Value, false);
                break;
            case Timestamp d:
                FormatDatetimePrecisionAndTz(f, "TIMESTAMP", d.Precision, d.TimezoneInfo);
                break;
            case TimestampNtz d:
                FormatTypeWithOptionalLength(f, "TIMESTAMP_NTZ", d.Value, false);
                break;
            case Datetime64 d:
                FormatClickhouseDatetimePrecisionAndTimezone(f, "DateTime64", d.Precision, d.Timezone);
                break;
            case Interval d:
                f.Write($"INTERVAL");
                if (d.Fields is { } fields)
                {
                    f.Write($" {fields}");
                }
                if (d.Precision is { } precision)
                {
                    f.Write($"({precision})");
                }
                break;
            case JSON:
                f.Write($"JSON");
                break;
            case JSONB:
                f.Write($"JSONB");
                break;
            case Regclass:
                f.Write($"REGCLASS");
                break;
            case Text:
                f.Write($"TEXT");
                break;
            case TinyText:
                f.Write($"TINYTEXT");
                break;
            case MediumText:
                f.Write($"MEDIUMTEXT");
                break;
            case LongText:
                f.Write($"LONGTEXT");
                break;
            case String d:
                FormatTypeWithOptionalLength(f, "STRING", d.Value, false);
                break;
            case Bytea:
                f.Write($"BYTEA");
                break;
            case Bit d:
                FormatTypeWithOptionalLength(f, "BIT", d.Value, false);
                break;
            case BitVarying d:
                FormatTypeWithOptionalLength(f, "BIT VARYING", d.Value, false);
                break;
            case VarBit d:
                FormatTypeWithOptionalLength(f, "VARBIT", d.Value, false);
                break;
            case Array d:
                switch (d.Value)
                {
                    case ArrayElemTypeDef.None:
                        f.Write($"ARRAY");
                        break;
                    case ArrayElemTypeDef.SquareBracket { Size: null } t:
                        f.Write($"{t.DataType}[]");
                        break;
                    case ArrayElemTypeDef.SquareBracket { Size: { } size } t:
                        f.Write($"{t.DataType}[{size}]");
                        break;
                    case ArrayElemTypeDef.AngleBracket t:
                        f.Write($"ARRAY<{t.Value}>");
                        break;
                    case ArrayElemTypeDef.Parenthesis t:
                        f.Write($"Array({t.Value})");
                        break;
                    case ArrayElemTypeDef.Qualified { Size: null } t:
                        f.Write($"{t.DataType} ARRAY");
                        break;
                    case ArrayElemTypeDef.Qualified { Size: { } size } t:
                        f.Write($"{t.DataType} ARRAY[{size}]");
                        break;
                    default:
                        throw new InvalidOperationException($"Variante de ArrayElemTypeDef não tratada: {d.Value.GetType().Name}");
                }
                break;
            case Custom d:
                if (d.Modifiers.IsEmpty)
                {
                    f.Write($"{d.Name}");
                }
                else
                {
                    f.Write($"{d.Name}({string.Join(", ", d.Modifiers)})");
                }
                break;
            case Enum d:
            {
                switch (d.Bits)
                {
                    case { } bits:
                        f.Write($"ENUM{bits}");
                        break;
                    case null:
                        f.Write($"ENUM");
                        break;
                }
                f.Write($"(");
                for (var i = 0; i < d.Values.Count; i++)
                {
                    if (i != 0)
                    {
                        f.Write($", ");
                    }
                    switch (d.Values[i])
                    {
                        case EnumMember.Name name:
                            f.Write($"'{Ast.Value.EscapeSingleQuoteString(name.Value)}'");
                            break;
                        case EnumMember.NamedValue namedValue:
                            f.Write($"'{Ast.Value.EscapeSingleQuoteString(namedValue.Name)}' = {namedValue.Value}");
                            break;
                        default:
                            throw new InvalidOperationException($"Variante de EnumMember não tratada: {d.Values[i].GetType().Name}");
                    }
                }
                f.Write($")");
                break;
            }
            case Set d:
                f.Write($"SET(");
                for (var i = 0; i < d.Value.Count; i++)
                {
                    if (i != 0)
                    {
                        f.Write($", ");
                    }
                    f.Write($"'{Ast.Value.EscapeSingleQuoteString(d.Value[i])}'");
                }
                f.Write($")");
                break;
            case Struct d:
                if (!d.Fields.IsEmpty)
                {
                    switch (d.Bracket)
                    {
                        case StructBracketKind.Parentheses:
                            f.Write($"STRUCT({DisplayCommaSeparated(d.Fields)})");
                            break;
                        case StructBracketKind.AngleBrackets:
                            f.Write($"STRUCT<{DisplayCommaSeparated(d.Fields)}>");
                            break;
                        default:
                            throw new InvalidOperationException($"Variante de StructBracketKind não tratada: {d.Bracket}");
                    }
                }
                else
                {
                    f.Write($"STRUCT");
                }
                break;
            case Union d:
                f.Write($"UNION({DisplayCommaSeparated(d.Value)})");
                break;
            // ClickHouse
            case Nullable d:
                f.Write($"Nullable({d.Value})");
                break;
            case FixedString d:
                f.Write($"FixedString({d.Value})");
                break;
            case LowCardinality d:
                f.Write($"LowCardinality({d.Value})");
                break;
            case Map d:
                switch (d.Bracket)
                {
                    case MapBracketKind.Parentheses:
                        f.Write($"Map({d.KeyDataType}, {d.ValueDataType})");
                        break;
                    case MapBracketKind.AngleBrackets:
                        f.Write($"MAP<{d.KeyDataType}, {d.ValueDataType}>");
                        break;
                    default:
                        throw new InvalidOperationException($"Variante de MapBracketKind não tratada: {d.Bracket}");
                }
                break;
            case Tuple d:
                f.Write($"Tuple({DisplayCommaSeparated(d.Value)})");
                break;
            case Nested d:
                f.Write($"Nested({DisplayCommaSeparated(d.Value)})");
                break;
            case Unspecified:
                break;
            case Trigger:
                f.Write($"TRIGGER");
                break;
            case AnyType:
                f.Write($"ANY TYPE");
                break;
            case Table d:
                switch (d.Value)
                {
                    case { } tableFields:
                        f.Write($"TABLE({DisplayCommaSeparated(tableFields)})");
                        break;
                    case null:
                        f.Write($"TABLE");
                        break;
                }
                break;
            case NamedTable d:
                f.Write($"{d.Name} TABLE ({DisplayCommaSeparated(d.Columns)})");
                break;
            case GeometricType d:
                f.Write($"{d.Value}");
                break;
            case TsVector:
                f.Write($"TSVECTOR");
                break;
            case TsQuery:
                f.Write($"TSQUERY");
                break;
            default:
                throw new InvalidOperationException($"Variante de DataType não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);

    // Rust: fn format_type_with_optional_length(f, sql_type: &'static str, len: &Option<u64>, unsigned: bool)
    private static void FormatTypeWithOptionalLength(Formatter f, string sqlType, ulong? len, bool unsigned)
    {
        f.Write($"{sqlType}");
        if (len is { } l)
        {
            f.Write($"({l})");
        }
        if (unsigned)
        {
            f.Write($" UNSIGNED");
        }
    }

    // Rust: fn format_character_string_type(f, sql_type: &str, size: &Option<CharacterLength>)
    private static void FormatCharacterStringType(Formatter f, string sqlType, CharacterLength? size)
    {
        f.Write($"{sqlType}");
        if (size is { } s)
        {
            f.Write($"({s})");
        }
    }

    // Rust: fn format_varbinary_type(f, sql_type: &str, size: &Option<BinaryLength>)
    private static void FormatVarbinaryType(Formatter f, string sqlType, BinaryLength? size)
    {
        f.Write($"{sqlType}");
        if (size is { } s)
        {
            f.Write($"({s})");
        }
    }

    // Rust: fn format_datetime_precision_and_tz(f, sql_type: &'static str, len: &Option<u64>, time_zone: &TimezoneInfo)
    private static void FormatDatetimePrecisionAndTz(Formatter f, string sqlType, ulong? len, TimezoneInfo timeZone)
    {
        f.Write($"{sqlType}");
        // let len_fmt = len.as_ref().map(|l| format!("({l})")).unwrap_or_default();
        var lenFmt = len is { } l ? "(" + l.ToString(CultureInfo.InvariantCulture) + ")" : "";

        switch (timeZone)
        {
            case TimezoneInfo.Tz:
                f.Write($"{timeZone}{lenFmt}");
                break;
            default:
                f.Write($"{lenFmt}{timeZone}");
                break;
        }
    }

    // Rust: fn format_clickhouse_datetime_precision_and_timezone(f, sql_type: &'static str, len: &u64, time_zone: &Option<String>)
    private static void FormatClickhouseDatetimePrecisionAndTimezone(Formatter f, string sqlType, ulong len, string? timeZone)
    {
        f.Write($"{sqlType}({len}");

        if (timeZone is { } tz)
        {
            f.Write($", '{tz}'");
        }

        f.Write($")");
    }
}
