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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/snowflake.rs.
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;

namespace SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://www.snowflake.com/">Snowflake</see>
/// </summary>
public class SnowflakeDialect : Dialect
{
    // Constantes de módulo no Rust (antes do struct). RESERVED_KEYWORDS_FOR_TABLE_FACTOR é usada só
    // por is_table_factor. As funções livres do módulo estão no fim da classe (caso 5).
    // Devolvida por GetReservedKeywordsForSelectItemOperator: lista somente leitura, não array
    // (convencoes.md §13, casos 10 e 11).
    private static readonly IReadOnlyList<Keyword> ReservedKeywordsForSelectItemOperator =
        System.Array.AsReadOnly(new[] { Keyword.CONNECT_BY_ROOT });

    // See: <https://docs.snowflake.com/en/sql-reference/reserved-keywords>
    private static readonly Keyword[] ReservedKeywordsForTableFactor =
    {
        Keyword.ALL,
        Keyword.ALTER,
        Keyword.AND,
        Keyword.ANY,
        Keyword.AS,
        Keyword.BETWEEN,
        Keyword.BY,
        Keyword.CHECK,
        Keyword.COLUMN,
        Keyword.CONNECT,
        Keyword.CREATE,
        Keyword.CROSS,
        Keyword.CURRENT,
        Keyword.DELETE,
        Keyword.DISTINCT,
        Keyword.DROP,
        Keyword.ELSE,
        Keyword.EXISTS,
        Keyword.FOLLOWING,
        Keyword.FOR,
        Keyword.FROM,
        Keyword.FULL,
        Keyword.GRANT,
        Keyword.GROUP,
        Keyword.HAVING,
        Keyword.ILIKE,
        Keyword.IN,
        Keyword.INCREMENT,
        Keyword.INNER,
        Keyword.INSERT,
        Keyword.INTERSECT,
        Keyword.INTO,
        Keyword.IS,
        Keyword.JOIN,
        Keyword.LEFT,
        Keyword.LIKE,
        Keyword.MINUS,
        Keyword.NATURAL,
        Keyword.NOT,
        Keyword.NULL,
        Keyword.OF,
        Keyword.ON,
        Keyword.OR,
        Keyword.ORDER,
        Keyword.QUALIFY,
        Keyword.REGEXP,
        Keyword.REVOKE,
        Keyword.RIGHT,
        Keyword.RLIKE,
        Keyword.ROW,
        Keyword.ROWS,
        Keyword.SAMPLE,
        Keyword.SELECT,
        Keyword.SET,
        Keyword.SOME,
        Keyword.START,
        Keyword.TABLE,
        Keyword.TABLESAMPLE,
        Keyword.THEN,
        Keyword.TO,
        Keyword.TRIGGER,
        Keyword.UNION,
        Keyword.UNIQUE,
        Keyword.UPDATE,
        Keyword.USING,
        Keyword.VALUES,
        Keyword.WHEN,
        Keyword.WHENEVER,
        Keyword.WHERE,
        Keyword.WINDOW,
        Keyword.WITH,
    };

    // see https://docs.snowflake.com/en/sql-reference/identifiers-syntax.html
    public override bool IsIdentifierStart(char ch) => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch == '_';

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/identifiers-syntax"/>
    /// </summary>
    public override char? IdentifierQuoteStyle(string identifier) => '"';

    public override bool SupportsProjectionTrailingCommas => true;

    public override bool SupportsFromTrailingCommas => true;

    // Snowflake supports double-dot notation when the schema name is not specified
    // In this case the default PUBLIC schema is used
    //
    // see https://docs.snowflake.com/en/sql-reference/name-resolution#resolution-when-schema-omitted-double-dot-notation
    public override bool SupportsObjectNameDoubleDotNotation => true;

    public override bool IsIdentifierPart(char ch) =>
        (ch >= 'a' && ch <= 'z')
            || (ch >= 'A' && ch <= 'Z')
            || (ch >= '0' && ch <= '9')
            || ch == '$'
            || ch == '_';

    // See https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#escape_sequences
    public override bool SupportsStringLiteralBackslashEscape => true;

    public override bool SupportsWithinAfterArrayAggregation => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/constructs/where#joins-in-the-where-clause"/>
    /// </summary>
    public override bool SupportsOuterJoinOperator => true;

    public override bool SupportsConnectBy => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/sql/execute-immediate"/>
    /// </summary>
    public override bool SupportsExecuteImmediate => true;

    public override bool SupportsMatchRecognize => true;

    // Snowflake uses this syntax for "object constants" (the values of which
    // are not actually required to be constants).
    //
    // https://docs.snowflake.com/en/sql-reference/data-types-semistructured#label-object-constant
    public override bool SupportsDictionarySyntax => true;

    // Snowflake doesn't document this but `FIRST_VALUE(arg, { IGNORE | RESPECT } NULLS)`
    // works (i.e. inside the argument list instead of after).
    public override bool SupportsWindowFunctionNullTreatmentArg => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/sql/set#syntax">doc</see>
    /// </summary>
    public override bool SupportsParenthesizedSetVariables => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/sql/comment">doc</see>
    /// </summary>
    public override bool SupportsCommentOn => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/functions/extract">doc</see>
    /// </summary>
    public override bool SupportsExtractCommaSyntax => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/functions/flatten">doc</see>
    /// </summary>
    public override bool SupportsSubqueryAsFunctionArg => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/sql/create-view#optional-parameters">doc</see>
    /// </summary>
    public override bool SupportsCreateViewCommentSyntax => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/data-types-semistructured#array">doc</see>
    /// </summary>
    public override bool SupportsArrayTypedefWithoutElementType => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/constructs/from">doc</see>
    /// </summary>
    public override bool SupportsParensAroundTableFactor => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/constructs/values">doc</see>
    /// </summary>
    public override bool SupportsValuesAsTableFactor => true;

    public override Statement? ParseStatement(Parser parser)
    {
        if (parser.ParseKeyword(Keyword.BEGIN))
        {
            // Snowflake supports both `BEGIN TRANSACTION` and `BEGIN ... END` blocks.
            // If the next keyword indicates a transaction statement, let the
            // standard parse_begin() handle it.
            if (parser
                    .PeekOneOfKeywords(Keyword.TRANSACTION, Keyword.WORK, Keyword.NAME) is not null
                || parser.PeekTokenRef().Token is Token.SemiColon or Token.EOF)
            {
                parser.PrevToken();
                return null;
            }
            return parser.ParseBeginExceptionEnd();
        }

        if (parser.ParseKeywords(Keyword.ALTER, Keyword.DYNAMIC, Keyword.TABLE))
        {
            // ALTER DYNAMIC TABLE
            return ParseAlterDynamicTable(parser);
        }

        if (parser.ParseKeywords(Keyword.ALTER, Keyword.EXTERNAL, Keyword.TABLE))
        {
            // ALTER EXTERNAL TABLE
            return ParseAlterExternalTable(parser);
        }

        if (parser.ParseKeywords(Keyword.ALTER, Keyword.SESSION))
        {
            // ALTER SESSION
            bool set;
            switch (parser.ParseOneOfKeywords(Keyword.SET, Keyword.UNSET))
            {
                case Keyword.SET:
                    set = true;
                    break;
                case Keyword.UNSET:
                    set = false;
                    break;
                default:
                    return parser.ExpectedRef<Statement>("SET or UNSET", parser.PeekTokenRef());
            }
            return ParseAlterSession(parser, set);
        }

        if (parser.ParseKeyword(Keyword.CREATE))
        {
            // possibly CREATE STAGE
            //[ OR  REPLACE ]
            var orReplace = parser.ParseKeywords(Keyword.OR, Keyword.REPLACE);
            // LOCAL | GLOBAL
            bool? global;
            switch (parser.ParseOneOfKeywords(Keyword.LOCAL, Keyword.GLOBAL))
            {
                case Keyword.LOCAL:
                    global = false;
                    break;
                case Keyword.GLOBAL:
                    global = true;
                    break;
                default:
                    global = null;
                    break;
            }

            var @dynamic = parser.ParseKeyword(Keyword.DYNAMIC);

            var temporary = false;
            var @volatile = false;
            var transient = false;
            var iceberg = false;

            switch (parser.ParseOneOfKeywords(
                Keyword.TEMP,
                Keyword.TEMPORARY,
                Keyword.VOLATILE,
                Keyword.TRANSIENT,
                Keyword.ICEBERG))
            {
                case Keyword.TEMP or Keyword.TEMPORARY:
                    temporary = true;
                    break;
                case Keyword.VOLATILE:
                    @volatile = true;
                    break;
                case Keyword.TRANSIENT:
                    transient = true;
                    break;
                case Keyword.ICEBERG:
                    iceberg = true;
                    break;
                default:
                    break;
            }

            if (parser.ParseKeyword(Keyword.STAGE))
            {
                // OK - this is CREATE STAGE statement
                return ParseCreateStage(orReplace, temporary, parser);
            }
            else if (parser.ParseKeyword(Keyword.TABLE))
            {
                // Rust: `.map(Into::into)` → Statement.From (decisão 7 da B1).
                return Statement.From(
                    ParseCreateTable(
                        orReplace, global, temporary, @volatile, transient, iceberg, @dynamic,
                        parser));
            }
            else if (parser.ParseKeyword(Keyword.DATABASE))
            {
                return ParseCreateDatabase(orReplace, transient, parser);
            }
            else if (parser.ParseKeywords(Keyword.FILE, Keyword.FORMAT))
            {
                return ParseCreateFileFormat(
                    orReplace, temporary, @volatile, parser);
            }
            else
            {
                // need to go back with the cursor
                var back = 1;
                if (orReplace)
                {
                    back += 2;
                }
                if (temporary)
                {
                    back += 1;
                }
                for (var _i = 0; _i < back; _i++)
                {
                    parser.PrevToken();
                }
            }
        }
        if (parser.ParseKeywords(Keyword.COPY, Keyword.INTO))
        {
            // COPY INTO
            return ParseCopyInto(parser);
        }

        if (parser.ParseOneOfKeywords(
            Keyword.LIST,
            Keyword.LS,
            Keyword.REMOVE,
            Keyword.RM) is { } kw)
        {
            return ParseFileStagingCommand(kw, parser);
        }

        if (parser.ParseKeyword(Keyword.PUT))
        {
            return ParsePut(parser);
        }

        if (parser.ParseKeyword(Keyword.SHOW))
        {
            var terse = parser.ParseKeyword(Keyword.TERSE);
            if (parser.ParseKeyword(Keyword.OBJECTS))
            {
                return ParseShowObjects(terse, parser);
            }
            //Give back Keyword::TERSE
            if (terse)
            {
                parser.PrevToken();
            }
            //Give back Keyword::SHOW
            parser.PrevToken();
        }

        // Check for multi-table INSERT
        // `INSERT [OVERWRITE] ALL ... or INSERT [OVERWRITE] FIRST ...`
        if (parser.ParseKeyword(Keyword.INSERT))
        {
            var insertToken = parser.GetCurrentToken();
            var overwrite = parser.ParseKeyword(Keyword.OVERWRITE);

            // Check for ALL or FIRST keyword
            // Rust: `if let Some(kw)`; o `kw` anterior (variável de padrão do `if` acima) continua no
            // escopo do método em C# (caso 72: `insertKw`).
            if (parser.ParseOneOfKeywords(Keyword.ALL, Keyword.FIRST) is { } insertKw)
            {
                MultiTableInsertType multiTableInsertType;
                switch (insertKw)
                {
                    case Keyword.FIRST:
                        multiTableInsertType = new MultiTableInsertType.First();
                        break;
                    default:
                        multiTableInsertType = new MultiTableInsertType.All();
                        break;
                }
                return ParseMultiTableInsert(
                    parser,
                    insertToken,
                    overwrite,
                    multiTableInsertType);
            }

            // Not a multi-table insert, rewind
            if (overwrite)
            {
                parser.PrevToken(); // rewind OVERWRITE
            }
            parser.PrevToken(); // rewind INSERT
        }

        return null;
    }

