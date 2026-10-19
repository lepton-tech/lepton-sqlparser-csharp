// Código próprio do port (ferramenta de desenvolvimento): modelo dos tipos da AST C# lido por reflexão
// do Lepton.SqlParser.Core.dll compilado sem o código gerado do visitor.

using System.Reflection;
using System.Text.RegularExpressions;

namespace VisitorGen;

/// <summary>Campo C# (parâmetro posicional do record, na ordem da declaração = ordem Rust).</summary>
internal sealed record CsField(string Name, Type FieldType, string? Hook);

/// <summary>Variante (record aninhado que herda do enum).</summary>
internal sealed record CsVariant(Type Type, string Name, List<CsField> Fields);

/// <summary>Forma de um tipo visitável: struct (campos) ou enum (variantes).</summary>
internal sealed class CsShape
{
    public required Type Type { get; init; }
    public required bool IsEnum { get; init; }
    public string? TypeHook { get; init; }
    public List<CsField> Fields { get; } = new();
    public List<CsVariant> Variants { get; } = new();
}

/// <summary>Declaração de tipo de topo encontrada nos fontes (para agrupar a saída e conferir <c>partial</c>).</summary>
internal sealed record SourceDecl(string RelativePath, bool IsPartial, string Modifiers, string Kind);

internal sealed class CsModel
{
    private const string AstNs = "Lepton.SqlParser.Core.Ast";
    private const string CoreNs = "Lepton.SqlParser.Core";
    private readonly Dictionary<Type, CsShape> _shapes = new();

    public CsModel(Assembly assembly, string sourcesRoot)
    {
        Assembly = assembly;
        TopLevel = assembly.GetTypes()
            .Where(t => !t.IsNested && t.IsPublic && (t.Namespace == AstNs || t.Namespace == CoreNs))
            .ToDictionary(t => (StripArity(t.Name), t.IsGenericTypeDefinition ? t.GetGenericArguments().Length : 0));
        Sources = ScanSources(sourcesRoot);
        VisitorType = assembly.GetType(AstNs + ".Visitor`1") ?? throw new InvalidOperationException("Visitor<TBreak> não encontrado");
        VisitorMutType = assembly.GetType(AstNs + ".VisitorMut`1") ?? throw new InvalidOperationException("VisitorMut<TBreak> não encontrado");
        IVisitType = assembly.GetType(AstNs + ".IVisit") ?? throw new InvalidOperationException("IVisit não encontrado");
        SequenceType = assembly.GetType(CoreNs + ".Sequence`1") ?? throw new InvalidOperationException("Sequence<T> não encontrado");
    }

    public Assembly Assembly { get; }
    public Dictionary<(string Name, int Arity), Type> TopLevel { get; }
    public Dictionary<(string Name, int Arity), List<SourceDecl>> Sources { get; }
    public Type VisitorType { get; }
    public Type VisitorMutType { get; }
    public Type IVisitType { get; }
    public Type SequenceType { get; }

    public static string StripArity(string name)
    {
        var i = name.IndexOf('`');
        return i < 0 ? name : name.Substring(0, i);
    }

