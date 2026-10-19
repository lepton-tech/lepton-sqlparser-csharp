// Código próprio do port (ferramenta de desenvolvimento): leitura mínima do código Rust do upstream
// para conferir o C# contra o derive Visit/VisitMut de sqlparser-rs (derive/src/visit.rs).
// Não é um parser de Rust completo: entende só as declarações de tipos com
// #[cfg_attr(feature = "visitor", derive(Visit, VisitMut))] e as anotações visit(with = "...").

using System.Text;
using System.Text.RegularExpressions;

namespace VisitorGen;

/// <summary>Campo Rust (nomeado ou posicional) com a anotação <c>visit(with = ...)</c>, se houver.</summary>
internal sealed record RustField(string Name, string? Hook);

/// <summary>Variante de enum Rust.</summary>
internal sealed record RustVariant(string Name, List<RustField> Fields, bool IsTuple);

/// <summary>Tipo Rust com <c>derive(Visit, VisitMut)</c>.</summary>
internal sealed class RustItem
{
    public required string Name { get; init; }
    public required string Kind { get; init; } // "struct" ou "enum"
    public required string File { get; init; }
    public required int Arity { get; init; }
    public string? TypeHook { get; init; }
    public bool IsTuple { get; init; }
    public bool IsMacro { get; init; }
    public List<RustField> Fields { get; } = new();
    public List<RustVariant> Variants { get; } = new();
}

internal static class RustModel
{
    private static readonly Regex DeriveRx = new(@"derive\s*\(\s*Visit\s*,\s*VisitMut\s*\)", RegexOptions.Compiled);
    private static readonly Regex ItemRx = new(@"\bpub\s+(struct|enum)\s+(\w+)", RegexOptions.Compiled);
    private static readonly Regex WithRx = new(@"visit\s*\(\s*with\s*=\s*""(\w+)""\s*\)", RegexOptions.Compiled);
    private static readonly Regex ManualRx = new(@"\bimpl\s+Visit\s+for\s+(\w+)", RegexOptions.Compiled);

