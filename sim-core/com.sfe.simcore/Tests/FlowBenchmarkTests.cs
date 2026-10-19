using NUnit.Framework;
using SFE.SimCore.Flow;

namespace SFE.SimCore.Tests
{
    /// <summary>
    /// Rechenzeit je Schritt für das Budget „≤ 4 ms pro Frame, ganzer Lauf ≤ 10 s“ (docs/04 Abschnitt 4.3).
    /// Nur auf Wunsch: <c>dotnet test -c Release --filter FlowBenchmark</c>. WebGL ist etwa 2- bis 3-mal langsamer.
    /// </summary>
    public class FlowBenchmarkTests
    {
        [Test, Explicit("Zeitmessung, kein Korrektheitstest")]
        public void FlowBenchmark_StepTime()
        {
            var lv = FlowGoldenTests.Level;
            foreach (var res in lv.Resolutions)
            {
                var run = new TrainRun(lv.Shapes["hint"], lv, res);
                run.Step(50);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                run.Step(200);
                double ms = sw.Elapsed.TotalMilliseconds / 200;
                TestContext.Progress.WriteLine($"{res.Id}: {res.Nx}x{res.Ny}, {ms:0.000} ms je Schritt, ganzer Lauf {ms * run.TotalSteps / 1000:0.0} s");
            }
        }
    }
}
