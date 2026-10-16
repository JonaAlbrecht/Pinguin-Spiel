using System;

namespace SFE.SimCore.Thermal
{
    /// <summary>
    /// Windrichtungen wie im Wetterbericht: Der Name sagt, <b>woher</b> der Wind kommt („NW“ = aus Nordwest).
    /// Die Vektoren zeigen, <b>wohin</b> er weht, normiert, mit x = Osten und y = Norden.
    /// </summary>
    public static class WindDirection
    {
        public static readonly string[] Names = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        public static void BlowsTowards(string from, out double east, out double north)
        {
            switch (from)
            {
                case "N": east = 0; north = -1; break;
                case "NE": east = -1; north = -1; break;
                case "E": east = -1; north = 0; break;
                case "SE": east = -1; north = 1; break;
                case "S": east = 0; north = 1; break;
                case "SW": east = 1; north = 1; break;
                case "W": east = 1; north = 0; break;
                case "NW": east = 1; north = -1; break;
                default: throw new ArgumentException($"Unbekannte Windrichtung '{from}'.");
            }
            double n = Math.Sqrt(east * east + north * north);
            east /= n;
            north /= n;
        }
    }
}
