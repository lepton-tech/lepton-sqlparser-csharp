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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/mod.rs (mod tests).
// Modificado: tradução de Rust para C#.

using LeptonTech.SqlParser.Core.Ast;

namespace LeptonTech.SqlParser.Core.Tests;

public class AstModTests
{
    [Fact]
    public void test_window_frame_default()
    {
        var windowFrame = WindowFrame.Default();
        Assert.Equal(new WindowFrameBound.Preceding(null), windowFrame.StartBound);
    }

    [Fact]
    public void test_grouping_sets_display()
    {
        // a and b in different group
        var groupingSets = new Expr.GroupingSets(Sequence.Of(
            Sequence.Of<Expr>(new Expr.Identifier(new Ident("a"))),
            Sequence.Of<Expr>(new Expr.Identifier(new Ident("b")))));
        Assert.Equal("GROUPING SETS ((a), (b))", Formatter.Format(groupingSets));

        // a and b in the same group
        groupingSets = new Expr.GroupingSets(Sequence.Of(Sequence.Of<Expr>(
            new Expr.Identifier(new Ident("a")),
            new Expr.Identifier(new Ident("b")))));
        Assert.Equal("GROUPING SETS ((a, b))", Formatter.Format(groupingSets));

        // (a, b) and (c, d) in different group
        groupingSets = new Expr.GroupingSets(Sequence.Of(
            Sequence.Of<Expr>(
                new Expr.Identifier(new Ident("a")),
                new Expr.Identifier(new Ident("b"))),
            Sequence.Of<Expr>(
                new Expr.Identifier(new Ident("c")),
                new Expr.Identifier(new Ident("d")))));
        Assert.Equal("GROUPING SETS ((a, b), (c, d))", Formatter.Format(groupingSets));
    }

    [Fact]
    public void test_rollup_display()
    {
        var rollup = new Expr.Rollup(Sequence.Of(Sequence.Of<Expr>(new Expr.Identifier(new Ident("a")))));
        Assert.Equal("ROLLUP (a)", Formatter.Format(rollup));

        rollup = new Expr.Rollup(Sequence.Of(Sequence.Of<Expr>(
            new Expr.Identifier(new Ident("a")),
            new Expr.Identifier(new Ident("b")))));
        Assert.Equal("ROLLUP ((a, b))", Formatter.Format(rollup));

        rollup = new Expr.Rollup(Sequence.Of(
            Sequence.Of<Expr>(new Expr.Identifier(new Ident("a"))),
            Sequence.Of<Expr>(new Expr.Identifier(new Ident("b")))));
        Assert.Equal("ROLLUP (a, b)", Formatter.Format(rollup));

        rollup = new Expr.Rollup(Sequence.Of(
            Sequence.Of<Expr>(new Expr.Identifier(new Ident("a"))),
            Sequence.Of<Expr>(
                new Expr.Identifier(new Ident("b")),
                new Expr.Identifier(new Ident("c"))),
            Sequence.Of<Expr>(new Expr.Identifier(new Ident("d")))));
        Assert.Equal("ROLLUP (a, (b, c), d)", Formatter.Format(rollup));
    }

    [Fact]
    public void test_cube_display()
    {
        var cube = new Expr.Cube(Sequence.Of(Sequence.Of<Expr>(new Expr.Identifier(new Ident("a")))));
        Assert.Equal("CUBE (a)", Formatter.Format(cube));

        cube = new Expr.Cube(Sequence.Of(Sequence.Of<Expr>(
            new Expr.Identifier(new Ident("a")),
            new Expr.Identifier(new Ident("b")))));
        Assert.Equal("CUBE ((a, b))", Formatter.Format(cube));

        cube = new Expr.Cube(Sequence.Of(
            Sequence.Of<Expr>(new Expr.Identifier(new Ident("a"))),
            Sequence.Of<Expr>(new Expr.Identifier(new Ident("b")))));
        Assert.Equal("CUBE (a, b)", Formatter.Format(cube));

        cube = new Expr.Cube(Sequence.Of(
            Sequence.Of<Expr>(new Expr.Identifier(new Ident("a"))),
            Sequence.Of<Expr>(
                new Expr.Identifier(new Ident("b")),
                new Expr.Identifier(new Ident("c"))),
            Sequence.Of<Expr>(new Expr.Identifier(new Ident("d")))));
        Assert.Equal("CUBE (a, (b, c), d)", Formatter.Format(cube));
    }

