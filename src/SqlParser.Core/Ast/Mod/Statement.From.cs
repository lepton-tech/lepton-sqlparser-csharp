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

namespace SqlParser.Core.Ast;

// Parte (partial) do tipo `Statement` (unidade A2, Statement.cs) com os `impl From<X> for Statement`
// do fim de src/ast/mod.rs (linhas 12284–12636), na ordem do Rust: `Statement.From(x)`
// (convencoes.md §13, caso 7). Os tipos dos parâmetros são qualificados com `Ast.` porque as
// variantes aninhadas de `Statement` escondem os tipos de topo homônimos (caso 38).
public abstract partial record Statement
{
    /// <summary>Rust: <c>impl From&lt;Set&gt; for Statement</c>.</summary>
    public static Statement From(Ast.Set s) => new Set(s);

    /// <summary>
    /// Rust: <c>impl From&lt;Query&gt; for Statement</c> (<c>Box::new(q).into()</c>) e
    /// <c>impl From&lt;Box&lt;Query&gt;&gt; for Statement</c>. Com <c>Box&lt;T&gt;</c> → <c>T</c>, as duas
    /// conversões têm a mesma assinatura em C# e viram esta única sobrecarga.
    /// </summary>
    public static Statement From(Ast.Query q) => new Query(q);

    /// <summary>Rust: <c>impl From&lt;Insert&gt; for Statement</c>.</summary>
    public static Statement From(Ast.Insert i) => new Insert(i);

    /// <summary>Rust: <c>impl From&lt;Update&gt; for Statement</c>.</summary>
    public static Statement From(Ast.Update u) => new Update(u);

    /// <summary>Rust: <c>impl From&lt;CreateView&gt; for Statement</c>.</summary>
    public static Statement From(Ast.CreateView cv) => new CreateView(cv);

    /// <summary>Rust: <c>impl From&lt;CreateRole&gt; for Statement</c>.</summary>
    public static Statement From(Ast.CreateRole cr) => new CreateRole(cr);

    /// <summary>Rust: <c>impl From&lt;AlterTable&gt; for Statement</c>.</summary>
    public static Statement From(Ast.AlterTable at) => new AlterTable(at);

    /// <summary>Rust: <c>impl From&lt;DropFunction&gt; for Statement</c>.</summary>
    public static Statement From(Ast.DropFunction df) => new DropFunction(df);

    /// <summary>Rust: <c>impl From&lt;CreateExtension&gt; for Statement</c>.</summary>
    public static Statement From(Ast.CreateExtension ce) => new CreateExtension(ce);

    /// <summary>Rust: <c>impl From&lt;CreateCollation&gt; for Statement</c>.</summary>
    public static Statement From(Ast.CreateCollation c) => new CreateCollation(c);

    /// <summary>Rust: <c>impl From&lt;DropExtension&gt; for Statement</c>.</summary>
    public static Statement From(Ast.DropExtension de) => new DropExtension(de);

    /// <summary>Rust: <c>impl From&lt;CaseStatement&gt; for Statement</c>.</summary>
    public static Statement From(Ast.CaseStatement c) => new Case(c);

    /// <summary>Rust: <c>impl From&lt;IfStatement&gt; for Statement</c>.</summary>
    public static Statement From(Ast.IfStatement i) => new If(i);

    /// <summary>Rust: <c>impl From&lt;WhileStatement&gt; for Statement</c>.</summary>
    public static Statement From(Ast.WhileStatement w) => new While(w);

    /// <summary>Rust: <c>impl From&lt;RaiseStatement&gt; for Statement</c>.</summary>
    public static Statement From(Ast.RaiseStatement r) => new Raise(r);

    /// <summary>Rust: <c>impl From&lt;ThrowStatement&gt; for Statement</c>.</summary>
    public static Statement From(Ast.ThrowStatement t) => new Throw(t);

    /// <summary>Rust: <c>impl From&lt;Function&gt; for Statement</c>.</summary>
    public static Statement From(Ast.Function f) => new Call(f);

