// Código próprio do port (ferramenta de desenvolvimento): cruza o modelo Rust (derive Visit) com a AST C#,
// valida e calcula quais tipos alcançam algum gancho do visitor.

namespace VisitorGen;

internal sealed class Analysis
{
    private readonly Dictionary<string, RustItem> _rust;
    private readonly HashSet<Type> _reach = new();
    private readonly HashSet<Type> _nodes = new();

    public Analysis(CsModel model, Dictionary<string, RustItem> rust, HashSet<string> rustManual)
    {
        Model = model;
        _rust = rust;
        foreach (var item in rust.Values.OrderBy(i => i.File, StringComparer.Ordinal).ThenBy(i => i.Name, StringComparer.Ordinal))
        {
            if (!model.TopLevel.TryGetValue((item.Name, item.Arity), out var t))
            {
                Errors.Add($"Tipo Rust com derive(Visit) sem correspondente C#: {item.Name} ({item.File})");
                continue;
            }
            if (t.IsEnum)
            {
                // Enum Rust sem dados e sem impl → enum C# (convencoes.md §6, regra 1): folha sem ganchos.
                LeafEnums.Add(t);
                continue;
            }
            if (!CsModel.IsRecord(t))
            {
                Errors.Add($"{t}: tipo visitável que não é record");
                continue;
            }
            Visitable.Add(t);
            RustOf[t] = item;
        }
        foreach (var name in rustManual)
        {
            if (!model.TopLevel.TryGetValue((name, 0), out var t) || !model.IsManual(t))
            {
                Errors.Add($"impl Visit manual do Rust sem port C# (IVisit escrito à mão): {name}");
                continue;
            }
            if (!Visitable.Contains(t))
            {
                Visitable.Add(t);
            }
        }
        foreach (var t in Visitable)
        {
            if (model.IsManual(t))
            {
                if (!rustManual.Contains(CsModel.StripArity(t.Name)))
                {
                    Errors.Add($"{t}: implementa IVisit à mão, mas o Rust usa o derive");
                }
                Manual.Add(t);
            }
        }
        ValidateShapes();
        CrossCheckRust();
        CheckHooksExist();
        CheckPartial();
        ComputeReach();
    }

    public CsModel Model { get; }
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public List<Type> Visitable { get; } = new();
    public List<Type> LeafEnums { get; } = new();
    public HashSet<Type> Manual { get; } = new();
    public Dictionary<Type, RustItem> RustOf { get; } = new();

    public static Type StripNullable(Type t) => Nullable.GetUnderlyingType(t) ?? t;

    public bool IsSequence(Type t, out Type element)
    {
        if (t.IsGenericType && t.GetGenericTypeDefinition() == Model.SequenceType)
        {
            element = t.GetGenericArguments()[0];
            return true;
        }
        element = typeof(void);
        return false;
    }

    /// <summary>Tipo da AST com <c>Visit</c> próprio (não genérico, genérico aberto ou fechado).</summary>
    public bool IsNode(Type t)
    {
        var def = t.IsGenericType && !t.IsGenericTypeDefinition ? t.GetGenericTypeDefinition() : t;
        return Visitable.Contains(def);
    }

    /// <summary>Folhas do Rust: <c>visit_noop!(u8, …, String)</c> e enums sem dados.</summary>
    private bool IsLeaf(Type t) =>
        t.IsPrimitive || t == typeof(string) || t == typeof(decimal) || t.IsEnum;

    public bool TypeReaches(Type t)
    {
        if (t.IsGenericParameter)
        {
            return true; // decidido em tempo de execução (IVisit/IVisitMut<T>)
        }
        if (Nullable.GetUnderlyingType(t) is { } u)
        {
            return TypeReaches(u);
        }
        if (IsSequence(t, out var e))
        {
            return TypeReaches(e);
        }
        if (IsNode(t))
        {
            return t.IsGenericTypeDefinition || _reach.Contains(t);
        }
        return false;
    }

    public bool FieldReaches(CsField f) => f.Hook is not null || TypeReaches(f.FieldType);

    /// <summary>Palavras da declaração <c>partial</c> gerada, iguais às do fonte (ex.: <c>public sealed partial record</c>).</summary>
    public string Declaration(Type t)
    {
        var key = (CsModel.StripArity(t.Name), t.IsGenericTypeDefinition ? t.GetGenericArguments().Length : 0);
        var decl = Model.Sources[key][0];
        return $"public {decl.Modifiers} {decl.Kind}";
    }

