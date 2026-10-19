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

namespace Lepton.SqlParser.Core.Ast;

// Parte (partial) do tipo `Statement` (unidade A2, Statement.cs) com os `impl From<X> for Statement`
// do fim de src/ast/mod.rs (linhas 12284–12636), na ordem do Rust: `Statement.From(x)`
// (convencoes.md §13, caso 7). Os tipos dos parâmetros são qualificados com `Ast.` porque as
// variantes aninhadas de `Statement` escondem os tipos de topo homônimos (caso 38).
public abstract partial record Statement
{
    // Rust: `impl From<Set> for Statement`.
    public static Statement From(Ast.Set s) => new Set(s);

    // Rust: `impl From<Query> for Statement` (`Box::new(q).into()`) e
    // `impl From<Box<Query>> for Statement`. Com `Box<T>` → `T`, as duas
    // conversões têm a mesma assinatura em C# e viram esta única sobrecarga.
    public static Statement From(Ast.Query q) => new Query(q);

    // Rust: `impl From<Insert> for Statement`.
    public static Statement From(Ast.Insert i) => new Insert(i);

    // Rust: `impl From<Update> for Statement`.
    public static Statement From(Ast.Update u) => new Update(u);

    // Rust: `impl From<CreateView> for Statement`.
    public static Statement From(Ast.CreateView cv) => new CreateView(cv);

    // Rust: `impl From<CreateRole> for Statement`.
    public static Statement From(Ast.CreateRole cr) => new CreateRole(cr);

    // Rust: `impl From<AlterTable> for Statement`.
    public static Statement From(Ast.AlterTable at) => new AlterTable(at);

    // Rust: `impl From<DropFunction> for Statement`.
    public static Statement From(Ast.DropFunction df) => new DropFunction(df);

    // Rust: `impl From<CreateExtension> for Statement`.
    public static Statement From(Ast.CreateExtension ce) => new CreateExtension(ce);

    // Rust: `impl From<CreateCollation> for Statement`.
    public static Statement From(Ast.CreateCollation c) => new CreateCollation(c);

    // Rust: `impl From<DropExtension> for Statement`.
    public static Statement From(Ast.DropExtension de) => new DropExtension(de);

    // Rust: `impl From<CaseStatement> for Statement`.
    public static Statement From(Ast.CaseStatement c) => new Case(c);

    // Rust: `impl From<IfStatement> for Statement`.
    public static Statement From(Ast.IfStatement i) => new If(i);

    // Rust: `impl From<WhileStatement> for Statement`.
    public static Statement From(Ast.WhileStatement w) => new While(w);

    // Rust: `impl From<RaiseStatement> for Statement`.
    public static Statement From(Ast.RaiseStatement r) => new Raise(r);

    // Rust: `impl From<ThrowStatement> for Statement`.
    public static Statement From(Ast.ThrowStatement t) => new Throw(t);

    // Rust: `impl From<Function> for Statement`.
    public static Statement From(Ast.Function f) => new Call(f);

    // Rust: `impl From<OpenStatement> for Statement`.
    public static Statement From(Ast.OpenStatement o) => new Open(o);

    // Rust: `impl From<Delete> for Statement`.
    public static Statement From(Ast.Delete d) => new Delete(d);

    // Rust: `impl From<CreateTable> for Statement`.
    public static Statement From(Ast.CreateTable c) => new CreateTable(c);

    // Rust: `impl From<CreateIndex> for Statement`.
    public static Statement From(Ast.CreateIndex c) => new CreateIndex(c);

    // Rust: `impl From<CreateServerStatement> for Statement`.
    public static Statement From(Ast.CreateServerStatement c) => new CreateServer(c);

    // Rust: `impl From<CreateConnector> for Statement`.
    public static Statement From(Ast.CreateConnector c) => new CreateConnector(c);

    // Rust: `impl From<CreateOperator> for Statement`.
    public static Statement From(Ast.CreateOperator c) => new CreateOperator(c);

    // Rust: `impl From<CreateOperatorFamily> for Statement`.
    public static Statement From(Ast.CreateOperatorFamily c) => new CreateOperatorFamily(c);

    // Rust: `impl From<CreateOperatorClass> for Statement`.
    public static Statement From(Ast.CreateOperatorClass c) => new CreateOperatorClass(c);

    // Rust: `impl From<CreateTextSearch> for Statement`.
    public static Statement From(Ast.CreateTextSearch c) => new CreateTextSearch(c);

