using System;
using System.Collections.Generic;

namespace SFE.SimCore.LinearAlgebra
{
    /// <summary>
    /// Sammelt Matrixeinträge (Assemblierung). Mehrfache Einträge an derselben Stelle werden summiert.
    /// Der optionale Callback meldet jeden Eintrag, damit die Spiel-Schicht den Systemaufbau zeigen kann.
    /// </summary>
    public sealed class SparseMatrixBuilder
    {
        private readonly Dictionary<long, double> _entries = new Dictionary<long, double>();
        private readonly Action<int, int, double> _onAdd;

        public int Size { get; }

        public SparseMatrixBuilder(int size, Action<int, int, double> onAdd = null)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            Size = size;
            _onAdd = onAdd;
        }

        public void Add(int row, int col, double value)
        {
            if ((uint)row >= (uint)Size || (uint)col >= (uint)Size) throw new ArgumentOutOfRangeException();
            long key = (long)row * Size + col;
            _entries.TryGetValue(key, out double current);
            _entries[key] = current + value;
            _onAdd?.Invoke(row, col, value);
        }

        public CsrMatrix Build()
        {
            var keys = new List<long>(_entries.Keys);
            keys.Sort();
            var rowStart = new int[Size + 1];
            var cols = new int[keys.Count];
            var vals = new double[keys.Count];
            for (int k = 0; k < keys.Count; k++)
            {
                int row = (int)(keys[k] / Size);
                cols[k] = (int)(keys[k] % Size);
                vals[k] = _entries[keys[k]];
                rowStart[row + 1]++;
            }
            for (int i = 0; i < Size; i++) rowStart[i + 1] += rowStart[i];
            return new CsrMatrix(Size, rowStart, cols, vals);
        }
    }
}
