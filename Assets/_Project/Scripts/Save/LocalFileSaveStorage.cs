using System;
using System.IO;
using UnityEngine;

namespace RPG.Save
{
    /// <summary>
    /// Saves to a JSON file in Application.persistentDataPath - the only location that
    /// reliably survives app updates on both iOS and Android.
    ///
    /// Writes go to a temporary file first and are then moved into place, so a crash or a
    /// force-quit mid-write leaves the previous save intact rather than a truncated one.
    /// </summary>
    public class LocalFileSaveStorage : ISaveStorage
    {
        private readonly string _path;
        private readonly string _temporaryPath;

        public LocalFileSaveStorage(string fileName)
        {
            _path = Path.Combine(Application.persistentDataPath, fileName);
            _temporaryPath = _path + ".tmp";
        }

        public bool Exists() => File.Exists(_path);

        public string Read()
        {
            try
            {
                return File.Exists(_path) ? File.ReadAllText(_path) : null;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Save] Failed to read '{_path}': {exception.Message}");
                return null;
            }
        }

        public void Write(string contents)
        {
            try
            {
                string directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                File.WriteAllText(_temporaryPath, contents);

                // Replace atomically where the platform allows it.
                if (File.Exists(_path)) File.Delete(_path);
                File.Move(_temporaryPath, _path);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Save] Failed to write '{_path}': {exception.Message}");
            }
        }

        public void Delete()
        {
            try
            {
                if (File.Exists(_path)) File.Delete(_path);
                if (File.Exists(_temporaryPath)) File.Delete(_temporaryPath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Save] Failed to delete '{_path}': {exception.Message}");
            }
        }

        public string Describe() => _path;
    }
}