    /// <summary>Arquivo-fonte principal do tipo (para agrupar a saída por pasta).</summary>
    public string PrimarySource(Type t)
    {
        var name = CsModel.StripArity(t.Name);
        var key = (name, t.IsGenericTypeDefinition ? t.GetGenericArguments().Length : 0);
        var decls = Model.Sources[key].Where(d => !d.RelativePath.StartsWith("Ast/Spans/", StringComparison.Ordinal)).ToList();
        return (decls.FirstOrDefault(d => Path.GetFileNameWithoutExtension(d.RelativePath) == name) ?? decls[0]).RelativePath;
    }

    // ------------------------------------------------------------------------------------------

    private void ValidateShapes()
    {
        foreach (var t in Visitable)
        {
            if (Manual.Contains(t))
            {
                continue;
            }
            var shape = Model.Shape(t);
            foreach (var (owner, f) in AllFields(shape))
            {
                CheckFieldType(f.FieldType, $"{owner}.{f.Name}");
                if (f.Hook is not null)
                {
                    var ft = StripNullable(f.FieldType);
                    if (!IsNode(ft) || ft.IsValueType)
                    {
                        Errors.Add($"{owner}.{f.Name}: visit(with) em campo que não é nó de referência ({ft})");
                    }
                }
            }
        }
    }

    private void CheckFieldType(Type t, string where)
    {
        if (t.IsGenericParameter || IsLeaf(t))
        {
            return;
        }
        if (Nullable.GetUnderlyingType(t) is { } u)
        {
            CheckFieldType(u, where);
            return;
        }
        if (IsSequence(t, out var e))
        {
            CheckFieldType(e, where);
            return;
        }
        if (IsNode(t))
        {
            return;
        }
        Errors.Add($"{where}: tipo de campo não visitável no Rust (sem derive Visit): {t}");
    }

    private static IEnumerable<(string Owner, CsField Field)> AllFields(CsShape shape)
    {
        var owner = CsModel.StripArity(shape.Type.Name);
        foreach (var f in shape.Fields)
        {
            yield return (owner, f);
        }
        foreach (var v in shape.Variants)
        {
            foreach (var f in v.Fields)
            {
                yield return ($"{owner}.{v.Name}", f);
            }
        }
    }

    private static string Pascal(string snake) =>
        string.Concat(snake.Split('_').Select(p => p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p.Substring(1)));

    /// <summary>Confere nomes, ordem e anotações dos campos C# contra o Rust.</summary>
    private void CrossCheckRust()
    {
        foreach (var t in Visitable)
        {
            if (Manual.Contains(t))
            {
                continue;
            }
            var item = RustOf[t];
            var shape = Model.Shape(t);
            var name = CsModel.StripArity(t.Name);
            if (item.TypeHook != shape.TypeHook)
            {
                Errors.Add($"{name}: gancho do tipo difere (Rust: {item.TypeHook ?? "nenhum"}; C#: {shape.TypeHook ?? "nenhum"})");
            }
            if (item.IsMacro)
            {
                continue;
            }
            if (item.Kind == "struct")
            {
                if (shape.IsEnum)
                {
                    Errors.Add($"{name}: struct no Rust, abstract record no C#");
                    continue;
                }
                CompareFields(name, item.Fields, item.IsTuple, shape.Fields);
            }
            else
            {
                if (!shape.IsEnum)
                {
                    Errors.Add($"{name}: enum no Rust, record não abstrato no C#");
                    continue;
                }
                // Renomeações registradas de variantes: sufixo Value (variante com o nome do enum, CS0542:
                // Distinct.DistinctValue, Password.PasswordValue) e Variant (FunctionArgOperator.EqualsVariant, CS0102).
                var csVariants = shape.Variants.ToDictionary(v => v.Name);
                var matched = new HashSet<string>();
                foreach (var rv in item.Variants)
                {
                    var cv = new[] { rv.Name, rv.Name + "Value", rv.Name + "Variant" }
                        .Select(n => csVariants.TryGetValue(n, out var found) ? found : null)
                        .FirstOrDefault(v => v is not null);
                    if (cv is null)
                    {
                        Errors.Add($"{name}::{rv.Name}: variante sem correspondente C#");
                        continue;
                    }
                    matched.Add(cv.Name);
                    CompareFields($"{name}.{cv.Name}", rv.Fields, rv.IsTuple, cv.Fields);
                }
                foreach (var cv in shape.Variants.Where(v => !matched.Contains(v.Name)))
                {
                    Errors.Add($"{name}.{cv.Name}: variante C# sem correspondente Rust");
                }
            }
        }
    }

