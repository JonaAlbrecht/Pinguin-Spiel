using System;
using System.Collections.Generic;
using SFE.SimCore.Data;

namespace SFE.SimCore.Thermal
{
    /// <summary>Zellsymbole der Siedlung (gleich in JSON, Python und C#).</summary>
    public static class Cell
    {
        public const char Ice = '.';
        public const char Igloo = 'I';
        public const char Lantern = 'L';
        public const char Wall = 'W';
        public const char Path = '=';
        public const char Water = '~';
    }

    public sealed class SettlementMaterial
    {
        public string Key;
        public char Symbol;
        /// <summary>Wärmeleitwert zu den Nachbarzellen.</summary>
        public double K;
        /// <summary>Wärmeverlust an die Luft (vor Wind- und Abschirmfaktor).</summary>
        public double H;
        /// <summary>Eigene Wärmequelle (Laternen nutzen stattdessen <see cref="SettlementLevel.LanternHeat"/>).</summary>
        public double Q;
    }

    public sealed class SettlementMaterialSet
    {
        public string Id;
        public double ShieldRange;
        public double ShieldCone;
        public double ShieldFactor;
        public double WindReference;
        public readonly Dictionary<char, SettlementMaterial> BySymbol = new Dictionary<char, SettlementMaterial>();

        public SettlementMaterial Get(char symbol)
        {
            if (!BySymbol.TryGetValue(symbol, out var m)) throw new ArgumentException($"Unbekanntes Zellsymbol '{symbol}'.");
            return m;
        }

        public static SettlementMaterialSet FromJson(string json)
        {
            var o = MiniJson.ParseObject(json);
            var set = new SettlementMaterialSet
            {
                Id = o.Str("id"),
                ShieldRange = o.Num("shieldRange"),
                ShieldCone = o.Num("shieldCone"),
                ShieldFactor = o.Num("shieldFactor"),
                WindReference = o.Num("windReference"),
            };
            foreach (var item in o.Arr("materials"))
            {
                var m = (Dictionary<string, object>)item;
                var mat = new SettlementMaterial
                {
                    Key = m.Str("key"),
                    Symbol = m.Str("symbol")[0],
                    K = m.Num("k"),
                    H = m.Num("h"),
                    Q = m.Num("q"),
                };
                set.BySymbol[mat.Symbol] = mat;
            }
            return set;
        }
    }

    public sealed class SettlementLevel
    {
        public string Id;
        public int Width;
        public int Height;
        public double CellSize;
        public double AirTemp;
        public double Wind;
        public string WindFrom;
        public string MaterialsId;
        public double LanternHeat;
        public int LanternCost;
        public int IglooLimit;
        public int WallLimit;
        public double ComfortMin;
        public double Melt;
        public int IglooTarget;
        public int Budget;
        public int Rounds;
        public int MaxMeltPasses;
        public string[] InitialTiles;
        public string[] KalleTiles;
        public string[] HintTiles;

        public static SettlementLevel FromJson(string json)
        {
            var o = MiniJson.ParseObject(json);
            var grid = o.Obj("grid");
            var climate = o.Obj("climate");
            var lantern = o.Obj("lantern");
            var goals = o.Obj("goals");
            return new SettlementLevel
            {
                Id = o.Str("id"),
                Width = grid.Int("width"),
                Height = grid.Int("height"),
                CellSize = grid.Num("cellSize"),
                AirTemp = climate.Num("airTemp"),
                Wind = climate.Num("wind"),
                WindFrom = climate.Str("windDirection"),
                MaterialsId = o.Str("materials"),
                LanternHeat = lantern.Num("heat"),
                LanternCost = lantern.Int("cost"),
                IglooLimit = o.Int("iglooLimit"),
                WallLimit = o.Int("wallLimit"),
                ComfortMin = goals.Num("iglooComfortMin"),
                Melt = goals.Num("iglooMelt"),
                IglooTarget = goals.Int("iglooTarget"),
                Budget = o.Int("budget"),
                Rounds = o.Int("rounds"),
                MaxMeltPasses = o.Int("maxMeltPasses"),
                InitialTiles = o.StrArray("initialTiles"),
                KalleTiles = o.StrArray("kalleTiles"),
                HintTiles = o.StrArray("hintTiles"),
            };
        }

        /// <summary>Wie viele Laternen das Strom-Budget erlaubt.</summary>
        public int MaxLanterns => LanternCost > 0 ? Budget / LanternCost : int.MaxValue;
    }

    /// <summary>Das Raster der Siedlung. Zeile 0 liegt im Norden, Spalte 0 im Westen.</summary>
    public sealed class SettlementGrid
    {
        private readonly char[] _cells;

        public int Width { get; }
        public int Height { get; }

        public SettlementGrid(int width, int height, char fill = Cell.Ice)
        {
            Width = width;
            Height = height;
            _cells = new char[width * height];
            for (int i = 0; i < _cells.Length; i++) _cells[i] = fill;
        }

        public static SettlementGrid FromRows(string[] rows)
        {
            if (rows == null || rows.Length == 0) throw new ArgumentException("Leeres Raster.");
            var g = new SettlementGrid(rows[0].Length, rows.Length);
            for (int y = 0; y < rows.Length; y++)
            {
                if (rows[y].Length != g.Width) throw new ArgumentException($"Zeile {y} hat die falsche Länge.");
                for (int x = 0; x < g.Width; x++) g[x, y] = rows[y][x];
            }
            return g;
        }

        public char this[int x, int y]
        {
            get => _cells[y * Width + x];
            set => _cells[y * Width + x] = value;
        }

        public char this[int index]
        {
            get => _cells[index];
            set => _cells[index] = value;
        }

        public int CellCount => _cells.Length;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public int Count(char symbol)
        {
            int n = 0;
            foreach (char c in _cells) if (c == symbol) n++;
            return n;
        }

        public SettlementGrid Clone()
        {
            var g = new SettlementGrid(Width, Height);
            Array.Copy(_cells, g._cells, _cells.Length);
            return g;
        }

        public string[] ToRows()
        {
            var rows = new string[Height];
            for (int y = 0; y < Height; y++) rows[y] = new string(_cells, y * Width, Width);
            return rows;
        }
    }
}
