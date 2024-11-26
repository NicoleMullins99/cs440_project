using TMPro;
using UnityEngine;

namespace Project.GameSystems.WayFindingTest.UI {
    public class WfTaskUi : MonoBehaviour {
        [SerializeField] private TMP_Text _tmpText;

        public void SetTaskText(string text) {
            _tmpText.text = text;
        }
    }
}