    /// <summary>Rust: <c>impl From&lt;OpenStatement&gt; for Statement</c>.</summary>
    public static Statement From(Ast.OpenStatement o) => new Open(o);

    /// <summary>Rust: <c>impl From&lt;Delete&gt; for Statement</c>.</summary>
    public static Statement From(Ast.Delete d) => new Delete(d);

    /// <summary>Rust: <c>impl From&lt;CreateTable&gt; for Statement</c>.</summary>
    public static Statement From(Ast.CreateTable c) => new CreateTable(c);

    /// <summary>Rust: <c>impl From&lt;CreateIndex&gt; for Statement</c>.</summary>
    public static Statement From(Ast.CreateIndex c) => new CreateIndex(c);

    /// <summary>Rust: <c>impl From&lt;CreateServerStatement&gt; for Statement</c>.</summary>
    public static Statement From(Ast.CreateServerStatement c) => new CreateServer(c);

    /// <summary>Rust: <c>impl From&lt;CreateConnector&gt; for Statement</c>.</summary>
    public static Statement From(Ast.CreateConnector c) => new CreateConnector(c);

    /// <summary>Rust: <c>impl From&lt;CreateOperator&gt; for Statement</c>.</summary>
    public static Statement From(Ast.CreateOperator c) => new CreateOperator(c);

    /// <summary>Rust: <c>impl From&lt;CreateOperatorFamily&gt; for Statement</c>.</summary>
    public static Statement From(Ast.CreateOperatorFamily c) => new CreateOperatorFamily(c);

    /// <summary>Rust: <c>impl From&lt;CreateOperatorClass&gt; for Statement</c>.</summary>
    public static Statement From(Ast.CreateOperatorClass c) => new CreateOperatorClass(c);

    /// <summary>Rust: <c>impl From&lt;CreateTextSearch&gt; for Statement</c>.</summary>
    public static Statement From(Ast.CreateTextSearch c) => new CreateTextSearch(c);

    /// <summary>Rust: <c>impl From&lt;AlterSchema&gt; for Statement</c>.</summary>
    public static Statement From(Ast.AlterSchema a) => new AlterSchema(a);

    /// <summary>Rust: <c>impl From&lt;AlterFunction&gt; for Statement</c>.</summary>
    public static Statement From(Ast.AlterFunction a) => new AlterFunction(a);

    /// <summary>Rust: <c>impl From&lt;AlterType&gt; for Statement</c>.</summary>
    public static Statement From(Ast.AlterType a) => new AlterType(a);

    /// <summary>Rust: <c>impl From&lt;AlterCollation&gt; for Statement</c>.</summary>
    public static Statement From(Ast.AlterCollation a) => new AlterCollation(a);

    /// <summary>Rust: <c>impl From&lt;AlterOperator&gt; for Statement</c>.</summary>
    public static Statement From(Ast.AlterOperator a) => new AlterOperator(a);

    /// <summary>Rust: <c>impl From&lt;AlterOperatorFamily&gt; for Statement</c>.</summary>
    public static Statement From(Ast.AlterOperatorFamily a) => new AlterOperatorFamily(a);

    /// <summary>Rust: <c>impl From&lt;AlterOperatorClass&gt; for Statement</c>.</summary>
    public static Statement From(Ast.AlterOperatorClass a) => new AlterOperatorClass(a);

    /// <summary>Rust: <c>impl From&lt;AlterTextSearch&gt; for Statement</c>.</summary>
    public static Statement From(Ast.AlterTextSearch a) => new AlterTextSearch(a);

    /// <summary>Rust: <c>impl From&lt;Merge&gt; for Statement</c>.</summary>
    public static Statement From(Ast.Merge m) => new Merge(m);

    /// <summary>Rust: <c>impl From&lt;AlterUser&gt; for Statement</c>.</summary>
    public static Statement From(Ast.AlterUser a) => new AlterUser(a);