    public static bool IsRecord(Type t) =>
        t.GetMethod("<Clone>$") is not null
        || t.GetMethod("PrintMembers", BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(System.Text.StringBuilder) }) is not null;

    /// <summary>O tipo já implementa <c>IVisit</c> fora do código gerado (<c>impl Visit</c> manual do Rust).</summary>
    public bool IsManual(Type t) => IVisitType.IsAssignableFrom(t.IsGenericType ? t.GetGenericTypeDefinition() : t);

    public static string? HookOf(MemberInfo m) =>
        m.GetCustomAttributesData()
            .Where(a => a.AttributeType.Name == "VisitWithAttribute")
            .Select(a => (string?)a.ConstructorArguments[0].Value)
            .FirstOrDefault();

    /// <summary>Forma (campos/variantes) de um tipo visitável, aberto ou fechado.</summary>
    public CsShape Shape(Type t)
    {
        if (_shapes.TryGetValue(t, out var cached))
        {
            return cached;
        }
        var def = t.IsGenericType && !t.IsGenericTypeDefinition ? t.GetGenericTypeDefinition() : t;
        var shape = new CsShape { Type = t, IsEnum = t.IsAbstract, TypeHook = HookOf(def) };
        _shapes[t] = shape;
        if (t.IsAbstract)
        {
            foreach (var nestedDef in def.GetNestedTypes(BindingFlags.Public).OrderBy(n => n.MetadataToken))
            {
                var baseType = nestedDef.BaseType;
                var isVariant = baseType == def
                    || (def.IsGenericTypeDefinition && baseType is { IsGenericType: true } && baseType.GetGenericTypeDefinition() == def);
                if (!isVariant)
                {
                    throw new InvalidOperationException($"{t}: tipo aninhado que não é variante: {nestedDef.Name}");
                }
                if (nestedDef.IsAbstract)
                {
                    throw new InvalidOperationException($"{t}: variante abstrata não suportada: {nestedDef.Name}");
                }
                var nested = t.IsGenericType && !t.IsGenericTypeDefinition
                    ? nestedDef.MakeGenericType(t.GetGenericArguments())
                    : nestedDef;
                shape.Variants.Add(new CsVariant(nested, StripArity(nestedDef.Name), FieldsOf(nested)));
            }
        }
        else
        {
            shape.Fields.AddRange(FieldsOf(t));
        }
        return shape;
    }

    /// <summary>
    /// Campos na ordem da declaração: os parâmetros do <c>Deconstruct</c> sintetizado (= construtor primário,
    /// convencoes.md §13, caso 36); sem construtor primário (builders), as propriedades públicas graváveis
    /// na ordem dos metadados.
    /// </summary>
    private static List<CsField> FieldsOf(Type t)
    {
        var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        var deconstruct = t.GetMethods(flags).Where(m => m.Name == "Deconstruct").ToList();
        if (deconstruct.Count > 1)
        {
            throw new InvalidOperationException($"{t}: mais de um Deconstruct");
        }
        var fields = new List<CsField>();
        if (deconstruct.Count == 1)
        {
            foreach (var p in deconstruct[0].GetParameters())
            {
                var prop = t.GetProperty(p.Name!, flags) ?? t.GetProperty(p.Name!, BindingFlags.Public | BindingFlags.Instance)
                    ?? throw new InvalidOperationException($"{t}: parâmetro posicional {p.Name} sem propriedade");
                if (prop.SetMethod is null)
                {
                    throw new InvalidOperationException($"{t}.{prop.Name}: sem init/set (o 'with' não funcionaria)");
                }
                fields.Add(new CsField(prop.Name, prop.PropertyType, HookOf(prop)));
            }
            return fields;
        }
        foreach (var prop in t.GetProperties(flags).Where(p => p.Name != "EqualityContract").OrderBy(p => p.MetadataToken))
        {
            if (prop.SetMethod is null || prop.GetIndexParameters().Length > 0)
            {
                continue; // propriedade calculada
            }
            fields.Add(new CsField(prop.Name, prop.PropertyType, HookOf(prop)));
        }
        return fields;
    }

    private static readonly Regex DeclRx = new(
        @"^(?:public|internal)\s+((?:(?:sealed|abstract|readonly|partial|static|unsafe|ref)\s+)*)(record\s+struct|record\s+class|record|class|struct|interface|enum)\s+(\w+)\s*(<[^>(]*>)?",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static Dictionary<(string, int), List<SourceDecl>> ScanSources(string root)
    {
        var result = new Dictionary<(string, int), List<SourceDecl>>();
        foreach (var path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
        {
            var rel = Path.GetRelativePath(root, path).Replace('\\', '/');
            if (rel.StartsWith("bin/", StringComparison.Ordinal) || rel.StartsWith("obj/", StringComparison.Ordinal)
                || rel.StartsWith("Ast/Visitor/Generated/", StringComparison.Ordinal))
            {
                continue;
            }
            foreach (Match m in DeclRx.Matches(File.ReadAllText(path)))
            {
                var mods = m.Groups[1].Value;
                var arity = m.Groups[4].Success ? m.Groups[4].Value.Count(c => c == ',') + 1 : 0;
                var key = (m.Groups[3].Value, arity);
                if (!result.TryGetValue(key, out var list))
                {
                    result[key] = list = new List<SourceDecl>();
                }
                list.Add(new SourceDecl(rel, Regex.IsMatch(mods, @"\bpartial\b"), mods.Trim(), m.Groups[2].Value));
            }
        }
        return result;
    }
}
