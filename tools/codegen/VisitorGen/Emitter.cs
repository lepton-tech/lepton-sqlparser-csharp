// Código próprio do port (ferramenta de desenvolvimento): emite o código C# equivalente ao que o
// derive Visit/VisitMut do Rust (derive/src/visit.rs) gera para cada tipo da AST.

using System.Text;

namespace VisitorGen;

internal sealed class Emitter
{
    private const string Flow = "global::SqlParser.Core.ControlFlow<TBreak>";
    private const string Support = "global::SqlParser.Core.Ast.VisitSupport";
    private readonly Analysis _a;
    private readonly StringBuilder _sb = new();
    private int _indent;
    private int _counter;

    public Emitter(Analysis analysis) => _a = analysis;

    public override string ToString() => _sb.ToString();

    public void Line(string text = "")
    {
        if (text.Length == 0)
        {
            _sb.Append('\n');
            return;
        }
        _sb.Append(' ', _indent * 4).Append(text).Append('\n');
    }

    public void Open(string text)
    {
        Line(text);
        Line("{");
        _indent++;
    }

    public void Close(string suffix = "")
    {
        _indent--;
        Line("}" + suffix);
    }

    private string Fresh(string prefix) => prefix + (++_counter).ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Nome C# totalmente qualificado (<c>global::</c>) de um tipo, com argumentos genéricos.</summary>
    public static string TypeName(Type t)
    {
        if (t.IsGenericParameter)
        {
            return t.Name;
        }
        if (Nullable.GetUnderlyingType(t) is { } u)
        {
            return TypeName(u) + "?";
        }
        if (t.IsArray)
        {
            return TypeName(t.GetElementType()!) + "[]";
        }
        var args = t.IsGenericType ? t.GetGenericArguments() : Type.EmptyTypes;
        if (t.IsNested)
        {
            var decl = t.DeclaringType!;
            var outerArity = decl.IsGenericTypeDefinition ? decl.GetGenericArguments().Length : 0;
            var outer = outerArity == 0
                ? TypeName(decl)
                : "global::" + decl.Namespace + "." + CsModel.StripArity(decl.Name) + "<" + string.Join(", ", args.Take(outerArity).Select(TypeName)) + ">";
            var own = args.Skip(outerArity).ToList();
            return outer + "." + CsModel.StripArity(t.Name) + (own.Count > 0 ? "<" + string.Join(", ", own.Select(TypeName)) + ">" : "");
        }
        var name = t.Namespace is { Length: > 0 } ns ? "global::" + ns + "." + CsModel.StripArity(t.Name) : "global::" + CsModel.StripArity(t.Name);
        return args.Length > 0 ? name + "<" + string.Join(", ", args.Select(TypeName)) + ">" : name;
    }

    /// <summary><c>visit_relation</c> → <c>VisitRelation</c> (os ganchos são <c>Pre…</c>/<c>Post…</c>).</summary>
    public static string HookName(string rustHook) =>
        string.Concat(rustHook.Split('_').Select(p => p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p.Substring(1)));

    // ------------------------------------------------------------------------------------------
    // Um tipo de topo: partial com IVisit + IVisitMut<T>.

