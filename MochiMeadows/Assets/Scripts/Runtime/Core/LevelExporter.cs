using System.IO;
using UnityEngine;

namespace MochiMeadows.Core
{
    // Dev tool: `-export-level` writes the current world layout as JSON
    // (persistentDataPath/level_export.json). Edit it and relaunch — the game
    // picks it up without a rebuild. Copy it to StreamingAssets/level.json to
    // ship a custom level with the build.
    public static class LevelExporter
    {
        public static string Export()
        {
            string dir = Application.persistentDataPath;
            string path = Path.Combine(dir, "level_export.json");
            File.WriteAllText(path, JsonUtility.ToJson(LevelConfig.Current, true));
            Debug.Log($"[Level] Exported current layout to {path}");
            return path;
        }
    }
}
