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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 11943–13249:
// parse_call … parse_data_type_helper).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>
    /// Parse a <c>CALL procedure_name(arg1, arg2, ...)</c>
    /// or <c>CALL procedure_name</c> statement
    /// </summary>
    public Statement ParseCall()
    {
        var objectName = ParseObjectName(false);
        if (PeekTokenRef().Token is Token.LParen)
        {
            switch (ParseFunction(objectName))
            {
                case Expr.Function { Value: var f }:
                    return new Statement.Call(f);
                case var other:
                    throw ParserErr(
                        $"Expected a simple procedure call but found: {other}",
                        PeekTokenRef().Span.Start);
            }
        }
        else
        {
            return new Statement.Call(new Function(
                Name: objectName,
                UsesOdbcSyntax: false,
                Parameters: new FunctionArguments.None(),
                Args: new FunctionArguments.None(),
                Over: null,
                Filter: null,
                NullTreatment: null,
                WithinGroup: Sequence<OrderByExpr>.Empty));
        }
    }

    /// <summary>Parse a copy statement</summary>
    public Statement ParseCopy()
    {
        CopySource source;
        if (ConsumeToken(new Token.LParen()))
        {
            source = new CopySource.Query(ParseQuery());
            ExpectToken(new Token.RParen());
        }
        else
        {
            var tableName = ParseObjectName(false);
            var columns = ParseParenthesizedColumnList(IsOptional.Optional, false);
            source = new CopySource.Table(
                TableName: tableName,
                Columns: columns);
        }
        bool to;
        switch (ParseOneOfKeywords(Keyword.FROM, Keyword.TO))
        {
            case Keyword.FROM:
                to = false;
                break;
            case Keyword.TO:
                to = true;
                break;
            default:
                to = ExpectedRef<bool>("FROM or TO", PeekTokenRef());
                break;
        }
        if (!to)
        {
            // Use a separate if statement to prevent Rust compiler from complaining about
            // "if statement in this position is unstable: https://github.com/rust-lang/rust/issues/53667"
            if (source is CopySource.Query)
            {
                throw new ParserException(new ParserError.ParserErrorValue(
                    "COPY ... FROM does not support query as a source"));
            }
        }
        CopyTarget target;
        if (ParseKeyword(Keyword.STDIN))
        {
            target = new CopyTarget.Stdin();
        }
        else if (ParseKeyword(Keyword.STDOUT))
        {
            target = new CopyTarget.Stdout();
        }
        else if (ParseKeyword(Keyword.PROGRAM))
        {
            target = new CopyTarget.Program(
                Command: ParseLiteralString());
        }
        else
        {
            target = new CopyTarget.File(
                Filename: ParseLiteralString());
        }
        _ = ParseKeyword(Keyword.WITH); // [ WITH ]
        var options = Sequence<CopyOption>.Empty;
        if (ConsumeToken(new Token.LParen()))
        {
            options = ParseCommaSeparated(p => p.ParseCopyOption());
            ExpectToken(new Token.RParen());
        }
        var legacyOptions = new List<CopyLegacyOption>();
        while (MaybeParse(parser => parser.ParseCopyLegacyOption()) is { } opt)
        {
            legacyOptions.Add(opt);
        }
        Sequence<string?> values;
        if (target is CopyTarget.Stdin && PeekTokenRef().Token is not Token.EOF)
        {
            ExpectToken(new Token.SemiColon());
            values = ParseTsv();
        }
        else
        {
            values = Sequence<string?>.Empty;
        }
        return new Statement.Copy(
            Source: source,
            To: to,
            Target: target,
            Options: options,
            LegacyOptions: legacyOptions.ToSequence(),
            Values: values);
    }

    /// <summary>Parse <see cref="Statement.Open"/></summary>
    internal Statement ParseOpen()
    {
        ExpectKeyword(Keyword.OPEN);
        return new Statement.Open(new OpenStatement(
            CursorName: ParseIdentifier()));
    }

    /// <summary>Parse a <c>CLOSE</c> cursor statement.</summary>
    public Statement ParseClose()
    {
        CloseCursor cursor;
        if (ParseKeyword(Keyword.ALL))
        {
            cursor = new CloseCursor.All();
        }
        else
        {
            var name = ParseIdentifier();

            cursor = new CloseCursor.Specific(Name: name);
        }

        return new Statement.Close(Cursor: cursor);
    }

    internal CopyOption ParseCopyOption()
    {
        CopyOption ret;
        switch (ParseOneOfKeywords(
            Keyword.FORMAT,
            Keyword.FREEZE,
            Keyword.DELIMITER,
            Keyword.NULL,
            Keyword.HEADER,
            Keyword.QUOTE,
            Keyword.ESCAPE,
            Keyword.FORCE_QUOTE,
            Keyword.FORCE_NOT_NULL,
            Keyword.FORCE_NULL,
            Keyword.ENCODING))
        {
            case Keyword.FORMAT:
                ret = new CopyOption.Format(ParseIdentifier());
                break;
            case Keyword.FREEZE:
                // !matches!(self.parse_one_of_keywords(&[TRUE, FALSE]), Some(Keyword::FALSE))
                ret = new CopyOption.Freeze(
                    ParseOneOfKeywords(Keyword.TRUE, Keyword.FALSE) is not Keyword.FALSE);
                break;
            case Keyword.DELIMITER:
                ret = new CopyOption.Delimiter(ParseLiteralChar());
                break;
            case Keyword.NULL:
                ret = new CopyOption.Null(ParseLiteralString());
                break;
            case Keyword.HEADER:
                ret = new CopyOption.Header(
                    ParseOneOfKeywords(Keyword.TRUE, Keyword.FALSE) is not Keyword.FALSE);
                break;
            case Keyword.QUOTE:
                ret = new CopyOption.Quote(ParseLiteralChar());
                break;
            case Keyword.ESCAPE:
                ret = new CopyOption.Escape(ParseLiteralChar());
                break;
            case Keyword.FORCE_QUOTE:
                ret = new CopyOption.ForceQuote(ParseParenthesizedColumnList(IsOptional.Mandatory, false));
                break;
            case Keyword.FORCE_NOT_NULL:
                ret = new CopyOption.ForceNotNull(ParseParenthesizedColumnList(IsOptional.Mandatory, false));
                break;
            case Keyword.FORCE_NULL:
                ret = new CopyOption.ForceNull(ParseParenthesizedColumnList(IsOptional.Mandatory, false));
                break;
            case Keyword.ENCODING:
                ret = new CopyOption.Encoding(ParseLiteralString());
                break;
            default:
                ret = ExpectedRef<CopyOption>("option", PeekTokenRef());
                break;
        }
        return ret;
    }

    internal CopyLegacyOption ParseCopyLegacyOption()
    {
        // FORMAT \[ AS \] is optional
        if (ParseKeyword(Keyword.FORMAT))
        {
            _ = ParseKeyword(Keyword.AS);
        }

        CopyLegacyOption ret;
        switch (ParseOneOfKeywords(
            Keyword.ACCEPTANYDATE,
            Keyword.ACCEPTINVCHARS,
            Keyword.ADDQUOTES,
            Keyword.ALLOWOVERWRITE,
            Keyword.BINARY,
            Keyword.BLANKSASNULL,
            Keyword.BZIP2,
            Keyword.CLEANPATH,
            Keyword.COMPUPDATE,
            Keyword.CREDENTIALS,
            Keyword.CSV,
            Keyword.DATEFORMAT,
            Keyword.DELIMITER,
            Keyword.EMPTYASNULL,
            Keyword.ENCRYPTED,
            Keyword.ESCAPE,
            Keyword.EXTENSION,
            Keyword.FIXEDWIDTH,
            Keyword.GZIP,
            Keyword.HEADER,
            Keyword.IAM_ROLE,
            Keyword.IGNOREHEADER,
            Keyword.JSON,
            Keyword.MANIFEST,
            Keyword.MAXFILESIZE,
            Keyword.NULL,
            Keyword.PARALLEL,
            Keyword.PARQUET,
            Keyword.PARTITION,
            Keyword.REGION,
            Keyword.REMOVEQUOTES,
            Keyword.ROWGROUPSIZE,
            Keyword.STATUPDATE,
            Keyword.TIMEFORMAT,
            Keyword.TRUNCATECOLUMNS,
            Keyword.ZSTD))
        {
            case Keyword.ACCEPTANYDATE:
                ret = new CopyLegacyOption.AcceptAnyDate();
                break;
            case Keyword.ACCEPTINVCHARS:
            {
                _ = ParseKeyword(Keyword.AS); // [ AS ]
                string? ch;
                if (PeekTokenRef().Token is Token.SingleQuotedString)
                {
                    ch = ParseLiteralString();
                }
                else
                {
                    ch = null;
                }
                ret = new CopyLegacyOption.AcceptInvChars(ch);
                break;
            }
            case Keyword.ADDQUOTES:
                ret = new CopyLegacyOption.AddQuotes();
                break;
            case Keyword.ALLOWOVERWRITE:
                ret = new CopyLegacyOption.AllowOverwrite();
                break;
            case Keyword.BINARY:
                ret = new CopyLegacyOption.Binary();
                break;
            case Keyword.BLANKSASNULL:
                ret = new CopyLegacyOption.BlankAsNull();
                break;
            case Keyword.BZIP2:
                ret = new CopyLegacyOption.Bzip2();
                break;
            case Keyword.CLEANPATH:
                ret = new CopyLegacyOption.CleanPath();
                break;
            case Keyword.COMPUPDATE:
            {
                var preset = ParseKeyword(Keyword.PRESET);
                bool? enabled;
                switch (ParseOneOfKeywords(
                    Keyword.TRUE,
                    Keyword.FALSE,
                    Keyword.ON,
                    Keyword.OFF))
                {
                    case Keyword.TRUE or Keyword.ON:
                        enabled = true;
                        break;
                    case Keyword.FALSE or Keyword.OFF:
                        enabled = false;
                        break;
                    default:
                        enabled = null;
                        break;
                }
                ret = new CopyLegacyOption.CompUpdate(Preset: preset, Enabled: enabled);
                break;
            }
            case Keyword.CREDENTIALS:
                ret = new CopyLegacyOption.Credentials(ParseLiteralString());
                break;
            case Keyword.CSV:
            {
                var opts = new List<CopyLegacyCsvOption>();
                while (MaybeParse(parser => parser.ParseCopyLegacyCsvOption()) is { } opt)
                {
                    opts.Add(opt);
                }
                ret = new CopyLegacyOption.Csv(opts.ToSequence());
                break;
            }
            case Keyword.DATEFORMAT:
            {
                _ = ParseKeyword(Keyword.AS);
                string? fmt;
                if (PeekTokenRef().Token is Token.SingleQuotedString)
                {
                    fmt = ParseLiteralString();
                }
                else
                {
                    fmt = null;
                }
                ret = new CopyLegacyOption.DateFormat(fmt);
                break;
            }
            case Keyword.DELIMITER:
                _ = ParseKeyword(Keyword.AS);
                ret = new CopyLegacyOption.Delimiter(ParseLiteralChar());
                break;
            case Keyword.EMPTYASNULL:
                ret = new CopyLegacyOption.EmptyAsNull();
                break;
            case Keyword.ENCRYPTED:
            {
                var auto = ParseKeyword(Keyword.AUTO);
                ret = new CopyLegacyOption.Encrypted(Auto: auto);
                break;
            }
            case Keyword.ESCAPE:
                ret = new CopyLegacyOption.Escape();
                break;
            case Keyword.EXTENSION:
            {
                var ext = ParseLiteralString();
                ret = new CopyLegacyOption.Extension(ext);
                break;
            }
            case Keyword.FIXEDWIDTH:
            {
                var spec = ParseLiteralString();
                ret = new CopyLegacyOption.FixedWidth(spec);
                break;
            }
            case Keyword.GZIP:
                ret = new CopyLegacyOption.Gzip();
                break;
            case Keyword.HEADER:
                ret = new CopyLegacyOption.Header();
                break;
            case Keyword.IAM_ROLE:
                ret = new CopyLegacyOption.IamRole(ParseIamRoleKind());
                break;
            case Keyword.IGNOREHEADER:
            {
                _ = ParseKeyword(Keyword.AS);
                var numRows = ParseLiteralUint();
                ret = new CopyLegacyOption.IgnoreHeader(numRows);
                break;
            }
            case Keyword.JSON:
            {
                _ = ParseKeyword(Keyword.AS);
                string? fmt;
                if (PeekTokenRef().Token is Token.SingleQuotedString)
                {
                    fmt = ParseLiteralString();
                }
                else
                {
                    fmt = null;
                }
                ret = new CopyLegacyOption.Json(fmt);
                break;
            }
            case Keyword.MANIFEST:
            {
                var verbose = ParseKeyword(Keyword.VERBOSE);
                ret = new CopyLegacyOption.Manifest(Verbose: verbose);
                break;
            }
            case Keyword.MAXFILESIZE:
            {
                _ = ParseKeyword(Keyword.AS);
                var size = ParseNumberValue();
                FileSizeUnit? unit;
                switch (ParseOneOfKeywords(Keyword.MB, Keyword.GB))
                {
                    case Keyword.MB:
                        unit = new FileSizeUnit.MB();
                        break;
                    case Keyword.GB:
                        unit = new FileSizeUnit.GB();
                        break;
                    default:
                        unit = null;
                        break;
                }
                ret = new CopyLegacyOption.MaxFileSize(new FileSize(Size: size, Unit: unit));
                break;
            }
            case Keyword.NULL:
                _ = ParseKeyword(Keyword.AS);
                ret = new CopyLegacyOption.Null(ParseLiteralString());
                break;
            case Keyword.PARALLEL:
            {
                bool? enabled;
                switch (ParseOneOfKeywords(
                    Keyword.TRUE,
                    Keyword.FALSE,
                    Keyword.ON,
                    Keyword.OFF))
                {
                    case Keyword.TRUE or Keyword.ON:
                        enabled = true;
                        break;
                    case Keyword.FALSE or Keyword.OFF:
                        enabled = false;
                        break;
                    default:
                        enabled = null;
                        break;
                }
                ret = new CopyLegacyOption.Parallel(enabled);
                break;
            }
            case Keyword.PARQUET:
                ret = new CopyLegacyOption.Parquet();
                break;
            case Keyword.PARTITION:
            {
                ExpectKeyword(Keyword.BY);
                var columns = ParseParenthesizedColumnList(IsOptional.Mandatory, false);
                var include = ParseKeyword(Keyword.INCLUDE);
                ret = new CopyLegacyOption.PartitionBy(new UnloadPartitionBy(Columns: columns, Include: include));
                break;
            }
            case Keyword.REGION:
            {
                _ = ParseKeyword(Keyword.AS);
                var region = ParseLiteralString();
                ret = new CopyLegacyOption.Region(region);
                break;
            }
            case Keyword.REMOVEQUOTES:
                ret = new CopyLegacyOption.RemoveQuotes();
                break;
            case Keyword.ROWGROUPSIZE:
            {
                _ = ParseKeyword(Keyword.AS);
                var fileSize = ParseFileSize();
                ret = new CopyLegacyOption.RowGroupSize(fileSize);
                break;
            }
            case Keyword.STATUPDATE:
            {
                bool? enabled;
                switch (ParseOneOfKeywords(
                    Keyword.TRUE,
                    Keyword.FALSE,
                    Keyword.ON,
                    Keyword.OFF))
                {
                    case Keyword.TRUE or Keyword.ON:
                        enabled = true;
                        break;
                    case Keyword.FALSE or Keyword.OFF:
                        enabled = false;
                        break;
                    default:
                        enabled = null;
                        break;
                }
                ret = new CopyLegacyOption.StatUpdate(enabled);
                break;
            }
            case Keyword.TIMEFORMAT:
            {
                _ = ParseKeyword(Keyword.AS);
                string? fmt;
                if (PeekTokenRef().Token is Token.SingleQuotedString)
                {
                    fmt = ParseLiteralString();
                }
                else
                {
                    fmt = null;
                }
                ret = new CopyLegacyOption.TimeFormat(fmt);
                break;
            }
            case Keyword.TRUNCATECOLUMNS:
                ret = new CopyLegacyOption.TruncateColumns();
                break;
            case Keyword.ZSTD:
                ret = new CopyLegacyOption.Zstd();
                break;
            default:
                ret = ExpectedRef<CopyLegacyOption>("option", PeekTokenRef());
                break;
        }
        return ret;
    }

    internal FileSize ParseFileSize()
    {
        var size = ParseNumberValue();
        var unit = MaybeParseFileSizeUnit();
        return new FileSize(Size: size, Unit: unit);
    }

    internal FileSizeUnit? MaybeParseFileSizeUnit()
    {
        switch (ParseOneOfKeywords(Keyword.MB, Keyword.GB))
        {
            case Keyword.MB:
                return new FileSizeUnit.MB();
            case Keyword.GB:
                return new FileSizeUnit.GB();
            default:
                return null;
        }
    }

    internal IamRoleKind ParseIamRoleKind()
    {
        if (ParseKeyword(Keyword.DEFAULT))
        {
            return new IamRoleKind.Default();
        }
        else
        {
            var arn = ParseLiteralString();
            return new IamRoleKind.Arn(arn);
        }
    }

    internal CopyLegacyCsvOption ParseCopyLegacyCsvOption()
    {
        CopyLegacyCsvOption ret;
        switch (ParseOneOfKeywords(
            Keyword.HEADER,
            Keyword.QUOTE,
            Keyword.ESCAPE,
            Keyword.FORCE))
        {
            case Keyword.HEADER:
                ret = new CopyLegacyCsvOption.Header();
                break;
            case Keyword.QUOTE:
                _ = ParseKeyword(Keyword.AS); // [ AS ]
                ret = new CopyLegacyCsvOption.Quote(ParseLiteralChar());
                break;
            case Keyword.ESCAPE:
                _ = ParseKeyword(Keyword.AS); // [ AS ]
                ret = new CopyLegacyCsvOption.Escape(ParseLiteralChar());
                break;
            // As guardas consomem tokens, como no Rust: são avaliadas em ordem, só quando o padrão casa.
            case Keyword.FORCE when ParseKeywords(Keyword.NOT, Keyword.NULL):
                ret = new CopyLegacyCsvOption.ForceNotNull(
                    ParseCommaSeparated(p => p.ParseIdentifier()));
                break;
            case Keyword.FORCE when ParseKeywords(Keyword.QUOTE):
                ret = new CopyLegacyCsvOption.ForceQuote(
                    ParseCommaSeparated(p => p.ParseIdentifier()));
                break;
            default:
                ret = ExpectedRef<CopyLegacyCsvOption>("csv option", PeekTokenRef());
                break;
        }
        return ret;
    }

    internal char ParseLiteralChar()
    {
        var s = ParseLiteralString();
        // `s.len()` do Rust conta bytes UTF-8 (convencoes.md §10): um caractere não ASCII tem
        // `len() > 1` e é rejeitado, como no Rust.
        if (System.Text.Encoding.UTF8.GetByteCount(s) != 1)
        {
            // self.tokens.get(self.index - 1).map_or(Location { line: 0, column: 0 }, |t| t.span.start)
            var loc = _index - 1 >= 0 && _index - 1 < _tokens.Count
                ? _tokens[_index - 1].Span.Start
                : new Location(0, 0);
            // `{s:?}` = Debug de `str` (TokenDebugExtensions.DebugStr).
            throw ParserErr($"Expect a char, found {TokenDebugExtensions.DebugStr(s)}", loc);
        }
        // Com um único byte UTF-8, o caractere é ASCII: `s.chars().next().unwrap()` = `s[0]`.
        return s[0];
    }

    /// <summary>
    /// Parse a tab separated values in
    /// COPY payload
    /// </summary>
    public Sequence<string?> ParseTsv() => ParseTabValue();

    /// <summary>Parse a single tab-separated value row used by <c>COPY</c> payload parsing.</summary>
    public Sequence<string?> ParseTabValue()
    {
        var values = new List<string?>();
        var content = new System.Text.StringBuilder();
        // while let Some(t) = self.next_token_no_skip().map(|t| &t.token)
        while (NextTokenNoSkip() is { } tokenWithSpan)
        {
            var t = tokenWithSpan.Token;
            switch (t)
            {
                case Token.Whitespace { Value: Whitespace.Tab }:
                    // core::mem::take(&mut content)
                    values.Add(content.ToString());
                    content.Clear();
                    break;
                case Token.Whitespace { Value: Whitespace.Newline }:
                    values.Add(content.ToString());
                    content.Clear();
                    break;
                case Token.Backslash:
                    if (ConsumeToken(new Token.Period()))
                    {
                        return values.ToSequence();
                    }
                    if (NextToken().Token is Token.Word { Value: var w })
                    {
                        if (w.Value == "N")
                        {
                            values.Add(null);
                        }
                    }
                    break;
                default:
                    content.Append(t.ToString());
                    break;
            }
        }
        return values.ToSequence();
    }

    /// <summary>Parse a literal value (numbers, strings, date/time, booleans)</summary>
    public ValueWithSpan ParseValue()
    {
        var nextToken = NextToken();
        var span = nextToken.Span;
        // let ok_value = |value: Value| Ok(value.with_span(span));
        ValueWithSpan OkValue(Value value) => value.WithSpan(span);
        switch (nextToken.Token)
        {
            case Token.Word { Value: var w }:
                switch (w.Keyword)
                {
                    case Keyword.TRUE when _dialect.SupportsBooleanLiterals:
                        return OkValue(new Value.Boolean(true));
                    case Keyword.FALSE when _dialect.SupportsBooleanLiterals:
                        return OkValue(new Value.Boolean(false));
                    case Keyword.NULL:
                        return OkValue(new Value.Null());
                    case Keyword.NoKeyword when w.QuoteStyle is not null:
                        switch (w.QuoteStyle)
                        {
                            case '"':
                                return OkValue(new Value.DoubleQuotedString(w.Value));
                            case '\'':
                                return OkValue(new Value.SingleQuotedString(w.Value));
                            default:
                                return Expected<ValueWithSpan>(
                                    "A value?",
                                    new TokenWithSpan(
                                        Token: new Token.Word(w),
                                        Span: span));
                        }
                    default:
                        return Expected<ValueWithSpan>(
                            "a concrete value",
                            new TokenWithSpan(
                                Token: new Token.Word(w),
                                Span: span));
                }
            // The call to n.parse() returns a bigdecimal when the
            // bigdecimal feature is enabled, and is otherwise a no-op
            // (i.e., it returns the input string).
            case Token.Number { Value: var n, Long: var l }:
                return OkValue(new Value.Number(Parse<string>(n, span.Start), l));
            case Token.SingleQuotedString { Value: var s }:
                return OkValue(new Value.SingleQuotedString(
                    MaybeConcatStringLiteral(s)));
            case Token.DoubleQuotedString { Value: var s }:
                return OkValue(new Value.DoubleQuotedString(
                    MaybeConcatStringLiteral(s)));
            case Token.TripleSingleQuotedString { Value: var s }:
                return OkValue(new Value.TripleSingleQuotedString(s));
            case Token.TripleDoubleQuotedString { Value: var s }:
                return OkValue(new Value.TripleDoubleQuotedString(s));
            case Token.DollarQuotedString { Value: var s }:
                return OkValue(new Value.DollarQuotedString(s));
            case Token.SingleQuotedByteStringLiteral { Value: var s }:
                return OkValue(new Value.SingleQuotedByteStringLiteral(s));
            case Token.DoubleQuotedByteStringLiteral { Value: var s }:
                return OkValue(new Value.DoubleQuotedByteStringLiteral(s));
            case Token.TripleSingleQuotedByteStringLiteral { Value: var s }:
                return OkValue(new Value.TripleSingleQuotedByteStringLiteral(s));
            case Token.TripleDoubleQuotedByteStringLiteral { Value: var s }:
                return OkValue(new Value.TripleDoubleQuotedByteStringLiteral(s));
            case Token.SingleQuotedRawStringLiteral { Value: var s }:
                return OkValue(new Value.SingleQuotedRawStringLiteral(s));
            case Token.DoubleQuotedRawStringLiteral { Value: var s }:
                return OkValue(new Value.DoubleQuotedRawStringLiteral(s));
            case Token.TripleSingleQuotedRawStringLiteral { Value: var s }:
                return OkValue(new Value.TripleSingleQuotedRawStringLiteral(s));
            case Token.TripleDoubleQuotedRawStringLiteral { Value: var s }:
                return OkValue(new Value.TripleDoubleQuotedRawStringLiteral(s));
            case Token.NationalStringLiteral { Value: var s }:
                return OkValue(new Value.NationalStringLiteral(s));
            case Token.QuoteDelimitedStringLiteral { Value: var v }:
                return OkValue(new Value.QuoteDelimitedStringLiteral(v));
            case Token.NationalQuoteDelimitedStringLiteral { Value: var v }:
                return OkValue(new Value.NationalQuoteDelimitedStringLiteral(v));
            case Token.EscapedStringLiteral { Value: var s }:
                return OkValue(new Value.EscapedStringLiteral(s));
            case Token.UnicodeStringLiteral { Value: var s }:
                return OkValue(new Value.UnicodeStringLiteral(s));
            case Token.HexStringLiteral { Value: var s }:
                return OkValue(new Value.HexStringLiteral(s));
            case Token.Placeholder { Value: var s }:
                return OkValue(new Value.Placeholder(s));
            case Token.Colon or Token.AtSign:
            {
                // `tok @ Token::Colon | tok @ Token::AtSign`
                var tok = nextToken.Token;
                // 1. Not calling self.parse_identifier(false)?
                //    because only in placeholder we want to check
                //    numbers as idfentifies.  This because snowflake
                //    allows numbers as placeholders
                // 2. Not calling self.next_token() to enforce `tok`
                //    be followed immediately by a word/number, ie.
                //    without any whitespace in between
                // Rust: `let next_token` sombreia o externo (caso 72).
                var placeholderToken = NextTokenNoSkip() ?? EofToken;
                Ident ident;
                switch (placeholderToken.Token)
                {
                    case Token.Word { Value: var placeholderWord } when placeholderWord.QuoteStyle is null:
                        ident = placeholderWord.IntoIdent(placeholderToken.Span);
                        break;
                    case Token.Number { Value: var placeholderNumber, Long: false }:
                        ident = Ident.WithSpan(placeholderToken.Span, placeholderNumber);
                        break;
                    default:
                        ident = Expected<Ident>("placeholder", placeholderToken);
                        break;
                }
                return new Value.Placeholder($"{tok}{ident.Value}")
                    .WithSpan(new Span(span.Start, ident.Span.End));
            }
            case var unexpected:
                return Expected<ValueWithSpan>(
                    "a value",
                    new TokenWithSpan(
                        Token: unexpected,
                        Span: span));
        }
    }

    internal string MaybeConcatStringLiteral(string str)
    {
        if (_dialect.SupportsStringLiteralConcatenation)
        {
            // while let Token::SingleQuotedString(ref s) | Token::DoubleQuotedString(ref s) = self.peek_token_ref().token
            // (o C# não liga variáveis em padrões `or`: decisão 5 da B2)
            while (true)
            {
                string s;
                switch (PeekTokenRef().Token)
                {
                    case Token.SingleQuotedString { Value: var singleQuoted }:
                        s = singleQuoted;
                        break;
                    case Token.DoubleQuotedString { Value: var doubleQuoted }:
                        s = doubleQuoted;
                        break;
                    default:
                        return str;
                }
                str += s;
                AdvanceToken();
            }
        }
        else if (_dialect.SupportsStringLiteralConcatenationWithNewline)
        {
            // We are iterating over tokens including whitespaces, to identify
            // string literals separated by newlines so we can concatenate them.
            var afterNewline = false;
            while (true)
            {
                var stop = false;
                switch (PeekTokenNoSkip().Token)
                {
                    case Token.Whitespace { Value: Whitespace.Newline }:
                        afterNewline = true;
                        NextTokenNoSkip();
                        break;
                    case Token.Whitespace:
                        NextTokenNoSkip();
                        break;
                    // Braço `Token::SingleQuotedString(ref s) | Token::DoubleQuotedString(ref s) if after_newline`:
                    // o C# não liga variáveis em padrões `or`, então o corpo aparece nas duas seções.
                    case Token.SingleQuotedString { Value: var s } when afterNewline:
                        str += s;
                        NextTokenNoSkip();
                        afterNewline = false;
                        break;
                    case Token.DoubleQuotedString { Value: var s } when afterNewline:
                        str += s;
                        NextTokenNoSkip();
                        afterNewline = false;
                        break;
                    default:
                        stop = true;
                        break;
                }
                if (stop)
                {
                    break;
                }
            }
        }

        return str;
    }

    /// <summary>Parse an unsigned numeric literal</summary>
    public ValueWithSpan ParseNumberValue()
    {
        var valueWrapper = ParseValue();
        switch (valueWrapper.Value)
        {
            case Value.Number:
                return valueWrapper;
            case Value.Placeholder:
                return valueWrapper;
            default:
                PrevToken();
                return ExpectedRef<ValueWithSpan>("literal number", PeekTokenRef());
        }
    }

    /// <summary>
    /// Parse a numeric literal as an expression. Returns a <see cref="Expr.UnaryOp"/> if the number is signed,
    /// otherwise returns a <see cref="Expr.Value"/>
    /// </summary>
    public Expr ParseNumber()
    {
        var nextToken = NextToken();
        switch (nextToken.Token)
        {
            case Token.Plus:
                return new Expr.UnaryOp(
                    Op: new UnaryOperator.Plus(),
                    Expr: new Expr.Value(ParseNumberValue()));
            case Token.Minus:
                return new Expr.UnaryOp(
                    Op: new UnaryOperator.Minus(),
                    Expr: new Expr.Value(ParseNumberValue()));
            default:
                PrevToken();
                return new Expr.Value(ParseNumberValue());
        }
    }

    internal Expr ParseIntroducedStringExpr()
    {
        var nextToken = NextToken();
        var span = nextToken.Span;
        switch (nextToken.Token)
        {
            case Token.SingleQuotedString { Value: var s }:
                return new Expr.Value(
                    new Value.SingleQuotedString(s).WithSpan(span));
            case Token.DoubleQuotedString { Value: var s }:
                return new Expr.Value(
                    new Value.DoubleQuotedString(s).WithSpan(span));
            case Token.HexStringLiteral { Value: var s }:
                return new Expr.Value(
                    new Value.HexStringLiteral(s).WithSpan(span));
            case var unexpected:
                return Expected<Expr>(
                    "a string value",
                    new TokenWithSpan(
                        Token: unexpected,
                        Span: span));
        }
    }

    /// <summary>Parse an unsigned literal integer/long</summary>
    public ulong ParseLiteralUint()
    {
        var nextToken = NextToken();
        switch (nextToken.Token)
        {
            case Token.Number { Value: var s }:
                return Parse<ulong>(s, nextToken.Span.Start);
            default:
                return Expected<ulong>("literal int", nextToken);
        }
    }

    /// <summary>
    /// Parse the body of a <c>CREATE FUNCTION</c> specified as a string.
    /// e.g. <c>CREATE FUNCTION ... AS $$ body $$</c>.
    /// </summary>
    internal CreateFunctionBody ParseCreateFunctionBodyString()
    {
        // Closure `parse_string_expr` (não captura nada): função local `static` (decisão 9 da B1).
        static Expr ParseStringExpr(Parser parser)
        {
            var peekToken = parser.PeekToken();
            var span = peekToken.Span;
            switch (peekToken.Token)
            {
                case Token.DollarQuotedString { Value: var s } when parser._dialect is PostgreSqlDialect or GenericDialect:
                    parser.NextToken();
                    return new Expr.Value(new Value.DollarQuotedString(s).WithSpan(span));
                default:
                    return new Expr.Value(
                        new Value.SingleQuotedString(parser.ParseLiteralString()).WithSpan(span));
            }
        }

        return new CreateFunctionBody.AsBeforeOptions(
            Body: ParseStringExpr(this),
            LinkSymbol: ConsumeToken(new Token.Comma())
                ? ParseStringExpr(this)
                : null);
    }

    /// <summary>Parse a literal string</summary>
    public string ParseLiteralString()
    {
        var nextToken = NextToken();
        switch (nextToken.Token)
        {
            case Token.Word { Value: { Keyword: Keyword.NoKeyword, Value: var value } }:
                return value;
            case Token.SingleQuotedString { Value: var s }:
                return s;
            case Token.DoubleQuotedString { Value: var s }:
                return s;
            case Token.EscapedStringLiteral { Value: var s } when _dialect is PostgreSqlDialect or GenericDialect:
                return s;
            case Token.UnicodeStringLiteral { Value: var s }:
                return s;
            default:
                return Expected<string>("literal string", nextToken);
        }
    }

    /// <summary>Parse a boolean string</summary>
    internal bool ParseBooleanString()
    {
        switch (ParseOneOfKeywords(Keyword.TRUE, Keyword.FALSE))
        {
            case Keyword.TRUE:
                return true;
            case Keyword.FALSE:
                return false;
            default:
                return ExpectedRef<bool>("TRUE or FALSE", PeekTokenRef());
        }
    }

    /// <summary>Parse the <c>IS [NOT] JSON</c> predicate after <c>JSON</c> (and optional <c>NOT</c>) was consumed.</summary>
    internal Expr ParseIsJsonPredicate(Expr expr, bool negated)
    {
        JsonPredicateType? kind;
        switch (ParseOneOfKeywords(
            Keyword.VALUE,
            Keyword.SCALAR,
            Keyword.ARRAY,
            Keyword.OBJECT))
        {
            case Keyword.VALUE:
                kind = new JsonPredicateType.Value();
                break;
            case Keyword.SCALAR:
                kind = new JsonPredicateType.Scalar();
                break;
            case Keyword.ARRAY:
                kind = new JsonPredicateType.Array();
                break;
            case Keyword.OBJECT:
                kind = new JsonPredicateType.Object();
                break;
            default:
                kind = null;
                break;
        }

        JsonKeyUniqueness? uniqueKeys;
        switch (ParseOneOfKeywords(Keyword.WITH, Keyword.WITHOUT))
        {
            case Keyword.WITH:
                ExpectKeywordIs(Keyword.UNIQUE);
                _ = ParseKeyword(Keyword.KEYS);
                uniqueKeys = new JsonKeyUniqueness.WithUniqueKeys();
                break;
            case Keyword.WITHOUT:
                ExpectKeywordIs(Keyword.UNIQUE);
                _ = ParseKeyword(Keyword.KEYS);
                uniqueKeys = new JsonKeyUniqueness.WithoutUniqueKeys();
                break;
            default:
                uniqueKeys = null;
                break;
        }

        return new Expr.IsJson(
            Expr: expr,
            Kind: kind,
            UniqueKeys: uniqueKeys,
            Negated: negated);
    }

    /// <summary>Parse a literal unicode normalization clause</summary>
    public Expr ParseUnicodeIsNormalized(Expr expr)
    {
        var neg = ParseKeyword(Keyword.NOT);
        var normalizedForm = MaybeParse<NormalizationForm>(parser =>
        {
            switch (parser.ParseOneOfKeywords(
                Keyword.NFC,
                Keyword.NFD,
                Keyword.NFKC,
                Keyword.NFKD))
            {
                case Keyword.NFC:
                    return new NormalizationForm.NFC();
                case Keyword.NFD:
                    return new NormalizationForm.NFD();
                case Keyword.NFKC:
                    return new NormalizationForm.NFKC();
                case Keyword.NFKD:
                    return new NormalizationForm.NFKD();
                default:
                    return parser.ExpectedRef<NormalizationForm>("unicode normalization form", parser.PeekTokenRef());
            }
        });
        if (ParseKeyword(Keyword.NORMALIZED))
        {
            return new Expr.IsNormalized(
                Expr: expr,
                Form: normalizedForm,
                Negated: neg);
        }
        return ExpectedRef<Expr>("unicode normalization form", PeekTokenRef());
    }

    /// <summary>Parse parenthesized enum members, used with <c>ENUM(...)</c> type definitions.</summary>
    public Sequence<EnumMember> ParseEnumValues()
    {
        ExpectToken(new Token.LParen());
        var values = ParseCommaSeparated(parser =>
        {
            var name = parser.ParseLiteralString();
            EnumMember e;
            if (parser.ConsumeToken(new Token.Eq()))
            {
                var value = parser.ParseNumber();
                e = new EnumMember.NamedValue(name, value);
            }
            else
            {
                e = new EnumMember.Name(name);
            }
            return e;
        });
        ExpectToken(new Token.RParen());

        return values;
    }

    /// <summary>Parse a SQL datatype (in the context of a CREATE TABLE statement for example)</summary>
    public DataType ParseDataType()
    {
        var (ty, trailingBracket) = ParseDataTypeHelper();
        if (trailingBracket.Value)
        {
            // parser_err!(MSG, self.peek_token_ref()): o segundo argumento é um token (decisão 4 da B2).
            throw new ParserException(new ParserError.ParserErrorValue(
                $"unmatched > after parsing data type {ty}" + PeekTokenRef().ToString()));
        }

        return ty;
    }

    internal (DataType DataType, MatchedTrailingBracket TrailingBracket) ParseDataTypeHelper()
    {
        // Rust: #[cfg_attr(feature = "recursive-protection", recursive::recursive)] (convencoes.md §13, caso 123).
        if (StackGuard.IsStackLow())
        {
            return StackGuard.Grow(this, static parser => parser.ParseDataTypeHelper());
        }

        using var _guard = _recursionCounter.TryDecrease();

        var dialect = _dialect;
        AdvanceToken();
        var nextToken = GetCurrentToken();
        var nextTokenIndex = GetCurrentIndex();

        // `let optional_precision = self.parse_optional_precision();` guarda o `Result` sem `?`: o erro só
        // é propagado no `optional_precision?`, depois do `parse_keyword(Keyword::UNSIGNED)` (decisão 4 da B1).
        (ulong? Value, ParserException? Error) ParseOptionalPrecisionResult()
        {
            try
            {
                return (ParseOptionalPrecision(), null);
            }
            catch (ParserException e)
            {
                return (null, e);
            }
        }

        // `optional_precision?`
        static ulong? Unwrap((ulong? Value, ParserException? Error) result) =>
            result.Error is { } error ? throw StackGuard.Rethrow(error) : result.Value;

        MatchedTrailingBracket trailingBracket = MatchedTrailingBracket.From(false);
        DataType data;
        switch (nextToken.Token)
        {
            case Token.Word { Value: var w }:
                switch (w.Keyword)
                {
                    case Keyword.BOOLEAN:
                        data = new DataType.Boolean();
                        break;
                    case Keyword.BOOL:
                        data = new DataType.Bool();
                        break;
                    case Keyword.FLOAT:
                    {
                        var precision = ParseExactNumberOptionalPrecisionScale();

                        if (ParseKeyword(Keyword.UNSIGNED))
                        {
                            data = new DataType.FloatUnsigned(precision);
                        }
                        else
                        {
                            data = new DataType.Float(precision);
                        }
                        break;
                    }
                    case Keyword.REAL:
                        if (ParseKeyword(Keyword.UNSIGNED))
                        {
                            data = new DataType.RealUnsigned();
                        }
                        else
                        {
                            data = new DataType.Real();
                        }
                        break;
                    case Keyword.FLOAT4:
                        data = new DataType.Float4();
                        break;
                    case Keyword.FLOAT32:
                        data = new DataType.Float32();
                        break;
                    case Keyword.FLOAT64:
                        data = new DataType.Float64();
                        break;
                    case Keyword.FLOAT8:
                        data = new DataType.Float8();
                        break;
                    case Keyword.DOUBLE:
                        if (ParseKeyword(Keyword.PRECISION))
                        {
                            if (ParseKeyword(Keyword.UNSIGNED))
                            {
                                data = new DataType.DoublePrecisionUnsigned();
                            }
                            else
                            {
                                data = new DataType.DoublePrecision();
                            }
                        }
                        else
                        {
                            var precision = ParseExactNumberOptionalPrecisionScale();

                            if (ParseKeyword(Keyword.UNSIGNED))
                            {
                                data = new DataType.DoubleUnsigned(precision);
                            }
                            else
                            {
                                data = new DataType.Double(precision);
                            }
                        }
                        break;
                    case Keyword.TINYINT:
                    {
                        var optionalPrecision = ParseOptionalPrecisionResult();
                        if (ParseKeyword(Keyword.UNSIGNED))
                        {
                            data = new DataType.TinyIntUnsigned(Unwrap(optionalPrecision));
                        }
                        else
                        {
                            if (dialect.SupportsDataTypeSignedSuffix)
                            {
                                _ = ParseKeyword(Keyword.SIGNED);
                            }
                            data = new DataType.TinyInt(Unwrap(optionalPrecision));
                        }
                        break;
                    }
                    case Keyword.INT2:
                    {
                        var optionalPrecision = ParseOptionalPrecisionResult();
                        if (ParseKeyword(Keyword.UNSIGNED))
                        {
                            data = new DataType.Int2Unsigned(Unwrap(optionalPrecision));
                        }
                        else
                        {
                            data = new DataType.Int2(Unwrap(optionalPrecision));
                        }
                        break;
                    }
                    case Keyword.SMALLINT:
                    {
                        var optionalPrecision = ParseOptionalPrecisionResult();
                        if (ParseKeyword(Keyword.UNSIGNED))
                        {
                            data = new DataType.SmallIntUnsigned(Unwrap(optionalPrecision));
                        }
                        else
                        {
                            if (dialect.SupportsDataTypeSignedSuffix)
                            {
                                _ = ParseKeyword(Keyword.SIGNED);
                            }
                            data = new DataType.SmallInt(Unwrap(optionalPrecision));
                        }
                        break;
                    }
                    case Keyword.MEDIUMINT:
                    {
                        var optionalPrecision = ParseOptionalPrecisionResult();
                        if (ParseKeyword(Keyword.UNSIGNED))
                        {
                            data = new DataType.MediumIntUnsigned(Unwrap(optionalPrecision));
                        }
                        else
                        {
                            if (dialect.SupportsDataTypeSignedSuffix)
                            {
                                _ = ParseKeyword(Keyword.SIGNED);
                            }
                            data = new DataType.MediumInt(Unwrap(optionalPrecision));
                        }
                        break;
                    }
                    case Keyword.INT:
                    {
                        var optionalPrecision = ParseOptionalPrecisionResult();
                        if (ParseKeyword(Keyword.UNSIGNED))
                        {
                            data = new DataType.IntUnsigned(Unwrap(optionalPrecision));
                        }
                        else
                        {
                            if (dialect.SupportsDataTypeSignedSuffix)
                            {
                                _ = ParseKeyword(Keyword.SIGNED);
                            }
                            data = new DataType.Int(Unwrap(optionalPrecision));
                        }
                        break;
                    }
                    case Keyword.INT4:
                    {
                        var optionalPrecision = ParseOptionalPrecisionResult();
                        if (ParseKeyword(Keyword.UNSIGNED))
                        {
                            data = new DataType.Int4Unsigned(Unwrap(optionalPrecision));
                        }
                        else
                        {
                            data = new DataType.Int4(Unwrap(optionalPrecision));
                        }
                        break;
                    }
                    case Keyword.INT8:
                    {
                        var optionalPrecision = ParseOptionalPrecisionResult();
                        if (ParseKeyword(Keyword.UNSIGNED))
                        {
                            data = new DataType.Int8Unsigned(Unwrap(optionalPrecision));
                        }
                        else
                        {
                            data = new DataType.Int8(Unwrap(optionalPrecision));
                        }
                        break;
                    }
                    case Keyword.INT16:
                        data = new DataType.Int16();
                        break;
                    case Keyword.INT32:
                        data = new DataType.Int32();
                        break;
                    case Keyword.INT64:
                        data = new DataType.Int64();
                        break;
                    case Keyword.INT128:
                        data = new DataType.Int128();
                        break;
                    case Keyword.INT256:
                        data = new DataType.Int256();
                        break;
                    case Keyword.INTEGER:
                    {
                        var optionalPrecision = ParseOptionalPrecisionResult();
                        if (ParseKeyword(Keyword.UNSIGNED))
                        {
                            data = new DataType.IntegerUnsigned(Unwrap(optionalPrecision));
                        }
                        else
                        {
                            if (dialect.SupportsDataTypeSignedSuffix)
                            {
                                _ = ParseKeyword(Keyword.SIGNED);
                            }
                            data = new DataType.Integer(Unwrap(optionalPrecision));
                        }
                        break;
                    }
                    case Keyword.BIGINT:
                    {
                        var optionalPrecision = ParseOptionalPrecisionResult();
                        if (ParseKeyword(Keyword.UNSIGNED))
                        {
                            data = new DataType.BigIntUnsigned(Unwrap(optionalPrecision));
                        }
                        else
                        {
                            if (dialect.SupportsDataTypeSignedSuffix)
                            {
                                _ = ParseKeyword(Keyword.SIGNED);
                            }
                            data = new DataType.BigInt(Unwrap(optionalPrecision));
                        }
                        break;
                    }
                    case Keyword.HUGEINT:
                        data = new DataType.HugeInt();
                        break;
                    case Keyword.UBIGINT:
                        data = new DataType.UBigInt();
                        break;
                    case Keyword.UHUGEINT:
                        data = new DataType.UHugeInt();
                        break;
                    case Keyword.USMALLINT:
                        data = new DataType.USmallInt();
                        break;
                    case Keyword.UTINYINT:
                        data = new DataType.UTinyInt();
                        break;
                    case Keyword.UINT8:
                        data = new DataType.UInt8();
                        break;
                    case Keyword.UINT16:
                        data = new DataType.UInt16();
                        break;
                    case Keyword.UINT32:
                        data = new DataType.UInt32();
                        break;
                    case Keyword.UINT64:
                        data = new DataType.UInt64();
                        break;
                    case Keyword.UINT128:
                        data = new DataType.UInt128();
                        break;
                    case Keyword.UINT256:
                        data = new DataType.UInt256();
                        break;
                    case Keyword.VARCHAR:
                        data = new DataType.Varchar(ParseOptionalCharacterLength());
                        break;
                    case Keyword.NVARCHAR:
                        data = new DataType.Nvarchar(ParseOptionalCharacterLength());
                        break;
                    case Keyword.CHARACTER:
                        if (ParseKeyword(Keyword.VARYING))
                        {
                            data = new DataType.CharacterVarying(
                                ParseOptionalCharacterLength());
                        }
                        else if (ParseKeywords(Keyword.LARGE, Keyword.OBJECT))
                        {
                            data = new DataType.CharacterLargeObject(
                                ParseOptionalPrecision());
                        }
                        else
                        {
                            data = new DataType.Character(ParseOptionalCharacterLength());
                        }
                        break;
                    case Keyword.CHAR:
                        if (ParseKeyword(Keyword.VARYING))
                        {
                            data = new DataType.CharVarying(
                                ParseOptionalCharacterLength());
                        }
                        else if (ParseKeywords(Keyword.LARGE, Keyword.OBJECT))
                        {
                            data = new DataType.CharLargeObject(ParseOptionalPrecision());
                        }
                        else
                        {
                            data = new DataType.Char(ParseOptionalCharacterLength());
                        }
                        break;
                    case Keyword.CLOB:
                        data = new DataType.Clob(ParseOptionalPrecision());
                        break;
                    case Keyword.BINARY:
                        data = new DataType.Binary(ParseOptionalPrecision());
                        break;
                    case Keyword.VARBINARY:
                        data = new DataType.Varbinary(ParseOptionalBinaryLength());
                        break;
                    case Keyword.BLOB:
                        data = new DataType.Blob(ParseOptionalPrecision());
                        break;
                    case Keyword.TINYBLOB:
                        data = new DataType.TinyBlob();
                        break;
                    case Keyword.MEDIUMBLOB:
                        data = new DataType.MediumBlob();
                        break;
                    case Keyword.LONGBLOB:
                        data = new DataType.LongBlob();
                        break;
                    case Keyword.LONG when _dialect.SupportsLongTypeAsBigint:
                        data = new DataType.BigInt(null);
                        break;
                    case Keyword.BYTES:
                        data = new DataType.Bytes(ParseOptionalPrecision());
                        break;
                    case Keyword.BIT:
                        if (ParseKeyword(Keyword.VARYING))
                        {
                            data = new DataType.BitVarying(ParseOptionalPrecision());
                        }
                        else
                        {
                            data = new DataType.Bit(ParseOptionalPrecision());
                        }
                        break;
                    case Keyword.VARBIT:
                        data = new DataType.VarBit(ParseOptionalPrecision());
                        break;
                    case Keyword.UUID:
                        data = new DataType.Uuid();
                        break;
                    case Keyword.DATE:
                        data = new DataType.Date();
                        break;
                    case Keyword.DATE32:
                        data = new DataType.Date32();
                        break;
                    case Keyword.DATETIME:
                        data = new DataType.Datetime(ParseOptionalPrecision());
                        break;
                    case Keyword.DATETIME64:
                    {
                        PrevToken();
                        var (precision, timeZone) = ParseDatetime64();
                        data = new DataType.Datetime64(precision, timeZone);
                        break;
                    }
                    case Keyword.TIMESTAMP:
                    {
                        var precision = ParseOptionalPrecision();
                        TimezoneInfo tz;
                        if (ParseKeyword(Keyword.WITH))
                        {
                            ExpectKeywords(Keyword.TIME, Keyword.ZONE);
                            tz = new TimezoneInfo.WithTimeZone();
                        }
                        else if (ParseKeyword(Keyword.WITHOUT))
                        {
                            ExpectKeywords(Keyword.TIME, Keyword.ZONE);
                            tz = new TimezoneInfo.WithoutTimeZone();
                        }
                        else
                        {
                            tz = new TimezoneInfo.None();
                        }
                        data = new DataType.Timestamp(precision, tz);
                        break;
                    }
                    case Keyword.TIMESTAMPTZ:
                        data = new DataType.Timestamp(
                            ParseOptionalPrecision(),
                            new TimezoneInfo.Tz());
                        break;
                    case Keyword.TIMESTAMP_NTZ:
                        data = new DataType.TimestampNtz(ParseOptionalPrecision());
                        break;
                    case Keyword.TIME:
                    {
                        var precision = ParseOptionalPrecision();
                        TimezoneInfo tz;
                        if (ParseKeyword(Keyword.WITH))
                        {
                            ExpectKeywords(Keyword.TIME, Keyword.ZONE);
                            tz = new TimezoneInfo.WithTimeZone();
                        }
                        else if (ParseKeyword(Keyword.WITHOUT))
                        {
                            ExpectKeywords(Keyword.TIME, Keyword.ZONE);
                            tz = new TimezoneInfo.WithoutTimeZone();
                        }
                        else
                        {
                            tz = new TimezoneInfo.None();
                        }
                        data = new DataType.Time(precision, tz);
                        break;
                    }
                    case Keyword.TIMETZ:
                        data = new DataType.Time(
                            ParseOptionalPrecision(),
                            new TimezoneInfo.Tz());
                        break;
                    case Keyword.INTERVAL:
                        if (_dialect.SupportsIntervalOptions)
                        {
                            var fields = MaybeParseOptionalIntervalFields();
                            var precision = ParseOptionalPrecision();
                            data = new DataType.Interval(Fields: fields, Precision: precision);
                        }
                        else
                        {
                            data = new DataType.Interval(
                                Fields: null,
                                Precision: null);
                        }
                        break;
                    case Keyword.JSON:
                        data = new DataType.JSON();
                        break;
                    case Keyword.JSONB:
                        data = new DataType.JSONB();
                        break;
                    case Keyword.REGCLASS:
                        data = new DataType.Regclass();
                        break;
                    case Keyword.STRING:
                        data = new DataType.String(ParseOptionalPrecision());
                        break;
                    case Keyword.FIXEDSTRING:
                    {
                        ExpectToken(new Token.LParen());
                        var characterLength = ParseLiteralUint();
                        ExpectToken(new Token.RParen());
                        data = new DataType.FixedString(characterLength);
                        break;
                    }
                    case Keyword.TEXT:
                        if (ParseOptionalTypeModifiers() is { } textModifiers)
                        {
                            data = new DataType.Custom(
                                ObjectName.From(Sequence.Of(new Ident("TEXT"))),
                                textModifiers);
                        }
                        else
                        {
                            data = new DataType.Text();
                        }
                        break;
                    case Keyword.TINYTEXT:
                        data = new DataType.TinyText();
                        break;
                    case Keyword.MEDIUMTEXT:
                        data = new DataType.MediumText();
                        break;
                    case Keyword.LONGTEXT:
                        data = new DataType.LongText();
                        break;
                    case Keyword.BYTEA:
                        data = new DataType.Bytea();
                        break;
                    case Keyword.NUMERIC:
                        data = new DataType.Numeric(
                            ParseExactNumberOptionalPrecisionScale());
                        break;
                    case Keyword.DECIMAL:
                    {
                        var precision = ParseExactNumberOptionalPrecisionScale();

                        if (ParseKeyword(Keyword.UNSIGNED))
                        {
                            data = new DataType.DecimalUnsigned(precision);
                        }
                        else
                        {
                            data = new DataType.Decimal(precision);
                        }
                        break;
                    }
                    case Keyword.DEC:
                    {
                        var precision = ParseExactNumberOptionalPrecisionScale();

                        if (ParseKeyword(Keyword.UNSIGNED))
                        {
                            data = new DataType.DecUnsigned(precision);
                        }
                        else
                        {
                            data = new DataType.Dec(precision);
                        }
                        break;
                    }
                    case Keyword.BIGNUMERIC:
                        data = new DataType.BigNumeric(
                            ParseExactNumberOptionalPrecisionScale());
                        break;
                    case Keyword.BIGDECIMAL:
                        data = new DataType.BigDecimal(
                            ParseExactNumberOptionalPrecisionScale());
                        break;
                    case Keyword.ENUM:
                        data = new DataType.Enum(ParseEnumValues(), null);
                        break;
                    case Keyword.ENUM8:
                        data = new DataType.Enum(ParseEnumValues(), 8);
                        break;
                    case Keyword.ENUM16:
                        data = new DataType.Enum(ParseEnumValues(), 16);
                        break;
                    case Keyword.SET:
                        data = new DataType.Set(ParseStringValues());
                        break;
                    case Keyword.ARRAY:
                        if (_dialect.SupportsArrayTypedefWithParentheses)
                        {
                            if (PeekTokenRef().Token is Token.LParen)
                            {
                                ExpectToken(new Token.LParen());
                                var internalType = ParseDataType();
                                var notNull = _dialect.SupportsArrayElementNotNull
                                    && ParseKeywords(Keyword.NOT, Keyword.NULL);
                                ExpectToken(new Token.RParen());

                                if (notNull)
                                {
                                    data = new DataType.Array(new ArrayElemTypeDef.ParenthesisNotNull(
                                        internalType));
                                }
                                else
                                {
                                    data = new DataType.Array(new ArrayElemTypeDef.Parenthesis(
                                        internalType));
                                }
                            }
                            else if (_dialect.SupportsArrayTypedefWithoutElementType)
                            {
                                data = new DataType.Array(new ArrayElemTypeDef.None());
                            }
                            else
                            {
                                data = Expected<DataType>("(", PeekToken());
                            }
                        }
                        else if (_dialect.SupportsArrayTypedefWithoutElementType)
                        {
                            data = new DataType.Array(new ArrayElemTypeDef.None());
                        }
                        else
                        {
                            ExpectToken(new Token.Lt());
                            // Rust: `_trailing_bracket` (caso 72: nome distinto do `trailing_bracket` externo).
                            var (insideType, insideTrailingBracket) =
                                ParseDataTypeWithOptionalCollation();
                            trailingBracket = ExpectClosingAngleBracket(insideTrailingBracket);
                            data = new DataType.Array(new ArrayElemTypeDef.AngleBracket(
                                insideType));
                        }
                        break;
                    case Keyword.OBJECT when PeekTokenRef().Token is Token.LParen:
                    {
                        if (MaybeParseValue(parser => parser.ParseStructuredObjectTypeDef()) is { } fields)
                        {
                            data = new DataType.Object(fields);
                        }
                        else
                        {
                            PrevToken();
                            var typeName = ParseObjectName(false);
                            var modifiers = ParseOptionalTypeModifiers() ?? Sequence<string>.Empty;
                            data = new DataType.Custom(typeName, modifiers);
                        }
                        break;
                    }
                    case Keyword.STRUCT when dialect is DuckDbDialect:
                    {
                        PrevToken();
                        var fieldDefs = ParseDuckdbStructTypeDef();
                        data = new DataType.Struct(fieldDefs, StructBracketKind.Parentheses);
                        break;
                    }
                    case Keyword.STRUCT when _dialect.SupportsStructLiteral:
                    {
                        PrevToken();
                        var (fieldDefs, structTrailingBracket) =
                            ParseStructTypeDef(p => p.ParseStructFieldDef());
                        trailingBracket = structTrailingBracket;
                        data = new DataType.Struct(
                            fieldDefs,
                            StructBracketKind.AngleBrackets);
                        break;
                    }
                    case Keyword.UNION when dialect is DuckDbDialect or GenericDialect:
                    {
                        PrevToken();
                        var fields = ParseUnionTypeDef();
                        data = new DataType.Union(fields);
                        break;
                    }
                    case Keyword.NULLABLE when dialect is ClickHouseDialect or GenericDialect:
                        data = ParseSubType(t => new DataType.Nullable(t));
                        break;
                    case Keyword.LOWCARDINALITY when dialect is ClickHouseDialect or GenericDialect:
                        data = ParseSubType(t => new DataType.LowCardinality(t));
                        break;
                    case Keyword.MAP when _dialect.SupportsMapLiteralWithAngleBrackets:
                    {
                        ExpectToken(new Token.Lt());
                        var (keyDataType, keyTrailingBracket) =
                            ParseDataTypeWithOptionalCollation();
                        if (keyTrailingBracket.Value)
                        {
                            // return parser_err!(MSG, self.peek_token_ref()): o segundo argumento é um
                            // token (caso 88).
                            throw new ParserException(new ParserError.ParserErrorValue(
                                $"unmatched > after parsing data type {keyDataType}" + PeekTokenRef().ToString()));
                        }
                        ExpectToken(new Token.Comma());
                        var (valueDataType, valueTrailingBracket) =
                            ParseDataTypeWithOptionalCollation();
                        trailingBracket = ExpectClosingAngleBracket(valueTrailingBracket);
                        data = new DataType.Map(
                            keyDataType,
                            valueDataType,
                            MapBracketKind.AngleBrackets);
                        break;
                    }
                    case Keyword.MAP when _dialect.SupportsMapTypedefWithParentheses:
                    {
                        PrevToken();
                        var (keyDataType, valueDataType, valueNotNull) =
                            ParseParenthesizedMapTypeDef();
                        var bracket = valueNotNull
                            ? MapBracketKind.ParenthesesNotNull
                            : MapBracketKind.Parentheses;
                        data = new DataType.Map(
                            keyDataType,
                            valueDataType,
                            bracket);
                        break;
                    }
                    case Keyword.NESTED when dialect is ClickHouseDialect or GenericDialect:
                    {
                        ExpectToken(new Token.LParen());
                        var fieldDefs = ParseCommaSeparated(p => p.ParseColumnDef());
                        ExpectToken(new Token.RParen());
                        data = new DataType.Nested(fieldDefs);
                        break;
                    }
                    case Keyword.TUPLE when dialect is ClickHouseDialect or GenericDialect:
                    {
                        PrevToken();
                        var fieldDefs = ParseClickHouseTupleDef();
                        data = new DataType.Tuple(fieldDefs);
                        break;
                    }
                    case Keyword.TRIGGER:
                        data = new DataType.Trigger();
                        break;
                    case Keyword.ANY when PeekKeyword(Keyword.TYPE):
                        _ = ParseKeyword(Keyword.TYPE);
                        data = new DataType.AnyType();
                        break;
                    case Keyword.TABLE:
                        // an LParen after the TABLE keyword indicates that table columns are being defined
                        // whereas no LParen indicates an anonymous table expression will be returned
                        if (PeekTokenRef().Token is Token.LParen)
                        {
                            var columns = ParseReturnsTableColumns();
                            data = new DataType.Table(columns);
                        }
                        else
                        {
                            data = new DataType.Table(null);
                        }
                        break;
                    case Keyword.SIGNED:
                        if (ParseKeyword(Keyword.INTEGER))
                        {
                            data = new DataType.SignedInteger();
                        }
                        else
                        {
                            data = new DataType.Signed();
                        }
                        break;
                    case Keyword.UNSIGNED:
                        if (ParseKeyword(Keyword.INTEGER))
                        {
                            data = new DataType.UnsignedInteger();
                        }
                        else
                        {
                            data = new DataType.Unsigned();
                        }
                        break;
                    case Keyword.TSVECTOR when dialect is PostgreSqlDialect or GenericDialect:
                        data = new DataType.TsVector();
                        break;
                    case Keyword.TSQUERY when dialect is PostgreSqlDialect or GenericDialect:
                        data = new DataType.TsQuery();
                        break;
                    default:
                    {
                        PrevToken();
                        var typeName = ParseObjectName(false);
                        if (ParseOptionalTypeModifiers() is { } modifiers)
                        {
                            data = new DataType.Custom(typeName, modifiers);
                        }
                        else
                        {
                            data = new DataType.Custom(typeName, Sequence<string>.Empty);
                        }
                        break;
                    }
                }
                break;
            default:
                data = ExpectedAt<DataType>("a data type name", nextTokenIndex);
                break;
        }

        if (_dialect.SupportsArrayTypedefWithBrackets)
        {
            while (ConsumeToken(new Token.LBracket()))
            {
                // Parse optional array data type size
                var size = MaybeParseValue(p => p.ParseLiteralUint());
                ExpectToken(new Token.RBracket());
                data = new DataType.Array(new ArrayElemTypeDef.SquareBracket(data, size));
            }
        }

        // Type-qualified array, e.g. `INT ARRAY` or `INT ARRAY[3]`. One-dimensional
        // with a single optional size. The `ARRAY` keyword is unambiguous everywhere.
        if (ParseKeyword(Keyword.ARRAY))
        {
            ulong? size;
            if (ConsumeToken(new Token.LBracket()))
            {
                // Rust: `let size` interno sombreia o externo (caso 72).
                var qualifiedSize = MaybeParseValue(p => p.ParseLiteralUint());
                ExpectToken(new Token.RBracket());
                size = qualifiedSize;
            }
            else
            {
                size = null;
            }
            data = new DataType.Array(new ArrayElemTypeDef.Qualified(data, size));
        }

        return (data, trailingBracket);
    }

    internal (DataType DataType, MatchedTrailingBracket TrailingBracket) ParseDataTypeWithOptionalCollation()
    {
        var (dataType, trailingBracket) = ParseDataTypeHelper();
        if (!trailingBracket.Value
            && _dialect.SupportsDataTypeCollation
            && ParseKeyword(Keyword.COLLATE))
        {
            dataType = new DataType.Collate(dataType, ParseObjectName(false));
        }
        return (dataType, trailingBracket);
    }
}
