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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/generic.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Dialects;

/// <summary>
/// A permissive, general purpose <see cref="Dialect"/>, which parses a wide variety of SQL
/// statements, from many different dialects.
/// </summary>
public class GenericDialect : Dialect
{
    public override bool SupportsMapTypedefWithParentheses => true;

    public override bool IsDelimitedIdentifierStart(char ch) => ch == '"' || ch == '`';

    public override bool IsIdentifierStart(char ch) => char.IsLetter(ch) || ch == '_' || ch == '#' || ch == '@';

    public override bool IsIdentifierPart(char ch) =>
        char.IsLetter(ch)
            || (ch >= '0' && ch <= '9')
            || ch == '@'
            || ch == '$'
            || ch == '#'
            || ch == '_';

    public override bool SupportsUnicodeStringLiteral => true;

    public override bool SupportsPartitionByAfterOrderBy => true;

    public override bool SupportsArrayJoinSyntax => true;

    public override bool SupportsGroupByExpr => true;

    public override bool SupportsGroupByWithModifier => true;

    public override bool SupportsLeftAssociativeJoinsWithoutParens => true;

    public override bool SupportsConnectBy => true;

    public override bool SupportsMatchRecognize => true;

    public override bool SupportsPipeOperator => true;

    public override bool SupportsStartTransactionModifier => true;

    public override bool SupportsWindowFunctionNullTreatmentArg => true;

    public override bool SupportsDictionarySyntax => true;

    public override bool SupportsWindowClauseNamedWindowReference => true;

    public override bool SupportsParenthesizedSetVariables => true;

    public override bool SupportsSelectWildcardExcept => true;

    public override bool SupportMapLiteralSyntax => true;

    public override bool AllowExtractCustom => true;

    public override bool AllowExtractSingleQuotes => true;

    public override bool SupportsExtractCommaSyntax => true;

    public override bool SupportsCreateViewCommentSyntax => true;

    public override bool SupportsParensAroundTableFactor => true;

    public override bool SupportsValuesAsTableFactor => true;

    public override bool SupportsCreateIndexWithClause => true;

    public override bool SupportsExplainWithUtilityOptions => true;

    public override bool SupportsExcludeConstraint => true;

    public override bool SupportsLimitComma => true;

    public override bool SupportsUpdateOrderBy => true;

    public override bool SupportsFromFirstSelect => true;

    public override bool SupportsProjectionTrailingCommas => true;

    public override bool SupportsAscDescInColumnDefinition => true;

    public override bool SupportsTryConvert => true;

    public override bool SupportsBitwiseShiftOperators => true;

    public override bool SupportsCommentOn => true;

    public override bool SupportsLoadExtension => true;

    public override bool SupportsNamedFnArgsWithAssignmentOperator => true;

    public override bool SupportsStructLiteral => true;

    public override bool SupportsEmptyProjections => true;

    public override bool SupportsNestedComments => true;

    public override bool SupportsMultilineCommentHints => true;

    public override bool SupportsUserHostGrantee => true;

    public override bool SupportsStringEscapeConstant => true;

    public override bool SupportsArrayTypedefWithBrackets => true;

    public override bool SupportsMatchAgainst => true;

    public override bool SupportsSetNames => true;

    public override bool SupportsCommaSeparatedSetAssignments => true;

    public override bool SupportsFilterDuringAggregation => true;

    public override bool SupportsSelectWildcardExclude => true;

    public override bool SupportsDataTypeSignedSuffix => true;

    public override bool SupportsIntervalOptions => true;

    public override bool SupportsQuoteDelimitedString => true;

    public override bool SupportsSelectWildcardReplace => true;

    public override bool SupportsSelectWildcardIlike => true;

    public override bool SupportsSelectWildcardRename => true;

    public override bool SupportsOptimizeTable => true;

    public override bool SupportsInstall => true;

    public override bool SupportsDetach => true;

    public override bool SupportsPrewhere => true;

    public override bool SupportsWithFill => true;

    public override bool SupportsLimitBy => true;

    public override bool SupportsInterpolate => true;

    public override bool SupportsSettings => true;

    public override bool SupportsTablePartitions => true;

    public override bool SupportsSelectFormat => true;

    public override bool SupportsCommentOptimizerHint => true;

    public override bool SupportsConstraintKeywordWithoutName => true;

    public override bool SupportsKeyColumnOption => true;

    public override bool SupportsCommaSeparatedTrim => true;

    public override bool SupportsCteWithoutAs => true;

    public override bool SupportsSelectItemMultiColumnAlias => true;

    public override bool SupportsXmlExpressions => true;

    public override bool SupportsAliasedFunctionArgs => true;

    public override bool SupportsAlterColumnPosition => true;
}
