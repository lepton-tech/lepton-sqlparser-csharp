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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/mod.rs.
// Modificado: tradução de Rust para C#.

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

// Rust: impl fmt::Display for Statement (mod.rs 5098–6521). O match inteiro vira UM switch, na ordem
// dos braços do Rust (que não é a ordem das variantes do enum).
public abstract partial record Statement : IDisplay
{
    /// <summary>
    /// Formats a SQL statement with support for pretty printing.
    /// <para>
    /// When using the alternate flag (<c>{:#}</c>), the statement will be formatted with proper
    /// indentation and line breaks. For example:
    /// </para>
    /// <code>
    /// # use sqlparser::dialect::GenericDialect;
    /// # use sqlparser::parser::Parser;
    /// let sql = "SELECT a, b FROM table_1";
    /// let ast = Parser::parse_sql(&amp;GenericDialect, sql).unwrap();
    ///
    /// // Regular formatting
    /// assert_eq!(format!("{}", ast[0]), "SELECT a, b FROM table_1");
    ///
    /// // Pretty printing
    /// assert_eq!(format!("{:#}", ast[0]),
    /// r#"SELECT
    ///   a,
    ///   b
    /// FROM
    ///   table_1"#);
    /// </code>
    /// </summary>
    // Clippy thinks this function is too complicated, but it is painful to
    // split up without extracting structs for each `Statement` variant.
    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Flush flush:
            {
                f.Write($"FLUSH");
                if (flush.Location is { } location)
                {
                    f.WriteStr(" ");
                    location.Fmt(f);
                }
                f.Write($" {flush.ObjectType}");

                if (flush.Channel is { } channel)
                {
                    f.Write($" FOR CHANNEL {channel}");
                }

                // write!(f, "{tables}{read}{export}", tables = format!(" {}", ..) ou String::new(), ..):
                // a lacuna `tables` é escrita com flags novos, como o format! do Rust.
                if (!flush.Tables.IsEmpty)
                {
                    f.Write($" {DisplayCommaSeparated(flush.Tables)}");
                }
                f.Write($"{(flush.ReadLock ? " WITH READ LOCK" : "")}{(flush.Export ? " FOR EXPORT" : "")}");
                break;
            }
            case Kill kill:
            {
                f.Write($"KILL ");

                if (kill.Modifier is { } m)
                {
                    f.Write($"{m} ");
                }

                f.Write($"{kill.Id}");
                break;
            }
            case ExplainTable explainTable:
            {
                f.Write($"{explainTable.DescribeAlias} ");

                if (explainTable.HiveFormat is { } format)
                {
                    f.Write($"{format} ");
                }
                if (explainTable.HasTableKeyword)
                {
                    f.Write($"TABLE ");
                }

                f.Write($"{explainTable.TableName}");
                break;
            }
            case Explain explain:
            {
                f.Write($"{explain.DescribeAlias} ");

                if (explain.QueryPlan)
                {
                    f.Write($"QUERY PLAN ");
                }
                if (explain.Analyze)
                {
                    f.Write($"ANALYZE ");
                }
                if (explain.Estimate)
                {
                    f.Write($"ESTIMATE ");
                }

                if (explain.Verbose)
                {
                    f.Write($"VERBOSE ");
                }

                if (explain.Format is { } format)
                {
                    f.Write($"{format} ");
                }

                if (explain.Options is { } options)
                {
                    f.Write($"({DisplayCommaSeparated(options)}) ");
                }

                f.Write($"{explain.Statement}");
                break;
            }
            case Query s:
                s.Value.Fmt(f);
                break;
            case Declare declare:
            {
                f.Write($"DECLARE ");
                f.Write($"{DisplaySeparated(declare.Stmts, "; ")}");
                break;
            }
            case Fetch fetch:
            {
                f.Write($"FETCH {fetch.Direction} {fetch.Position} {fetch.Name}");

                if (fetch.Into is { } into)
                {
                    f.Write($" INTO {into}");
                }

                break;
            }
            case Directory directory:
            {
                f.Write($"INSERT{(directory.Overwrite ? " OVERWRITE" : "")}{(directory.Local ? " LOCAL" : "")} DIRECTORY '{directory.Path}'");
                if (directory.FileFormat is { } ff)
                {
                    f.Write($" STORED AS {ff}");
                }
                f.Write($" {directory.Source}");
                break;
            }
            case Msck msck:
                msck.Value.Fmt(f);
                break;
            case Truncate truncate:
                truncate.Value.Fmt(f);
                break;
            case Case stmt:
                f.Write($"{stmt.Value}");
                break;
            case If stmt:
                f.Write($"{stmt.Value}");
                break;
            case While stmt:
                f.Write($"{stmt.Value}");
                break;
            case Raise stmt:
                f.Write($"{stmt.Value}");
                break;
            case AttachDatabase attachDatabase:
            {
                var keyword = attachDatabase.Database ? "DATABASE " : "";
                f.Write($"ATTACH {keyword}{attachDatabase.DatabaseFileName} AS {attachDatabase.SchemaName}");
                break;
            }
            case AttachDuckDBDatabase attach:
            {
                f.Write($"ATTACH{(attach.Database ? " DATABASE" : "")}{(attach.IfNotExists ? " IF NOT EXISTS" : "")} {attach.DatabasePath}");
                if (attach.DatabaseAlias is { } alias)
                {
                    f.Write($" AS {alias}");
                }
                if (!attach.AttachOptions.IsEmpty)
                {
                    f.Write($" ({DisplayCommaSeparated(attach.AttachOptions)})");
                }
                break;
            }
            case DetachDuckDBDatabase detach:
            {
                f.Write($"DETACH{(detach.Database ? " DATABASE" : "")}{(detach.IfExists ? " IF EXISTS" : "")} {detach.DatabaseAlias}");
                break;
            }
            case Analyze analyze:
                analyze.Value.Fmt(f);
                break;
            case Insert insert:
                insert.Value.Fmt(f);
                break;
            case Install install:
                f.Write($"INSTALL {install.ExtensionName}");
                break;

