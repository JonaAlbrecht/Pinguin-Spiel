using System;

namespace SFE.SimCore.LinearAlgebra
{
    /// <summary>Dünnbesetzte Matrix im CSR-Format (Compressed Sparse Row).</summary>
    public sealed class CsrMatrix
    {
        public int Size { get; }
        public int[] RowStart { get; }
        public int[] Columns { get; }
        public double[] Values { get; }

        public CsrMatrix(int size, int[] rowStart, int[] columns, double[] values)
        {
            if (rowStart.Length != size + 1) throw new ArgumentException("rowStart muss Size + 1 Einträge haben.");
            Size = size;
            RowStart = rowStart;
            Columns = columns;
            Values = values;
        }

        public int NonZeros => Values.Length;

        /// <summary>y = A·x (y wird überschrieben, keine Allokation).</summary>
        public void Multiply(double[] x, double[] y)
        {
            for (int i = 0; i < Size; i++)
            {
                double sum = 0.0;
                for (int k = RowStart[i]; k < RowStart[i + 1]; k++)
                    sum += Values[k] * x[Columns[k]];
                y[i] = sum;
            }
        }

        public double Get(int row, int col)
        {
            for (int k = RowStart[row]; k < RowStart[row + 1]; k++)
                if (Columns[k] == col) return Values[k];
            return 0.0;
        }
    }
}