    public override Option<ColumnOption?> ParseColumnOption(Parser parser)
    {
        // Rust: `parser.maybe_parse(|parser| { .. Ok(f(parser).map(|p| Some(..))) .. })`. A closure devolve
        // `Ok(Result<Option<ColumnOption>>)`: o erro das funções auxiliares fica DENTRO do `Ok` (o
        // `maybe_parse` não volta a posição) e o chamador o propaga com `return option`. Aqui a closure
        // devolve a tupla (Value, Error) e o erro guardado é relançado depois do `MaybeParseValue`.
        var parsed = parser.MaybeParseValue(parser =>
        {
            var @with = parser.ParseKeyword(Keyword.WITH);

            if (parser.ParseKeyword(Keyword.IDENTITY))
            {
                return Map(() => new ColumnOption.Identity(
                    new IdentityPropertyKind.Identity(ParseIdentityProperty(parser))));
            }
            else if (parser.ParseKeyword(Keyword.AUTOINCREMENT))
            {
                return Map(() => new ColumnOption.Identity(
                    new IdentityPropertyKind.Autoincrement(ParseIdentityProperty(parser))));
            }
            else if (parser.ParseKeywords(Keyword.MASKING, Keyword.POLICY))
            {
                return Map(() => new ColumnOption.Policy(
                    new ColumnPolicy.MaskingPolicy(ParseColumnPolicyProperty(parser, @with))));
            }
            else if (parser.ParseKeywords(Keyword.PROJECTION, Keyword.POLICY))
            {
                return Map(() => new ColumnOption.Policy(
                    new ColumnPolicy.ProjectionPolicy(ParseColumnPolicyProperty(parser, @with))));
            }
            else if (parser.ParseKeywords(Keyword.TAG))
            {
                return Map(() => new ColumnOption.Tags(ParseColumnTags(parser, @with)));
            }
            else
            {
                throw new ParserException(new ParserError.ParserErrorValue("not found match"));
            }
        });

        if (parsed is not { } result)
        {
            return Option<ColumnOption?>.None;
        }
        if (result.Error is { } error)
        {
            throw StackGuard.Rethrow(error);
        }
        return Option<ColumnOption?>.Some(result.Value);

        // Rust: `f(parser).map(|p| Some(..))` dentro de `Ok(..)`: o erro de `f` vira valor, não exceção.
        static (ColumnOption? Value, ParserException? Error) Map(Func<ColumnOption> f)
        {
            try
            {
                return (f(), null);
            }
            catch (ParserException e)
            {
                return (null, e);
            }
        }
    }

    public override byte? GetNextPrecedence(Parser parser)
    {
        var token = parser.PeekTokenRef();
        // Snowflake supports the `:` cast operator unlike other dialects
        switch (token.Token)
        {
            case Token.Colon:
                return PrecValue(Precedence.DoubleColon);
            default:
                return null;
        }
    }

    public override bool DescribeRequiresTableKeyword => true;

    public override bool AllowExtractCustom => true;

    public override bool AllowExtractSingleQuotes => true;

    /// <summary>
    /// Snowflake expects the <c>LIKE</c> option before the <c>IN</c> option,
    /// for example: <see href="https://docs.snowflake.com/en/sql-reference/sql/show-views#syntax"/>
    /// </summary>
    public override bool SupportsShowLikeBeforeIn => true;

    public override bool SupportsLeftAssociativeJoinsWithoutParens => false;

    public override bool IsReservedForIdentifier(Keyword kw)
    {
        // Unreserve some keywords that Snowflake accepts as identifiers
        // See: https://docs.snowflake.com/en/sql-reference/reserved-keywords
        if (kw is Keyword.INTERVAL)
        {
            return false;
        }
        else
        {
            return Keywords.ReservedForIdentifier.Contains(kw);
        }
    }

    public override bool SupportsPartiql => true;

    public override bool IsColumnAlias(Keyword kw, Parser parser)
    {
        switch (kw)
        {
            // The following keywords can be considered an alias as long as
            // they are not followed by other tokens that may change their meaning
            // e.g. `SELECT * EXCEPT (col1) FROM tbl`
            case Keyword.EXCEPT
                // e.g. `INSERT INTO t SELECT 1 RETURNING *`
                or Keyword.RETURNING when parser.PeekTokenRef().Token is not (Token.Comma or Token.EOF):
                return false;

            // e.g. `SELECT 1 LIMIT 5` - not an alias
            // e.g. `SELECT 1 OFFSET 5 ROWS` - not an alias
            case Keyword.LIMIT or Keyword.OFFSET when PeekForLimitOptions(parser):
                return false;

            // `FETCH` can be considered an alias as long as it's not followed by `FIRST`` or `NEXT`
            // which would give it a different meanings, for example:
            // `SELECT 1 FETCH FIRST 10 ROWS` - not an alias
            // `SELECT 1 FETCH 10` - not an alias
            case Keyword.FETCH when parser.PeekOneOfKeywords(Keyword.FIRST, Keyword.NEXT) is not null
                    || PeekForLimitOptions(parser):
                return false;

            // Reserved keywords by the Snowflake dialect, which seem to be less strictive
            // than what is listed in `keywords::RESERVED_FOR_COLUMN_ALIAS`. The following
            // keywords were tested with the this statement: `SELECT 1 <KW>`.
            case Keyword.FROM
                or Keyword.GROUP
                or Keyword.HAVING
                or Keyword.INTERSECT
                or Keyword.INTO
                or Keyword.MINUS
                or Keyword.ORDER
                or Keyword.SELECT
                or Keyword.UNION
                or Keyword.WHERE
                or Keyword.WITH:
                return false;

            // Any other word is considered an alias
            default:
                return true;
        }
    }

    public override bool IsTableAlias(Keyword kw, Parser parser)
    {
        switch (kw)
        {
            // The following keywords can be considered an alias as long as
            // they are not followed by other tokens that may change their meaning
            case Keyword.RETURNING
                or Keyword.INNER
                or Keyword.USING
                or Keyword.PIVOT
                or Keyword.UNPIVOT
                or Keyword.EXCEPT
                or Keyword.MATCH_RECOGNIZE
                when parser.PeekTokenRef().Token is not (Token.SemiColon or Token.EOF):
                return false;

            // `LIMIT` can be considered an alias as long as it's not followed by a value. For example:
            // `SELECT * FROM tbl LIMIT WHERE 1=1` - alias
            // `SELECT * FROM tbl LIMIT 3` - not an alias
            case Keyword.LIMIT or Keyword.OFFSET when PeekForLimitOptions(parser):
                return false;

            // `FETCH` can be considered an alias as long as it's not followed by `FIRST`` or `NEXT`
            // which would give it a different meanings, for example:
            // `SELECT * FROM tbl FETCH FIRST 10 ROWS` - not an alias
            // `SELECT * FROM tbl FETCH 10` - not an alias
            case Keyword.FETCH
                when parser
                    .PeekOneOfKeywords(Keyword.FIRST, Keyword.NEXT) is not null
                    || PeekForLimitOptions(parser):
                return false;

            // All sorts of join-related keywords can be considered aliases unless additional
            // keywords change their meaning.
            case Keyword.RIGHT or Keyword.LEFT or Keyword.SEMI or Keyword.ANTI
                when parser
                    .PeekOneOfKeywords(Keyword.JOIN, Keyword.OUTER) is not null:
                return false;

            case Keyword.GLOBAL when parser.PeekKeyword(Keyword.FULL):
                return false;

            // Reserved keywords by the Snowflake dialect, which seem to be less strictive
            // than what is listed in `keywords::RESERVED_FOR_TABLE_ALIAS`. The following
            // keywords were tested with the this statement: `SELECT <KW>.* FROM tbl <KW>`.
            case Keyword.WITH
                or Keyword.ORDER
                or Keyword.SELECT
                or Keyword.WHERE
                or Keyword.GROUP
                or Keyword.HAVING
                or Keyword.LATERAL
                or Keyword.UNION
                or Keyword.INTERSECT
                or Keyword.MINUS
                or Keyword.ON
                or Keyword.JOIN
                or Keyword.INNER
                or Keyword.CROSS
                or Keyword.FULL
                or Keyword.LEFT
                or Keyword.RIGHT
                or Keyword.NATURAL
                or Keyword.USING
                or Keyword.ASOF
                or Keyword.MATCH_CONDITION
                or Keyword.SET
                or Keyword.QUALIFY
                or Keyword.FOR
                or Keyword.START
                or Keyword.CONNECT
                or Keyword.SAMPLE
                or Keyword.TABLESAMPLE
                or Keyword.FROM:
                return false;

            // Any other word is considered an alias
            default:
                return true;
        }
    }