    /// <summary>Tipos com <c>derive(Visit, VisitMut)</c>, por nome, e tipos com <c>impl Visit</c> manual.</summary>
    public static (Dictionary<string, RustItem> Items, HashSet<string> Manual) Load(string upstreamRoot)
    {
        var items = new Dictionary<string, RustItem>(StringComparer.Ordinal);
        var manual = new HashSet<string>(StringComparer.Ordinal);
        var src = Path.Combine(upstreamRoot, "src");
        foreach (var path in Directory.EnumerateFiles(src, "*.rs", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
        {
            var rel = Path.GetRelativePath(upstreamRoot, path).Replace('\\', '/');
            if (rel == "src/ast/visitor.rs")
            {
                continue; // só a infraestrutura (e exemplos em comentários)
            }
            var text = StripComments(File.ReadAllText(path));
            foreach (Match m in ManualRx.Matches(text))
            {
                manual.Add(m.Groups[1].Value);
            }
            foreach (Match d in DeriveRx.Matches(text))
            {
                var attrStart = text.LastIndexOf("#[", d.Index, StringComparison.Ordinal);
                var im = ItemRx.Match(text, d.Index);
                if (!im.Success)
                {
                    throw new InvalidOperationException($"{rel}: derive(Visit) sem item a seguir");
                }
                var attrs = text.Substring(attrStart, im.Index - attrStart);
                var hook = WithRx.Match(attrs);
                var pos = im.Index + im.Length;
                var arity = 0;
                pos = SkipWs(text, pos);
                if (text[pos] == '<')
                {
                    var end = MatchClose(text, pos);
                    arity = SplitTopLevel(text.Substring(pos + 1, end - pos - 1)).Count(s => s.Trim().Length > 0);
                    pos = SkipWs(text, end + 1);
                }
                if (text.Substring(pos).StartsWith("where", StringComparison.Ordinal))
                {
                    while (text[pos] is not ('{' or '(' or ';'))
                    {
                        pos++;
                    }
                }
                var kind = im.Groups[1].Value;
                var name = im.Groups[2].Value;
                var isTuple = text[pos] == '(';
                var item = new RustItem
                {
                    Name = name,
                    Kind = kind,
                    File = rel,
                    Arity = arity,
                    TypeHook = hook.Success ? hook.Groups[1].Value : null,
                    IsTuple = isTuple,
                    IsMacro = false,
                };
                if (text[pos] == ';')
                {
                    // struct unitário
                }
                else
                {
                    var close = MatchClose(text, pos);
                    var body = text.Substring(pos + 1, close - pos - 1);
                    if (body.Contains('$'))
                    {
                        item = new RustItem { Name = name, Kind = kind, File = rel, Arity = arity, TypeHook = item.TypeHook, IsMacro = true };
                    }
                    else if (kind == "struct")
                    {
                        item.Fields.AddRange(ParseFields(body, isTuple));
                    }
                    else
                    {
                        item.Variants.AddRange(ParseVariants(body));
                    }
                }
                if (!items.TryAdd(name, item))
                {
                    throw new InvalidOperationException($"Tipo Rust duplicado: {name} ({rel} e {items[name].File})");
                }
            }
        }
        return (items, manual);
    }

    private static IEnumerable<RustField> ParseFields(string body, bool isTuple)
    {
        var index = 0;
        foreach (var raw in SplitTopLevel(body))
        {
            if (raw.Trim().Length == 0)
            {
                continue;
            }
            var hook = WithRx.Match(raw);
            var seg = StripVisibility(RemoveAttributes(raw).Trim());
            string name;
            if (isTuple)
            {
                name = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                var m = Regex.Match(seg, @"^(?:r#)?(\w+)\s*:", RegexOptions.Singleline);
                if (!m.Success)
                {
                    throw new InvalidOperationException($"Campo Rust não reconhecido: '{seg}'");
                }
                name = m.Groups[1].Value;
            }
            index++;
            yield return new RustField(name, hook.Success ? hook.Groups[1].Value : null);
        }
    }

    private static IEnumerable<RustVariant> ParseVariants(string body)
    {
        foreach (var raw in SplitTopLevel(body))
        {
            // Só os atributos da própria variante saem; os dos campos (visit(with)) ficam no corpo.
            var seg = RemoveLeadingAttributes(raw).Trim();
            if (seg.Length == 0)
            {
                continue;
            }
            var m = Regex.Match(seg, @"^(\w+)\s*(.*)$", RegexOptions.Singleline);
            if (!m.Success)
            {
                throw new InvalidOperationException($"Variante Rust não reconhecida: '{seg}'");
            }
            var name = m.Groups[1].Value;
            var rest = m.Groups[2].Value.Trim();
            if (rest.Length == 0 || rest.StartsWith('='))
            {
                yield return new RustVariant(name, new List<RustField>(), false);
                continue;
            }
            var isTuple = rest[0] == '(';
            var close = MatchClose(rest, 0);
            var inner = rest.Substring(1, close - 1);
            yield return new RustVariant(name, ParseFields(inner, isTuple).ToList(), isTuple);
        }
    }

    private static string StripVisibility(string seg)
    {
        var m = Regex.Match(seg, @"^pub(\s*\([^)]*\))?\s+", RegexOptions.Singleline);
        return m.Success ? seg.Substring(m.Length) : seg;
    }

    /// <summary>Remove os atributos <c>#[...]</c> do início do segmento.</summary>
    private static string RemoveLeadingAttributes(string s)
    {
        var i = SkipWs(s, 0);
        while (i + 1 < s.Length && s[i] == '#' && s[i + 1] == '[')
        {
            i = SkipWs(s, MatchClose(s, i + 1) + 1);
        }
        return s.Substring(i);
    }

    /// <summary>Remove atributos <c>#[...]</c> (balanceados).</summary>
    private static string RemoveAttributes(string s)
    {
        var sb = new StringBuilder();
        var i = 0;
        while (i < s.Length)
        {
            if (s[i] == '#' && i + 1 < s.Length && s[i + 1] == '[')
            {
                i = MatchClose(s, i + 1) + 1;
                continue;
            }
            sb.Append(s[i]);
            i++;
        }
        return sb.ToString();
    }

    private static int SkipWs(string s, int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i]))
        {
            i++;
        }
        return i;
    }

    /// <summary>Posição do fechamento correspondente ao abridor em <paramref name="open"/> (ignora strings).</summary>
    private static int MatchClose(string s, int open)
    {
        var depth = 0;
        for (var i = open; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '"')
            {
                i = SkipString(s, i);
                continue;
            }
            if (c is '(' or '[' or '{' or '<')
            {
                depth++;
            }
            else if (c is ')' or ']' or '}' or '>')
            {
                if (c == '>' && i > 0 && s[i - 1] is '-' or '=')
                {
                    continue;
                }
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }
        throw new InvalidOperationException("Delimitador sem fechamento");
    }

    private static int SkipString(string s, int i)
    {
        i++;
        while (i < s.Length && s[i] != '"')
        {
            if (s[i] == '\\')
            {
                i++;
            }
            i++;
        }
        return i;
    }

    /// <summary>Divide por vírgulas de nível zero.</summary>
    private static List<string> SplitTopLevel(string s)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '"')
            {
                i = SkipString(s, i);
                continue;
            }
            if (c is '(' or '[' or '{' or '<')
            {
                depth++;
            }
            else if (c is ')' or ']' or '}' or '>')
            {
                if (c == '>' && i > 0 && s[i - 1] is '-' or '=')
                {
                    continue;
                }
                depth--;
            }
            else if (c == ',' && depth == 0)
            {
                parts.Add(s.Substring(start, i - start));
                start = i + 1;
            }
        }
        parts.Add(s.Substring(start));
        return parts;
    }

    /// <summary>Remove comentários (<c>//</c>, <c>/* */</c>), preservando strings e literais de caractere.</summary>
    private static string StripComments(string s)
    {
        var sb = new StringBuilder(s.Length);
        var i = 0;
        while (i < s.Length)
        {
            var c = s[i];
            if (c == '/' && i + 1 < s.Length && s[i + 1] == '/')
            {
                while (i < s.Length && s[i] != '\n')
                {
                    i++;
                }
                continue;
            }
            if (c == '/' && i + 1 < s.Length && s[i + 1] == '*')
            {
                var depth = 1;
                i += 2;
                while (i < s.Length && depth > 0)
                {
                    if (s[i] == '/' && i + 1 < s.Length && s[i + 1] == '*')
                    {
                        depth++;
                        i += 2;
                    }
                    else if (s[i] == '*' && i + 1 < s.Length && s[i + 1] == '/')
                    {
                        depth--;
                        i += 2;
                    }
                    else
                    {
                        i++;
                    }
                }
                sb.Append(' ');
                continue;
            }
            if (c == 'r' && i + 1 < s.Length && (s[i + 1] == '"' || s[i + 1] == '#') && (i == 0 || !IsIdent(s[i - 1])))
            {
                var j = i + 1;
                var hashes = 0;
                while (j < s.Length && s[j] == '#')
                {
                    hashes++;
                    j++;
                }
                if (j < s.Length && s[j] == '"')
                {
                    var terminator = "\"" + new string('#', hashes);
                    var end = s.IndexOf(terminator, j + 1, StringComparison.Ordinal);
                    if (end < 0)
                    {
                        throw new InvalidOperationException("String bruta sem fechamento");
                    }
                    end += terminator.Length;
                    sb.Append(s, i, end - i);
                    i = end;
                    continue;
                }
            }
            if (c == '"')
            {
                var end = SkipString(s, i);
                sb.Append(s, i, end - i + 1);
                i = end + 1;
                continue;
            }
            if (c == '\'')
            {
                if (i + 1 < s.Length && s[i + 1] == '\\')
                {
                    var end = s.IndexOf('\'', i + 3);
                    sb.Append(s, i, end - i + 1);
                    i = end + 1;
                    continue;
                }
                if (i + 2 < s.Length && s[i + 2] == '\'')
                {
                    sb.Append(s, i, 3);
                    i += 3;
                    continue;
                }
                if (i + 3 < s.Length && char.IsHighSurrogate(s[i + 1]) && s[i + 3] == '\'')
                {
                    sb.Append(s, i, 4);
                    i += 4;
                    continue;
                }
            }
            sb.Append(c);
            i++;
        }
        return sb.ToString();
    }

    private static bool IsIdent(char c) => char.IsLetterOrDigit(c) || c == '_';
}
