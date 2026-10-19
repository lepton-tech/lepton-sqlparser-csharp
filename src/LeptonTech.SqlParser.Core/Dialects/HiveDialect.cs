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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/hive.rs.
// Modificado: tradução de Rust para C#.

namespace LeptonTech.SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://hive.apache.org/">Hive</see>.
/// </summary>
public class HiveDialect : Dialect
{
    public override bool IsDelimitedIdentifierStart(char ch) => (ch == '"') || (ch == '`');

    /// <summary>
    /// See <see href="https://cwiki.apache.org/confluence/display/Hive/LanguageManual+DDL#LanguageManualDDL-RulesforColumnNames"/>
    /// </summary>
    public override char? IdentifierQuoteStyle(string identifier) => '`';

    public override bool IsIdentifierStart(char ch) => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '$';

    public override bool IsIdentifierPart(char ch) =>
        (ch >= 'a' && ch <= 'z')
            || (ch >= 'A' && ch <= 'Z')
            || (ch >= '0' && ch <= '9')
            || ch == '_'
            || ch == '$'
            || ch == '{'
            || ch == '}';

    public override bool SupportsFilterDuringAggregation => true;

    public override bool SupportsNumericPrefix => true;

    public override bool RequireIntervalQualifier => true;

    /// <summary>
    /// See <see href="https://cwiki.apache.org/confluence/pages/viewpage.action?pageId=27362061#Tutorial-BuiltInOperators"/>
    /// </summary>
    public override bool SupportsBangNotOperator => true;

    /// <summary>
    /// See <see href="https://cwiki.apache.org/confluence/pages/viewpage.action?pageId=27362036#LanguageManualDML-Loadingfilesintotables"/>
    /// </summary>
    public override bool SupportsLoadData => true;

    /// <summary>
    /// See <see href="https://cwiki.apache.org/confluence/display/hive/languagemanual+sampling"/>
    /// </summary>
    public override bool SupportsTableSampleBeforeAlias => true;

    /// <summary>
    /// See <see href="https://cwiki.apache.org/confluence/pages/viewpage.action?pageId=30151323#EnhancedAggregation,Cube,GroupingandRollup-CubesandRollupsr"/>
    /// </summary>
    public override bool SupportsGroupByWithModifier => true;

    /// <summary>
    /// See <see href="https://hive.apache.org/docs/latest/language/common-table-expression/"/>
    /// </summary>
    // TODO: The parsing of the FROM keyword seems wrong, as it happens within the CTE.
    // See https://github.com/apache/datafusion-sqlparser-rs/issues/2236 for more details.
    public override bool SupportsFromFirstInsert => true;

    /// <summary>
    /// See <see href="https://hive.apache.org/docs/latest/language/languagemanual-types/"/>
    /// </summary>
    public override bool SupportsMapLiteralWithAngleBrackets => true;
}