    public override bool IsTableFactor(Keyword kw, Parser parser)
    {
        switch (kw)
        {
            case Keyword.LIMIT when PeekForLimitOptions(parser):
                return false;
            // Table function
            case Keyword.TABLE when parser.PeekTokenRef().Token is Token.LParen:
                return true;
            default:
                return !ReservedKeywordsForTableFactor.Contains(kw);
        }
    }

    /// <summary>
    /// See: <see href="https://docs.snowflake.com/en/sql-reference/constructs/at-before"/>
    /// </summary>
    public override bool SupportsTableVersioning => true;

    /// <summary>
    /// See: <see href="https://docs.snowflake.com/en/sql-reference/constructs/group-by"/>
    /// </summary>
    public override bool SupportsGroupByExpr => true;

    /// <summary>
    /// See: <see href="https://docs.snowflake.com/en/sql-reference/constructs/connect-by"/>
    /// </summary>
    public override IReadOnlyList<Keyword> GetReservedKeywordsForSelectItemOperator => ReservedKeywordsForSelectItemOperator;

    public override bool SupportsSpaceSeparatedColumnOptions => true;

    public override bool SupportsCommaSeparatedDropColumnList => true;

    public override bool IsIdentifierGeneratingFunctionName(
        Ident ident,
        IReadOnlyList<ObjectNamePart> nameParts) =>
        ident.QuoteStyle is null
            // PORT-DEVIATION DIV-001: `to_lowercase()` → `ToLowerInvariant()` (mapeamento simples). Sem efeito
            // aqui, em todos os alvos: fora do ASCII, só U+212A (vira `k`) e U+0130 (vira `i` + U+0307 no Rust;
            // fica como está no .NET) minusculizam para algo que começa por letra ASCII, e nos dois lados a
            // comparação com "identifier" dá o mesmo resultado.
            && ident.Value.ToLowerInvariant() == "identifier"
            && !nameParts
                .Any(p => p is ObjectNamePart.Function);

    // For example: `SELECT IDENTIFIER('alias1').* FROM tbl AS alias1`
    public override bool SupportsSelectExprStar => true;

    public override bool SupportsSelectWildcardExclude => true;

    public override bool SupportsSemanticViewTableFactor => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/sql/select#parameters"/>
    /// </summary>
    public override bool SupportsSelectWildcardReplace => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/sql/select#parameters"/>
    /// </summary>
    public override bool SupportsSelectWildcardIlike => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/sql/select#parameters"/>
    /// </summary>
    public override bool SupportsSelectWildcardRename => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/user-guide/querying-semistructured#label-higher-order-functions"/>
    /// </summary>
    public override bool SupportsLambdaFunctions => true;

    public override bool SupportsCommaSeparatedTrim => true;

    // Rust: funções livres de snowflake.rs (linhas 700–1990, depois do `impl Dialect`) → membros `static`
    // do tipo do arquivo (convencoes.md §13, caso 5); `pub fn` → `public`, `fn` → `internal` (casos 20 e
    // 52). `parse_snowflake_stage_name` é reexportada por dialect/mod.rs e usada pelo parser.

