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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/display_utils.rs (mod tests).
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Tests;

public class DisplayUtilsTests
{
    [Fact]
    public void test_indent()
    {
        var indent = new Indent<TwoLines>(new TwoLines());
        Assert.Equal(
            indent.ToString(),
            new TwoLines().ToString());
        Assert.Equal("  line 1\n  line 2", Formatter.FormatAlternate(indent));
    }

    private sealed class TwoLines : IDisplay
    {
        public void Fmt(Formatter f)
        {
            f.WriteStr("line 1");
            SpaceOrNewline.Instance.Fmt(f);
            f.WriteStr("line 2");
        }

        public override string ToString() => Formatter.Format(this);
    }
}

// Testes próprios do port: verificam o Formatter (Support/Formatter.cs), que não existe no Rust.
public class FormatterPortTests
{
    [Fact]
    public void Interpolacao_escreve_lacunas_no_mesmo_destino_e_respeita_alternate()
    {
        var sb = new System.Text.StringBuilder();
        var f = new Formatter(sb);
        var inner = new Indent<TwoLines>(new TwoLines());
        f.Write($"a {inner} b {inner:#} {Keyword.END_EXEC} {true} {42}");
        Assert.Equal("a line 1 line 2 b   line 1\n  line 2 END-EXEC true 42", sb.ToString());
    }

    [Fact]
    public void Indent_aninhado_acumula_indentacao()
    {
        var nested = new Indent<Indent<TwoLines>>(new Indent<TwoLines>(new TwoLines()));
        Assert.Equal("    line 1\n    line 2", Formatter.FormatAlternate(nested));
    }

    [Fact]
    public void Tipos_sem_Display_falham_alto()
    {
        var f = new Formatter(new System.Text.StringBuilder());
        Assert.Throws<InvalidOperationException>(() => f.WriteValue<string?>(null));
        Assert.Throws<InvalidOperationException>(() => f.WriteValue(DayOfWeek.Monday));
        Assert.Throws<InvalidOperationException>(() => f.WriteValue(1.5));
    }

    [Fact]
    public void Sequence_tem_igualdade_estrutural_e_default_vazio()
    {
        Assert.Equal(Sequence.Of(1, 2, 3), new[] { 1, 2, 3 }.ToSequence());
        Assert.NotEqual(Sequence.Of(1, 2), Sequence.Of(2, 1));
        Assert.Equal(Sequence.Of(1, 2).GetHashCode(), Sequence.Of(1, 2).GetHashCode());
        Assert.True(default(Sequence<string>).IsEmpty);
        Assert.Equal(Sequence<string>.Empty, Sequence.Of<string>());
        Assert.Equal(3, Sequence.Of("a", "b", "c").Last == "c" ? 3 : 0);
    }

    private sealed class TwoLines : IDisplay
    {
        public void Fmt(Formatter f)
        {
            f.WriteStr("line 1");
            SpaceOrNewline.Instance.Fmt(f);
            f.WriteStr("line 2");
        }

        public override string ToString() => Formatter.Format(this);
    }
}