    [Fact]
    public void test_interval_display()
    {
        var interval = new Expr.Interval(new Interval(
            Value: new Expr.Value(
                new Value.SingleQuotedString("123:45.67").WithEmptySpan()),
            LeadingField: new DateTimeField.Minute(),
            LeadingPrecision: 10,
            LastField: new DateTimeField.Second(),
            FractionalSecondsPrecision: 9));
        Assert.Equal(
            "INTERVAL '123:45.67' MINUTE (10) TO SECOND (9)",
            Formatter.Format(interval));

        interval = new Expr.Interval(new Interval(
            Value: new Expr.Value(
                new Value.SingleQuotedString("5").WithEmptySpan()),
            LeadingField: new DateTimeField.Second(),
            LeadingPrecision: 1,
            LastField: null,
            FractionalSecondsPrecision: 3));
        Assert.Equal("INTERVAL '5' SECOND (1, 3)", Formatter.Format(interval));
    }

    // `impl Deref<Target = [T]>`, `impl AsRef<[T]>` e `impl IntoIterator for &OneOrManyWithParens<T>`
    // viraram a implementação de IReadOnlyList<T> (decisão da A1, docs/port/onda-a/A1.md):
    // `x.deref()`/`x.as_ref()` → o próprio `x` visto como IReadOnlyList<T>; `x.index(i)`/`x[i]` → indexador;
    // `x.len()` → Count. As chamadas qualificadas pelo trait (`<.. as Deref>::deref(&x)`) viram um cast
    // para IReadOnlyList<T>. Literais vão na posição "expected" do Assert (regra xUnit2000).
    // xUnit2013 (Assert.Equal(1, x.Count)) está suprimido: o Rust verifica `len()`, e Assert.Single
    // enumeraria em vez de ler Count.
#pragma warning disable xUnit2013
    [Fact]
    public void test_one_or_many_with_parens_deref()
    {
        var one = new OneOrManyWithParens<string>.One("a");

        Assert.Equal(one, new[] { "a" });
        Assert.Equal((IReadOnlyList<string>)one, new[] { "a" });

        Assert.Equal("a", one[0]);
        Assert.Equal("a", one[0]);
        Assert.Equal(
            "a",
            ((IReadOnlyList<string>)one)[0]);

        Assert.Equal(1, one.Count);
        Assert.Equal(1, ((IReadOnlyList<string>)one).Count);

        var many1 = new OneOrManyWithParens<string>.Many(Sequence.Of("b"));

        Assert.Equal(many1, new[] { "b" });
        Assert.Equal((IReadOnlyList<string>)many1, new[] { "b" });

        Assert.Equal("b", many1[0]);
        Assert.Equal("b", many1[0]);
        Assert.Equal(
            "b",
            ((IReadOnlyList<string>)many1)[0]);

        Assert.Equal(1, many1.Count);
        Assert.Equal(1, ((IReadOnlyList<string>)many1).Count);

        var many2 = new OneOrManyWithParens<string>.Many(Sequence.Of("c", "d"));

        Assert.Equal(many2, new[] { "c", "d" });
        Assert.Equal(
            (IReadOnlyList<string>)many2,
            new[] { "c", "d" });

        Assert.Equal("c", many2[0]);
        Assert.Equal("c", many2[0]);
        Assert.Equal(
            "c",
            ((IReadOnlyList<string>)many2)[0]);

        Assert.Equal("d", many2[1]);
        Assert.Equal("d", many2[1]);
        Assert.Equal(
            "d",
            ((IReadOnlyList<string>)many2)[1]);

        Assert.Equal(2, many2.Count);
        Assert.Equal(2, ((IReadOnlyList<string>)many2).Count);
    }
#pragma warning restore xUnit2013

    [Fact]
    public void test_one_or_many_with_parens_as_ref()
    {
        var one = new OneOrManyWithParens<string>.One("a");

        Assert.Equal(one, new[] { "a" });
        Assert.Equal((IReadOnlyList<string>)one, new[] { "a" });

        var many1 = new OneOrManyWithParens<string>.Many(Sequence.Of("b"));

        Assert.Equal(many1, new[] { "b" });
        Assert.Equal((IReadOnlyList<string>)many1, new[] { "b" });

        var many2 = new OneOrManyWithParens<string>.Many(Sequence.Of("c", "d"));

        Assert.Equal(many2, new[] { "c", "d" });
        Assert.Equal(
            (IReadOnlyList<string>)many2,
            new[] { "c", "d" });
    }

    [Fact]
    public void test_one_or_many_with_parens_ref_into_iter()
    {
        var one = new OneOrManyWithParens<string>.One("a");

        Assert.Equal(new List<string>(one), new List<string> { "a" });

        var many1 = new OneOrManyWithParens<string>.Many(Sequence.Of("b"));

        Assert.Equal(new List<string>(many1), new List<string> { "b" });

        var many2 = new OneOrManyWithParens<string>.Many(Sequence.Of("c", "d"));

        Assert.Equal(new List<string>(many2), new List<string> { "c", "d" });
    }