    // Peeks ahead to identify tokens that are expected after
    // a LIMIT/FETCH keyword.
    internal static bool PeekForLimitOptions(Parser parser)
    {
        switch (parser.PeekTokenRef().Token)
        {
            case Token.Number or Token.Placeholder:
                return true;
            case Token.SingleQuotedString { Value: var val } when val.Length == 0:
                return true;
            case Token.DollarQuotedString { Value: { Value: var value } } when value.Length == 0:
                return true;
            case Token.Word { Value: var w } when w.Keyword == Keyword.NULL:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Parse a Snowflake <c>PUT &lt;source&gt; &lt;stage&gt; [ options ]</c> statement. The caller
    /// is expected to have already consumed <c>PUT</c>.
    /// </summary>
    /// <remarks>See <see href="https://docs.snowflake.com/en/sql-reference/sql/put"/>.</remarks>
    internal static Statement ParsePut(Parser parser)
    {
        var source = parser.ParseLiteralString();
        var stage = ParseSnowflakeStageName(parser);
        // Rust: `&[]` → chamada sem os `endWords` (params).
        var options = parser.ParseKeyValueOptions(false);
        return new Statement.Put(
            Source: source,
            Stage: stage,
            Options: options);
    }

    internal static Statement ParseFileStagingCommand(Keyword kw, Parser parser)
    {
        var stage = ParseSnowflakeStageName(parser);
        string? pattern;
        if (parser.ParseKeyword(Keyword.PATTERN))
        {
            parser.ExpectToken(new Token.Eq());
            pattern = parser.ParseLiteralString();
        }
        else
        {
            pattern = null;
        }

        switch (kw)
        {
            case Keyword.LIST or Keyword.LS:
                return new Statement.List(new FileStagingCommand(Stage: stage, Pattern: pattern));
            case Keyword.REMOVE or Keyword.RM:
                return new Statement.Remove(new FileStagingCommand(Stage: stage, Pattern: pattern));
            default:
                throw new ParserException(new ParserError.ParserErrorValue(
                    "unexpected stage command, expecting LIST, LS, REMOVE or RM"));
        }
    }

    /// <summary>Parse snowflake alter dynamic table.</summary>
    /// <remarks><see href="https://docs.snowflake.com/en/sql-reference/sql/alter-table"/></remarks>
    internal static Statement ParseAlterDynamicTable(Parser parser)
    {
        // Use parse_object_name(true) to support IDENTIFIER() function
        var tableName = parser.ParseObjectName(true);

        // Parse the operation (REFRESH, SUSPEND, or RESUME)
        AlterTableOperation operation;
        if (parser.ParseKeyword(Keyword.REFRESH))
        {
            operation = new AlterTableOperation.Refresh(Subpath: null);
        }
        else if (parser.ParseKeyword(Keyword.SUSPEND))
        {
            operation = new AlterTableOperation.Suspend();
        }
        else if (parser.ParseKeyword(Keyword.RESUME))
        {
            operation = new AlterTableOperation.Resume();
        }
        else
        {
            return parser.ExpectedRef<Statement>(
                "REFRESH, SUSPEND, or RESUME after ALTER DYNAMIC TABLE",
                parser.PeekTokenRef());
        }

        TokenWithSpan endToken;
        if (parser.PeekTokenRef().Token is Token.SemiColon)
        {
            endToken = parser.PeekTokenRef();
        }
        else
        {
            endToken = parser.GetCurrentToken();
        }

        return new Statement.AlterTable(new AlterTable(
            Name: tableName,
            IfExists: false,
            Only: false,
            Operations: Sequence.Of(operation),
            Location: null,
            OnCluster: null,
            TableType: AlterTableType.Dynamic,
            EndToken: new AttachedToken(endToken)));
    }

    /// <summary>Parse snowflake alter external table.</summary>
    /// <remarks><see href="https://docs.snowflake.com/en/sql-reference/sql/alter-external-table"/></remarks>
    internal static Statement ParseAlterExternalTable(Parser parser)
    {
        var ifExists = parser.ParseKeywords(Keyword.IF, Keyword.EXISTS);
        var tableName = parser.ParseObjectName(true);

        // Parse the operation (REFRESH for now)
        AlterTableOperation operation;
        if (parser.ParseKeyword(Keyword.REFRESH))
        {
            // Optional subpath for refreshing specific partitions
            string? subpath;
            switch (parser.PeekToken().Token)
            {
                case Token.SingleQuotedString { Value: var s }:
                    parser.NextToken();
                    subpath = s;
                    break;
                default:
                    subpath = null;
                    break;
            }
            operation = new AlterTableOperation.Refresh(Subpath: subpath);
        }
        else
        {
            return parser.ExpectedRef<Statement>(
                "REFRESH after ALTER EXTERNAL TABLE",
                parser.PeekTokenRef());
        }

        TokenWithSpan endToken;
        if (parser.PeekTokenRef().Token is Token.SemiColon)
        {
            endToken = parser.PeekTokenRef();
        }
        else
        {
            endToken = parser.GetCurrentToken();
        }

        return new Statement.AlterTable(new AlterTable(
            Name: tableName,
            IfExists: ifExists,
            Only: false,
            Operations: Sequence.Of(operation),
            Location: null,
            OnCluster: null,
            TableType: AlterTableType.External,
            EndToken: new AttachedToken(endToken)));
    }

    /// <summary>Parse snowflake alter session.</summary>
    /// <remarks><see href="https://docs.snowflake.com/en/sql-reference/sql/alter-session"/></remarks>
    internal static Statement ParseAlterSession(Parser parser, bool set)
    {
        var sessionOptions = ParseSessionOptions(parser, set);
        return new Statement.AlterSession(
            Set: set,
            SessionParams: new KeyValueOptions(
                Options: sessionOptions,
                Delimiter: KeyValueOptionsDelimiter.Space));
    }

    /// <summary>Parse snowflake create table statement.</summary>
    /// <remarks>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table"/>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-iceberg-table"/>
    /// </remarks>
    // #[allow(clippy::too_many_arguments)]: não traduzido.
    public static CreateTable ParseCreateTable(
        bool orReplace,
        bool? global,
        bool temporary,
        bool @volatile,
        bool transient,
        bool iceberg,
        bool @dynamic,
        Parser parser)
    {
        var ifNotExists = parser.ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var tableName = parser.ParseObjectName(false);

        var builder = new CreateTableBuilder(tableName)
            .SetOrReplace(orReplace)
            .SetIfNotExists(ifNotExists)
            .SetTemporary(temporary)
            .SetTransient(transient)
            .SetVolatile(@volatile)
            .SetIceberg(iceberg)
            .SetGlobal(global)
            .SetDynamic(@dynamic)
            .SetHiveFormats(null);

        // Snowflake does not enforce order of the parameters in the statement. The parser needs to
        // parse the statement in a loop.
        //
        // "CREATE TABLE x COPY GRANTS (c INT)" and "CREATE TABLE x (c INT) COPY GRANTS" are both
        // accepted by Snowflake

        var plainOptions = new List<SqlOption>();

        while (true)
        {
            var nextToken = parser.NextToken();
            // Rust: `break` do `loop` dentro do `match` usado como instrução → flag `stop` (decisão 1 da B4).
            var stop = false;
            switch (nextToken.Token)
            {
                case Token.Word { Value: var word }:
                {
                    switch (word.Keyword)
                    {
                        case Keyword.COPY:
                        {
                            parser.ExpectKeywordIs(Keyword.GRANTS);
                            builder = builder.SetCopyGrants(true);
                            break;
                        }
                        case Keyword.COMMENT:
                        {
                            // Rewind the COMMENT keyword
                            parser.PrevToken();
                            if (parser.ParseOptionalInlineComment() is { } commentDef)
                            {
                                plainOptions.Add(new SqlOption.Comment(commentDef));
                            }
                            break;
                        }
                        case Keyword.AS:
                        {
                            var query = parser.ParseQuery();
                            builder = builder.SetQuery(query);
                            break;
                        }
                        case Keyword.CLONE:
                        {
                            // Rust: `parser.parse_object_name(false).ok()` (decisão 1 da B2: sem restaurar a posição).
                            ObjectName? clone;
                            try
                            {
                                clone = parser.ParseObjectName(false);
                            }
                            catch (ParserException)
                            {
                                clone = null;
                            }
                            builder = builder.CloneClause(clone);
                            break;
                        }
                        case Keyword.LIKE:
                        {
                            var name = parser.ParseObjectName(false);
                            builder = builder.SetLike(new CreateTableLikeKind.Plain(
                                new CreateTableLike(
                                    Name: name,
                                    Defaults: null)));
                            break;
                        }
                        case Keyword.CLUSTER:
                        {
                            parser.ExpectKeywordIs(Keyword.BY);
                            parser.ExpectToken(new Token.LParen());
                            var clusterBy = new WrappedCollection<Sequence<Expr>>.Parentheses(
                                parser.ParseCommaSeparated(p => p.ParseExpr()));
                            parser.ExpectToken(new Token.RParen());

                            builder = builder.SetClusterBy(clusterBy);
                            break;
                        }
                        case Keyword.ENABLE_SCHEMA_EVOLUTION:
                        {
                            parser.ExpectToken(new Token.Eq());
                            builder = builder.SetEnableSchemaEvolution(parser.ParseBooleanString());
                            break;
                        }
                        case Keyword.CHANGE_TRACKING:
                        {
                            parser.ExpectToken(new Token.Eq());
                            builder = builder.SetChangeTracking(parser.ParseBooleanString());
                            break;
                        }
                        case Keyword.DATA_RETENTION_TIME_IN_DAYS:
                        {
                            parser.ExpectToken(new Token.Eq());
                            var dataRetentionTimeInDays = parser.ParseLiteralUint();
                            builder =
                                builder.SetDataRetentionTimeInDays(dataRetentionTimeInDays);
                            break;
                        }
                        case Keyword.MAX_DATA_EXTENSION_TIME_IN_DAYS:
                        {
                            parser.ExpectToken(new Token.Eq());
                            var maxDataExtensionTimeInDays = parser.ParseLiteralUint();
                            builder = builder
                                .SetMaxDataExtensionTimeInDays(maxDataExtensionTimeInDays);
                            break;
                        }
                        case Keyword.DEFAULT_DDL_COLLATION:
                        {
                            parser.ExpectToken(new Token.Eq());
                            var defaultDdlCollation = parser.ParseLiteralString();
                            builder = builder.SetDefaultDdlCollation(defaultDdlCollation);
                            break;
                        }
                        // WITH is optional, we just verify that next token is one of the expected ones and
                        // fallback to the default match statement
                        case Keyword.WITH:
                        {
                            parser.ExpectOneOfKeywords(
                                Keyword.AGGREGATION,
                                Keyword.STORAGE,
                                Keyword.TAG,
                                Keyword.ROW);
                            parser.PrevToken();
                            break;
                        }
                        case Keyword.AGGREGATION:
                        {
                            parser.ExpectKeywordIs(Keyword.POLICY);
                            var aggregationPolicy = parser.ParseObjectName(false);
                            builder = builder.SetWithAggregationPolicy(aggregationPolicy);
                            break;
                        }
                        case Keyword.ROW:
                        {
                            parser.ExpectKeywords(Keyword.ACCESS, Keyword.POLICY);
                            var policy = parser.ParseObjectName(false);
                            parser.ExpectKeywordIs(Keyword.ON);
                            parser.ExpectToken(new Token.LParen());
                            var columns = parser.ParseCommaSeparated(p => p.ParseIdentifier());
                            parser.ExpectToken(new Token.RParen());

                            builder =
                                builder.SetWithRowAccessPolicy(new RowAccessPolicy(policy, columns));
                            break;
                        }
                        case Keyword.STORAGE:
                        {
                            parser.ExpectKeywords(Keyword.LIFECYCLE, Keyword.POLICY);
                            var policy = parser.ParseObjectName(false);
                            parser.ExpectKeywordIs(Keyword.ON);
                            parser.ExpectToken(new Token.LParen());
                            var columns = parser.ParseCommaSeparated(p => p.ParseIdentifier());
                            parser.ExpectToken(new Token.RParen());

                            builder = builder.SetWithStorageLifecyclePolicy(new StorageLifecyclePolicy(
                                Policy: policy,
                                On: columns));
                            break;
                        }
                        case Keyword.TAG:
                        {
                            parser.ExpectToken(new Token.LParen());
                            var tags = parser.ParseCommaSeparated(p => p.ParseTag());
                            parser.ExpectToken(new Token.RParen());
                            builder = builder.SetWithTags(tags);
                            break;
                        }
                        // Guarda com efeito colateral (consome COMMIT), na ordem do Rust (decisão 6 da B3).
                        case Keyword.ON when parser.ParseKeyword(Keyword.COMMIT):
                        {
                            var onCommit = parser.ParseCreateTableOnCommit();
                            builder = builder.SetOnCommit(onCommit);
                            break;
                        }
                        case Keyword.EXTERNAL_VOLUME:
                        {
                            parser.ExpectToken(new Token.Eq());
                            builder.ExternalVolume = parser.ParseLiteralString();
                            break;
                        }
                        case Keyword.CATALOG:
                        {
                            parser.ExpectToken(new Token.Eq());
                            builder.Catalog = parser.ParseLiteralString();
                            break;
                        }
                        case Keyword.BASE_LOCATION:
                        {
                            parser.ExpectToken(new Token.Eq());
                            builder.BaseLocation = parser.ParseLiteralString();
                            break;
                        }
                        case Keyword.CATALOG_SYNC:
                        {
                            parser.ExpectToken(new Token.Eq());
                            builder.CatalogSync = parser.ParseLiteralString();
                            break;
                        }
                        case Keyword.STORAGE_SERIALIZATION_POLICY:
                        {
                            parser.ExpectToken(new Token.Eq());

                            builder.StorageSerializationPolicy =
                                ParseStorageSerializationPolicy(parser);
                            break;
                        }
                        case Keyword.IF when parser.ParseKeywords(Keyword.NOT, Keyword.EXISTS):
                        {
                            builder = builder.SetIfNotExists(true);
                            break;
                        }
                        case Keyword.TARGET_LAG:
                        {
                            parser.ExpectToken(new Token.Eq());
                            var targetLag = parser.ParseLiteralString();
                            builder = builder.SetTargetLag(targetLag);
                            break;
                        }
                        case Keyword.WAREHOUSE:
                        {
                            parser.ExpectToken(new Token.Eq());
                            var warehouse = parser.ParseIdentifier();
                            builder = builder.SetWarehouse(warehouse);
                            break;
                        }
                        case Keyword.AT or Keyword.BEFORE:
                        {
                            parser.PrevToken();
                            var version = parser.MaybeParseTableVersion();
                            builder = builder.SetVersion(version);
                            break;
                        }
                        case Keyword.REFRESH_MODE:
                        {
                            parser.ExpectToken(new Token.Eq());
                            RefreshModeKind? refreshMode;
                            switch (parser.ParseOneOfKeywords(
                                Keyword.AUTO,
                                Keyword.FULL,
                                Keyword.INCREMENTAL))
                            {
                                case Keyword.AUTO:
                                    refreshMode = new RefreshModeKind.Auto();
                                    break;
                                case Keyword.FULL:
                                    refreshMode = new RefreshModeKind.Full();
                                    break;
                                case Keyword.INCREMENTAL:
                                    refreshMode = new RefreshModeKind.Incremental();
                                    break;
                                default:
                                    return parser.Expected<CreateTable>("AUTO, FULL or INCREMENTAL", nextToken);
                            }
                            builder = builder.SetRefreshMode(refreshMode);
                            break;
                        }
                        case Keyword.INITIALIZE:
                        {
                            parser.ExpectToken(new Token.Eq());
                            InitializeKind? initialize;
                            switch (parser
                                .ParseOneOfKeywords(Keyword.ON_CREATE, Keyword.ON_SCHEDULE))
                            {
                                case Keyword.ON_CREATE:
                                    initialize = new InitializeKind.OnCreate();
                                    break;
                                case Keyword.ON_SCHEDULE:
                                    initialize = new InitializeKind.OnSchedule();
                                    break;
                                default:
                                    return parser.Expected<CreateTable>("ON_CREATE or ON_SCHEDULE", nextToken);
                            }
                            builder = builder.SetInitialize(initialize);
                            break;
                        }
                        // Guarda com efeito colateral (consome USER), na ordem do Rust (decisão 6 da B3).
                        case Keyword.REQUIRE when parser.ParseKeyword(Keyword.USER):
                        {
                            builder = builder.SetRequireUser(true);
                            break;
                        }
                        default:
                        {
                            return parser.Expected<CreateTable>("end of statement", nextToken);
                        }
                    }
                    break;
                }
                case Token.LParen:
                {
                    parser.PrevToken();
                    var (columns, constraints) = parser.ParseColumns();
                    builder = builder.SetColumns(columns).SetConstraints(constraints);
                    break;
                }
                case Token.EOF:
                {
                    stop = true;
                    break;
                }
                case Token.SemiColon:
                {
                    parser.PrevToken();
                    stop = true;
                    break;
                }
                default:
                {
                    return parser.Expected<CreateTable>("end of statement", nextToken);
                }
            }
            if (stop)
            {
                break;
            }
        }
        CreateTableOptions tableOptions;
        if (plainOptions.Count != 0)
        {
            tableOptions = new CreateTableOptions.Plain(plainOptions.ToSequence());
        }
        else
        {
            tableOptions = new CreateTableOptions.None();
        }

        builder = builder.SetTableOptions(tableOptions);

        if (iceberg && builder.BaseLocation is null)
        {
            throw new ParserException(new ParserError.ParserErrorValue(
                "BASE_LOCATION is required for ICEBERG tables"));
        }

        return builder.Build();
    }

    /// <summary>Parse snowflake create database statement.</summary>
    /// <remarks><see href="https://docs.snowflake.com/en/sql-reference/sql/create-database"/></remarks>
    public static Statement ParseCreateDatabase(
        bool orReplace,
        bool transient,
        Parser parser)
    {
        var ifNotExists = parser.ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var name = parser.ParseObjectName(false);

        var builder = new CreateDatabaseBuilder(name)
            .SetOrReplace(orReplace)
            .SetTransient(transient)
            .SetIfNotExists(ifNotExists);

        while (true)
        {
            var nextToken = parser.NextToken();
            // Rust: `break` do `loop` dentro do `match` usado como instrução → flag `stop` (decisão 1 da B4).
            var stop = false;
            switch (nextToken.Token)
            {
                case Token.Word { Value: var word }:
                {
                    switch (word.Keyword)
                    {
                        case Keyword.CLONE:
                        {
                            builder = builder.CloneClause(parser.ParseObjectName(false));
                            break;
                        }
                        case Keyword.DATA_RETENTION_TIME_IN_DAYS:
                        {
                            parser.ExpectToken(new Token.Eq());
                            builder =
                                builder.SetDataRetentionTimeInDays(parser.ParseLiteralUint());
                            break;
                        }
                        case Keyword.MAX_DATA_EXTENSION_TIME_IN_DAYS:
                        {
                            parser.ExpectToken(new Token.Eq());
                            builder =
                                builder.SetMaxDataExtensionTimeInDays(parser.ParseLiteralUint());
                            break;
                        }
                        case Keyword.EXTERNAL_VOLUME:
                        {
                            parser.ExpectToken(new Token.Eq());
                            builder = builder.SetExternalVolume(parser.ParseLiteralString());
                            break;
                        }
                        case Keyword.CATALOG:
                        {
                            parser.ExpectToken(new Token.Eq());
                            builder = builder.SetCatalog(parser.ParseLiteralString());
                            break;
                        }
                        case Keyword.REPLACE_INVALID_CHARACTERS:
                        {
                            parser.ExpectToken(new Token.Eq());
                            builder =
                                builder.SetReplaceInvalidCharacters(parser.ParseBooleanString());
                            break;
                        }
                        case Keyword.DEFAULT_DDL_COLLATION:
                        {
                            parser.ExpectToken(new Token.Eq());
                            builder = builder.SetDefaultDdlCollation(parser.ParseLiteralString());
                            break;
                        }
                        case Keyword.STORAGE_SERIALIZATION_POLICY:
                        {
                            parser.ExpectToken(new Token.Eq());
                            var policy = ParseStorageSerializationPolicy(parser);
                            builder = builder.SetStorageSerializationPolicy(policy);
                            break;
                        }
                        case Keyword.COMMENT:
                        {
                            parser.ExpectToken(new Token.Eq());
                            builder = builder.SetComment(parser.ParseLiteralString());
                            break;
                        }
                        case Keyword.CATALOG_SYNC:
                        {
                            parser.ExpectToken(new Token.Eq());
                            builder = builder.SetCatalogSync(parser.ParseLiteralString());
                            break;
                        }
                        case Keyword.CATALOG_SYNC_NAMESPACE_FLATTEN_DELIMITER:
                        {
                            parser.ExpectToken(new Token.Eq());
                            builder = builder.SetCatalogSyncNamespaceFlattenDelimiter(
                                parser.ParseLiteralString());
                            break;
                        }
                        case Keyword.CATALOG_SYNC_NAMESPACE_MODE:
                        {
                            parser.ExpectToken(new Token.Eq());
                            CatalogSyncNamespaceMode mode;
                            switch (parser.ParseOneOfKeywords(Keyword.NEST, Keyword.FLATTEN))
                            {
                                case Keyword.NEST:
                                    mode = new CatalogSyncNamespaceMode.Nest();
                                    break;
                                case Keyword.FLATTEN:
                                    mode = new CatalogSyncNamespaceMode.Flatten();
                                    break;
                                default:
                                    return parser.Expected<Statement>("NEST or FLATTEN", nextToken);
                            }
                            builder = builder.SetCatalogSyncNamespaceMode(mode);
                            break;
                        }
                        case Keyword.WITH:
                        {
                            if (parser.ParseKeyword(Keyword.TAG))
                            {
                                parser.ExpectToken(new Token.LParen());
                                var tags = parser.ParseCommaSeparated(p => p.ParseTag());
                                parser.ExpectToken(new Token.RParen());
                                builder = builder.SetWithTags(tags);
                            }
                            else if (parser.ParseKeyword(Keyword.CONTACT))
                            {
                                parser.ExpectToken(new Token.LParen());
                                var contacts = parser.ParseCommaSeparated(p =>
                                {
                                    var purpose = p.ParseIdentifier().Value;
                                    p.ExpectToken(new Token.Eq());
                                    var contact = p.ParseIdentifier().Value;
                                    return new ContactEntry(Purpose: purpose, Contact: contact);
                                });
                                parser.ExpectToken(new Token.RParen());
                                builder = builder.SetWithContacts(contacts);
                            }
                            else
                            {
                                return parser.Expected<Statement>("TAG or CONTACT", nextToken);
                            }
                            break;
                        }
                        default:
                            return parser.Expected<Statement>("end of statement", nextToken);
                    }
                    break;
                }
                case Token.SemiColon or Token.EOF:
                    stop = true;
                    break;
                default:
                    return parser.Expected<Statement>("end of statement", nextToken);
            }
            if (stop)
            {
                break;
            }
        }
        return builder.Build();
    }

    public static StorageSerializationPolicy ParseStorageSerializationPolicy(
        Parser parser)
    {
        var nextToken = parser.NextToken();
        switch (nextToken.Token)
        {
            case Token.Word { Value: var w }:
                switch (w.Keyword)
                {
                    case Keyword.COMPATIBLE:
                        return new StorageSerializationPolicy.Compatible();
                    case Keyword.OPTIMIZED:
                        return new StorageSerializationPolicy.Optimized();
                    default:
                        return parser.Expected<StorageSerializationPolicy>("storage_serialization_policy", nextToken);
                }
            default:
                return parser.Expected<StorageSerializationPolicy>("storage_serialization_policy", nextToken);
        }
    }

    public static Statement ParseCreateStage(
        bool orReplace,
        bool temporary,
        Parser parser)
    {
        //[ IF NOT EXISTS ]
        var ifNotExists = parser.ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var name = parser.ParseObjectName(false);
        var directoryTableParams = Sequence<KeyValueOption>.Empty;
        var fileFormat = Sequence<KeyValueOption>.Empty;
        var copyOptions = Sequence<KeyValueOption>.Empty;
        string? comment = null;

        // [ internalStageParams | externalStageParams ]
        var stageParams = ParseStageParams(parser);

        // [ directoryTableParams ]
        if (parser.ParseKeyword(Keyword.DIRECTORY))
        {
            parser.ExpectToken(new Token.Eq());
            directoryTableParams = parser.ParseKeyValueOptions(true).Options;
        }

        // [ file_format]
        if (parser.ParseKeyword(Keyword.FILE_FORMAT))
        {
            parser.ExpectToken(new Token.Eq());
            fileFormat = parser.ParseKeyValueOptions(true).Options;
        }

        // [ copy_options ]
        if (parser.ParseKeyword(Keyword.COPY_OPTIONS))
        {
            parser.ExpectToken(new Token.Eq());
            copyOptions = parser.ParseKeyValueOptions(true).Options;
        }

        // [ comment ]
        if (parser.ParseKeyword(Keyword.COMMENT))
        {
            parser.ExpectToken(new Token.Eq());
            comment = parser.ParseCommentValue();
        }

        return new Statement.CreateStage(
            OrReplace: orReplace,
            Temporary: temporary,
            IfNotExists: ifNotExists,
            Name: name,
            StageParams: stageParams,
            DirectoryTableParams: new KeyValueOptions(
                Options: directoryTableParams,
                Delimiter: KeyValueOptionsDelimiter.Space),
            FileFormat: new KeyValueOptions(
                Options: fileFormat,
                Delimiter: KeyValueOptionsDelimiter.Space),
            CopyOptions: new KeyValueOptions(
                Options: copyOptions,
                Delimiter: KeyValueOptionsDelimiter.Space),
            Comment: comment);
    }

    /// <summary>Parse a Snowflake <c>CREATE FILE FORMAT</c> statement.</summary>
    /// <remarks>See <see href="https://docs.snowflake.com/en/sql-reference/sql/create-file-format"/></remarks>
    public static Statement ParseCreateFileFormat(
        bool orReplace,
        bool temporary,
        bool @volatile,
        Parser parser)
    {
        var ifNotExists = parser.ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var name = parser.ParseObjectName(true);
        var options = parser.ParseKeyValueOptions(false, Keyword.COMMENT);
        string? comment;
        if (parser.ParseKeyword(Keyword.COMMENT))
        {
            parser.ExpectToken(new Token.Eq());
            comment = parser.ParseCommentValue();
        }
        else
        {
            comment = null;
        }

        return new Statement.CreateFileFormat(
            OrReplace: orReplace,
            Temporary: temporary,
            Volatile: @volatile,
            IfNotExists: ifNotExists,
            Name: name,
            Options: options,
            Comment: comment);
    }

    public static Ident ParseStageNameIdentifier(Parser parser)
    {
        // Rust: `String` montada com push/push_str → StringBuilder (System.Text qualificado: decisão 8 da B3).
        var ident = new System.Text.StringBuilder();
        while (parser.NextTokenNoSkip() is { } nextToken)
        {
            // Rust: `break` do `while let` dentro do `match` → flag `stop` (decisão 1 da B4).
            var stop = false;
            switch (nextToken.Token)
            {
                case Token.Whitespace or Token.SemiColon:
                    stop = true;
                    break;
                case Token.Period:
                    parser.PrevToken();
                    stop = true;
                    break;
                case Token.LParen or Token.RParen:
                    parser.PrevToken();
                    stop = true;
                    break;
                case Token.AtSign:
                    ident.Append('@');
                    break;
                case Token.Tilde:
                    ident.Append('~');
                    break;
                case Token.Mod:
                    ident.Append('%');
                    break;
                case Token.Div:
                    ident.Append('/');
                    break;
                case Token.Plus:
                    ident.Append('+');
                    break;
                case Token.Minus:
                    ident.Append('-');
                    break;
                case Token.Eq:
                    ident.Append('=');
                    break;
                case Token.Colon:
                    ident.Append(':');
                    break;
                case Token.Number { Value: var n }:
                    ident.Append(n);
                    break;
                case Token.Word { Value: var w }:
                    ident.Append(w.ToString());
                    break;
                default:
                    return parser.ExpectedRef<Ident>("stage name identifier", parser.PeekTokenRef());
            }
            if (stop)
            {
                break;
            }
        }
        return new Ident(ident.ToString());
    }

    /// <summary>Parses a Snowflake stage name, which may start with <c>@</c> for internal stages.</summary>
    /// <remarks>Examples: <c>@mystage</c>, <c>@namespace.stage</c>, <c>schema.table</c></remarks>
    public static ObjectName ParseSnowflakeStageName(Parser parser)
    {
        switch (parser.NextToken().Token)
        {
            case Token.AtSign:
            {
                parser.PrevToken();
                var idents = new List<Ident>();
                while (true)
                {
                    idents.Add(ParseStageNameIdentifier(parser));
                    if (!parser.ConsumeToken(new Token.Period()))
                    {
                        break;
                    }
                }
                return ObjectName.From(idents.ToSequence());
            }
            default:
            {
                parser.PrevToken();
                return parser.ParseObjectName(false);
            }
        }
    }

    /// <summary>
    /// Parses a <c>COPY INTO</c> statement. Snowflake has two variants, <c>COPY INTO &lt;table&gt;</c>
    /// and <c>COPY INTO &lt;location&gt;</c> which have different syntax.
    /// </summary>
    public static Statement ParseCopyInto(Parser parser)
    {
        CopyIntoSnowflakeKind kind;
        switch (parser.PeekTokenRef().Token)
        {
            // Indicates an internal stage
            case Token.AtSign:
                kind = CopyIntoSnowflakeKind.Location;
                break;
            // Indicates an external stage, i.e. s3://, gcs:// or azure://
            case Token.SingleQuotedString { Value: var s } when s.Contains("://"):
                kind = CopyIntoSnowflakeKind.Location;
                break;
            default:
                kind = CopyIntoSnowflakeKind.Table;
                break;
        }

        var files = new List<string>();
        Sequence<StageLoadSelectItemKind>? fromTransformations = null;
        Ident? fromStageAlias = null;
        ObjectName? fromStage = null;
        var stageParams = new StageParamsObject(
            Url: null,
            Encryption: new KeyValueOptions(
                Options: Sequence<KeyValueOption>.Empty,
                Delimiter: KeyValueOptionsDelimiter.Space),
            Endpoint: null,
            StorageIntegration: null,
            Credentials: new KeyValueOptions(
                Options: Sequence<KeyValueOption>.Empty,
                Delimiter: KeyValueOptionsDelimiter.Space));
        Query? fromQuery = null;
        Expr? partition = null;
        var fileFormat = Sequence<KeyValueOption>.Empty;
        string? pattern = null;
        string? validationMode = null;
        // Rust: `Vec` que recebe `push` e também é substituído por `.options` → List<T> (convencoes.md §5).
        var copyOptions = new List<KeyValueOption>();

        var @into = ParseSnowflakeStageName(parser);
        if (kind == CopyIntoSnowflakeKind.Location)
        {
            stageParams = ParseStageParams(parser);
        }

        Sequence<Ident>? intoColumns;
        switch (parser.PeekToken().Token)
        {
            case Token.LParen:
                intoColumns = parser.ParseParenthesizedColumnList(IsOptional.Optional, true);
                break;
            default:
                intoColumns = null;
                break;
        }

        parser.ExpectKeywordIs(Keyword.FROM);
        switch (parser.NextToken().Token)
        {
            case Token.LParen when kind == CopyIntoSnowflakeKind.Table:
            {
                // Data load with transformations
                parser.ExpectKeywordIs(Keyword.SELECT);
                fromTransformations = ParseSelectItemsForDataLoad(parser);

                parser.ExpectKeywordIs(Keyword.FROM);
                fromStage = ParseSnowflakeStageName(parser);
                stageParams = ParseStageParams(parser);

                // Parse an optional alias
                fromStageAlias = parser
                    .MaybeParseTableAlias()
                    ?.Name;
                parser.ExpectToken(new Token.RParen());
                break;
            }
            case Token.LParen when kind == CopyIntoSnowflakeKind.Location:
            {
                // Data unload with a query
                fromQuery = parser.ParseQuery();
                parser.ExpectToken(new Token.RParen());
                break;
            }
            default:
            {
                parser.PrevToken();
                fromStage = ParseSnowflakeStageName(parser);
                stageParams = ParseStageParams(parser);

                // as
                if (parser.ParseKeyword(Keyword.AS))
                {
                    switch (parser.NextToken().Token)
                    {
                        case Token.Word { Value: var w }:
                            fromStageAlias = new Ident(w.Value);
                            break;
                        default:
                            fromStageAlias = parser.ExpectedRef<Ident>("stage alias", parser.PeekTokenRef());
                            break;
                    }
                }
                else
                {
                    fromStageAlias = null;
                }
                break;
            }
        }

        while (true)
        {
            // FILE_FORMAT
            if (parser.ParseKeyword(Keyword.FILE_FORMAT))
            {
                parser.ExpectToken(new Token.Eq());
                fileFormat = parser.ParseKeyValueOptions(true).Options;
            // PARTITION BY
            }
            else if (parser.ParseKeywords(Keyword.PARTITION, Keyword.BY))
            {
                partition = parser.ParseExpr();
            // FILES
            }
            else if (parser.ParseKeyword(Keyword.FILES))
            {
                parser.ExpectToken(new Token.Eq());
                parser.ExpectToken(new Token.LParen());
                var continueLoop = true;
                while (continueLoop)
                {
                    continueLoop = false;
                    var nextToken = parser.NextToken();
                    switch (nextToken.Token)
                    {
                        case Token.SingleQuotedString { Value: var s }:
                            files.Add(s);
                            break;
                        default:
                            parser.Expected("file token", nextToken);
                            break;
                    }
                    if (parser.NextToken().Token is Token.Comma)
                    {
                        continueLoop = true;
                    }
                    else
                    {
                        parser.PrevToken(); // not a comma, need to go back
                    }
                }
                parser.ExpectToken(new Token.RParen());
            // PATTERN
            }
            else if (parser.ParseKeyword(Keyword.PATTERN))
            {
                parser.ExpectToken(new Token.Eq());
                var nextToken = parser.NextToken();
                switch (nextToken.Token)
                {
                    case Token.SingleQuotedString { Value: var s }:
                        pattern = s;
                        break;
                    default:
                        pattern = parser.Expected<string>("pattern", nextToken);
                        break;
                }
            // VALIDATION MODE
            }
            else if (parser.ParseKeyword(Keyword.VALIDATION_MODE))
            {
                parser.ExpectToken(new Token.Eq());
                validationMode = parser.NextToken().Token.ToString();
            // COPY OPTIONS
            }
            else if (parser.ParseKeyword(Keyword.COPY_OPTIONS))
            {
                parser.ExpectToken(new Token.Eq());
                copyOptions = new List<KeyValueOption>(parser.ParseKeyValueOptions(true).Options);
            }
            else
            {
                // Rust: `break` do `loop` dentro do `match` → flag `stop` (decisão 1 da B4); o `continue`
                // do braço `Token::Comma` continua sendo `continue` (decisão 10 da B5).
                var stop = false;
                switch (parser.NextToken().Token)
                {
                    case Token.SemiColon or Token.EOF:
                        stop = true;
                        break;
                    case Token.Comma:
                        continue;
                    // In `COPY INTO <location>` the copy options do not have a shared key
                    // like in `COPY INTO <table>`
                    case Token.Word { Value: var key }:
                        copyOptions.Add(parser.ParseKeyValueOption(key));
                        break;
                    default:
                        return parser
                            .ExpectedRef<Statement>("another copy option, ; or EOF'", parser.PeekTokenRef());
                }
                if (stop)
                {
                    break;
                }
            }
        }

        return new Statement.CopyIntoSnowflake(
            Kind: kind,
            Into: @into,
            IntoColumns: intoColumns,
            FromObj: fromStage,
            FromObjAlias: fromStageAlias,
            StageParams: stageParams,
            FromTransformations: fromTransformations,
            FromQuery: fromQuery,
            Files: files.Count == 0 ? (Sequence<string>?)null : files.ToSequence(),
            Pattern: pattern,
            FileFormat: new KeyValueOptions(
                Options: fileFormat,
                Delimiter: KeyValueOptionsDelimiter.Space),
            CopyOptions: new KeyValueOptions(
                Options: copyOptions.ToSequence(),
                Delimiter: KeyValueOptionsDelimiter.Space),
            ValidationMode: validationMode,
            Partition: partition);
    }

    internal static Sequence<StageLoadSelectItemKind>? ParseSelectItemsForDataLoad(
        Parser parser)
    {
        var selectItems = new List<StageLoadSelectItemKind>();
        while (true)
        {
            switch (parser.MaybeParse<StageLoadSelectItem>(ParseSelectItemForDataLoad))
            {
                // [<alias>.]$<file_col_num>[.<element>] [ , [<alias>.]$<file_col_num>[.<element>] ... ]
                case { } item:
                    selectItems.Add(new StageLoadSelectItemKind.StageLoadSelectItem(item));
                    break;
                // Fallback, try to parse a standard SQL select item
                case null:
                    selectItems.Add(new StageLoadSelectItemKind.SelectItem(
                        parser.ParseSelectItem()));
                    break;
            }
            if (parser.PeekTokenRef().Token is Token.Comma)
            {
                parser.AdvanceToken();
            }
            else
            {
                break;
            }
        }
        return selectItems.ToSequence();
    }

    internal static StageLoadSelectItem ParseSelectItemForDataLoad(
        Parser parser)
    {
        Ident? alias = null;
        var fileColNum = 0;
        Ident? element = null;
        Ident? itemAs = null;

        var nextToken = parser.NextToken();
        switch (nextToken.Token)
        {
            case Token.Placeholder { Value: var w }:
            {
                // Rust: `w.to_string().split_off(1).parse::<i32>().map_err(|e| ..)?`; o texto de `e` é o
                // `Display` de `ParseIntError` (Parser.RustParseInteger, caso 47).
                var parseError = Parser.RustParseInteger(
                    w.Substring(1), signed: true, out var magnitude, out var negative, int.MaxValue);
                if (parseError is not null)
                {
                    throw new ParserException(new ParserError.ParserErrorValue(
                        $"Could not parse '{w}' as i32: {parseError}"));
                }
                fileColNum = negative ? (int)-(long)magnitude : (int)magnitude;
                break;
            }
            case Token.Word { Value: var w }:
            {
                alias = new Ident(w.Value);
                break;
            }
            default:
            {
                parser.Expected("alias or file_col_num", nextToken);
                break;
            }
        }

        if (alias is not null)
        {
            parser.ExpectToken(new Token.Period());
            // now we get col_num token
            var colNumToken = parser.NextToken();
            switch (colNumToken.Token)
            {
                case Token.Placeholder { Value: var w }:
                {
                    // Rust: idem acima.
                    var parseError = Parser.RustParseInteger(
                        w.Substring(1), signed: true, out var magnitude, out var negative, int.MaxValue);
                    if (parseError is not null)
                    {
                        throw new ParserException(new ParserError.ParserErrorValue(
                            $"Could not parse '{w}' as i32: {parseError}"));
                    }
                    fileColNum = negative ? (int)-(long)magnitude : (int)magnitude;
                    break;
                }
                default:
                {
                    parser.Expected("file_col_num", colNumToken);
                    break;
                }
            }
        }

        // try extracting optional element
        switch (parser.NextToken().Token)
        {
            case Token.Colon:
            {
                // parse element
                string elementValue;
                switch (parser.NextToken().Token)
                {
                    case Token.Word { Value: var w }:
                        elementValue = w.Value;
                        break;
                    default:
                        elementValue = parser.ExpectedRef<string>("file_col_num", parser.PeekTokenRef());
                        break;
                }
                element = new Ident(elementValue);
                break;
            }
            default:
            {
                // element not present move back
                parser.PrevToken();
                break;
            }
        }

        // A trailing `::` means this is a cast expression (e.g.
        // `$1:"col"::NUMBER(38,0)`), not a stage-load-select-item.
        if (parser.PeekTokenRef().Token is Token.DoubleColon)
        {
            return parser.Expected<StageLoadSelectItem>("stage load select item", parser.PeekToken());
        }

        // as
        if (parser.ParseKeyword(Keyword.AS))
        {
            switch (parser.NextToken().Token)
            {
                case Token.Word { Value: var w }:
                    itemAs = new Ident(w.Value);
                    break;
                default:
                    itemAs = parser.ExpectedRef<Ident>("column item alias", parser.PeekTokenRef());
                    break;
            }
        }

        return new StageLoadSelectItem(
            Alias: alias,
            FileColNum: fileColNum,
            Element: element,
            ItemAs: itemAs);
    }

    internal static StageParamsObject ParseStageParams(Parser parser)
    {
        // Rust: `let (mut url, mut storage_integration, mut endpoint) = (None, None, None);`
        string? url = null;
        string? storageIntegration = null;
        string? endpoint = null;
        var encryption = new KeyValueOptions(
            Options: Sequence<KeyValueOption>.Empty,
            Delimiter: KeyValueOptionsDelimiter.Space);
        var credentials = new KeyValueOptions(
            Options: Sequence<KeyValueOption>.Empty,
            Delimiter: KeyValueOptionsDelimiter.Space);

        // URL
        if (parser.ParseKeyword(Keyword.URL))
        {
            parser.ExpectToken(new Token.Eq());
            switch (parser.NextToken().Token)
            {
                case Token.SingleQuotedString { Value: var word }:
                    url = word;
                    break;
                default:
                    url = parser.ExpectedRef<string>("a URL statement", parser.PeekTokenRef());
                    break;
            }
        }

        // STORAGE INTEGRATION
        if (parser.ParseKeyword(Keyword.STORAGE_INTEGRATION))
        {
            parser.ExpectToken(new Token.Eq());
            storageIntegration = parser.NextToken().Token.ToString();
        }

        // ENDPOINT
        if (parser.ParseKeyword(Keyword.ENDPOINT))
        {
            parser.ExpectToken(new Token.Eq());
            switch (parser.NextToken().Token)
            {
                case Token.SingleQuotedString { Value: var word }:
                    endpoint = word;
                    break;
                default:
                    endpoint = parser.ExpectedRef<string>("an endpoint statement", parser.PeekTokenRef());
                    break;
            }
        }

        // CREDENTIALS
        if (parser.ParseKeyword(Keyword.CREDENTIALS))
        {
            parser.ExpectToken(new Token.Eq());
            credentials = new KeyValueOptions(
                Options: parser.ParseKeyValueOptions(true).Options,
                Delimiter: KeyValueOptionsDelimiter.Space);
        }

        // ENCRYPTION
        if (parser.ParseKeyword(Keyword.ENCRYPTION))
        {
            parser.ExpectToken(new Token.Eq());
            encryption = new KeyValueOptions(
                Options: parser.ParseKeyValueOptions(true).Options,
                Delimiter: KeyValueOptionsDelimiter.Space);
        }

        return new StageParamsObject(
            Url: url,
            Encryption: encryption,
            Endpoint: endpoint,
            StorageIntegration: storageIntegration,
            Credentials: credentials);
    }

    /// <summary>
    /// Parses options separated by blank spaces, commas, or new lines like:
    /// ABORT_DETACHED_QUERY = { TRUE | FALSE }
    ///      [ ACTIVE_PYTHON_PROFILER = { 'LINE' | 'MEMORY' } ]
    ///      [ BINARY_INPUT_FORMAT = '&lt;string&gt;' ]
    /// </summary>
    internal static Sequence<KeyValueOption> ParseSessionOptions(
        Parser parser,
        bool set)
    {
        var options = new List<KeyValueOption>();
        // Rust: `let empty = String::new;` (função que cria a String vazia) → `string.Empty` no ponto de uso.
        while (true)
        {
            var peekedToken = parser.PeekToken();
            // Rust: `break` do `loop` dentro do `match` → flag `stop` (decisão 1 da B4); o `continue` do
            // braço `Token::Comma` continua sendo `continue` (decisão 10 da B5).
            var stop = false;
            switch (peekedToken.Token)
            {
                case Token.SemiColon or Token.EOF:
                    stop = true;
                    break;
                case Token.Comma:
                    parser.AdvanceToken();
                    continue;
                case Token.Word { Value: var key }:
                    parser.AdvanceToken();
                    if (set)
                    {
                        var option = parser.ParseKeyValueOption(key);
                        options.Add(option);
                    }
                    else
                    {
                        options.Add(new KeyValueOption(
                            OptionName: key.Value,
                            OptionValue: new KeyValueOptionKind.Single(
                                new Value.Placeholder(string.Empty).WithSpan(new Span(
                                    Start: peekedToken.Span.End,
                                    End: peekedToken.Span.End)))));
                    }
                    break;
                default:
                    return parser.Expected<Sequence<KeyValueOption>>("another option or end of statement", peekedToken);
            }
            if (stop)
            {
                break;
            }
        }
        if (options.Count == 0)
        {
            throw new ParserException(new ParserError.ParserErrorValue(
                "expected at least one option"));
        }
        else
        {
            return options.ToSequence();
        }
    }

    /// <summary>Parsing a property of identity or autoincrement column option</summary>
    /// <remarks>
    /// Syntax:
    /// <code>
    /// [ (seed , increment) | START num INCREMENT num ] [ ORDER | NOORDER ]
    /// </code>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table">Snowflake</see>
    /// </remarks>
    internal static IdentityProperty ParseIdentityProperty(Parser parser)
    {
        IdentityPropertyFormatKind? parameters;
        if (parser.ConsumeToken(new Token.LParen()))
        {
            var seed = parser.ParseNumber();
            parser.ExpectToken(new Token.Comma());
            var increment = parser.ParseNumber();
            parser.ExpectToken(new Token.RParen());

            parameters = new IdentityPropertyFormatKind.FunctionCall(
                new IdentityParameters(Seed: seed, Increment: increment));
        }
        else if (parser.ParseKeyword(Keyword.START))
        {
            var seed = parser.ParseNumber();
            parser.ExpectKeywordIs(Keyword.INCREMENT);
            var increment = parser.ParseNumber();

            parameters = new IdentityPropertyFormatKind.StartAndIncrement(
                new IdentityParameters(Seed: seed, Increment: increment));
        }
        else
        {
            parameters = null;
        }
        IdentityPropertyOrder? order;
        switch (parser.ParseOneOfKeywords(Keyword.ORDER, Keyword.NOORDER))
        {
            case Keyword.ORDER:
                order = new IdentityPropertyOrder.Order();
                break;
            case Keyword.NOORDER:
                order = new IdentityPropertyOrder.NoOrder();
                break;
            default:
                order = null;
                break;
        }
        return new IdentityProperty(Parameters: parameters, Order: order);
    }

    /// <summary>Parsing a policy property of column option</summary>
    /// <remarks>
    /// Syntax:
    /// <code>
    /// &lt;policy_name&gt; [ USING ( &lt;col_name&gt; , &lt;cond_col1&gt; , ... )
    /// </code>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table">Snowflake</see>
    /// </remarks>
    internal static ColumnPolicyProperty ParseColumnPolicyProperty(
        Parser parser,
        bool @with)
    {
        var policyName = parser.ParseObjectName(false);
        Sequence<Ident>? usingColumns;
        if (parser.ParseKeyword(Keyword.USING))
        {
            parser.ExpectToken(new Token.LParen());
            var columns = parser.ParseCommaSeparated(p => p.ParseIdentifier());
            parser.ExpectToken(new Token.RParen());
            usingColumns = columns;
        }
        else
        {
            usingColumns = null;
        }

        return new ColumnPolicyProperty(
            With: @with,
            PolicyName: policyName,
            UsingColumns: usingColumns);
    }

    /// <summary>Parsing tags list of column</summary>
    /// <remarks>
    /// Syntax:
    /// <code>
    /// ( &lt;tag_name&gt; = '&lt;tag_value&gt;' [ , &lt;tag_name&gt; = '&lt;tag_value&gt;' , ... ] )
    /// </code>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table">Snowflake</see>
    /// </remarks>
    internal static TagsColumnOption ParseColumnTags(Parser parser, bool @with)
    {
        parser.ExpectToken(new Token.LParen());
        var tags = parser.ParseCommaSeparated(p => p.ParseTag());
        parser.ExpectToken(new Token.RParen());

        return new TagsColumnOption(With: @with, Tags: tags);
    }

    /// <summary>Parse snowflake show objects.</summary>
    /// <remarks><see href="https://docs.snowflake.com/en/sql-reference/sql/show-objects"/></remarks>
    internal static Statement ParseShowObjects(bool terse, Parser parser)
    {
        var showOptions = parser.ParseShowStmtOptions();
        return new Statement.ShowObjects(new ShowObjects(
            Terse: terse,
            ShowOptions: showOptions));
    }

    /// <summary>Parse multi-table INSERT statement.</summary>
    /// <remarks>
    /// Syntax:
    /// <code>
    /// -- Unconditional multi-table insert
    /// INSERT [ OVERWRITE ] ALL
    ///   intoClause [ ... ]
    /// &lt;subquery&gt;
    ///
    /// -- Conditional multi-table insert
    /// INSERT [ OVERWRITE ] { FIRST | ALL }
    ///   { WHEN &lt;condition&gt; THEN intoClause [ ... ] }
    ///   [ ... ]
    ///   [ ELSE intoClause ]
    /// &lt;subquery&gt;
    /// </code>
    /// See: <see href="https://docs.snowflake.com/en/sql-reference/sql/insert-multi-table"/>
    /// </remarks>
    internal static Statement ParseMultiTableInsert(
        Parser parser,
        TokenWithSpan insertToken,
        bool overwrite,
        MultiTableInsertType multiTableInsertType)
    {
        // Check if this is conditional (has WHEN clauses) or unconditional (direct INTO clauses)
        var isConditional = parser.PeekKeyword(Keyword.WHEN);

        // Rust: `let (a, b, c) = if .. { (..) } else { (..) };` (decisão 7 da B2).
        Sequence<MultiTableInsertIntoClause> multiTableIntoClauses;
        Sequence<MultiTableInsertWhenClause> multiTableWhenClauses;
        Sequence<MultiTableInsertIntoClause>? multiTableElseClause;
        if (isConditional)
        {
            // Conditional multi-table insert: WHEN clauses
            var (whenClauses, elseClause) = ParseMultiTableInsertWhenClauses(parser);
            (multiTableIntoClauses, multiTableWhenClauses, multiTableElseClause) =
                (Sequence<MultiTableInsertIntoClause>.Empty, whenClauses, elseClause);
        }
        else
        {
            // Unconditional multi-table insert: direct INTO clauses
            var intoClauses = ParseMultiTableInsertIntoClauses(parser);
            (multiTableIntoClauses, multiTableWhenClauses, multiTableElseClause) =
                (intoClauses, Sequence<MultiTableInsertWhenClause>.Empty, null);
        }

        // Parse the source query
        var source = parser.ParseQuery();

        return new Statement.Insert(new Insert(
            InsertToken: AttachedToken.From(insertToken),
            OptimizerHints: Sequence<OptimizerHint>.Empty,
            Or: null,
            Ignore: false,
            Into: false,
            Table: new TableObject.TableName(new ObjectName(Sequence<ObjectNamePart>.Empty)), // Not used for multi-table insert
            TableAlias: null,
            Columns: Sequence<ObjectName>.Empty,
            Overwrite: overwrite,
            Source: source,
            Assignments: Sequence<Assignment>.Empty,
            Partitioned: null,
            AfterColumns: Sequence<Ident>.Empty,
            HasTableKeyword: false,
            On: null,
            Returning: null,
            Output: null,
            ReplaceInto: false,
            Priority: null,
            InsertAlias: null,
            Settings: null,
            FormatClause: null,
            MultiTableInsertType: multiTableInsertType,
            MultiTableIntoClauses: multiTableIntoClauses,
            MultiTableWhenClauses: multiTableWhenClauses,
            MultiTableElseClause: multiTableElseClause));
    }

    /// <summary>Parse one or more INTO clauses for multi-table INSERT.</summary>
    internal static Sequence<MultiTableInsertIntoClause> ParseMultiTableInsertIntoClauses(
        Parser parser)
    {
        var intoClauses = new List<MultiTableInsertIntoClause>();
        while (parser.ParseKeyword(Keyword.INTO))
        {
            intoClauses.Add(ParseMultiTableInsertIntoClause(parser));
        }
        if (intoClauses.Count == 0)
        {
            return parser.ExpectedRef<Sequence<MultiTableInsertIntoClause>>(
                "INTO clause in multi-table INSERT",
                parser.PeekTokenRef());
        }
        return intoClauses.ToSequence();
    }

    /// <summary>Parse a single INTO clause for multi-table INSERT.</summary>
    /// <remarks>Syntax: <c>INTO &lt;table&gt; [ ( &lt;columns&gt; ) ] [ VALUES ( &lt;values&gt; ) ]</c></remarks>
    internal static MultiTableInsertIntoClause ParseMultiTableInsertIntoClause(
        Parser parser)
    {
        var tableName = parser.ParseObjectName(false);

        // Parse optional column list: ( <column_name> [, ...] )
        // Rust: `.unwrap_or_default()` → vazio (o `default` de Sequence<T>, convencoes.md §13, caso 31).
        var columns = parser
            .MaybeParseValue(p => p.ParseParenthesizedColumnList(IsOptional.Mandatory, false))
            ?? Sequence<Ident>.Empty;

        // Parse optional VALUES clause
        MultiTableInsertValues? values;
        if (parser.ParseKeyword(Keyword.VALUES))
        {
            parser.ExpectToken(new Token.LParen());
            // Rust: o `let values` interno sombreia o externo (caso 72).
            var parsedValues = parser.ParseCommaSeparated<MultiTableInsertValue>(ParseMultiTableInsertValue);
            parser.ExpectToken(new Token.RParen());
            values = new MultiTableInsertValues(Values: parsedValues);
        }
        else
        {
            values = null;
        }

        return new MultiTableInsertIntoClause(
            TableName: tableName,
            Columns: columns,
            Values: values);
    }

    /// <summary>Parse a single value in a multi-table INSERT VALUES clause.</summary>
    internal static MultiTableInsertValue ParseMultiTableInsertValue(
        Parser parser)
    {
        if (parser.ParseKeyword(Keyword.DEFAULT))
        {
            return new MultiTableInsertValue.Default();
        }
        else
        {
            return new MultiTableInsertValue.Expr(parser.ParseExpr());
        }
    }

    /// <summary>Parse WHEN clauses for conditional multi-table INSERT.</summary>
    internal static (Sequence<MultiTableInsertWhenClause> WhenClauses, Sequence<MultiTableInsertIntoClause>? ElseClause)
        ParseMultiTableInsertWhenClauses(Parser parser)
    {
        var whenClauses = new List<MultiTableInsertWhenClause>();
        Sequence<MultiTableInsertIntoClause>? elseClause = null;

        // Parse WHEN clauses
        while (parser.ParseKeyword(Keyword.WHEN))
        {
            var condition = parser.ParseExpr();
            parser.ExpectKeyword(Keyword.THEN);

            // Parse INTO clauses for this WHEN
            var intoClauses = ParseMultiTableInsertIntoClauses(parser);

            whenClauses.Add(new MultiTableInsertWhenClause(
                Condition: condition,
                IntoClauses: intoClauses));
        }

        // Parse optional ELSE clause
        if (parser.ParseKeyword(Keyword.ELSE))
        {
            elseClause = ParseMultiTableInsertIntoClauses(parser);
        }

        if (whenClauses.Count == 0)
        {
            return parser.ExpectedRef<(Sequence<MultiTableInsertWhenClause>, Sequence<MultiTableInsertIntoClause>?)>(
                "at least one WHEN clause in conditional multi-table INSERT",
                parser.PeekTokenRef());
        }

        return (whenClauses.ToSequence(), elseClause);
    }
}