    private void CompareFields(string owner, List<RustField> rust, bool isTuple, List<CsField> cs)
    {
        if (rust.Count != cs.Count)
        {
            Errors.Add($"{owner}: {rust.Count} campo(s) no Rust, {cs.Count} no C#");
            return;
        }
        for (var i = 0; i < rust.Count; i++)
        {
            var r = rust[i];
            var c = cs[i];
            if (!isTuple)
            {
                var expected = Pascal(r.Name);
                // Exceções registradas: sufixo Value (convencoes.md §13, casos 41 e 59).
                if (c.Name != expected && c.Name != expected + "Value")
                {
                    Errors.Add($"{owner}: campo {i} é '{r.Name}' no Rust e '{c.Name}' no C# (ordem ou nome divergente)");
                }
            }
            if (r.Hook != c.Hook)
            {
                Errors.Add($"{owner}.{c.Name}: visit(with) difere (Rust: {r.Hook ?? "nenhum"}; C#: {c.Hook ?? "nenhum"})");
            }
        }
    }

    private void CheckHooksExist()
    {
        var hooks = new HashSet<string>();
        foreach (var t in Visitable.Where(t => !Manual.Contains(t)))
        {
            var shape = Model.Shape(t);
            if (shape.TypeHook is { } th)
            {
                hooks.Add(th);
            }
            foreach (var (_, f) in AllFields(shape))
            {
                if (f.Hook is { } fh)
                {
                    hooks.Add(fh);
                }
            }
        }
        foreach (var h in hooks)
        {
            var n = Emitter.HookName(h);
            foreach (var vt in new[] { Model.VisitorType, Model.VisitorMutType })
            {
                if (vt.GetMethod("Pre" + n) is null || vt.GetMethod("Post" + n) is null)
                {
                    Errors.Add($"{vt.Name}: faltam os ganchos Pre{n}/Post{n} ({h})");
                }
            }
        }
    }

    private void CheckPartial()
    {
        foreach (var t in Visitable.Where(t => !Manual.Contains(t)))
        {
            var key = (CsModel.StripArity(t.Name), t.IsGenericTypeDefinition ? t.GetGenericArguments().Length : 0);
            if (!Model.Sources.TryGetValue(key, out var decls))
            {
                Errors.Add($"{t}: declaração não encontrada nos fontes");
                continue;
            }
            foreach (var d in decls.Where(d => !d.IsPartial))
            {
                Errors.Add($"{t}: declaração sem 'partial' em {d.RelativePath}");
            }
        }
    }

    /// <summary>
    /// Ponto fixo: um nó alcança um gancho se tem gancho de tipo, se é <c>impl Visit</c> manual (Ident),
    /// se um campo tem <c>visit(with)</c> ou se algum campo/variante alcança outro nó que alcança.
    /// </summary>
    private void ComputeReach()
    {
        var work = new Queue<Type>(Visitable.Where(t => !t.IsGenericTypeDefinition));
        while (work.Count > 0)
        {
            var t = work.Dequeue();
            if (!_nodes.Add(t) || Manual.Contains(t))
            {
                continue;
            }
            foreach (var (_, f) in AllFields(Model.Shape(t)))
            {
                foreach (var child in NodeTypes(f.FieldType))
                {
                    if (!_nodes.Contains(child))
                    {
                        work.Enqueue(child);
                    }
                }
            }
        }
        foreach (var t in _nodes)
        {
            if (Manual.Contains(t))
            {
                _reach.Add(t);
                continue;
            }
            var shape = Model.Shape(t);
            if (shape.TypeHook is not null || AllFields(shape).Any(p => p.Field.Hook is not null))
            {
                _reach.Add(t);
            }
        }
        bool changed;
        do
        {
            changed = false;
            foreach (var t in _nodes.Where(n => !_reach.Contains(n)))
            {
                if (AllFields(Model.Shape(t)).Any(p => NodeTypes(p.Field.FieldType).Any(_reach.Contains)))
                {
                    _reach.Add(t);
                    changed = true;
                }
            }
        }
        while (changed);
    }

    private IEnumerable<Type> NodeTypes(Type t)
    {
        if (Nullable.GetUnderlyingType(t) is { } u)
        {
            return NodeTypes(u);
        }
        if (IsSequence(t, out var e))
        {
            return NodeTypes(e);
        }
        return IsNode(t) && !t.IsGenericTypeDefinition && !t.ContainsGenericParameters ? new[] { t } : Array.Empty<Type>();
    }

    public int ReachCount => _reach.Count;
    public int NodeCount => _nodes.Count;
}