    [Fact]
    public void test_one_or_many_with_parens_value_into_iter()
    {
        // use core::iter::once;

        TestSteps(new OneOrManyWithParens<int>.One(1), new OnceIter<int>(1), 3);
        TestSteps(new OneOrManyWithParens<int>.Many(Sequence.Of(2)), new VecIntoIter<int>(Sequence.Of(2)), 3);
        TestSteps(new OneOrManyWithParens<int>.Many(Sequence.Of(3, 4)), new VecIntoIter<int>(Sequence.Of(3, 4)), 4);
    }

    //tests that our iterator implemented methods behaves exactly as it's inner iterator, at every step up to n calls to next/next_back
    // Rust: `fn test_steps<I>` aninhada no teste, com
    // `I: IntoIterator<Item = usize, IntoIter: DoubleEndedIterator + Clone> + Clone`. O C# não tem
    // `IntoIterator`: `inner` já chega como o iterador (`once(1)` → `new OnceIter<int>(1)`; `vec![2]` →
    // `new VecIntoIter<int>(Sequence.Of(2))`, o resultado de `vec![2].into_iter()`), então
    // `inner.clone().into_iter()` → `inner.Clone()`. `ours.clone().into_iter()` → `ours.IntoIter()`
    // (records são imutáveis: o `clone()` não tem tradução, convencoes.md §9).
    private static void TestSteps<I>(OneOrManyWithParens<int> ours, I inner, int n)
        where I : IDoubleEndedIterator<int, I>
    {
        var oursNext = ours.IntoIter();
        var innerNext = inner.Clone();

        for (var i = 0; i < n; i++)
        {
            Checks(oursNext.Clone(), innerNext.Clone());

            Assert.Equal(oursNext.Next(), innerNext.Next());
        }

        var oursNextBack = ours.IntoIter();
        var innerNextBack = inner.Clone();

        for (var i = 0; i < n; i++)
        {
            Checks(oursNextBack.Clone(), innerNextBack.Clone());

            Assert.Equal(oursNextBack.NextBack(), innerNextBack.NextBack());
        }

        var oursMixed = ours.IntoIter();
        var innerMixed = inner.Clone();

        for (var i = 0; i < n; i++)
        {
            Checks(oursMixed.Clone(), innerMixed.Clone());

            if (i % 2 == 0)
            {
                Assert.Equal(oursMixed.NextBack(), innerMixed.NextBack());
            }
            else
            {
                Assert.Equal(oursMixed.Next(), innerMixed.Next());
            }
        }

        var oursMixed2 = ours.IntoIter();
        var innerMixed2 = inner; // inner.into_iter(): último uso de `inner` (movido no Rust)

        for (var i = 0; i < n; i++)
        {
            Checks(oursMixed2.Clone(), innerMixed2.Clone());

            if (i % 2 == 0)
            {
                Assert.Equal(oursMixed2.Next(), innerMixed2.Next());
            }
            else
            {
                Assert.Equal(oursMixed2.NextBack(), innerMixed2.NextBack());
            }
        }
    }

    // Rust: `fn checks<I>` aninhada em `test_steps`, com `I: Iterator<Item = usize> + Clone + DoubleEndedIterator`.
    private static void Checks<I>(OneOrManyWithParensIntoIter<int> ours, I inner)
        where I : IDoubleEndedIterator<int, I>
    {
        Assert.Equal(ours.SizeHint(), inner.SizeHint());
        Assert.Equal(ours.Clone().Count(), inner.Clone().Count());

        Assert.Equal(
            ours.Clone().Fold(1, (a, v) => a + v),
            inner.Clone().Fold(1, (a, v) => a + v));

        Assert.Equal(new List<int>(ours.Clone()), new List<int>(inner.Clone()));
        Assert.Equal(
            new List<int>(ours.Clone().Rev()),
            new List<int>(inner.Clone().Rev()));
    }

    // Tests that the position in the code of an `Ident` does not affect its
    // ordering.
    [Fact]
    public void test_ident_ord()
    {
        var a = Ident.WithSpan(new Span(new Location(1, 1), new Location(1, 1)), "a");
        var b = Ident.WithSpan(new Span(new Location(2, 2), new Location(2, 2)), "b");

        Assert.True(Comparer<Ident>.Default.Compare(a, b) < 0);
        // std::mem::swap(&mut a.span, &mut b.span)
        (a, b) = (a with { Span = b.Span }, b with { Span = a.Span });
        Assert.True(Comparer<Ident>.Default.Compare(a, b) < 0);
    }
}
