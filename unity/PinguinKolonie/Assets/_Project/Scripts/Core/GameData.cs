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
        public Dictionary<string, object> Mg1Content;
        public Dictionary<string, object> UiText;
        public Dictionary<string, object> TransferCard;
        public readonly Dictionary<string, List<DialogueLine>> Dialogue = new Dictionary<string, List<DialogueLine>>();
        public readonly Dictionary<string, Speaker> Speakers = new Dictionary<string, Speaker>();

        public static GameData Load()
        {
            var d = new GameData
            {
                Settlement = SettlementLevel.FromJson(Text("Data/Levels/MG1_Huddle_Settlement")),
                Mg1Content = MiniJson.ParseObject(Text("Data/Content/mg1_content_de")),
                UiText = MiniJson.ParseObject(Text("Data/Content/ui_de")),
                TransferCard = MiniJson.ParseObject(Text("Data/Transfer/transfer_MG1_switchgear")),
            };
            d.Materials = SettlementMaterialSet.FromJson(Text("Data/Materials/" + d.Settlement.MaterialsId));

            // Jedes Minigame bringt eigene Textdateien mit (ui_mg2_de, dialogue_mg2_de …) – so arbeiten mehrere
            // Pakete nebeneinander, ohne dieselbe Datei zu ändern. Doppelte Schlüssel sind ein Fehler.
            foreach (var asset in Resources.LoadAll<TextAsset>("Data/Content"))
                if (asset.name.StartsWith("ui_") && asset.name != "ui_de") MergeUi(d.UiText, MiniJson.ParseObject(asset.text), asset.name);
            d.LoadDialogue(Text("Data/Dialogue/dialogue_de"));
            foreach (var asset in Resources.LoadAll<TextAsset>("Data/Dialogue"))
                if (asset.name != "dialogue_de") d.LoadDialogue(asset.text);
            return d;
        }

        private void LoadDialogue(string json)
        {
            var dlg = MiniJson.ParseObject(json);
            foreach (var s in dlg.Arr("speakers"))
            {
                var o = (Dictionary<string, object>)s;
                ColorUtility.TryParseHtmlString(o.Str("color"), out Color c);
                Speakers[o.Str("id")] = new Speaker { Name = o.Str("name"), Color = c };
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
                if (Dialogue.ContainsKey(o.Str("id"))) Debug.LogError("Dialog doppelt: " + o.Str("id"));
                Dialogue[o.Str("id")] = lines;
            }
        }

        /// <summary>Oberflächentexte eines Minigames dazunehmen; <c>underTheHood</c> und dessen <c>formulas</c> werden zusammengeführt.</summary>
        private static void MergeUi(Dictionary<string, object> into, Dictionary<string, object> extra, string source)
        {
            foreach (var kv in extra)
            {
                if (kv.Value is Dictionary<string, object> sub && into.TryGetValue(kv.Key, out object existing) &&
                    existing is Dictionary<string, object> target)
                {
                    MergeUi(target, sub, source);
                    continue;
                }
                if (kv.Key == "note") continue; // Kommentar in der JSON-Datei, kein Text (sonst „doppelt“ bei jedem Minigame)
                if (into.ContainsKey(kv.Key)) Debug.LogError($"Oberflächentext doppelt ({source}): {kv.Key}");
                into[kv.Key] = kv.Value;
            }
        }

        public string Content(string key) => Mg1Content.Str(key);

        private readonly Dictionary<string, Dictionary<string, object>> _minigameContent = new Dictionary<string, Dictionary<string, object>>();

        /// <summary>Inhalte eines Minigames aus <c>Data/Content/&lt;id&gt;_content_de.json</c> (Karten, Glossar, Phasen …).</summary>
        public Dictionary<string, object> MinigameContent(string id)
        {
            if (!_minigameContent.TryGetValue(id, out var c))
                _minigameContent[id] = c = MiniJson.ParseObject(Text("Data/Content/" + id + "_content_de"));
            return c;
        }

        /// <summary>
        /// Oberflächentext aus <c>ui_de.json</c>. Alle Spieltexte kommen von hier oder aus den Dialogen –
        /// nie als C#-Literal, sonst prüft sie der Text-Lint nicht gegen die Sprachregeln.
        /// </summary>
        public string Ui(string key, params object[] args) => Format(UiText, key, args);

        /// <summary>Technischer Text, nur für „Blick unter die Haube“ (vom Text-Lint ausgenommen).</summary>
        public string Tech(string key, params object[] args) => Format(UiText.Obj("underTheHood"), key, args);

        private static string Format(Dictionary<string, object> table, string key, object[] args)
        {
            if (!table.TryGetValue(key, out object v) || !(v is string s))
            {
                Debug.LogWarning("Oberflächentext fehlt: " + key);
                return key;
            }
            return args == null || args.Length == 0 ? s : string.Format(German, s, args);
        }

        /// <summary>
        /// Deutsche Zahlen (Komma, Tausenderpunkt) ohne Kulturdaten: WebGL-Builds laufen mit invarianter
        /// Globalisierung, dort fehlt „de-DE“ – darum von Hand statt CultureInfo.GetCultureInfo.
        /// </summary>
        public static readonly System.IFormatProvider German = MakeGerman();

        private static System.IFormatProvider MakeGerman()
        {
            var nfi = (System.Globalization.NumberFormatInfo)System.Globalization.CultureInfo.InvariantCulture.NumberFormat.Clone();
            nfi.NumberDecimalSeparator = ",";
            nfi.NumberGroupSeparator = ".";
            return nfi;
        }

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
        public bool Mg1Done;
        /// <summary>-1 = noch keine Runde gespielt. Gilt über „nochmal planen“ hinweg (F-02: die beste Runde zählt).</summary>
        public int Mg1BestStars = -1;
        public string[] Mg1BestLayout;
        /// <summary>End-Raster der besten Runde (geschmolzene Iglus als Wasser) – dazu passen die IglooStates.</summary>
        public string[] Mg1BestFinalRows;
        public SettlementResult Mg1BestResult;
        public double[] Mg1BestTemperatures;
        public Texture2D Mg1Snapshot;
        /// <summary>Foto von Kalles geschmolzener Siedlung draußen im Hub – in Schritt 4 neben der Rechnung.</summary>
        public Texture2D KallePhoto;
        public readonly List<string> Glossary = new List<string>();
        /// <summary>Wie viele Iglus im ganzen Spiel zu Pfützen wurden – Flockes Sammlung im Notizbuch.</summary>
        public int Puddles;
        /// <summary>Freigeschaltete Karten der Siemens-Landkarte (docs/00 Abschnitt 4).</summary>
        public int MapCards;
        /// <summary>Schaltschrank (MG1), MG2, MG3 – docs/00 Abschnitt 4. Der Akku-Transfer nach dem Prolog entfiel am 18.10.2026.</summary>
        public const int MapCardsTotal = 3;
        /// <summary>Satz zum Weitererzählen, sobald die Transfer-Karte gesehen wurde.</summary>
        public string Takeaway;
    }
}