    public void EmitType(Type t, string header)
    {
        var shape = _a.Model.Shape(t);
        var self = TypeName(t);
        var decl = _a.Declaration(t);
        foreach (var h in header.Split('\n'))
        {
            Line("// " + h);
        }
        var generic = t.IsGenericTypeDefinition ? "<" + string.Join(", ", t.GetGenericArguments().Select(a => a.Name)) + ">" : "";
        Open($"{decl} {CsModel.StripArity(t.Name)}{generic} : global::SqlParser.Core.Ast.IVisit, global::SqlParser.Core.Ast.IVisitMut<{self}>");
        var trivial = !_a.TypeReaches(t);

        // impl Visit for X
        Line("/// <inheritdoc/>");
        if (trivial)
        {
            // Nota do port em // e não em /// (convencoes.md §12): não aparece no IntelliSense.
            Line("// Nenhum campo alcança um gancho do Visitor<TBreak>: a visita do derive Rust não tem efeito.");
            Line($"public {Flow} Visit<TBreak>(global::SqlParser.Core.Ast.Visitor<TBreak> visitor) => {Flow}.Continue;");
        }
        else
        {
            EmitVisit(shape);
        }
        Line();

        // impl VisitMut for X
        Line("/// <inheritdoc/>");
        Open($"public {Flow} Visit<TBreak>(global::SqlParser.Core.Ast.VisitorMut<TBreak> visitor, out {self} self)");
        if (trivial)
        {
            Line("self = this;");
            Line($"return {Flow}.Continue;");
        }
        else
        {
            Line("var node = this;");
            Line("var flow = VisitMut(ref node, visitor);");
            Line("self = node;");
            Line("return flow;");
        }
        Close();
        if (!trivial)
        {
            Line();
            EmitVisitMut(shape);
        }
        Close();
    }

    // ------------------------------------------------------------------------------------------
    // Visit (imutável): pre_visit; campos na ordem; post_visit.

    // Enums: o despachante só testa o tipo (sem variáveis de padrão) e chama um método por variante
    // (VisitVariant/VisitMutVariant). Assim o quadro de pilha de cada nível fica pequeno mesmo no build
    // Debug, que não reaproveita variáveis locais: o teste `overflow` do Rust visita 1000 níveis de Expr.

    private void EmitVisit(CsShape shape)
    {
        _counter = 0;
        Open($"public {Flow} Visit<TBreak>(global::SqlParser.Core.Ast.Visitor<TBreak> visitor)");
        Line("// #[cfg_attr(feature = \"recursive-protection\", recursive::recursive)]");
        Line($"if ({Support}.StackIsLow()) return {Support}.VisitOnNewStack(this, visitor);");
        Line($"{Flow} flow;");
        if (shape.TypeHook is { } th)
        {
            Line($"flow = visitor.Pre{HookName(th)}(this);");
            Line("if (flow.IsBreak) return flow;");
        }
        var variants = shape.Variants.Where(v => v.Fields.Any(_a.FieldReaches)).ToList();
        if (shape.IsEnum)
        {
            if (variants.Count > 0)
            {
                Open("switch (this)");
                foreach (var v in variants)
                {
                    var vt = TypeName(v.Type);
                    Line($"case {vt}:");
                    _indent++;
                    Line($"flow = VisitVariant(({vt})this, visitor);");
                    Line("if (flow.IsBreak) return flow;");
                    Line("break;");
                    _indent--;
                }
                Close();
            }
        }
        else
        {
            foreach (var f in shape.Fields)
            {
                EmitVisitField("this", f);
            }
        }
        Line(shape.TypeHook is { } th2 ? $"return visitor.Post{HookName(th2)}(this);" : $"return {Flow}.Continue;");
        Close();
        foreach (var v in variants)
        {
            _counter = 0;
            Line();
            Line($"// {v.Name}");
            Open($"private static {Flow} VisitVariant<TBreak>({TypeName(v.Type)} v, global::SqlParser.Core.Ast.Visitor<TBreak> visitor)");
            Line($"{Flow} flow;");
            foreach (var f in v.Fields)
            {
                EmitVisitField("v", f);
            }
            Line($"return {Flow}.Continue;");
            Close();
        }
    }

