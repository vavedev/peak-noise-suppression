using System.IO;
using UnityEngine;

namespace PeakNoiseSuppression
{
    public sealed class ConfigReloader : MonoBehaviour
    {
        private FileSystemWatcher? watcher;
        private string? configPath;

        private void Start()
        {
            configPath = Plugin.Instance.Config.ConfigFilePath;
            if (string.IsNullOrEmpty(configPath)) return;

            watcher = new FileSystemWatcher(Path.GetDirectoryName(configPath)!)
            {
                Filter = Path.GetFileName(configPath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            watcher.Changed += (_, __) => OnConfigChanged();
            watcher.EnableRaisingEvents = true;
        }

        private void OnConfigChanged()
        {
            try
            {
                Plugin.Instance.Config.Reload();
                foreach (var f in Object.FindObjectsByType<SpectralGateFilter>(FindObjectsSortMode.None))
                    f.UpdateParameters();
            }
            catch { }
        }

        private void OnDestroy() => watcher?.Dispose();
    }
}
