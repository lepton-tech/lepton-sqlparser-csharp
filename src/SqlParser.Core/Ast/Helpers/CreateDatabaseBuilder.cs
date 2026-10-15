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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/helpers/stmt_create_database.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>Builder for create database statement variant (<c>Statement::CreateDatabase</c>).</summary>
/// <remarks>
/// This structure helps building and accessing a create database with more ease, without needing to:
/// match the enum itself a lot of times; or moving a lot of variables around the code.
/// <code>
/// var builder = new CreateDatabaseBuilder(ObjectName.From(Sequence.Of(new Ident("database_name"))))
///    .SetIfNotExists(true);
/// // You can access internal elements with ease
/// // builder.IfNotExists == true
/// // Convert to a statement
/// // builder.Build().ToString() == "CREATE DATABASE IF NOT EXISTS database_name"
/// </code>
/// </remarks>
// Mesmo padrão de CreateTableBuilder: `sealed record` com propriedades mutáveis
// (igualdade, hash e cópia `with { }` = `clone()`); métodos com o nome de um campo recebem o
// prefixo `Set` (CS0102); o campo `clone` vira `CloneValue` (CS8859).
public sealed partial record CreateDatabaseBuilder
{
    /// <summary>
    /// Create a new <c>CreateDatabaseBuilder</c> with the given database name.
    /// </summary>
    /// <param name="name">The name of the database to be created.</param>
    public CreateDatabaseBuilder(ObjectName name)
    {
        DbName = name;
        IfNotExists = false;
        Location = null;
        ManagedLocation = null;
        OrReplace = false;
        Transient = false;
        CloneValue = null;
        DataRetentionTimeInDays = null;
        MaxDataExtensionTimeInDays = null;
        ExternalVolume = null;
        Catalog = null;
        ReplaceInvalidCharacters = null;
        DefaultDdlCollation = null;
        StorageSerializationPolicy = null;
        Comment = null;
        DefaultCharset = null;
        DefaultCollation = null;
        CatalogSync = null;
        CatalogSyncNamespaceMode = null;
        CatalogSyncNamespaceFlattenDelimiter = null;
        WithTags = null;
        WithContacts = null;
    }

    /// <summary>The database name to create.</summary>
    public ObjectName DbName { get; set; }

    /// <summary>Whether <c>IF NOT EXISTS</c> was specified.</summary>
    public bool IfNotExists { get; set; }

    /// <summary>Optional storage location for the database.</summary>
    public string? Location { get; set; }

    /// <summary>Optional managed storage location.</summary>
    public string? ManagedLocation { get; set; }

    /// <summary>Whether <c>OR REPLACE</c> was specified.</summary>
    public bool OrReplace { get; set; }

    /// <summary>Whether the database is <c>TRANSIENT</c>.</summary>
    public bool Transient { get; set; }

    /// <summary>Optional <c>CLONE</c> source object name.</summary>
    // Rust: `clone` (CS8859).
    public ObjectName? CloneValue { get; set; }

    /// <summary>Optional data retention time in days.</summary>
    public ulong? DataRetentionTimeInDays { get; set; }

    /// <summary>Optional max data extension time in days.</summary>
    public ulong? MaxDataExtensionTimeInDays { get; set; }

    /// <summary>Optional external volume identifier.</summary>
    public string? ExternalVolume { get; set; }

    /// <summary>Optional catalog name.</summary>
    public string? Catalog { get; set; }

    /// <summary>Whether to replace invalid characters.</summary>
    public bool? ReplaceInvalidCharacters { get; set; }

    /// <summary>Optional default DDL collation.</summary>
    public string? DefaultDdlCollation { get; set; }

    /// <summary>Optional storage serialization policy.</summary>
    public StorageSerializationPolicy? StorageSerializationPolicy { get; set; }

    /// <summary>Optional comment attached to the database.</summary>
    public string? Comment { get; set; }

    /// <summary>Optional default character set (MySQL).</summary>
    /// <remarks><see href="https://dev.mysql.com/doc/refman/8.4/en/create-database.html"/></remarks>
    public string? DefaultCharset { get; set; }

    /// <summary>Optional default collation (MySQL).</summary>
    /// <remarks><see href="https://dev.mysql.com/doc/refman/8.4/en/create-database.html"/></remarks>
    public string? DefaultCollation { get; set; }