    private void EmitVisitField(string owner, CsField f)
    {
        if (!_a.FieldReaches(f))
        {
            Line($"// {f.Name}: nenhum gancho alcançável");
            return;
        }
        Line($"// {f.Name}" + (f.Hook is { } fh ? $" [visit(with = \"{fh}\")]" : ""));
        var expr = $"{owner}.{f.Name}";
        if (f.Hook is { } hook)
        {
            // derive: `if let Some(value) = &self.x { pre(value)?; visit(value)?; post(value)?; }` (Option)
            // ou `pre(&self.x)?; visit(&self.x)?; post(&self.x)?;`.
            var x = Fresh("x");
            Open($"if ({expr} is {{ }} {x})");
            Line($"flow = visitor.Pre{HookName(hook)}({x});");
            Line("if (flow.IsBreak) return flow;");
            EmitVisitValue(Analysis.StripNullable(f.FieldType), x, knownNotNull: true);
            Line($"flow = visitor.Post{HookName(hook)}({x});");
            Line("if (flow.IsBreak) return flow;");
            Close();
            return;
        }
        EmitVisitValue(f.FieldType, expr, knownNotNull: false);
    }

    private void EmitVisitValue(Type t, string expr, bool knownNotNull)
    {
        if (!_a.TypeReaches(t))
        {
            return;
        }
        if (Nullable.GetUnderlyingType(t) is { } u)
        {
            var y = Fresh("x");
            Open($"if ({expr} is {{ }} {y})");
            EmitVisitValue(u, y, knownNotNull: true);
            Close();
            return;
        }
        if (_a.IsSequence(t, out var element))
        {
            // impl<T: Visit> Visit for Vec<T>: `for v in self { v.visit(visitor)?; }`
            var s = Fresh("s");
            var i = Fresh("i");
            Line("{");
            _indent++;
            Line($"var {s} = {expr};");
            Open($"for (var {i} = 0; {i} < {s}.Count; {i}++)");
            EmitVisitValue(element, $"{s}[{i}]", knownNotNull: false);
            Close();
            _indent--;
            Line("}");
            return;
        }
        if (t.IsGenericParameter)
        {
            var g = Fresh("x");
            Open($"if ({expr} is global::SqlParser.Core.Ast.IVisit {g})");
            Line($"flow = {g}.Visit(visitor);");
            Line("if (flow.IsBreak) return flow;");
            Close();
            return;
        }
        if (knownNotNull)
        {
            Line($"flow = {expr}.Visit(visitor);");
            Line("if (flow.IsBreak) return flow;");
            return;
        }
        var x = Fresh("x");
        Open($"if ({expr} is {{ }} {x})");
        Line($"flow = {x}.Visit(visitor);");
        Line("if (flow.IsBreak) return flow;");
        Close();
    }

    // ------------------------------------------------------------------------------------------
    // VisitMut: o nó é imutável; `&mut self` vira `ref` e cada mudança reconstrói o pai com `with`.

    private void EmitVisitMut(CsShape shape)
    {
        _counter = 0;
        var self = TypeName(shape.Type);
        Open($"internal static {Flow} VisitMut<TBreak>(ref {self} node, global::SqlParser.Core.Ast.VisitorMut<TBreak> visitor)");
        Line("// #[cfg_attr(feature = \"recursive-protection\", recursive::recursive)]");
        Line($"if ({Support}.StackIsLow()) return {Support}.VisitMutOnNewStack(ref node, visitor);");
        Line($"var flow = {Flow}.Continue;");
        if (shape.TypeHook is { } th)
        {
            Line($"flow = visitor.Pre{HookName(th)}(ref node);");
            Line("if (flow.IsBreak) return flow;");
        }
        var variants = shape.Variants.Where(v => v.Fields.Any(_a.FieldReaches)).ToList();
        if (shape.IsEnum)
        {
            if (variants.Count > 0)
            {
                Open("switch (node)");
                foreach (var v in variants)
                {
                    var vt = TypeName(v.Type);
                    Line($"case {vt}:");
                    _indent++;
                    Line($"flow = VisitMutVariant(ref node, ({vt})node, visitor);");
                    Line("break;");
                    _indent--;
                }
                Close();
                if (shape.TypeHook is not null)
                {
                    Line("if (flow.IsBreak) return flow;");
                }
            }
        }
        else
        {
            foreach (var f in shape.Fields)
            {
                EmitVisitMutField("node", f, "return flow;");
            }
        }
        Line(shape.TypeHook is { } th2 ? $"return visitor.Post{HookName(th2)}(ref node);" : "return flow;");
        Close();
        foreach (var v in variants)
        {
            _counter = 0;
            Line();
            Line($"// {v.Name}");
            Open($"private static {Flow} VisitMutVariant<TBreak>(ref {self} node, {TypeName(v.Type)} v, global::SqlParser.Core.Ast.VisitorMut<TBreak> visitor)");
            Line($"var flow = {Flow}.Continue;");
            Line("var n = v;");
            foreach (var f in v.Fields)
            {
                EmitVisitMutField("n", f, "{ node = n; return flow; }");
            }
            Line("node = n;");
            Line("return flow;");
            Close();
        }
    }

