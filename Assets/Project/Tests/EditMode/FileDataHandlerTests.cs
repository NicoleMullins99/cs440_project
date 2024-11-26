using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Project.GameSystems.DataManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Project.Tests.EditMode {
    public class FileDataHandlerTests {

        private const string TestDataPath = "Project\\Tests\\Test Data";
        
        [Test]
        public void Save_InvalidFolderName() {
            FileDataHandler.Save(new SaveFileData(), "]]]test::'", "test.game");
            LogAssert.Expect(LogType.Error, new Regex(".*Error while trying to save data to.*"));
        }
        
        [Test]
        public void Save_InvalidFileName() {
            FileDataHandler.Save(new SaveFileData(), "testFolder", "\\\\\\\\test.game");
            LogAssert.Expect(LogType.Error, new Regex(".*Error while trying to save data to.*"));
        }
        
        [Test]
        public void Save_ValidFormat() {
            Assert.AreEqual(true,FileDataHandler.Save(new SaveFileData(), "testFolder", "test.game"));
        }
        
        [Test]
        public void Load_InvalidFilePath() {
            FileDataHandler.Load<SaveFileData>("\\\\\\\\test.game", out _);
            LogAssert.Expect(LogType.Error, new Regex(".*File does not exist at path.*"));
        }
        
        [Test]
        public void Load_InvalidFile() {
            string path = Path.Combine(Application.dataPath, TestDataPath, "invalid_save.json");
            FileDataHandler.Load<SaveFileData>(path, out _);
            LogAssert.Expect(LogType.Error, new Regex(".*Error while loading file at path.*"));
        }
        
        [Test]
        public void Load_ValidFile() {
            string path = Path.Combine(Application.dataPath, TestDataPath, "valid_save.json");
            FileDataHandler.Load(path, out SaveFileData data);
            Assert.NotNull(data);
        }
    }
}