    /// <summary>Optional catalog sync configuration.</summary>
    public string? CatalogSync { get; set; }

    /// <summary>Optional catalog sync namespace mode.</summary>
    public CatalogSyncNamespaceMode? CatalogSyncNamespaceMode { get; set; }

    /// <summary>Optional namespace flatten delimiter for catalog sync.</summary>
    public string? CatalogSyncNamespaceFlattenDelimiter { get; set; }

    /// <summary>Optional tags attached to the database.</summary>
    public Sequence<Tag>? WithTags { get; set; }

    /// <summary>Optional contact entries associated with the database.</summary>
    public Sequence<ContactEntry>? WithContacts { get; set; }

    /// <summary>Set the location for the database.</summary>
    public CreateDatabaseBuilder SetLocation(string? location)
    {
        Location = location;
        return this;
    }

    /// <summary>Set the managed location for the database.</summary>
    public CreateDatabaseBuilder SetManagedLocation(string? managedLocation)
    {
        ManagedLocation = managedLocation;
        return this;
    }

    /// <summary>Set whether this is an <c>OR REPLACE</c> operation.</summary>
    public CreateDatabaseBuilder SetOrReplace(bool orReplace)
    {
        OrReplace = orReplace;
        return this;
    }

    /// <summary>Set whether this is a transient database.</summary>
    public CreateDatabaseBuilder SetTransient(bool transient)
    {
        Transient = transient;
        return this;
    }

    /// <summary>Set whether to use <c>IF NOT EXISTS</c>.</summary>
    public CreateDatabaseBuilder SetIfNotExists(bool ifNotExists)
    {
        IfNotExists = ifNotExists;
        return this;
    }

    /// <summary>Set the clone clause for the database.</summary>
    public CreateDatabaseBuilder CloneClause(ObjectName? clone)
    {
        CloneValue = clone;
        return this;
    }

    /// <summary>Set the data retention time in days.</summary>
    public CreateDatabaseBuilder SetDataRetentionTimeInDays(ulong? dataRetentionTimeInDays)
    {
        DataRetentionTimeInDays = dataRetentionTimeInDays;
        return this;
    }

    /// <summary>Set the maximum data extension time in days.</summary>
    public CreateDatabaseBuilder SetMaxDataExtensionTimeInDays(ulong? maxDataExtensionTimeInDays)
    {
        MaxDataExtensionTimeInDays = maxDataExtensionTimeInDays;
        return this;
    }

    /// <summary>Set the external volume for the database.</summary>
    public CreateDatabaseBuilder SetExternalVolume(string? externalVolume)
    {
        ExternalVolume = externalVolume;
        return this;
    }

    /// <summary>Set the catalog for the database.</summary>
    public CreateDatabaseBuilder SetCatalog(string? catalog)
    {
        Catalog = catalog;
        return this;
    }

    /// <summary>Set whether to replace invalid characters.</summary>
    public CreateDatabaseBuilder SetReplaceInvalidCharacters(bool? replaceInvalidCharacters)
    {
        ReplaceInvalidCharacters = replaceInvalidCharacters;
        return this;
    }

    /// <summary>Set the default DDL collation.</summary>
    public CreateDatabaseBuilder SetDefaultDdlCollation(string? defaultDdlCollation)
    {
        DefaultDdlCollation = defaultDdlCollation;
        return this;
    }

    /// <summary>Set the storage serialization policy.</summary>
    public CreateDatabaseBuilder SetStorageSerializationPolicy(StorageSerializationPolicy? storageSerializationPolicy)
    {
        StorageSerializationPolicy = storageSerializationPolicy;
        return this;
    }

    /// <summary>Set the comment for the database.</summary>
    public CreateDatabaseBuilder SetComment(string? comment)
    {
        Comment = comment;
        return this;
    }

    /// <summary>Set the default character set for the database.</summary>
    public CreateDatabaseBuilder SetDefaultCharset(string? defaultCharset)
    {
        DefaultCharset = defaultCharset;
        return this;
    }

    /// <summary>Set the default collation for the database.</summary>
    public CreateDatabaseBuilder SetDefaultCollation(string? defaultCollation)
    {
        DefaultCollation = defaultCollation;
        return this;
    }

    /// <summary>Set the catalog sync for the database.</summary>
    public CreateDatabaseBuilder SetCatalogSync(string? catalogSync)
    {
        CatalogSync = catalogSync;
        return this;
    }