    // Rust: `impl From<AlterSchema> for Statement`.
    public static Statement From(Ast.AlterSchema a) => new AlterSchema(a);

    // Rust: `impl From<AlterFunction> for Statement`.
    public static Statement From(Ast.AlterFunction a) => new AlterFunction(a);

    // Rust: `impl From<AlterType> for Statement`.
    public static Statement From(Ast.AlterType a) => new AlterType(a);

    // Rust: `impl From<AlterCollation> for Statement`.
    public static Statement From(Ast.AlterCollation a) => new AlterCollation(a);

    // Rust: `impl From<AlterOperator> for Statement`.
    public static Statement From(Ast.AlterOperator a) => new AlterOperator(a);

    // Rust: `impl From<AlterOperatorFamily> for Statement`.
    public static Statement From(Ast.AlterOperatorFamily a) => new AlterOperatorFamily(a);

    // Rust: `impl From<AlterOperatorClass> for Statement`.
    public static Statement From(Ast.AlterOperatorClass a) => new AlterOperatorClass(a);

    // Rust: `impl From<AlterTextSearch> for Statement`.
    public static Statement From(Ast.AlterTextSearch a) => new AlterTextSearch(a);

    // Rust: `impl From<Merge> for Statement`.
    public static Statement From(Ast.Merge m) => new Merge(m);

    // Rust: `impl From<AlterUser> for Statement`.
    public static Statement From(Ast.AlterUser a) => new AlterUser(a);

    // Rust: `impl From<DropDomain> for Statement`.
    public static Statement From(Ast.DropDomain d) => new DropDomain(d);

    // Rust: `impl From<ShowCharset> for Statement`.
    public static Statement From(Ast.ShowCharset s) => new ShowCharset(s);

    // Rust: `impl From<ShowObjects> for Statement`.
    public static Statement From(Ast.ShowObjects s) => new ShowObjects(s);

    // Rust: `impl From<Use> for Statement`.
    public static Statement From(Ast.Use u) => new Use(u);

    // Rust: `impl From<CreateFunction> for Statement`.
    public static Statement From(Ast.CreateFunction c) => new CreateFunction(c);

    // Rust: `impl From<CreateTrigger> for Statement`.
    public static Statement From(Ast.CreateTrigger c) => new CreateTrigger(c);

    // Rust: `impl From<DropTrigger> for Statement`.
    public static Statement From(Ast.DropTrigger d) => new DropTrigger(d);

    // Rust: `impl From<DropOperator> for Statement`.
    public static Statement From(Ast.DropOperator d) => new DropOperator(d);

    // Rust: `impl From<DropOperatorFamily> for Statement`.
    public static Statement From(Ast.DropOperatorFamily d) => new DropOperatorFamily(d);

    // Rust: `impl From<DropOperatorClass> for Statement`.
    public static Statement From(Ast.DropOperatorClass d) => new DropOperatorClass(d);

    // Rust: `impl From<DenyStatement> for Statement`.
    public static Statement From(Ast.DenyStatement d) => new Deny(d);

    // Rust: `impl From<CreateDomain> for Statement`.
    public static Statement From(Ast.CreateDomain c) => new CreateDomain(c);

    // Rust: `impl From<RenameTable> for Statement` (`vec![r].into()`).
    public static Statement From(Ast.RenameTable r) => From(Sequence.Of(r));

    // Rust: `impl From<Vec<RenameTable>> for Statement`.
    public static Statement From(Sequence<Ast.RenameTable> r) => new RenameTable(r);

    // Rust: `impl From<PrintStatement> for Statement`.
    public static Statement From(Ast.PrintStatement p) => new Print(p);

    // Rust: `impl From<ReturnStatement> for Statement`.
    public static Statement From(Ast.ReturnStatement r) => new Return(r);

    // Rust: `impl From<ExportData> for Statement`.
    public static Statement From(Ast.ExportData e) => new ExportData(e);

    // Rust: `impl From<CreateUser> for Statement`.
    public static Statement From(Ast.CreateUser c) => new CreateUser(c);

    // Rust: `impl From<CreateWarehouse> for Statement`.
    public static Statement From(Ast.CreateWarehouse c) => new CreateWarehouse(c);

    // Rust: `impl From<VacuumStatement> for Statement`.
    public static Statement From(Ast.VacuumStatement v) => new Vacuum(v);

    // Rust: `impl From<ResetStatement> for Statement`.
    public static Statement From(Ast.ResetStatement r) => new Reset(r);
}
