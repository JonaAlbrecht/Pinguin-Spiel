using NUnit.Framework;
using SFE.SimCore.LinearAlgebra;

namespace SFE.SimCore.Tests
{
    public class LinearAlgebraTests
    {
        [Test]
        public void Builder_SumsDuplicateEntries_AndReportsEachAdd()
        {
            int calls = 0;
            var b = new SparseMatrixBuilder(2, (i, j, v) => calls++);
            b.Add(0, 0, 1.5);
            b.Add(0, 0, 2.5);
            b.Add(1, 0, -1.0);
            var a = b.Build();
            Assert.AreEqual(4.0, a.Get(0, 0));
            Assert.AreEqual(-1.0, a.Get(1, 0));
            Assert.AreEqual(0.0, a.Get(1, 1));
            Assert.AreEqual(2, a.NonZeros);
            Assert.AreEqual(3, calls);
        }

        [Test]
        public void ConjugateGradient_SolvesSmallSpdSystem()
        {
            // [4 1; 1 3] x = [1; 2]  ->  x = [1/11; 7/11]
            var b = new SparseMatrixBuilder(2);
            b.Add(0, 0, 4); b.Add(0, 1, 1); b.Add(1, 0, 1); b.Add(1, 1, 3);
            var cg = new ConjugateGradientSolver(b.Build(), new[] { 1.0, 2.0 });
            var x = cg.SolveToEnd();
            Assert.AreEqual(1.0 / 11.0, x[0], 1e-12);
            Assert.AreEqual(7.0 / 11.0, x[1], 1e-12);
            Assert.LessOrEqual(cg.Iteration, 2, "CG löst ein 2×2-System in höchstens 2 Schritten.");
        }

        [Test]
        public void ConjugateGradient_IsSteppable()
        {
            const int n = 50;
            var b = new SparseMatrixBuilder(n);
            for (int i = 0; i < n; i++)
            {
                b.Add(i, i, 2.1);
                if (i > 0) { b.Add(i, i - 1, -1); b.Add(i - 1, i, -1); }
            }
            var rhs = new double[n];
            for (int i = 0; i < n; i++) rhs[i] = 1.0;
            var cg = new ConjugateGradientSolver(b.Build(), rhs);
            cg.Step();
            Assert.AreEqual(1, cg.Iteration);
            Assert.IsFalse(cg.IsConverged);
            Assert.AreNotEqual(0.0, cg.Solution[0], "Ein Schritt verändert die Lösung.");
            cg.Step(5);
            Assert.AreEqual(6, cg.Iteration);
            cg.SolveToEnd();
            Assert.IsTrue(cg.IsConverged);
            Assert.LessOrEqual(cg.Iteration, n, "CG konvergiert in höchstens n Schritten.");
        }
    }
}
