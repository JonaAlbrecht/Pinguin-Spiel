using System.Collections.Generic;
using SFE.SimCore.Data;
using SFE.SimCore.Thermal;
using UnityEngine;

namespace SFE.Game
{
    public sealed class DialogueLine
    {
        public string Speaker;
        public string Text;
    }

    public sealed class Speaker
    {
        public string Name;
        public Color Color;
    }

    /// <summary>
    /// Lädt alle Leveldaten und Texte aus Resources/Data. Die Dateien sind dieselben, die auch die
    /// Python-Referenz und die dotnet-Tests lesen (docs/04 Abschnitt 5.4).
    /// </summary>
    public sealed class GameData
    {
        public SettlementLevel Settlement;
        public SettlementMaterialSet Materials;
        public HuddleLevel Huddle;
        public Dictionary<string, object> Mg1Content;
        public Dictionary<string, object> TransferCard;
        public readonly Dictionary<string, List<DialogueLine>> Dialogue = new Dictionary<string, List<DialogueLine>>();
        public readonly Dictionary<string, Speaker> Speakers = new Dictionary<string, Speaker>();

        public static GameData Load()
        {
            var d = new GameData
            {
                Settlement = SettlementLevel.FromJson(Text("Data/Levels/MG1_Huddle_Settlement")),
                Huddle = HuddleLevel.FromJson(Text("Data/Levels/MG1A_LastHuddle")),
                Mg1Content = MiniJson.ParseObject(Text("Data/Content/mg1_content_de")),
                TransferCard = MiniJson.ParseObject(Text("Data/Transfer/transfer_MG1_switchgear")),
            };
            d.Materials = SettlementMaterialSet.FromJson(Text("Data/Materials/" + d.Settlement.MaterialsId));

            var dlg = MiniJson.ParseObject(Text("Data/Dialogue/dialogue_de"));
            foreach (var s in dlg.Arr("speakers"))
            {
                var o = (Dictionary<string, object>)s;
                ColorUtility.TryParseHtmlString(o.Str("color"), out Color c);
                d.Speakers[o.Str("id")] = new Speaker { Name = o.Str("name"), Color = c };
            }
            foreach (var n in dlg.Arr("nodes"))
            {
                var o = (Dictionary<string, object>)n;
                var lines = new List<DialogueLine>();
                foreach (var l in o.Arr("lines"))
                {
                    var lo = (Dictionary<string, object>)l;
                    lines.Add(new DialogueLine { Speaker = lo.Str("speaker"), Text = lo.Str("text") });
                }
                d.Dialogue[o.Str("id")] = lines;
            }
            return d;
        }

        public string Content(string key) => Mg1Content.Str(key);

        public static string Text(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null) throw new System.IO.FileNotFoundException("Ressource fehlt: Resources/" + resourcePath + ".json");
            return asset.text;
        }

        /// <summary>Text in der aktuellen Sprache aus einem Objekt wie {"de": "…"} (Frage K-01: Prototyp nur DE).</summary>
        public static string Loc(object localized)
        {
            if (localized is Dictionary<string, object> o && o.TryGetValue("de", out object v)) return v as string ?? "";
            return localized as string ?? "";
        }
    }

    /// <summary>Spielstand der laufenden Sitzung (Frage R-01: im Prototyp kein Speichern).</summary>
    public sealed class GameState
    {
        public bool PrologueDone;
        public bool Mg1Done;
        public int Mg1BestStars;
        public string[] Mg1BestLayout;
        public SettlementResult Mg1BestResult;
        public double[] Mg1BestTemperatures;
        public Texture2D Mg1Snapshot;
        public readonly List<string> Glossary = new List<string>();
    }
}
