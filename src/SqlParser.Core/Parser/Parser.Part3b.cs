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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 10649–11942:
// parse_alter_table_operation … parse_alter_schema).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>Parse a single <c>ALTER TABLE</c> operation and return an <c>AlterTableOperation</c>.</summary>
    public AlterTableOperation ParseAlterTableOperation()
    {
        AlterTableOperation operation;
        if (ParseKeyword(Keyword.ADD))
        {
            if (ParseOptionalTableConstraint() is { } constraint)
            {
                var notValid = ParseKeywords(Keyword.NOT, Keyword.VALID);
                operation = new AlterTableOperation.AddConstraint(
                    Constraint: constraint,
                    NotValid: notValid);
            }
            else if (_dialect is ClickHouseDialect or GenericDialect
                && ParseKeyword(Keyword.PROJECTION))
            {
                return ParseAlterTableAddProjection();
            }
            else
            {
                var ifNotExists =
                    ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
                var newPartitions = new List<Partition>();
                while (true)
                {
                    if (ParseKeyword(Keyword.PARTITION))
                    {
                        newPartitions.Add(ParsePartition());
                    }
                    else
                    {
                        break;
                    }
                }
                if (newPartitions.Count != 0)
                {
                    operation = new AlterTableOperation.AddPartitions(
                        IfNotExists: ifNotExists,
                        NewPartitions: newPartitions.ToSequence());
                }
                else
                {
                    var columnKeyword = ParseKeyword(Keyword.COLUMN);

                    // Rust: `let if_not_exists` interno sombreia o externo (caso 72).
                    bool columnIfNotExists;
                    if (_dialect is PostgreSqlDialect or BigQueryDialect or DuckDbDialect or GenericDialect)
                    {
                        columnIfNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS)
                            || ifNotExists;
                    }
                    else
                    {
                        columnIfNotExists = false;
                    }

                    var columnDef = ParseColumnDef();

                    var columnPosition = ParseColumnPosition();

                    operation = new AlterTableOperation.AddColumn(
                        ColumnKeyword: columnKeyword,
                        IfNotExists: columnIfNotExists,
                        ColumnDef: columnDef,
                        ColumnPosition: columnPosition);
                }
            }
        }
        else if (ParseKeyword(Keyword.RENAME))
        {
            if (_dialect is PostgreSqlDialect && ParseKeyword(Keyword.CONSTRAINT))
            {
                var oldName = ParseIdentifier();
                ExpectKeywordIs(Keyword.TO);
                var newName = ParseIdentifier();
                operation = new AlterTableOperation.RenameConstraint(OldName: oldName, NewName: newName);
            }
            else if (ParseKeyword(Keyword.TO))
            {
                var tableName = ParseObjectName(false);
                operation = new AlterTableOperation.RenameTable(
                    TableName: new RenameTableNameKind.To(tableName));
            }
            else if (ParseKeyword(Keyword.AS))
            {
                var tableName = ParseObjectName(false);
                operation = new AlterTableOperation.RenameTable(
                    TableName: new RenameTableNameKind.As(tableName));
            }
            else
            {
                _ = ParseKeyword(Keyword.COLUMN); // [ COLUMN ]
                var oldColumnName = ParseIdentifier();
                ExpectKeywordIs(Keyword.TO);
                var newColumnName = ParseIdentifier();
                operation = new AlterTableOperation.RenameColumn(
                    OldColumnName: oldColumnName,
                    NewColumnName: newColumnName);
            }
        }
        else if (ParseKeyword(Keyword.DISABLE))
        {
            if (ParseKeywords(Keyword.ROW, Keyword.LEVEL, Keyword.SECURITY))
            {
                operation = new AlterTableOperation.DisableRowLevelSecurity();
            }
            else if (ParseKeyword(Keyword.RULE))
            {
                var name = ParseIdentifier();
                operation = new AlterTableOperation.DisableRule(Name: name);
            }
            else if (ParseKeyword(Keyword.TRIGGER))
            {
                var name = ParseIdentifier();
                operation = new AlterTableOperation.DisableTrigger(Name: name);
            }
            else
            {
                return ExpectedRef<AlterTableOperation>(
                    "ROW LEVEL SECURITY, RULE, or TRIGGER after DISABLE",
                    PeekTokenRef());
            }
        }
        else if (ParseKeyword(Keyword.ENABLE))
        {
            if (ParseKeywords(Keyword.ALWAYS, Keyword.RULE))
            {
                var name = ParseIdentifier();
                operation = new AlterTableOperation.EnableAlwaysRule(Name: name);
            }
            else if (ParseKeywords(Keyword.ALWAYS, Keyword.TRIGGER))
            {
                var name = ParseIdentifier();
                operation = new AlterTableOperation.EnableAlwaysTrigger(Name: name);
            }
            else if (ParseKeywords(Keyword.ROW, Keyword.LEVEL, Keyword.SECURITY))
            {
                operation = new AlterTableOperation.EnableRowLevelSecurity();
            }
            else if (ParseKeywords(Keyword.REPLICA, Keyword.RULE))
            {
                var name = ParseIdentifier();
                operation = new AlterTableOperation.EnableReplicaRule(Name: name);
            }
            else if (ParseKeywords(Keyword.REPLICA, Keyword.TRIGGER))
            {
                var name = ParseIdentifier();
                operation = new AlterTableOperation.EnableReplicaTrigger(Name: name);
            }
            else if (ParseKeyword(Keyword.RULE))
            {
                var name = ParseIdentifier();
                operation = new AlterTableOperation.EnableRule(Name: name);
            }
            else if (ParseKeyword(Keyword.TRIGGER))
            {
                var name = ParseIdentifier();
                operation = new AlterTableOperation.EnableTrigger(Name: name);
            }
            else
            {
                return ExpectedRef<AlterTableOperation>(
                    "ALWAYS, REPLICA, ROW LEVEL SECURITY, RULE, or TRIGGER after ENABLE",
                    PeekTokenRef());
            }
        }
        else if (ParseKeywords(
            Keyword.FORCE,
            Keyword.ROW,
            Keyword.LEVEL,
            Keyword.SECURITY))
        {
            operation = new AlterTableOperation.ForceRowLevelSecurity();
        }
        else if (ParseKeywords(
            Keyword.NO,
            Keyword.FORCE,
            Keyword.ROW,
            Keyword.LEVEL,
            Keyword.SECURITY))
        {
            operation = new AlterTableOperation.NoForceRowLevelSecurity();
        }
        else if (ParseKeywords(Keyword.CLEAR, Keyword.PROJECTION)
            && _dialect is ClickHouseDialect or GenericDialect)
        {
            var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
            var name = ParseIdentifier();
            Ident? partition;
            if (ParseKeywords(Keyword.IN, Keyword.PARTITION))
            {
                partition = ParseIdentifier();
            }
            else
            {
                partition = null;
            }
            operation = new AlterTableOperation.ClearProjection(
                IfExists: ifExists,
                Name: name,
                Partition: partition);
        }
        else if (ParseKeywords(Keyword.MATERIALIZE, Keyword.PROJECTION)
            && _dialect is ClickHouseDialect or GenericDialect)
        {
            var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
            var name = ParseIdentifier();
            Ident? partition;
            if (ParseKeywords(Keyword.IN, Keyword.PARTITION))
            {
                partition = ParseIdentifier();
            }
            else
            {
                partition = null;
            }
            operation = new AlterTableOperation.MaterializeProjection(
                IfExists: ifExists,
                Name: name,
                Partition: partition);
        }
        else if (ParseKeyword(Keyword.DROP))
        {
            if (ParseKeywords(Keyword.IF, Keyword.EXISTS, Keyword.PARTITION))
            {
                ExpectToken(new Token.LParen());
                var partitions = ParseCommaSeparated(p => p.ParseExpr());
                ExpectToken(new Token.RParen());
                operation = new AlterTableOperation.DropPartitions(
                    Partitions: partitions,
                    IfExists: true);
            }
            else if (ParseKeyword(Keyword.PARTITION))
            {
                ExpectToken(new Token.LParen());
                var partitions = ParseCommaSeparated(p => p.ParseExpr());
                ExpectToken(new Token.RParen());
                operation = new AlterTableOperation.DropPartitions(
                    Partitions: partitions,
                    IfExists: false);
            }
            else if (ParseKeyword(Keyword.CONSTRAINT))
            {
                var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
                var name = ParseIdentifier();
                var dropBehavior = ParseOptionalDropBehavior();
                operation = new AlterTableOperation.DropConstraint(
                    IfExists: ifExists,
                    Name: name,
                    DropBehavior: dropBehavior);
            }
            else if (ParseKeywords(Keyword.PRIMARY, Keyword.KEY))
            {
                var dropBehavior = ParseOptionalDropBehavior();
                operation = new AlterTableOperation.DropPrimaryKey(DropBehavior: dropBehavior);
            }
            else if (ParseKeywords(Keyword.FOREIGN, Keyword.KEY))
            {
                var name = ParseIdentifier();
                var dropBehavior = ParseOptionalDropBehavior();
                operation = new AlterTableOperation.DropForeignKey(
                    Name: name,
                    DropBehavior: dropBehavior);
            }
            else if (ParseKeyword(Keyword.INDEX))
            {
                var name = ParseIdentifier();
                operation = new AlterTableOperation.DropIndex(Name: name);
            }
            else if (ParseKeyword(Keyword.PROJECTION)
                && _dialect is ClickHouseDialect or GenericDialect)
            {
                var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
                var name = ParseIdentifier();
                operation = new AlterTableOperation.DropProjection(IfExists: ifExists, Name: name);
            }
            else if (ParseKeywords(Keyword.CLUSTERING, Keyword.KEY))
            {
                operation = new AlterTableOperation.DropClusteringKey();
            }
            else
            {
                var hasColumnKeyword = ParseKeyword(Keyword.COLUMN); // [ COLUMN ]
                var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
                Sequence<Ident> columnNames;
                if (_dialect.SupportsCommaSeparatedDropColumnList)
                {
                    columnNames = ParseCommaSeparated(p => p.ParseIdentifier());
                }
                else
                {
                    columnNames = Sequence.Of(ParseIdentifier());
                }
                var dropBehavior = ParseOptionalDropBehavior();
                operation = new AlterTableOperation.DropColumn(
                    HasColumnKeyword: hasColumnKeyword,
                    ColumnNames: columnNames,
                    IfExists: ifExists,
                    DropBehavior: dropBehavior);
            }
        }
        else if (ParseKeyword(Keyword.PARTITION))
        {
            ExpectToken(new Token.LParen());
            var before = ParseCommaSeparated(p => p.ParseExpr());
            ExpectToken(new Token.RParen());
            ExpectKeywordIs(Keyword.RENAME);
            ExpectKeywords(Keyword.TO, Keyword.PARTITION);
            ExpectToken(new Token.LParen());
            var renames = ParseCommaSeparated(p => p.ParseExpr());
            ExpectToken(new Token.RParen());
            operation = new AlterTableOperation.RenamePartitions(
                OldPartitions: before,
                NewPartitions: renames);
        }
        else if (ParseKeyword(Keyword.CHANGE))
        {
            _ = ParseKeyword(Keyword.COLUMN); // [ COLUMN ]
            var oldName = ParseIdentifier();
            var newName = ParseIdentifier();
            var dataType = ParseDataType();
            var options = new List<ColumnOption>();
            while (ParseOptionalColumnOption() is { } option)
            {
                options.Add(option);
            }

            var columnPosition = ParseColumnPosition();

            operation = new AlterTableOperation.ChangeColumn(
                OldName: oldName,
                NewName: newName,
                DataType: dataType,
                Options: options.ToSequence(),
                ColumnPosition: columnPosition);
        }
        else if (ParseKeyword(Keyword.MODIFY))
        {
            _ = ParseKeyword(Keyword.COLUMN); // [ COLUMN ]
            var colName = ParseIdentifier();
            var dataType = ParseDataType();
            var options = new List<ColumnOption>();
            while (ParseOptionalColumnOption() is { } option)
            {
                options.Add(option);
            }

            var columnPosition = ParseColumnPosition();

            operation = new AlterTableOperation.ModifyColumn(
                ColName: colName,
                DataType: dataType,
                Options: options.ToSequence(),
                ColumnPosition: columnPosition);
        }
        else if (ParseKeyword(Keyword.ALTER))
        {
            if (PeekKeyword(Keyword.SORTKEY))
            {
                PrevToken();
                return ParseAlterSortKey();
            }

            _ = ParseKeyword(Keyword.COLUMN); // [ COLUMN ]
            var columnName = ParseIdentifier();
            var isPostgresql = _dialect is PostgreSqlDialect;

            AlterColumnOperation op;
            if (ParseKeywords(
                Keyword.SET,
                Keyword.NOT,
                Keyword.NULL))
            {
                op = new AlterColumnOperation.SetNotNull();
            }
            else if (ParseKeywords(Keyword.DROP, Keyword.NOT, Keyword.NULL))
            {
                op = new AlterColumnOperation.DropNotNull();
            }
            else if (ParseKeywords(Keyword.SET, Keyword.DEFAULT))
            {
                op = new AlterColumnOperation.SetDefault(
                    Value: ParseExpr());
            }
            else if (ParseKeywords(Keyword.DROP, Keyword.DEFAULT))
            {
                op = new AlterColumnOperation.DropDefault();
            }
            else if (ParseKeywords(Keyword.SET, Keyword.DATA, Keyword.TYPE))
            {
                op = ParseSetDataType(true);
            }
            else if (ParseKeyword(Keyword.TYPE))
            {
                op = ParseSetDataType(false);
            }
            else if (ParseKeywords(Keyword.ADD, Keyword.GENERATED))
            {
                GeneratedAs? generatedAs;
                if (ParseKeyword(Keyword.ALWAYS))
                {
                    generatedAs = GeneratedAs.Always;
                }
                else if (ParseKeywords(Keyword.BY, Keyword.DEFAULT))
                {
                    generatedAs = GeneratedAs.ByDefault;
                }
                else
                {
                    generatedAs = null;
                }

                ExpectKeywords(Keyword.AS, Keyword.IDENTITY);

                Sequence<SequenceOptions>? sequenceOptions = null;

                if (PeekTokenRef().Token is Token.LParen)
                {
                    ExpectToken(new Token.LParen());
                    sequenceOptions = ParseCreateSequenceOptions();
                    ExpectToken(new Token.RParen());
                }

                op = new AlterColumnOperation.AddGenerated(
                    GeneratedAs: generatedAs,
                    SequenceOptions: sequenceOptions);
            }
            else
            {
                string message;
                if (isPostgresql)
                {
                    message = "SET/DROP NOT NULL, SET DEFAULT, SET DATA TYPE, or ADD GENERATED after ALTER COLUMN";
                }
                else
                {
                    message = "SET/DROP NOT NULL, SET DEFAULT, or SET DATA TYPE after ALTER COLUMN";
                }

                return ExpectedRef<AlterTableOperation>(message, PeekTokenRef());
            }
            operation = new AlterTableOperation.AlterColumn(ColumnName: columnName, Op: op);
        }
        else if (ParseKeyword(Keyword.SWAP))
        {
            ExpectKeywordIs(Keyword.WITH);
            var tableName = ParseObjectName(false);
            operation = new AlterTableOperation.SwapWith(TableName: tableName);
        }
        else if (_dialect is PostgreSqlDialect or GenericDialect
            && ParseKeywords(Keyword.OWNER, Keyword.TO))
        {
            var newOwner = ParseOwner();
            operation = new AlterTableOperation.OwnerTo(NewOwner: newOwner);
        }
        else if (_dialect is ClickHouseDialect or GenericDialect
            && ParseKeyword(Keyword.ATTACH))
        {
            operation = new AlterTableOperation.AttachPartition(
                Partition: ParsePartOrPartition());
        }
        else if (_dialect is ClickHouseDialect or GenericDialect
            && ParseKeyword(Keyword.DETACH))
        {
            operation = new AlterTableOperation.DetachPartition(
                Partition: ParsePartOrPartition());
        }
        else if (_dialect is ClickHouseDialect or GenericDialect
            && ParseKeyword(Keyword.FREEZE))
        {
            var partition = ParsePartOrPartition();
            Ident? withName;
            if (ParseKeyword(Keyword.WITH))
            {
                ExpectKeywordIs(Keyword.NAME);
                withName = ParseIdentifier();
            }
            else
            {
                withName = null;
            }
            operation = new AlterTableOperation.FreezePartition(
                Partition: partition,
                WithName: withName);
        }
        else if (_dialect is ClickHouseDialect or GenericDialect
            && ParseKeyword(Keyword.UNFREEZE))
        {
            var partition = ParsePartOrPartition();
            Ident? withName;
            if (ParseKeyword(Keyword.WITH))
            {
                ExpectKeywordIs(Keyword.NAME);
                withName = ParseIdentifier();
            }
            else
            {
                withName = null;
            }
            operation = new AlterTableOperation.UnfreezePartition(
                Partition: partition,
                WithName: withName);
        }
        else if (ParseKeywords(Keyword.CLUSTER, Keyword.BY))
        {
            ExpectToken(new Token.LParen());
            var exprs = ParseCommaSeparated(parser => parser.ParseExpr());
            ExpectToken(new Token.RParen());
            operation = new AlterTableOperation.ClusterBy(Exprs: exprs);
        }
        else if (ParseKeywords(Keyword.SUSPEND, Keyword.RECLUSTER))
        {
            operation = new AlterTableOperation.SuspendRecluster();
        }
        else if (ParseKeywords(Keyword.RESUME, Keyword.RECLUSTER))
        {
            operation = new AlterTableOperation.ResumeRecluster();
        }
        else if (ParseKeyword(Keyword.LOCK))
        {
            var @equals = ConsumeToken(new Token.Eq());
            AlterTableLock @lock;
            switch (ParseOneOfKeywords(
                Keyword.DEFAULT,
                Keyword.EXCLUSIVE,
                Keyword.NONE,
                Keyword.SHARED))
            {
                case Keyword.DEFAULT:
                    @lock = new AlterTableLock.Default();
                    break;
                case Keyword.EXCLUSIVE:
                    @lock = new AlterTableLock.Exclusive();
                    break;
                case Keyword.NONE:
                    @lock = new AlterTableLock.None();
                    break;
                case Keyword.SHARED:
                    @lock = new AlterTableLock.Shared();
                    break;
                default:
                    @lock = ExpectedRef<AlterTableLock>(
                        "DEFAULT, EXCLUSIVE, NONE or SHARED after LOCK [=]",
                        PeekTokenRef());
                    break;
            }
            operation = new AlterTableOperation.Lock(EqualsValue: @equals, LockValue: @lock);
        }
        else if (ParseKeyword(Keyword.ALGORITHM))
        {
            var @equals = ConsumeToken(new Token.Eq());
            AlterTableAlgorithm algorithm;
            switch (ParseOneOfKeywords(
                Keyword.DEFAULT,
                Keyword.INSTANT,
                Keyword.INPLACE,
                Keyword.COPY))
            {
                case Keyword.DEFAULT:
                    algorithm = new AlterTableAlgorithm.Default();
                    break;
                case Keyword.INSTANT:
                    algorithm = new AlterTableAlgorithm.Instant();
                    break;
                case Keyword.INPLACE:
                    algorithm = new AlterTableAlgorithm.Inplace();
                    break;
                case Keyword.COPY:
                    algorithm = new AlterTableAlgorithm.Copy();
                    break;
                default:
                    algorithm = ExpectedRef<AlterTableAlgorithm>(
                        "DEFAULT, INSTANT, INPLACE, or COPY after ALGORITHM [=]",
                        PeekTokenRef());
                    break;
            }
            operation = new AlterTableOperation.Algorithm(EqualsValue: @equals, AlgorithmValue: algorithm);
        }
        else if (ParseKeyword(Keyword.AUTO_INCREMENT))
        {
            var @equals = ConsumeToken(new Token.Eq());
            var value = ParseNumberValue();
            operation = new AlterTableOperation.AutoIncrement(EqualsValue: @equals, Value: value);
        }
        else if (ParseKeywords(Keyword.REPLICA, Keyword.IDENTITY))
        {
            ReplicaIdentity identity;
            if (ParseKeyword(Keyword.NOTHING))
            {
                identity = new ReplicaIdentity.Nothing();
            }
            else if (ParseKeyword(Keyword.FULL))
            {
                identity = new ReplicaIdentity.Full();
            }
            else if (ParseKeyword(Keyword.DEFAULT))
            {
                identity = new ReplicaIdentity.Default();
            }
            else if (ParseKeywords(Keyword.USING, Keyword.INDEX))
            {
                identity = new ReplicaIdentity.Index(ParseIdentifier());
            }
            else
            {
                return ExpectedRef<AlterTableOperation>(
                    "NOTHING, FULL, DEFAULT, or USING INDEX index_name after REPLICA IDENTITY",
                    PeekTokenRef());
            }

            operation = new AlterTableOperation.ReplicaIdentity(Identity: identity);
        }
        else if (ParseKeywords(Keyword.VALIDATE, Keyword.CONSTRAINT))
        {
            var name = ParseIdentifier();
            operation = new AlterTableOperation.ValidateConstraint(Name: name);
        }
        else if (ParseKeywords(Keyword.SET, Keyword.LOGGED))
        {
            operation = new AlterTableOperation.SetLogged();
        }
        else if (ParseKeywords(Keyword.SET, Keyword.UNLOGGED))
        {
            operation = new AlterTableOperation.SetUnlogged();
        }
        else
        {
            var options =
                ParseOptionsWithKeywords(Keyword.SET, Keyword.TBLPROPERTIES);
            if (!options.IsEmpty)
            {
                operation = new AlterTableOperation.SetTblProperties(
                    TableProperties: options);
            }
            else
            {
                options = ParseOptions(Keyword.SET);
                if (!options.IsEmpty)
                {
                    operation = new AlterTableOperation.SetOptionsParens(Options: options);
                }
                else
                {
                    return ExpectedRef<AlterTableOperation>(
                        "ADD, RENAME, PARTITION, SWAP, DROP, REPLICA IDENTITY, SET, or SET TBLPROPERTIES after ALTER TABLE",
                        PeekTokenRef());
                }
            }
        }
        return operation;
    }

    internal AlterColumnOperation ParseSetDataType(bool hadSet)
    {
        var dataType = ParseDataType();
        Expr? @using;
        if (_dialect.SupportsAlterColumnTypeUsing
            && ParseKeyword(Keyword.USING))
        {
            @using = ParseExpr();
        }
        else
        {
            @using = null;
        }
        return new AlterColumnOperation.SetDataType(
            DataType: dataType,
            Using: @using,
            HadSet: hadSet);
    }

    internal Partition ParsePartOrPartition()
    {
        var keyword = ExpectOneOfKeywords(Keyword.PART, Keyword.PARTITION);
        switch (keyword)
        {
            case Keyword.PART:
                return new Partition.Part(ParseExpr());
            case Keyword.PARTITION:
                return new Partition.Expr(ParseExpr());
            // unreachable because expect_one_of_keywords used above
            case var unexpectedKeyword:
                throw new ParserException(new ParserError.ParserErrorValue(
                    $"Internal parser error: expected any of {{PART, PARTITION}}, got {unexpectedKeyword.ToString()}"));
        }
    }

    /// <summary>Parse an <c>ALTER &lt;object&gt;</c> statement and dispatch to the appropriate alter handler.</summary>
    public Statement ParseAlter()
    {
        if (PeekKeywords(Keyword.TEXT, Keyword.SEARCH))
        {
            return Statement.From(ParseAlterTextSearch());
        }

        var objectType = ExpectOneOfKeywords(
            Keyword.VIEW,
            Keyword.TYPE,
            Keyword.COLLATION,
            Keyword.TABLE,
            Keyword.INDEX,
            Keyword.FUNCTION,
            Keyword.AGGREGATE,
            Keyword.ROLE,
            Keyword.POLICY,
            Keyword.CONNECTOR,
            Keyword.ICEBERG,
            Keyword.SCHEMA,
            Keyword.USER,
            Keyword.OPERATOR);
        switch (objectType)
        {
            case Keyword.SCHEMA:
                PrevToken();
                PrevToken();
                return ParseAlterSchema();
            case Keyword.VIEW:
                return ParseAlterView();
            case Keyword.TYPE:
                return ParseAlterType();
            case Keyword.COLLATION:
                return Statement.From(ParseAlterCollation());
            case Keyword.TABLE:
                return ParseAlterTable(false);
            case Keyword.ICEBERG:
                ExpectKeyword(Keyword.TABLE);
                return ParseAlterTable(true);
            case Keyword.INDEX:
            {
                var indexName = ParseObjectName(false);
                AlterIndexOperation operation;
                if (ParseKeyword(Keyword.RENAME))
                {
                    if (ParseKeyword(Keyword.TO))
                    {
                        // Rust: `let index_name` interno sombreia o externo (caso 72).
                        var newIndexName = ParseObjectName(false);
                        operation = new AlterIndexOperation.RenameIndex(IndexName: newIndexName);
                    }
                    else
                    {
                        return ExpectedRef<Statement>("TO after RENAME", PeekTokenRef());
                    }
                }
                else
                {
                    return ExpectedRef<Statement>("RENAME after ALTER INDEX", PeekTokenRef());
                }

                return new Statement.AlterIndex(
                    Name: indexName,
                    Operation: operation);
            }
            case Keyword.FUNCTION:
                return ParseAlterFunction(new AlterFunctionKind.Function());
            case Keyword.AGGREGATE:
                return ParseAlterFunction(new AlterFunctionKind.Aggregate());
            case Keyword.OPERATOR:
                if (ParseKeyword(Keyword.FAMILY))
                {
                    return Statement.From(ParseAlterOperatorFamily());
                }
                else if (ParseKeyword(Keyword.CLASS))
                {
                    return Statement.From(ParseAlterOperatorClass());
                }
                else
                {
                    return Statement.From(ParseAlterOperator());
                }
            case Keyword.ROLE:
                return ParseAlterRole();
            case Keyword.POLICY:
                return Statement.From(ParseAlterPolicy());
            case Keyword.CONNECTOR:
                return ParseAlterConnector();
            case Keyword.USER when _dialect.SupportsAlterUserAsAlterRole:
                return ParseAlterRole();
            case Keyword.USER:
                return Statement.From(ParseAlterUser());
            // unreachable because expect_one_of_keywords used above
            case var unexpectedKeyword:
                throw new ParserException(new ParserError.ParserErrorValue(
                    $"Internal parser error: expected any of {{TEXT SEARCH, VIEW, TYPE, COLLATION, TABLE, INDEX, FUNCTION, AGGREGATE, ROLE, POLICY, CONNECTOR, ICEBERG, SCHEMA, USER, OPERATOR}}, got {unexpectedKeyword.ToString()}"));
        }
    }

    internal (FunctionDesc Function, bool AggregateStar, Sequence<OperateFunctionArg>? AggregateOrderBy) ParseAlterAggregateSignature()
    {
        var name = ParseObjectName(false);
        ExpectToken(new Token.LParen());

        if (ConsumeToken(new Token.Mul()))
        {
            ExpectToken(new Token.RParen());
            return (
                new FunctionDesc(
                    Name: name,
                    Args: Sequence<OperateFunctionArg>.Empty),
                true,
                null);
        }

        Sequence<OperateFunctionArg> args;
        if (PeekKeyword(Keyword.ORDER) || PeekTokenRef().Token is Token.RParen)
        {
            args = Sequence<OperateFunctionArg>.Empty;
        }
        else
        {
            args = ParseCommaSeparated(p => p.ParseAggregateFunctionArg());
        }

        Sequence<OperateFunctionArg>? aggregateOrderBy;
        if (ParseKeywords(Keyword.ORDER, Keyword.BY))
        {
            aggregateOrderBy = ParseCommaSeparated(p => p.ParseAggregateFunctionArg());
        }
        else
        {
            aggregateOrderBy = null;
        }

        ExpectToken(new Token.RParen());
        return (
            new FunctionDesc(
                Name: name,
                Args: args),
            false,
            aggregateOrderBy);
    }

    internal AlterFunctionAction? ParseAlterFunctionAction()
    {
        AlterFunctionAction? action;
        if (ParseKeywords(
            Keyword.CALLED,
            Keyword.ON,
            Keyword.NULL,
            Keyword.INPUT))
        {
            action = new AlterFunctionAction.CalledOnNull(
                new FunctionCalledOnNull.CalledOnNullInput());
        }
        else if (ParseKeywords(
            Keyword.RETURNS,
            Keyword.NULL,
            Keyword.ON,
            Keyword.NULL,
            Keyword.INPUT))
        {
            action = new AlterFunctionAction.CalledOnNull(
                new FunctionCalledOnNull.ReturnsNullOnNullInput());
        }
        else if (ParseKeyword(Keyword.STRICT))
        {
            action = new AlterFunctionAction.CalledOnNull(
                new FunctionCalledOnNull.Strict());
        }
        else if (ParseKeyword(Keyword.IMMUTABLE))
        {
            action = new AlterFunctionAction.Behavior(new FunctionBehavior.Immutable());
        }
        else if (ParseKeyword(Keyword.STABLE))
        {
            action = new AlterFunctionAction.Behavior(new FunctionBehavior.Stable());
        }
        else if (ParseKeyword(Keyword.VOLATILE))
        {
            action = new AlterFunctionAction.Behavior(new FunctionBehavior.Volatile());
        }
        else if (ParseKeyword(Keyword.NOT))
        {
            ExpectKeyword(Keyword.LEAKPROOF);
            action = new AlterFunctionAction.Leakproof(false);
        }
        else if (ParseKeyword(Keyword.LEAKPROOF))
        {
            action = new AlterFunctionAction.Leakproof(true);
        }
        else if (ParseKeyword(Keyword.EXTERNAL))
        {
            ExpectKeyword(Keyword.SECURITY);
            FunctionSecurity security;
            if (ParseKeyword(Keyword.DEFINER))
            {
                security = new FunctionSecurity.Definer();
            }
            else if (ParseKeyword(Keyword.INVOKER))
            {
                security = new FunctionSecurity.Invoker();
            }
            else
            {
                return ExpectedRef<AlterFunctionAction?>("DEFINER or INVOKER", PeekTokenRef());
            }
            action = new AlterFunctionAction.Security(
                External: true,
                SecurityValue: security);
        }
        else if (ParseKeyword(Keyword.SECURITY))
        {
            FunctionSecurity security;
            if (ParseKeyword(Keyword.DEFINER))
            {
                security = new FunctionSecurity.Definer();
            }
            else if (ParseKeyword(Keyword.INVOKER))
            {
                security = new FunctionSecurity.Invoker();
            }
            else
            {
                return ExpectedRef<AlterFunctionAction?>("DEFINER or INVOKER", PeekTokenRef());
            }
            action = new AlterFunctionAction.Security(
                External: false,
                SecurityValue: security);
        }
        else if (ParseKeyword(Keyword.PARALLEL))
        {
            FunctionParallel parallel;
            if (ParseKeyword(Keyword.UNSAFE))
            {
                parallel = new FunctionParallel.Unsafe();
            }
            else if (ParseKeyword(Keyword.RESTRICTED))
            {
                parallel = new FunctionParallel.Restricted();
            }
            else if (ParseKeyword(Keyword.SAFE))
            {
                parallel = new FunctionParallel.Safe();
            }
            else
            {
                return ExpectedRef<AlterFunctionAction?>("one of UNSAFE | RESTRICTED | SAFE", PeekTokenRef());
            }
            action = new AlterFunctionAction.Parallel(parallel);
        }
        else if (ParseKeyword(Keyword.COST))
        {
            action = new AlterFunctionAction.Cost(ParseNumber());
        }
        else if (ParseKeyword(Keyword.ROWS))
        {
            action = new AlterFunctionAction.Rows(ParseNumber());
        }
        else if (ParseKeyword(Keyword.SUPPORT))
        {
            action = new AlterFunctionAction.Support(ParseObjectName(false));
        }
        else if (ParseKeyword(Keyword.SET))
        {
            var name = ParseObjectName(false);
            FunctionSetValue value;
            if (ParseKeywords(Keyword.FROM, Keyword.CURRENT))
            {
                value = new FunctionSetValue.FromCurrent();
            }
            else
            {
                if (!ConsumeToken(new Token.Eq()) && !ParseKeyword(Keyword.TO))
                {
                    return ExpectedRef<AlterFunctionAction?>("= or TO", PeekTokenRef());
                }
                if (ParseKeyword(Keyword.DEFAULT))
                {
                    value = new FunctionSetValue.Default();
                }
                else
                {
                    value = new FunctionSetValue.Values(ParseCommaSeparated(p => p.ParseExpr()));
                }
            }
            action = new AlterFunctionAction.Set(new FunctionDefinitionSetParam(
                Name: name,
                Value: value));
        }
        else if (ParseKeyword(Keyword.RESET))
        {
            ResetConfig resetConfig;
            if (ParseKeyword(Keyword.ALL))
            {
                resetConfig = new ResetConfig.ALL();
            }
            else
            {
                resetConfig = new ResetConfig.ConfigName(ParseObjectName(false));
            }
            action = new AlterFunctionAction.Reset(resetConfig);
        }
        else
        {
            action = null;
        }

        return action;
    }

    internal (Sequence<AlterFunctionAction> Actions, bool Restrict) ParseAlterFunctionActions()
    {
        var actions = new List<AlterFunctionAction>();
        while (ParseAlterFunctionAction() is { } action)
        {
            actions.Add(action);
        }
        if (actions.Count == 0)
        {
            return ExpectedRef<(Sequence<AlterFunctionAction>, bool)>(
                "at least one ALTER FUNCTION action", PeekTokenRef());
        }
        var restrict = ParseKeyword(Keyword.RESTRICT);
        return (actions.ToSequence(), restrict);
    }

    /// <summary>Parse an <c>ALTER FUNCTION</c> or <c>ALTER AGGREGATE</c> statement.</summary>
    public Statement ParseAlterFunction(AlterFunctionKind kind)
    {
        FunctionDesc function;
        bool aggregateStar;
        Sequence<OperateFunctionArg>? aggregateOrderBy;
        switch (kind)
        {
            case AlterFunctionKind.Function:
                (function, aggregateStar, aggregateOrderBy) = (ParseFunctionDesc(), false, null);
                break;
            case AlterFunctionKind.Aggregate:
                (function, aggregateStar, aggregateOrderBy) = ParseAlterAggregateSignature();
                break;
            default:
                throw new InvalidOperationException($"Variante de AlterFunctionKind não tratada: {kind.GetType().Name}");
        }

        AlterFunctionOperation operation;
        if (ParseKeywords(Keyword.RENAME, Keyword.TO))
        {
            var newName = ParseIdentifier();
            operation = new AlterFunctionOperation.RenameTo(NewName: newName);
        }
        else if (ParseKeywords(Keyword.OWNER, Keyword.TO))
        {
            operation = new AlterFunctionOperation.OwnerTo(ParseOwner());
        }
        else if (ParseKeywords(Keyword.SET, Keyword.SCHEMA))
        {
            operation = new AlterFunctionOperation.SetSchema(
                SchemaName: ParseObjectName(false));
        }
        else if (kind is AlterFunctionKind.Function && ParseKeyword(Keyword.NO))
        {
            if (!ParseKeyword(Keyword.DEPENDS))
            {
                return ExpectedRef<Statement>("DEPENDS after NO", PeekTokenRef());
            }
            ExpectKeywords(Keyword.ON, Keyword.EXTENSION);
            operation = new AlterFunctionOperation.DependsOnExtension(
                No: true,
                ExtensionName: ParseObjectName(false));
        }
        else if (kind is AlterFunctionKind.Function
            && ParseKeyword(Keyword.DEPENDS))
        {
            ExpectKeywords(Keyword.ON, Keyword.EXTENSION);
            operation = new AlterFunctionOperation.DependsOnExtension(
                No: false,
                ExtensionName: ParseObjectName(false));
        }
        else if (kind is AlterFunctionKind.Function)
        {
            var (actions, restrict) = ParseAlterFunctionActions();
            operation = new AlterFunctionOperation.Actions(ActionsValue: actions, Restrict: restrict);
        }
        else
        {
            return ExpectedRef<Statement>(
                "RENAME TO, OWNER TO, or SET SCHEMA after ALTER AGGREGATE",
                PeekTokenRef());
        }

        return new Statement.AlterFunction(new AlterFunction(
            Kind: kind,
            Function: function,
            AggregateOrderBy: aggregateOrderBy,
            AggregateStar: aggregateStar,
            Operation: operation));
    }

    /// <summary>Parse a <see cref="Statement.AlterTable"/></summary>
    public Statement ParseAlterTable(bool iceberg)
    {
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
        var only = ParseKeyword(Keyword.ONLY); // [ ONLY ]
        var tableName = ParseObjectName(false);
        var onCluster = ParseOptionalOnCluster();
        var operations = ParseCommaSeparated(p => p.ParseAlterTableOperation());

        HiveSetLocation? location = null;
        if (ParseKeyword(Keyword.LOCATION))
        {
            location = new HiveSetLocation(
                HasSet: false,
                Location: ParseIdentifier());
        }
        else if (ParseKeywords(Keyword.SET, Keyword.LOCATION))
        {
            location = new HiveSetLocation(
                HasSet: true,
                Location: ParseIdentifier());
        }

        TokenWithSpan endToken;
        if (PeekTokenRef().Token is Token.SemiColon)
        {
            endToken = PeekTokenRef();
        }
        else
        {
            endToken = GetCurrentToken();
        }

        return Statement.From(new AlterTable(
            Name: tableName,
            IfExists: ifExists,
            Only: only,
            Operations: operations,
            Location: location,
            OnCluster: onCluster,
            TableType: iceberg ? AlterTableType.Iceberg : null,
            EndToken: new AttachedToken(endToken)));
    }

    /// <summary>Parse an <c>ALTER VIEW</c> statement.</summary>
    public Statement ParseAlterView()
    {
        var name = ParseObjectName(false);
        var columns = ParseParenthesizedColumnList(IsOptional.Optional, false);

        var withOptions = ParseOptions(Keyword.WITH);

        ExpectKeywordIs(Keyword.AS);
        var query = ParseQuery();

        return new Statement.AlterView(
            Name: name,
            Columns: columns,
            Query: query,
            WithOptions: withOptions);
    }

    /// <summary>Parse a <see cref="Statement.AlterType"/></summary>
    public Statement ParseAlterType()
    {
        var name = ParseObjectName(false);

        if (ParseKeywords(Keyword.RENAME, Keyword.TO))
        {
            var newName = ParseIdentifier();
            return new Statement.AlterType(new AlterType(
                Name: name,
                Operation: new AlterTypeOperation.Rename(new AlterTypeRename(NewName: newName))));
        }
        else if (ParseKeywords(Keyword.ADD, Keyword.VALUE))
        {
            var ifNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
            var newEnumValue = ParseIdentifier();
            AlterTypeAddValuePosition? position;
            if (ParseKeyword(Keyword.BEFORE))
            {
                position = new AlterTypeAddValuePosition.Before(ParseIdentifier());
            }
            else if (ParseKeyword(Keyword.AFTER))
            {
                position = new AlterTypeAddValuePosition.After(ParseIdentifier());
            }
            else
            {
                position = null;
            }

            return new Statement.AlterType(new AlterType(
                Name: name,
                Operation: new AlterTypeOperation.AddValue(new AlterTypeAddValue(
                    IfNotExists: ifNotExists,
                    Value: newEnumValue,
                    Position: position))));
        }
        else if (ParseKeywords(Keyword.RENAME, Keyword.VALUE))
        {
            var existingEnumValue = ParseIdentifier();
            ExpectKeyword(Keyword.TO);
            var newEnumValue = ParseIdentifier();

            return new Statement.AlterType(new AlterType(
                Name: name,
                Operation: new AlterTypeOperation.RenameValue(new AlterTypeRenameValue(
                    From: existingEnumValue,
                    To: newEnumValue))));
        }
        else
        {
            return ExpectedRef<Statement>(
                "{RENAME TO | { RENAME | ADD } VALUE}",
                PeekTokenRef());
        }
    }

    /// <summary>Parse a <see cref="Statement.AlterCollation"/>.</summary>
    /// <remarks><see href="https://www.postgresql.org/docs/current/sql-altercollation.html">PostgreSQL Documentation</see></remarks>
    public AlterCollation ParseAlterCollation()
    {
        var name = ParseObjectName(false);
        AlterCollationOperation operation;
        if (ParseKeywords(Keyword.RENAME, Keyword.TO))
        {
            operation = new AlterCollationOperation.RenameTo(
                NewName: ParseIdentifier());
        }
        else if (ParseKeywords(Keyword.OWNER, Keyword.TO))
        {
            operation = new AlterCollationOperation.OwnerTo(ParseOwner());
        }
        else if (ParseKeywords(Keyword.SET, Keyword.SCHEMA))
        {
            operation = new AlterCollationOperation.SetSchema(
                SchemaName: ParseObjectName(false));
        }
        else if (ParseKeywords(Keyword.REFRESH, Keyword.VERSION))
        {
            operation = new AlterCollationOperation.RefreshVersion();
        }
        else
        {
            return ExpectedRef<AlterCollation>(
                "RENAME TO, OWNER TO, SET SCHEMA, or REFRESH VERSION after ALTER COLLATION",
                PeekTokenRef());
        }

        return new AlterCollation(Name: name, Operation: operation);
    }

    /// <summary>Parse a <see cref="Statement.AlterOperator"/></summary>
    /// <remarks><see href="https://www.postgresql.org/docs/current/sql-alteroperator.html">PostgreSQL Documentation</see></remarks>
    public AlterOperator ParseAlterOperator()
    {
        var name = ParseOperatorName();

        // Parse (left_type, right_type)
        ExpectToken(new Token.LParen());

        DataType? leftType;
        if (ParseKeyword(Keyword.NONE))
        {
            leftType = null;
        }
        else
        {
            leftType = ParseDataType();
        }

        ExpectToken(new Token.Comma());
        var rightType = ParseDataType();
        ExpectToken(new Token.RParen());

        // Parse the operation
        AlterOperatorOperation operation;
        if (ParseKeywords(Keyword.OWNER, Keyword.TO))
        {
            Owner owner;
            if (ParseKeyword(Keyword.CURRENT_ROLE))
            {
                owner = new Owner.CurrentRole();
            }
            else if (ParseKeyword(Keyword.CURRENT_USER))
            {
                owner = new Owner.CurrentUser();
            }
            else if (ParseKeyword(Keyword.SESSION_USER))
            {
                owner = new Owner.SessionUser();
            }
            else
            {
                owner = new Owner.Ident(ParseIdentifier());
            }
            operation = new AlterOperatorOperation.OwnerTo(owner);
        }
        else if (ParseKeywords(Keyword.SET, Keyword.SCHEMA))
        {
            var schemaName = ParseObjectName(false);
            operation = new AlterOperatorOperation.SetSchema(SchemaName: schemaName);
        }
        else if (ParseKeyword(Keyword.SET))
        {
            ExpectToken(new Token.LParen());

            var options = new List<OperatorOption>();
            while (true)
            {
                var keyword = ExpectOneOfKeywords(
                    Keyword.RESTRICT,
                    Keyword.JOIN,
                    Keyword.COMMUTATOR,
                    Keyword.NEGATOR,
                    Keyword.HASHES,
                    Keyword.MERGES);

                switch (keyword)
                {
                    case Keyword.RESTRICT:
                    {
                        ExpectToken(new Token.Eq());
                        ObjectName? procName;
                        if (ParseKeyword(Keyword.NONE))
                        {
                            procName = null;
                        }
                        else
                        {
                            procName = ParseObjectName(false);
                        }
                        options.Add(new OperatorOption.Restrict(procName));
                        break;
                    }
                    case Keyword.JOIN:
                    {
                        ExpectToken(new Token.Eq());
                        ObjectName? procName;
                        if (ParseKeyword(Keyword.NONE))
                        {
                            procName = null;
                        }
                        else
                        {
                            procName = ParseObjectName(false);
                        }
                        options.Add(new OperatorOption.Join(procName));
                        break;
                    }
                    case Keyword.COMMUTATOR:
                    {
                        ExpectToken(new Token.Eq());
                        var opName = ParseOperatorName();
                        options.Add(new OperatorOption.Commutator(opName));
                        break;
                    }
                    case Keyword.NEGATOR:
                    {
                        ExpectToken(new Token.Eq());
                        var opName = ParseOperatorName();
                        options.Add(new OperatorOption.Negator(opName));
                        break;
                    }
                    case Keyword.HASHES:
                        options.Add(new OperatorOption.Hashes());
                        break;
                    case Keyword.MERGES:
                        options.Add(new OperatorOption.Merges());
                        break;
                    case var unexpectedKeyword:
                        // `{unexpected_keyword}` = Display de Keyword (ToSql, decisão 8 da B2).
                        throw new ParserException(new ParserError.ParserErrorValue(
                            $"Internal parser error: unexpected keyword `{unexpectedKeyword.ToSql()}` in operator option"));
                }

                if (!ConsumeToken(new Token.Comma()))
                {
                    break;
                }
            }

            ExpectToken(new Token.RParen());
            operation = new AlterOperatorOperation.Set(Options: options.ToSequence());
        }
        else
        {
            return ExpectedRef<AlterOperator>(
                "OWNER TO, SET SCHEMA, or SET after ALTER OPERATOR",
                PeekTokenRef());
        }

        return new AlterOperator(
            Name: name,
            LeftType: leftType,
            RightType: rightType,
            Operation: operation);
    }

    /// <summary>Parse an operator item for ALTER OPERATOR FAMILY ADD operations</summary>
    internal OperatorFamilyItem ParseOperatorFamilyAddOperator()
    {
        var strategyNumber = ParseLiteralUint();
        var operatorName = ParseOperatorName();

        // Operator argument types (required for ALTER OPERATOR FAMILY)
        ExpectToken(new Token.LParen());
        var opTypes = ParseCommaSeparated(p => p.ParseDataType());
        ExpectToken(new Token.RParen());

        // Optional purpose
        OperatorPurpose? purpose;
        if (ParseKeyword(Keyword.FOR))
        {
            if (ParseKeyword(Keyword.SEARCH))
            {
                purpose = new OperatorPurpose.ForSearch();
            }
            else if (ParseKeywords(Keyword.ORDER, Keyword.BY))
            {
                var sortFamily = ParseObjectName(false);
                purpose = new OperatorPurpose.ForOrderBy(SortFamily: sortFamily);
            }
            else
            {
                return ExpectedRef<OperatorFamilyItem>("SEARCH or ORDER BY after FOR", PeekTokenRef());
            }
        }
        else
        {
            purpose = null;
        }

        return new OperatorFamilyItem.Operator(
            StrategyNumber: strategyNumber,
            OperatorName: operatorName,
            OpTypes: opTypes,
            Purpose: purpose);
    }

    /// <summary>Parse a function item for ALTER OPERATOR FAMILY ADD operations</summary>
    internal OperatorFamilyItem ParseOperatorFamilyAddFunction()
    {
        var supportNumber = ParseLiteralUint();

        // Optional operator types
        Sequence<DataType>? opTypes;
        if (ConsumeToken(new Token.LParen()) && PeekTokenRef().Token is not Token.RParen)
        {
            var types = ParseCommaSeparated(p => p.ParseDataType());
            ExpectToken(new Token.RParen());
            opTypes = types;
        }
        else if (ConsumeToken(new Token.LParen()))
        {
            ExpectToken(new Token.RParen());
            opTypes = Sequence<DataType>.Empty;
        }
        else
        {
            opTypes = null;
        }

        var functionName = ParseObjectName(false);

        // Function argument types
        Sequence<DataType> argumentTypes;
        if (ConsumeToken(new Token.LParen()))
        {
            if (PeekTokenRef().Token is Token.RParen)
            {
                ExpectToken(new Token.RParen());
                argumentTypes = Sequence<DataType>.Empty;
            }
            else
            {
                var types = ParseCommaSeparated(p => p.ParseDataType());
                ExpectToken(new Token.RParen());
                argumentTypes = types;
            }
        }
        else
        {
            argumentTypes = Sequence<DataType>.Empty;
        }

        return new OperatorFamilyItem.Function(
            SupportNumber: supportNumber,
            OpTypes: opTypes,
            FunctionName: functionName,
            ArgumentTypes: argumentTypes);
    }

    /// <summary>Parse an operator item for ALTER OPERATOR FAMILY DROP operations</summary>
    internal OperatorFamilyDropItem ParseOperatorFamilyDropOperator()
    {
        var strategyNumber = ParseLiteralUint();

        // Operator argument types (required for DROP)
        ExpectToken(new Token.LParen());
        var opTypes = ParseCommaSeparated(p => p.ParseDataType());
        ExpectToken(new Token.RParen());

        return new OperatorFamilyDropItem.Operator(
            StrategyNumber: strategyNumber,
            OpTypes: opTypes);
    }

    /// <summary>Parse a function item for ALTER OPERATOR FAMILY DROP operations</summary>
    internal OperatorFamilyDropItem ParseOperatorFamilyDropFunction()
    {
        var supportNumber = ParseLiteralUint();

        // Operator types (required for DROP)
        ExpectToken(new Token.LParen());
        var opTypes = ParseCommaSeparated(p => p.ParseDataType());
        ExpectToken(new Token.RParen());

        return new OperatorFamilyDropItem.Function(
            SupportNumber: supportNumber,
            OpTypes: opTypes);
    }

    /// <summary>Parse an operator family item for ADD operations (dispatches to operator or function parsing)</summary>
    internal OperatorFamilyItem ParseOperatorFamilyAddItem()
    {
        if (ParseKeyword(Keyword.OPERATOR))
        {
            return ParseOperatorFamilyAddOperator();
        }
        else if (ParseKeyword(Keyword.FUNCTION))
        {
            return ParseOperatorFamilyAddFunction();
        }
        else
        {
            return ExpectedRef<OperatorFamilyItem>("OPERATOR or FUNCTION", PeekTokenRef());
        }
    }

    /// <summary>Parse an operator family item for DROP operations (dispatches to operator or function parsing)</summary>
    internal OperatorFamilyDropItem ParseOperatorFamilyDropItem()
    {
        if (ParseKeyword(Keyword.OPERATOR))
        {
            return ParseOperatorFamilyDropOperator();
        }
        else if (ParseKeyword(Keyword.FUNCTION))
        {
            return ParseOperatorFamilyDropFunction();
        }
        else
        {
            return ExpectedRef<OperatorFamilyDropItem>("OPERATOR or FUNCTION", PeekTokenRef());
        }
    }

    /// <summary>
    /// Parse a <see cref="Statement.AlterOperatorFamily"/>
    /// See <see href="https://www.postgresql.org/docs/current/sql-alteropfamily.html"/>
    /// </summary>
    public AlterOperatorFamily ParseAlterOperatorFamily()
    {
        var name = ParseObjectName(false);
        ExpectKeyword(Keyword.USING);
        var @using = ParseIdentifier();

        AlterOperatorFamilyOperation operation;
        if (ParseKeyword(Keyword.ADD))
        {
            var items = ParseCommaSeparated(p => p.ParseOperatorFamilyAddItem());
            operation = new AlterOperatorFamilyOperation.Add(Items: items);
        }
        else if (ParseKeyword(Keyword.DROP))
        {
            var items = ParseCommaSeparated(p => p.ParseOperatorFamilyDropItem());
            operation = new AlterOperatorFamilyOperation.Drop(Items: items);
        }
        else if (ParseKeywords(Keyword.RENAME, Keyword.TO))
        {
            var newName = ParseObjectName(false);
            operation = new AlterOperatorFamilyOperation.RenameTo(NewName: newName);
        }
        else if (ParseKeywords(Keyword.OWNER, Keyword.TO))
        {
            var owner = ParseOwner();
            operation = new AlterOperatorFamilyOperation.OwnerTo(owner);
        }
        else if (ParseKeywords(Keyword.SET, Keyword.SCHEMA))
        {
            var schemaName = ParseObjectName(false);
            operation = new AlterOperatorFamilyOperation.SetSchema(SchemaName: schemaName);
        }
        else
        {
            return ExpectedRef<AlterOperatorFamily>(
                "ADD, DROP, RENAME TO, OWNER TO, or SET SCHEMA after ALTER OPERATOR FAMILY",
                PeekTokenRef());
        }

        return new AlterOperatorFamily(
            Name: name,
            Using: @using,
            Operation: operation);
    }

    /// <summary>Parse an <c>ALTER OPERATOR CLASS</c> statement.</summary>
    /// <remarks>Handles operations like <c>RENAME TO</c>, <c>OWNER TO</c>, and <c>SET SCHEMA</c>.</remarks>
    public AlterOperatorClass ParseAlterOperatorClass()
    {
        var name = ParseObjectName(false);
        ExpectKeyword(Keyword.USING);
        var @using = ParseIdentifier();

        AlterOperatorClassOperation operation;
        if (ParseKeywords(Keyword.RENAME, Keyword.TO))
        {
            var newName = ParseObjectName(false);
            operation = new AlterOperatorClassOperation.RenameTo(NewName: newName);
        }
        else if (ParseKeywords(Keyword.OWNER, Keyword.TO))
        {
            var owner = ParseOwner();
            operation = new AlterOperatorClassOperation.OwnerTo(owner);
        }
        else if (ParseKeywords(Keyword.SET, Keyword.SCHEMA))
        {
            var schemaName = ParseObjectName(false);
            operation = new AlterOperatorClassOperation.SetSchema(SchemaName: schemaName);
        }
        else
        {
            return ExpectedRef<AlterOperatorClass>(
                "RENAME TO, OWNER TO, or SET SCHEMA after ALTER OPERATOR CLASS",
                PeekTokenRef());
        }

        return new AlterOperatorClass(
            Name: name,
            Using: @using,
            Operation: operation);
    }

    /// <summary>Parse an <c>ALTER SCHEMA</c> statement.</summary>
    /// <remarks>Supports operations such as setting options, renaming, adding/dropping replicas, and changing owner.</remarks>
    public Statement ParseAlterSchema()
    {
        ExpectKeywords(Keyword.ALTER, Keyword.SCHEMA);
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
        var name = ParseObjectName(false);
        AlterSchemaOperation operation;
        if (ParseKeywords(Keyword.SET, Keyword.OPTIONS))
        {
            PrevToken();
            var options = ParseOptions(Keyword.OPTIONS);
            operation = new AlterSchemaOperation.SetOptionsParens(Options: options);
        }
        else if (ParseKeywords(Keyword.SET, Keyword.DEFAULT, Keyword.COLLATE))
        {
            var collate = ParseExpr();
            operation = new AlterSchemaOperation.SetDefaultCollate(Collate: collate);
        }
        else if (ParseKeywords(Keyword.ADD, Keyword.REPLICA))
        {
            var replica = ParseIdentifier();
            Sequence<SqlOption>? options;
            if (PeekKeyword(Keyword.OPTIONS))
            {
                options = ParseOptions(Keyword.OPTIONS);
            }
            else
            {
                options = null;
            }
            operation = new AlterSchemaOperation.AddReplica(Replica: replica, Options: options);
        }
        else if (ParseKeywords(Keyword.DROP, Keyword.REPLICA))
        {
            var replica = ParseIdentifier();
            operation = new AlterSchemaOperation.DropReplica(Replica: replica);
        }
        else if (ParseKeywords(Keyword.RENAME, Keyword.TO))
        {
            var newName = ParseObjectName(false);
            operation = new AlterSchemaOperation.Rename(Name: newName);
        }
        else if (ParseKeywords(Keyword.OWNER, Keyword.TO))
        {
            var owner = ParseOwner();
            operation = new AlterSchemaOperation.OwnerTo(Owner: owner);
        }
        else
        {
            return ExpectedRef<Statement>("ALTER SCHEMA operation", PeekTokenRef());
        }
        return new Statement.AlterSchema(new AlterSchema(
            Name: name,
            IfExists: ifExists,
            Operations: Sequence.Of(operation)));
    }
}
