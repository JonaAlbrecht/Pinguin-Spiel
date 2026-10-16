namespace SFE.SimCore
{
    /// <summary>
    /// Ein Löser, der in kleinen Schritten rechnet. So kann die Spiel-Schicht die Iterationen
    /// über mehrere Frames verteilen und animieren, ohne dass WebGL blockiert.
    /// </summary>
    public interface IStepSolver
    {
        bool IsConverged { get; }
        int Iteration { get; }
        double Residual { get; }

        /// <summary>Rechnet höchstens <paramref name="maxIterations"/> Iterationen.</summary>
        void Step(int maxIterations = 1);
    }
}
