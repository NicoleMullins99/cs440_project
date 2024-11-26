using System.Collections;
using NUnit.Framework;
using Project.GameSystems.DataManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode {
    public class PlayerDataStreamTests
    {
        [UnityTest]
        public IEnumerator LSL_Stream_OpenOnStartUp()
        {
            GameObject gm = GameObject.Instantiate(new GameObject());
            PlayerDataStream playerDataStream = gm.AddComponent<PlayerDataStream>();
            yield return null;
            Assert.That(playerDataStream.IsStreamOpen());
        }
    }
}