    /// <summary>Rust: <c>impl From&lt;DropDomain&gt; for Statement</c>.</summary>
    public static Statement From(Ast.DropDomain d) => new DropDomain(d);

    /// <summary>Rust: <c>impl From&lt;ShowCharset&gt; for Statement</c>.</summary>
    public static Statement From(Ast.ShowCharset s) => new ShowCharset(s);

    /// <summary>Rust: <c>impl From&lt;ShowObjects&gt; for Statement</c>.</summary>
    public static Statement From(Ast.ShowObjects s) => new ShowObjects(s);

    /// <summary>Rust: <c>impl From&lt;Use&gt; for Statement</c>.</summary>
    public static Statement From(Ast.Use u) => new Use(u);

    /// <summary>Rust: <c>impl From&lt;CreateFunction&gt; for Statement</c>.</summary>
    public static Statement From(Ast.CreateFunction c) => new CreateFunction(c);

    /// <summary>Rust: <c>impl From&lt;CreateTrigger&gt; for Statement</c>.</summary>
    public static Statement From(Ast.CreateTrigger c) => new CreateTrigger(c);

    /// <summary>Rust: <c>impl From&lt;DropTrigger&gt; for Statement</c>.</summary>
    public static Statement From(Ast.DropTrigger d) => new DropTrigger(d);

    /// <summary>Rust: <c>impl From&lt;DropOperator&gt; for Statement</c>.</summary>
    public static Statement From(Ast.DropOperator d) => new DropOperator(d);

    /// <summary>Rust: <c>impl From&lt;DropOperatorFamily&gt; for Statement</c>.</summary>
    public static Statement From(Ast.DropOperatorFamily d) => new DropOperatorFamily(d);

    /// <summary>Rust: <c>impl From&lt;DropOperatorClass&gt; for Statement</c>.</summary>
    public static Statement From(Ast.DropOperatorClass d) => new DropOperatorClass(d);

    /// <summary>Rust: <c>impl From&lt;DenyStatement&gt; for Statement</c>.</summary>
    public static Statement From(Ast.DenyStatement d) => new Deny(d);

    /// <summary>Rust: <c>impl From&lt;CreateDomain&gt; for Statement</c>.</summary>
    public static Statement From(Ast.CreateDomain c) => new CreateDomain(c);

    /// <summary>Rust: <c>impl From&lt;RenameTable&gt; for Statement</c> (<c>vec![r].into()</c>).</summary>
    public static Statement From(Ast.RenameTable r) => From(Sequence.Of(r));

    /// <summary>Rust: <c>impl From&lt;Vec&lt;RenameTable&gt;&gt; for Statement</c>.</summary>
    public static Statement From(Sequence<Ast.RenameTable> r) => new RenameTable(r);

    /// <summary>Rust: <c>impl From&lt;PrintStatement&gt; for Statement</c>.</summary>
    public static Statement From(Ast.PrintStatement p) => new Print(p);

    /// <summary>Rust: <c>impl From&lt;ReturnStatement&gt; for Statement</c>.</summary>
    public static Statement From(Ast.ReturnStatement r) => new Return(r);

    /// <summary>Rust: <c>impl From&lt;ExportData&gt; for Statement</c>.</summary>
    public static Statement From(Ast.ExportData e) => new ExportData(e);

    /// <summary>Rust: <c>impl From&lt;CreateUser&gt; for Statement</c>.</summary>
    public static Statement From(Ast.CreateUser c) => new CreateUser(c);

    /// <summary>Rust: <c>impl From&lt;CreateWarehouse&gt; for Statement</c>.</summary>
    public static Statement From(Ast.CreateWarehouse c) => new CreateWarehouse(c);

    /// <summary>Rust: <c>impl From&lt;VacuumStatement&gt; for Statement</c>.</summary>
    public static Statement From(Ast.VacuumStatement v) => new Vacuum(v);

    /// <summary>Rust: <c>impl From&lt;ResetStatement&gt; for Statement</c>.</summary>
    public static Statement From(Ast.ResetStatement r) => new Reset(r);
}
