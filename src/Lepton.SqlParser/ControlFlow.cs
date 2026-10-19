// Código próprio da camada pública (sem origem no sqlparser-rs). Imita o ControlFlow do SqlParser-cs
// (enum com Continue e Break) sem copiar código dele. Ver docs/adr/0009-api-publica-fase-2.md.

namespace Lepton.SqlParser;

/// <summary>
/// Resultado de um gancho de <see cref="SqlVisitor"/> e de uma visita: seguir normalmente ou interromper.
/// </summary>
/// <remarks>
/// <para>
/// É o <c>ControlFlow</c> do SqlParser-cs. O Core usa <c>Lepton.SqlParser.Core.ControlFlow&lt;TBreak&gt;</c>
/// (Rust: <c>core::ops::ControlFlow&lt;B&gt;</c>), cujo <c>Break</c> leva um valor. Aqui o <see cref="Break"/>
/// não leva valor: o visitor guarda o que precisar nos próprios campos. Quem precisar do valor pode derivar
/// direto de <c>Lepton.SqlParser.Core.Ast.Visitor&lt;TBreak&gt;</c>.
/// </para>
/// <para>
/// Os dois tipos convivem sem conflito de nomes (CS0104), porque têm aridades diferentes:
/// <c>ControlFlow</c> é este enum e <c>ControlFlow&lt;T&gt;</c> é o tipo do Core.
/// </para>
/// </remarks>
public enum ControlFlow
{
    /// <summary>Segue a visita normalmente.</summary>
    Continue = 0,

    /// <summary>
    /// Interrompe a visita: nenhum outro gancho é chamado, inclusive os <c>PostVisit*</c> dos nós que estavam
    /// abertos, e a visita devolve <see cref="Break"/>.
    /// </summary>
    Break = 1,
}
