using System;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Project.GameSystems.DataManagement {
    public static class FileDataHandler {
        public static bool Save<T>(T data, string saveFolderName, string dataFileName) {
            // Base case if null
            if (saveFolderName == null) {
                return false;
            }

            // Different OS have different file path syntax
            string fullPath = Path.Combine(Application.persistentDataPath, saveFolderName, dataFileName);

            try {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                string dataToStore = JsonUtility.ToJson(data, true);

                // using block ensures the connection to the file is closed when done reading / writing
                using (FileStream stream = new FileStream(fullPath, FileMode.Create)) {
                    using (StreamWriter writer = new StreamWriter(stream)) {
                        writer.Write(dataToStore);
                    }
                }
            }
            catch (Exception e) {
                Debug.LogError("Error while trying to save data to " + fullPath + " : " + e);
                return false;
            }

            return true;
        }

        public static bool Load<T>(string filePath, out T saveFileData) {
            // Base case if null
            saveFileData = default(T);

            if (filePath == null) {
                return false;
            }

            if (File.Exists(filePath)) {
                try {
                    // Load serialized data
                    string dataToLoad = "";
                    using (FileStream stream = new FileStream(filePath, FileMode.Open)) {
                        using (StreamReader reader = new StreamReader(stream)) {
                            dataToLoad = reader.ReadToEnd();
                        }
                    }

                    // Deserialize
                    saveFileData = JsonUtility.FromJson<T>(dataToLoad);
                    return true;
                }
                catch (Exception e) {
                    Debug.LogError("Error while loading file at path: " + filePath + "\n" + e);
                    return false;
                }
            }

            Debug.LogError("File does not exist at path: " + filePath + "\n");
            return false;
        }

        public static bool FileExists(string saveFilePath) {
            return File.Exists(saveFilePath);
        }
    }
}