            case Load load:
                f.Write($"LOAD {load.ExtensionName}");
                break;

            case Call function:
                f.Write($"CALL {function.Value}");
                break;

            case Copy copy:
            {
                f.Write($"COPY");
                switch (copy.Source)
                {
                    case CopySource.Query query:
                        f.Write($" ({query.Value})");
                        break;
                    case CopySource.Table table:
                    {
                        f.Write($" {table.TableName}");
                        if (!table.Columns.IsEmpty)
                        {
                            f.Write($" ({DisplayCommaSeparated(table.Columns)})");
                        }
                        break;
                    }
                    default:
                        throw new InvalidOperationException($"Variante de CopySource não tratada: {copy.Source.GetType().Name}");
                }
                f.Write($" {(copy.To ? "TO" : "FROM")} {copy.Target}");
                if (!copy.Options.IsEmpty)
                {
                    f.Write($" ({DisplayCommaSeparated(copy.Options)})");
                }
                if (!copy.LegacyOptions.IsEmpty)
                {
                    f.Write($" {DisplaySeparated(copy.LegacyOptions, " ")}");
                }
                if (!copy.Values.IsEmpty)
                {
                    // writeln!(f, ";")
                    f.Write($";\n");
                    var delim = "";
                    foreach (var v in copy.Values)
                    {
                        f.Write($"{delim}");
                        delim = "\t";
                        if (v is { } value)
                        {
                            f.Write($"{value}");
                        }
                        else
                        {
                            f.Write($"\\N");
                        }
                    }
                    f.Write($"\n\\.");
                }
                break;
            }
            case Update update:
                update.Value.Fmt(f);
                break;
            case Delete delete:
                delete.Value.Fmt(f);
                break;
            case Open open:
                open.Value.Fmt(f);
                break;
            case Close close:
            {
                f.Write($"CLOSE {close.Cursor}");

                break;
            }
            case CreateDatabase createDatabase:
            {
                f.Write($"CREATE {(createDatabase.OrReplace ? "OR REPLACE " : "")}{(createDatabase.Transient ? "TRANSIENT " : "")}DATABASE {(createDatabase.IfNotExists ? "IF NOT EXISTS " : "")}{createDatabase.DbName}");

                if (createDatabase.Location is { } l)
                {
                    f.Write($" LOCATION '{l}'");
                }
                if (createDatabase.ManagedLocation is { } ml)
                {
                    f.Write($" MANAGEDLOCATION '{ml}'");
                }
                if (createDatabase.CloneValue is { } clone)
                {
                    f.Write($" CLONE {clone}");
                }

                if (createDatabase.DataRetentionTimeInDays is { } dataRetentionTimeInDays)
                {
                    f.Write($" DATA_RETENTION_TIME_IN_DAYS = {dataRetentionTimeInDays}");
                }

                if (createDatabase.MaxDataExtensionTimeInDays is { } maxDataExtensionTimeInDays)
                {
                    f.Write($" MAX_DATA_EXTENSION_TIME_IN_DAYS = {maxDataExtensionTimeInDays}");
                }

                if (createDatabase.ExternalVolume is { } vol)
                {
                    f.Write($" EXTERNAL_VOLUME = '{vol}'");
                }

                if (createDatabase.Catalog is { } cat)
                {
                    f.Write($" CATALOG = '{cat}'");
                }

                if (createDatabase.ReplaceInvalidCharacters == true)
                {
                    f.Write($" REPLACE_INVALID_CHARACTERS = TRUE");
                }
                else if (createDatabase.ReplaceInvalidCharacters == false)
                {
                    f.Write($" REPLACE_INVALID_CHARACTERS = FALSE");
                }

                if (createDatabase.DefaultDdlCollation is { } ddlCollation)
                {
                    f.Write($" DEFAULT_DDL_COLLATION = '{ddlCollation}'");
                }

                if (createDatabase.StorageSerializationPolicy is { } policy)
                {
                    f.Write($" STORAGE_SERIALIZATION_POLICY = {policy}");
                }

                if (createDatabase.Comment is { } comment)
                {
                    f.Write($" COMMENT = '{comment}'");
                }

                if (createDatabase.DefaultCharset is { } charset)
                {
                    f.Write($" DEFAULT CHARACTER SET {charset}");
                }

                if (createDatabase.DefaultCollation is { } collation)
                {
                    f.Write($" DEFAULT COLLATE {collation}");
                }

                if (createDatabase.CatalogSync is { } sync)
                {
                    f.Write($" CATALOG_SYNC = '{sync}'");
                }

                if (createDatabase.CatalogSyncNamespaceMode is { } mode)
                {
                    f.Write($" CATALOG_SYNC_NAMESPACE_MODE = {mode}");
                }

                if (createDatabase.CatalogSyncNamespaceFlattenDelimiter is { } delim)
                {
                    f.Write($" CATALOG_SYNC_NAMESPACE_FLATTEN_DELIMITER = '{delim}'");
                }

                if (createDatabase.WithTags is { } tags)
                {
                    f.Write($" WITH TAG ({DisplayCommaSeparated(tags)})");
                }

                if (createDatabase.WithContacts is { } contacts)
                {
                    f.Write($" WITH CONTACT ({DisplayCommaSeparated(contacts)})");
                }
                break;
            }
            case CreateFunction createFunction:
                createFunction.Value.Fmt(f);
                break;
            case CreateDomain createDomain:
                createDomain.Value.Fmt(f);
                break;
            case CreateTrigger createTrigger:
                createTrigger.Value.Fmt(f);
                break;
            case DropTrigger dropTrigger:
                dropTrigger.Value.Fmt(f);
                break;
            case CreateProcedure createProcedure:
            {
                f.Write($"CREATE {(createProcedure.OrAlter ? "OR ALTER " : "")}PROCEDURE {createProcedure.Name}");

                if (createProcedure.Params is { } p)
                {
                    if (!p.IsEmpty)
                    {
                        f.Write($" ({DisplayCommaSeparated(p)})");
                    }
                }

                if (createProcedure.Language is { } language)
                {
                    f.Write($" LANGUAGE {language}");
                }

                f.Write($" AS {createProcedure.Body}");
                break;
            }
            case CreateMacro createMacro:
            {
                f.Write($"CREATE {(createMacro.OrReplace ? "OR REPLACE " : "")}{(createMacro.Temporary ? "TEMPORARY " : "")}MACRO {createMacro.Name}");
                if (createMacro.Args is { } args)
                {
                    f.Write($"({DisplayCommaSeparated(args)})");
                }
                switch (createMacro.Definition)
                {
                    case MacroDefinition.Expr expr:
                        f.Write($" AS {expr.Value}");
                        break;
                    case MacroDefinition.Table query:
                        f.Write($" AS TABLE {query.Value}");
                        break;
                    default:
                        throw new InvalidOperationException($"Variante de MacroDefinition não tratada: {createMacro.Definition.GetType().Name}");
                }
                break;
            }
            case CreateView createView:
                createView.Value.Fmt(f);
                break;
            case CreateTable createTable:
                createTable.Value.Fmt(f);
                break;
            case LoadData loadData:
            {
                f.Write($"LOAD DATA {(loadData.Local ? "LOCAL " : "")}INPATH '{loadData.Inpath}' {(loadData.Overwrite ? "OVERWRITE " : "")}INTO TABLE {loadData.TableName}");
                if (loadData.Partitioned is { } parts)
                {
                    if (!parts.IsEmpty)
                    {
                        f.Write($" PARTITION ({DisplayCommaSeparated(parts)})");
                    }
                }
                if (loadData.TableFormat is { } tableFormat)
                {
                    f.Write($" INPUTFORMAT {tableFormat.InputFormat} SERDE {tableFormat.Serde}");
                }
                break;
            }
            case CreateVirtualTable createVirtualTable:
            {
                f.Write($"CREATE VIRTUAL TABLE {(createVirtualTable.IfNotExists ? "IF NOT EXISTS " : "")}{createVirtualTable.Name} USING {createVirtualTable.ModuleName}");
                if (!createVirtualTable.ModuleArgs.IsEmpty)
                {
                    f.Write($" ({DisplayCommaSeparated(createVirtualTable.ModuleArgs)})");
                }
                break;
            }
            case CreateIndex createIndex:
                createIndex.Value.Fmt(f);
                break;
            case CreateExtension createExtension:
                f.Write($"{createExtension.Value}");
                break;
            case CreateCollation createCollation:
                f.Write($"{createCollation.Value}");
                break;
            case DropExtension dropExtension:
                f.Write($"{dropExtension.Value}");
                break;
            case DropOperator dropOperator:
                f.Write($"{dropOperator.Value}");
                break;
            case DropOperatorFamily dropOperatorFamily:
                f.Write($"{dropOperatorFamily.Value}");
                break;
            case DropOperatorClass dropOperatorClass:
                f.Write($"{dropOperatorClass.Value}");
                break;
            case CreateRole createRole:
                f.Write($"{createRole.Value}");
                break;
            case CreateSecret createSecret:
            {
                f.Write($"CREATE {(createSecret.OrReplace ? "OR REPLACE " : "")}");
                if (createSecret.Temporary is { } t)
                {
                    f.Write($"{(t ? "TEMPORARY " : "PERSISTENT ")}");
                }
                f.Write($"SECRET {(createSecret.IfNotExists ? "IF NOT EXISTS " : "")}");
                if (createSecret.Name is { } n)
                {
                    f.Write($"{n} ");
                }
                if (createSecret.StorageSpecifier is { } s)
                {
                    f.Write($"IN {s} ");
                }
                f.Write($"( TYPE {createSecret.SecretType}");
                if (!createSecret.Options.IsEmpty)
                {
                    f.Write($", {DisplayCommaSeparated(createSecret.Options)}");
                }
                f.Write($" )");
                break;
            }
            case CreateServer stmt:
                f.Write($"{stmt.Value}");
                break;
            case CreateForeignTable stmt:
                f.Write($"{stmt.Value}");
                break;
            case CreatePolicy policy:
                f.Write($"{policy.Value}");
                break;
            case CreateConnector createConnector:
                createConnector.Value.Fmt(f);
                break;
            case CreateOperator createOperator:
                createOperator.Value.Fmt(f);
                break;
            case CreateOperatorFamily createOperatorFamily:
                createOperatorFamily.Value.Fmt(f);
                break;
            case CreateOperatorClass createOperatorClass:
                createOperatorClass.Value.Fmt(f);
                break;
            case CreateTextSearch createTextSearch:
                createTextSearch.Value.Fmt(f);
                break;
            case AlterTable alterTable:
                f.Write($"{alterTable.Value}");
                break;
            case AlterIndex alterIndex:
                f.Write($"ALTER INDEX {alterIndex.Name} {alterIndex.Operation}");
                break;
            case AlterView alterView:
            {
                f.Write($"ALTER VIEW {alterView.Name}");
                if (!alterView.WithOptions.IsEmpty)
                {
                    f.Write($" WITH ({DisplayCommaSeparated(alterView.WithOptions)})");
                }
                if (!alterView.Columns.IsEmpty)
                {
                    f.Write($" ({DisplayCommaSeparated(alterView.Columns)})");
                }
                f.Write($" AS {alterView.Query}");
                break;
            }
            case AlterFunction alterFunction:
                f.Write($"{alterFunction.Value}");
                break;
            case AlterType { Value: var alterType }:
                f.Write($"ALTER TYPE {alterType.Name} {alterType.Operation}");
                break;
            case AlterCollation alterCollation:
                f.Write($"{alterCollation.Value}");
                break;
            case AlterOperator alterOperator:
                f.Write($"{alterOperator.Value}");
                break;
            case AlterOperatorFamily alterOperatorFamily:
                f.Write($"{alterOperatorFamily.Value}");
                break;
            case AlterOperatorClass alterOperatorClass:
                f.Write($"{alterOperatorClass.Value}");
                break;
            case AlterTextSearch alterTextSearch:
                f.Write($"{alterTextSearch.Value}");
                break;
            case AlterRole alterRole:
                f.Write($"ALTER ROLE {alterRole.Name} {alterRole.Operation}");
                break;
            case AlterPolicy alterPolicy:
                f.Write($"{alterPolicy.Value}");
                break;
            case AlterConnector alterConnector:
            {
                f.Write($"ALTER CONNECTOR {alterConnector.Name}");
                if (alterConnector.Properties is { } properties)
                {
                    f.Write($" SET DCPROPERTIES({DisplayCommaSeparated(properties)})");
                }
                if (alterConnector.Url is { } url)
                {
                    f.Write($" SET URL '{url}'");
                }
                if (alterConnector.Owner is { } owner)
                {
                    f.Write($" SET OWNER {owner}");
                }
                break;
            }
            case AlterSession alterSession:
            {
                f.Write($"ALTER SESSION {(alterSession.Set ? "SET" : "UNSET")}");
                if (!alterSession.SessionParams.Options.IsEmpty)
                {
                    if (alterSession.Set)
                    {
                        f.Write($" {alterSession.SessionParams}");
                    }
                    else
                    {
                        var options = alterSession.SessionParams
                            .Options
                            .Select(p => p.OptionName)
                            .ToSequence();
                        f.Write($" {DisplaySeparated(options, ", ")}");
                    }
                }
                break;
            }
            case Drop drop:
            {
                f.Write($"DROP {(drop.Temporary ? "TEMPORARY " : "")}{drop.ObjectType}{(drop.IfExists ? " IF EXISTS" : "")} {DisplayCommaSeparated(drop.Names)}{(drop.Cascade ? " CASCADE" : "")}{(drop.Restrict ? " RESTRICT" : "")}{(drop.Purge ? " PURGE" : "")}");
                if (drop.Table is { } tableName)
                {
                    f.Write($" ON {tableName}");
                }
                break;
            }
            case DropFunction dropFunction:
                f.Write($"{dropFunction.Value}");
                break;
            case DropDomain { Value: var dropDomain }:
            {
                f.Write($"DROP DOMAIN{(dropDomain.IfExists ? " IF EXISTS" : "")} {dropDomain.Name}");
                if (dropDomain.DropBehavior is { } op)
                {
                    f.Write($" {op}");
                }
                break;
            }
            case DropProcedure dropProcedure:
            {
                f.Write($"DROP PROCEDURE{(dropProcedure.IfExists ? " IF EXISTS" : "")} {DisplayCommaSeparated(dropProcedure.ProcDesc)}");
                if (dropProcedure.DropBehavior is { } op)
                {
                    f.Write($" {op}");
                }
                break;
            }
            case DropSecret dropSecret:
            {
                f.Write($"DROP ");
                if (dropSecret.Temporary is { } t)
                {
                    f.Write($"{(t ? "TEMPORARY " : "PERSISTENT ")}");
                }
                f.Write($"SECRET {(dropSecret.IfExists ? "IF EXISTS " : "")}{dropSecret.Name}");
                if (dropSecret.StorageSpecifier is { } s)
                {
                    f.Write($" FROM {s}");
                }
                break;
            }
            case DropPolicy policy:
                f.Write($"{policy.Value}");
                break;
            case DropConnector dropConnector:
            {
                f.Write($"DROP CONNECTOR {(dropConnector.IfExists ? "IF EXISTS " : "")}{dropConnector.Name}");
                break;
            }
            case Discard discard:
            {
                f.Write($"DISCARD {discard.ObjectType}");
                break;
            }
            case Set set:
                f.Write($"{set.Value}");
                break;
            case ShowVariable showVariable:
            {
                f.Write($"SHOW");
                if (!showVariable.Variable.IsEmpty)
                {
                    f.Write($" {DisplaySeparated(showVariable.Variable, " ")}");
                }
                break;
            }
            case ShowStatus showStatus:
            {
                f.Write($"SHOW");
                if (showStatus.Global)
                {
                    f.Write($" GLOBAL");
                }
                if (showStatus.Session)
                {
                    f.Write($" SESSION");
                }
                f.Write($" STATUS");
                if (showStatus.Filter is { } filter)
                {
                    f.Write($" {filter}");
                }
                break;
            }
            case ShowVariables showVariables:
            {
                f.Write($"SHOW");
                if (showVariables.Global)
                {
                    f.Write($" GLOBAL");
                }
                if (showVariables.Session)
                {
                    f.Write($" SESSION");
                }
                f.Write($" VARIABLES");
                if (showVariables.Filter is { } filter)
                {
                    f.Write($" {filter}");
                }
                break;
            }
            case ShowCreate showCreate:
            {
                f.Write($"SHOW CREATE {showCreate.ObjType} {showCreate.ObjName}");
                break;
            }
            case ShowColumns showColumns:
            {
                f.Write($"SHOW {(showColumns.Extended ? "EXTENDED " : "")}{(showColumns.Full ? "FULL " : "")}COLUMNS{showColumns.ShowOptions}");
                break;
            }
            case ShowDatabases showDatabases:
            {
                f.Write($"SHOW {(showDatabases.Terse ? "TERSE " : "")}DATABASES{(showDatabases.History ? " HISTORY" : "")}{showDatabases.ShowOptions}");
                break;
            }
            case ShowCatalogs showCatalogs:
            {
                f.Write($"SHOW {(showCatalogs.Terse ? "TERSE " : "")}CATALOGS{(showCatalogs.History ? " HISTORY" : "")}{showCatalogs.ShowOptions}");
                break;
            }
            case ShowProcessList showProcessList:
            {
                f.Write($"SHOW {(showProcessList.Full ? "FULL " : "")}PROCESSLIST");
                break;
            }
            case ShowSchemas showSchemas:
            {
                f.Write($"SHOW {(showSchemas.Terse ? "TERSE " : "")}SCHEMAS{(showSchemas.History ? " HISTORY" : "")}{showSchemas.ShowOptions}");
                break;
            }
            case ShowObjects { Value: var showObjects }:
            {
                f.Write($"SHOW {(showObjects.Terse ? "TERSE " : "")}OBJECTS{showObjects.ShowOptions}");
                break;
            }
            case ShowTables showTables:
            {
                f.Write($"SHOW {(showTables.Terse ? "TERSE " : "")}{(showTables.Extended ? "EXTENDED " : "")}{(showTables.Full ? "FULL " : "")}{(showTables.External ? "EXTERNAL " : "")}TABLES{(showTables.History ? " HISTORY" : "")}{showTables.ShowOptions}");
                break;
            }
            case ShowViews showViews:
            {
                f.Write($"SHOW {(showViews.Terse ? "TERSE " : "")}{(showViews.Materialized ? "MATERIALIZED " : "")}VIEWS{showViews.ShowOptions}");
                break;
            }
            case ShowFunctions showFunctions:
            {
                f.Write($"SHOW FUNCTIONS");
                if (showFunctions.Filter is { } filter)
                {
                    f.Write($" {filter}");
                }
                break;
            }
            case Use useExpr:
                useExpr.Value.Fmt(f);
                break;
            case ShowCollation showCollation:
            {
                f.Write($"SHOW COLLATION");
                if (showCollation.Filter is { } filter)
                {
                    f.Write($" {filter}");
                }
                break;
            }
            case ShowCharset showStm:
                showStm.Value.Fmt(f);
                break;
            case StartTransaction startTransaction:
            {
                if (startTransaction.Begin)
                {
                    if (startTransaction.Modifier is { } modifier)
                    {
                        f.Write($"BEGIN {modifier}");
                    }
                    else
                    {
                        f.Write($"BEGIN");
                    }
                }
                else
                {
                    f.Write($"START");
                }
                if (startTransaction.Transaction is { } transaction)
                {
                    f.Write($" {transaction}");
                }
                if (!startTransaction.Modes.IsEmpty)
                {
                    f.Write($" {DisplayCommaSeparated(startTransaction.Modes)}");
                }
                if (!startTransaction.Statements.IsEmpty)
                {
                    f.Write($" ");
                    FormatStatementList(f, startTransaction.Statements);
                }
                if (startTransaction.Exception is { } exceptionWhen)
                {
                    f.Write($" EXCEPTION");
                    foreach (var whenClause in exceptionWhen)
                    {
                        f.Write($" {whenClause}");
                    }
                }
                if (startTransaction.HasEndKeyword)
                {
                    f.Write($" END");
                }
                break;
            }
            case Commit commit:
            {
                if (commit.End)
                {
                    f.Write($"END");
                    if (commit.Modifier is { } modifier)
                    {
                        f.Write($" {modifier}");
                    }
                    if (commit.Chain)
                    {
                        f.Write($" AND CHAIN");
                    }
                }
                else
                {
                    f.Write($"COMMIT{(commit.Chain ? " AND CHAIN" : "")}");
                }
                break;
            }
            case Rollback rollback:
            {
                f.Write($"ROLLBACK");

                if (rollback.Chain)
                {
                    f.Write($" AND CHAIN");
                }

                if (rollback.Savepoint is { } savepoint)
                {
                    f.Write($" TO SAVEPOINT {savepoint}");
                }

                break;
            }
            case CreateSchema createSchema:
            {
                f.Write($"CREATE {(createSchema.OrReplace ? "OR REPLACE " : "")}SCHEMA {(createSchema.IfNotExists ? "IF NOT EXISTS " : "")}{createSchema.SchemaName}");

                if (createSchema.DefaultCollateSpec is { } collate)
                {
                    f.Write($" DEFAULT COLLATE {collate}");
                }

                if (createSchema.With is { } with)
                {
                    f.Write($" WITH ({DisplayCommaSeparated(with)})");
                }

                if (createSchema.Options is { } options)
                {
                    f.Write($" OPTIONS({DisplayCommaSeparated(options)})");
                }

                if (createSchema.CloneValue is { } clone)
                {
                    f.Write($" CLONE {clone}");
                }
                break;
            }
            case Assert assert:
            {
                f.Write($"ASSERT {assert.Condition}");
                if (assert.Message is { } m)
                {
                    f.Write($" AS {m}");
                }
                break;
            }
            case Grant grant:
                f.Write($"{grant.Value}");
                break;
            case Deny s:
                f.Write($"{s.Value}");
                break;
            case Revoke revoke:
                f.Write($"{revoke.Value}");
                break;
            case Deallocate deallocate:
                f.Write($"DEALLOCATE {(deallocate.Prepare ? "PREPARE " : "")}{deallocate.Name}");
                break;
            case Execute execute:
            {
                var (open, close) = execute.HasParentheses
                    // Space before `(` only when there is no name directly preceding it.
                    ? (execute.Name is not null ? "(" : " (", ")")
                    : (execute.Parameters.IsEmpty ? "" : " ", "");
                f.Write($"EXECUTE");
                if (execute.Immediate)
                {
                    f.Write($" IMMEDIATE");
                }
                if (execute.Name is { } name)
                {
                    f.Write($" {name}");
                }
                f.Write($"{open}{DisplayCommaSeparated(execute.Parameters)}{close}");
                if (!execute.Into.IsEmpty)
                {
                    f.Write($" INTO {DisplayCommaSeparated(execute.Into)}");
                }
                if (!execute.Using.IsEmpty)
                {
                    f.Write($" USING {DisplayCommaSeparated(execute.Using)}");
                }
                if (execute.Output)
                {
                    f.Write($" OUTPUT");
                }
                if (execute.Default)
                {
                    f.Write($" DEFAULT");
                }
                break;
            }
            case Prepare prepare:
            {
                f.Write($"PREPARE {prepare.Name} ");
                if (!prepare.DataTypes.IsEmpty)
                {
                    f.Write($"({DisplayCommaSeparated(prepare.DataTypes)}) ");
                }
                f.Write($"AS {prepare.Statement}");
                break;
            }
            case Comment comment:
            {
                f.Write($"COMMENT ");
                if (comment.IfExists)
                {
                    f.Write($"IF EXISTS ");
                }
                f.Write($"ON {comment.ObjectType} {comment.ObjectName} IS ");
                if (comment.CommentValue is { } c)
                {
                    f.Write($"'{c}'");
                }
                else
                {
                    f.Write($"NULL");
                }
                break;
            }
            case Savepoint savepoint:
            {
                f.Write($"SAVEPOINT ");
                f.Write($"{savepoint.Name}");
                break;
            }
            case ReleaseSavepoint releaseSavepoint:
                f.Write($"RELEASE SAVEPOINT {releaseSavepoint.Name}");
                break;
            case Merge merge:
                merge.Value.Fmt(f);
                break;
            case Cache cache:
            {
                if (cache.TableFlag is { } tableFlag)
                {
                    f.Write($"CACHE {tableFlag} TABLE {cache.TableName}");
                }
                else
                {
                    f.Write($"CACHE TABLE {cache.TableName}");
                }

                if (!cache.Options.IsEmpty)
                {
                    f.Write($" OPTIONS({DisplayCommaSeparated(cache.Options)})");
                }

                switch ((cache.HasAs, cache.Query))
                {
                    case (true, { } query):
                        f.Write($" AS {query}");
                        break;
                    case (true, null):
                        f.WriteStr(" AS");
                        break;
                    case (false, { } query):
                        f.Write($" {query}");
                        break;
                    case (false, null):
                        break;
                    default:
                        throw new InvalidOperationException("Combinação de (has_as, query) não tratada");
                }
                break;
            }
            case UNCache uncache:
            {
                if (uncache.IfExists)
                {
                    f.Write($"UNCACHE TABLE IF EXISTS {uncache.TableName}");
                }
                else
                {
                    f.Write($"UNCACHE TABLE {uncache.TableName}");
                }
                break;
            }
            case CreateSequence createSequence:
            {
                // Rust: as_type = [" AS ", &dt.to_string()].concat() (ou ""), escrito logo após o nome.
                // A lacuna {dt} usa flags novos, como o to_string(); DataType não contém "\n".
                f.Write($"CREATE {(createSequence.Temporary ? "TEMPORARY " : "")}SEQUENCE {(createSequence.IfNotExists ? "IF NOT EXISTS " : "")}{createSequence.Name}");
                if (createSequence.DataType is { } dt)
                {
                    f.Write($" AS {dt}");
                }
                foreach (var sequenceOption in createSequence.SequenceOptions)
                {
                    f.Write($"{sequenceOption}");
                }
                if (createSequence.OwnedBy is { } ob)
                {
                    f.Write($" OWNED BY {ob}");
                }
                // write!(f, "") não escreve nada.
                break;
            }
            case CreateStage createStage:
            {
                f.Write($"CREATE {(createStage.OrReplace ? "OR REPLACE " : "")}{(createStage.Temporary ? "TEMPORARY " : "")}STAGE {(createStage.IfNotExists ? "IF NOT EXISTS " : "")}{createStage.Name}{createStage.StageParams}");
                if (!createStage.DirectoryTableParams.Options.IsEmpty)
                {
                    f.Write($" DIRECTORY=({createStage.DirectoryTableParams})");
                }
                if (!createStage.FileFormat.Options.IsEmpty)
                {
                    f.Write($" FILE_FORMAT=({createStage.FileFormat})");
                }
                if (!createStage.CopyOptions.Options.IsEmpty)
                {
                    f.Write($" COPY_OPTIONS=({createStage.CopyOptions})");
                }
                if (createStage.Comment is { } comment)
                {
                    f.Write($" COMMENT='{comment}'");
                }
                break;
            }
            case CreateFileFormat createFileFormat:
            {
                f.Write($"CREATE {(createFileFormat.OrReplace ? "OR REPLACE " : "")}{(createFileFormat.Temporary ? "TEMPORARY " : "")}{(createFileFormat.Volatile ? "VOLATILE " : "")}FILE FORMAT {(createFileFormat.IfNotExists ? "IF NOT EXISTS " : "")}{createFileFormat.Name}");
                if (!createFileFormat.Options.Options.IsEmpty)
                {
                    f.Write($" {createFileFormat.Options}");
                }
                if (createFileFormat.Comment is { } comment)
                {
                    f.Write($" COMMENT='{comment}'");
                }
                break;
            }
            case CreateWarehouse s:
                f.Write($"{s.Value}");
                break;
            case CopyIntoSnowflake copyInto:
            {
                f.Write($"COPY INTO {copyInto.Into}");
                if (copyInto.IntoColumns is { } intoColumns)
                {
                    f.Write($" ({DisplayCommaSeparated(intoColumns)})");
                }
                if (copyInto.FromTransformations is { } fromTransformations)
                {
                    // Data load with transformation
                    if (copyInto.FromObj is { } fromStage)
                    {
                        f.Write($" FROM (SELECT {DisplaySeparated(fromTransformations, ", ")} FROM {fromStage}{copyInto.StageParams}");
                    }
                    if (copyInto.FromObjAlias is { } fromObjAlias)
                    {
                        f.Write($" AS {fromObjAlias}");
                    }
                    f.Write($")");
                }
                else if (copyInto.FromObj is { } fromObj)
                {
                    // Standard data load
                    f.Write($" FROM {fromObj}{copyInto.StageParams}");
                    if (copyInto.FromObjAlias is { } fromObjAlias2)
                    {
                        f.Write($" AS {fromObjAlias2}");
                    }
                }
                else if (copyInto.FromQuery is { } fromQuery)
                {
                    // Data unload from query
                    f.Write($" FROM ({fromQuery})");
                }

                if (copyInto.Files is { } files)
                {
                    f.Write($" FILES = ('{DisplaySeparated(files, "', '")}')");
                }
                if (copyInto.Pattern is { } pattern)
                {
                    f.Write($" PATTERN = '{pattern}'");
                }
                if (copyInto.Partition is { } partition)
                {
                    f.Write($" PARTITION BY {partition}");
                }
                if (!copyInto.FileFormat.Options.IsEmpty)
                {
                    f.Write($" FILE_FORMAT=({copyInto.FileFormat})");
                }
                if (!copyInto.CopyOptions.Options.IsEmpty)
                {
                    switch (copyInto.Kind)
                    {
                        case CopyIntoSnowflakeKind.Table:
                            f.Write($" COPY_OPTIONS=({copyInto.CopyOptions})");
                            break;
                        case CopyIntoSnowflakeKind.Location:
                            f.Write($" {copyInto.CopyOptions}");
                            break;
                        default:
                            throw new InvalidOperationException($"Variante de CopyIntoSnowflakeKind não tratada: {copyInto.Kind}");
                    }
                }
                if (copyInto.ValidationMode is { } validationMode)
                {
                    f.Write($" VALIDATION_MODE = {validationMode}");
                }
                break;
            }
            case CreateType createType:
            {
                f.Write($"CREATE TYPE {createType.Name}");
                if (createType.Representation is { } repr)
                {
                    f.Write($" {repr}");
                }
                break;
            }
            case Pragma pragma:
            {
                f.Write($"PRAGMA {pragma.Name}");
                if (pragma.Value is { } value)
                {
                    if (pragma.IsEq)
                    {
                        f.Write($" = {value}");
                    }
                    else
                    {
                        f.Write($"({value})");
                    }
                }
                break;
            }
            case Lock @lock:
                @lock.Value.Fmt(f);
                break;
            case LockTables lockTables:
                f.Write($"LOCK TABLES {DisplayCommaSeparated(lockTables.Tables)}");
                break;
            case UnlockTables:
                f.Write($"UNLOCK TABLES");
                break;
            case Unload unload:
            {
                f.Write($"UNLOAD(");
                if (unload.Query is { } query)
                {
                    f.Write($"{query}");
                }
                if (unload.QueryText is { } queryText)
                {
                    f.Write($"'{queryText}'");
                }
                f.Write($") TO {unload.To}");
                if (unload.Auth is { } auth)
                {
                    f.Write($" IAM_ROLE {auth}");
                }
                if (!unload.With.IsEmpty)
                {
                    f.Write($" WITH ({DisplayCommaSeparated(unload.With)})");
                }
                if (!unload.Options.IsEmpty)
                {
                    f.Write($" {DisplaySeparated(unload.Options, " ")}");
                }
                break;
            }
            case OptimizeTable optimizeTable:
            {
                f.Write($"OPTIMIZE");
                if (optimizeTable.HasTableKeyword)
                {
                    f.Write($" TABLE");
                }
                f.Write($" {optimizeTable.Name}");
                if (optimizeTable.OnCluster is { } onCluster)
                {
                    f.Write($" ON CLUSTER {onCluster}");
                }
                if (optimizeTable.Partition is { } partition)
                {
                    f.Write($" {partition}");
                }
                if (optimizeTable.IncludeFinal)
                {
                    f.Write($" FINAL");
                }
                if (optimizeTable.Deduplicate is { } deduplicate)
                {
                    f.Write($" {deduplicate}");
                }
                if (optimizeTable.Predicate is { } predicate)
                {
                    f.Write($" WHERE {predicate}");
                }
                if (optimizeTable.Zorder is { } zorder)
                {
                    f.Write($" ZORDER BY ({DisplayCommaSeparated(zorder)})");
                }
                break;
            }
            case LISTEN listen:
            {
                f.Write($"LISTEN {listen.Channel}");
                break;
            }
            case UNLISTEN unlisten:
            {
                f.Write($"UNLISTEN {unlisten.Channel}");
                break;
            }
            case NOTIFY notify:
            {
                f.Write($"NOTIFY {notify.Channel}");
                if (notify.Payload is { } payload)
                {
                    f.Write($", '{payload}'");
                }
                break;
            }
            case RenameTable renameTables:
                f.Write($"RENAME TABLE {DisplayCommaSeparated(renameTables.Value)}");
                break;
            case RaisError raisError:
            {
                f.Write($"RAISERROR({raisError.Message}, {raisError.Severity}, {raisError.State}");
                if (!raisError.Arguments.IsEmpty)
                {
                    f.Write($", {DisplayCommaSeparated(raisError.Arguments)}");
                }
                f.Write($")");
                if (!raisError.Options.IsEmpty)
                {
                    f.Write($" WITH {DisplayCommaSeparated(raisError.Options)}");
                }
                break;
            }
            case Throw s:
                f.Write($"{s.Value}");
                break;
            case Print s:
                f.Write($"{s.Value}");
                break;
            case WaitFor s:
                f.Write($"{s.Value}");
                break;
            case Return r:
                f.Write($"{r.Value}");
                break;
            case List command:
                f.Write($"LIST {command.Value}");
                break;
            case Put put:
            {
                f.Write($"PUT '{put.Source}' {put.Stage}");
                if (!put.Options.Options.IsEmpty)
                {
                    f.Write($" {put.Options}");
                }
                break;
            }
            case Remove command:
                f.Write($"REMOVE {command.Value}");
                break;
            case ExportData e:
                f.Write($"{e.Value}");
                break;
            case CreateUser s:
                f.Write($"{s.Value}");
                break;
            case AlterSchema s:
                f.Write($"{s.Value}");
                break;
            case Vacuum s:
                f.Write($"{s.Value}");
                break;
            case AlterUser s:
                f.Write($"{s.Value}");
                break;
            case Reset s:
                f.Write($"{s.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de Statement não tratada: {GetType().Name}");
        }
    }

    // Base de hierarquia de records: "sealed" impede o ToString() sintetizado em cada variante.
    public sealed override string ToString() => Formatter.Format(this);
}
