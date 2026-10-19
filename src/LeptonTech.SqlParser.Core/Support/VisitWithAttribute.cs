// Código próprio do port: preserva as anotações `#[cfg_attr(feature = "visitor", visit(with = "..."))]`
// do Rust, que o visitor (gerado mais adiante na Fase 2) vai usar. Ver convencoes.md §13 (caso 32).

namespace LeptonTech.SqlParser.Core;

/// <summary>
/// Rust: <c>#[cfg_attr(feature = "visitor", visit(with = "visit_relation"))]</c>. Indica qual hook
/// pré/pós do visitor é chamado para o tipo ou campo anotado.
/// </summary>
/// <remarks>
/// Em tipo: <c>[VisitWith("visit_query")]</c> antes da declaração. Em campo de record posicional:
/// <c>[property: VisitWith("visit_relation")] ObjectName Name</c>. O texto é o nome Rust do hook, sem mudança.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class VisitWithAttribute : Attribute
{
    /// <summary>Cria a anotação com o nome Rust do hook (ex.: <c>visit_relation</c>).</summary>
    public VisitWithAttribute(string hook) => Hook = hook;

    /// <summary>Nome Rust do hook.</summary>
    public string Hook { get; }
}
