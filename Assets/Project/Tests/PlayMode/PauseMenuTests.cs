using System.Collections;
using NUnit.Framework;
using Project.GameSystems.Management;
using UnityEngine;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode {
    public class PauseMenuTests
    {
        [UnityTest]
        public IEnumerator PauseMenu_DefaultState()
        {
            GameObject gm = GameObject.Instantiate(new GameObject());
            PauseManager pauseManager = gm.AddComponent<PauseManager>();
            yield return null;
            Assert.That(pauseManager.IsPaused == false);
        }
    }
}