    /// <summary>Set the catalog sync namespace mode for the database.</summary>
    public CreateDatabaseBuilder SetCatalogSyncNamespaceMode(CatalogSyncNamespaceMode? catalogSyncNamespaceMode)
    {
        CatalogSyncNamespaceMode = catalogSyncNamespaceMode;
        return this;
    }

    /// <summary>Set the catalog sync namespace flatten delimiter for the database.</summary>
    public CreateDatabaseBuilder SetCatalogSyncNamespaceFlattenDelimiter(string? catalogSyncNamespaceFlattenDelimiter)
    {
        CatalogSyncNamespaceFlattenDelimiter = catalogSyncNamespaceFlattenDelimiter;
        return this;
    }

    /// <summary>Set the tags for the database.</summary>
    public CreateDatabaseBuilder SetWithTags(Sequence<Tag>? withTags)
    {
        WithTags = withTags;
        return this;
    }

    /// <summary>Set the contacts for the database.</summary>
    public CreateDatabaseBuilder SetWithContacts(Sequence<ContactEntry>? withContacts)
    {
        WithContacts = withContacts;
        return this;
    }

    /// <summary>Build the <c>CREATE DATABASE</c> statement.</summary>
    public Statement Build() =>
        new Statement.CreateDatabase(
            DbName: DbName,
            IfNotExists: IfNotExists,
            Location: Location,
            ManagedLocation: ManagedLocation,
            OrReplace: OrReplace,
            Transient: Transient,
            CloneValue: CloneValue,
            DataRetentionTimeInDays: DataRetentionTimeInDays,
            MaxDataExtensionTimeInDays: MaxDataExtensionTimeInDays,
            ExternalVolume: ExternalVolume,
            Catalog: Catalog,
            ReplaceInvalidCharacters: ReplaceInvalidCharacters,
            DefaultDdlCollation: DefaultDdlCollation,
            StorageSerializationPolicy: StorageSerializationPolicy,
            Comment: Comment,
            DefaultCharset: DefaultCharset,
            DefaultCollation: DefaultCollation,
            CatalogSync: CatalogSync,
            CatalogSyncNamespaceMode: CatalogSyncNamespaceMode,
            CatalogSyncNamespaceFlattenDelimiter: CatalogSyncNamespaceFlattenDelimiter,
            WithTags: WithTags,
            WithContacts: WithContacts);

    /// <exception cref="ParserException">The statement is not a <c>Statement::CreateDatabase</c>.</exception>
    // Rust: `impl TryFrom<Statement> for CreateDatabaseBuilder`. O `Err(ParserError)` devolvido quando a
    // instrução não é `Statement::CreateDatabase` vira ParserException (convencoes.md §8).
    public static CreateDatabaseBuilder TryFrom(Statement stmt)
    {
        switch (stmt)
        {
            case Statement.CreateDatabase s:
                return new CreateDatabaseBuilder(s.DbName)
                {
                    IfNotExists = s.IfNotExists,
                    Location = s.Location,
                    ManagedLocation = s.ManagedLocation,
                    OrReplace = s.OrReplace,
                    Transient = s.Transient,
                    CloneValue = s.CloneValue,
                    DataRetentionTimeInDays = s.DataRetentionTimeInDays,
                    MaxDataExtensionTimeInDays = s.MaxDataExtensionTimeInDays,
                    ExternalVolume = s.ExternalVolume,
                    Catalog = s.Catalog,
                    ReplaceInvalidCharacters = s.ReplaceInvalidCharacters,
                    DefaultDdlCollation = s.DefaultDdlCollation,
                    StorageSerializationPolicy = s.StorageSerializationPolicy,
                    Comment = s.Comment,
                    DefaultCharset = s.DefaultCharset,
                    DefaultCollation = s.DefaultCollation,
                    CatalogSync = s.CatalogSync,
                    CatalogSyncNamespaceMode = s.CatalogSyncNamespaceMode,
                    CatalogSyncNamespaceFlattenDelimiter = s.CatalogSyncNamespaceFlattenDelimiter,
                    WithTags = s.WithTags,
                    WithContacts = s.WithContacts,
                };
            default:
                throw new ParserException(new ParserError.ParserErrorValue(
                    $"Expected create database statement, but received: {stmt}"));
        }
    }
}