    private void EmitVisitMutField(string owner, CsField f, string onBreak)
    {
        if (!_a.FieldReaches(f))
        {
            Line($"// {f.Name}: nenhum gancho alcançável");
            return;
        }
        Line($"// {f.Name}" + (f.Hook is { } fh ? $" [visit(with = \"{fh}\")]" : ""));
        var x = Fresh("x");
        Line("{");
        _indent++;
        Line($"var {x} = {owner}.{f.Name};");
        if (f.Hook is { } hook)
        {
            var y = Fresh("y");
            Open($"if ({x} is {{ }} {y})");
            Line($"flow = visitor.Pre{HookName(hook)}(ref {y});");
            Open("if (!flow.IsBreak)");
            EmitVisitMutValue(Analysis.StripNullable(f.FieldType), y);
            Line($"if (!flow.IsBreak) flow = visitor.Post{HookName(hook)}(ref {y});");
            Close();
            Line($"{x} = {y};");
            Close();
        }
        else
        {
            EmitVisitMutValue(f.FieldType, x);
        }
        Line($"if (!{Support}.IsSame({x}, {owner}.{f.Name})) {owner} = {owner} with {{ {f.Name} = {x} }};");
        Line($"if (flow.IsBreak) {onBreak}");
        _indent--;
        Line("}");
    }

    /// <summary>Visita a variável local <paramref name="v"/> (pode reatribuí-la); deixa o resultado em <c>flow</c>.</summary>
    private void EmitVisitMutValue(Type t, string v)
    {
        if (!_a.TypeReaches(t))
        {
            return;
        }
        if (Nullable.GetUnderlyingType(t) is { } u)
        {
            var y = Fresh("y");
            Open($"if ({v} is {{ }} {y})");
            EmitVisitMutValue(u, y);
            Line($"{v} = {y};");
            Close();
            return;
        }
        if (_a.IsSequence(t, out var element))
        {
            // impl<T: VisitMut> VisitMut for Vec<T>: `for v in self { v.visit(visitor)?; }`
            var arr = Fresh("a");
            var i = Fresh("i");
            var e = Fresh("e");
            var elementName = TypeName(element);
            Line("{");
            _indent++;
            Line($"{elementName}[]? {arr} = null;");
            Open($"for (var {i} = 0; {i} < {v}.Count; {i}++)");
            Line($"var {e} = {v}[{i}];");
            EmitVisitMutValue(element, e);
            Open($"if (!{Support}.IsSame({e}, {v}[{i}]))");
            Line($"{arr} ??= global::System.Linq.Enumerable.ToArray({v});");
            Line($"{arr}[{i}] = {e};");
            Close();
            Line("if (flow.IsBreak) break;");
            Close();
            Line($"if ({arr} is not null) {v} = global::SqlParser.Core.Sequence<{elementName}>.FromArrayNoCopy({arr});");
            _indent--;
            Line("}");
            return;
        }
        if (t.IsGenericParameter)
        {
            Line($"flow = {Support}.VisitMutGeneric(ref {v}, visitor);");
            return;
        }
        var y2 = Fresh("y");
        Open($"if ({v} is {{ }} {y2})");
        Line($"flow = {TypeName(t)}.VisitMut(ref {y2}, visitor);");
        Line($"{v} = {y2};");
        Close();
    }
}
