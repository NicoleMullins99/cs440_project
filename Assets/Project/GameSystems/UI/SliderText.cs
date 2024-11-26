using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.GameSystems.UI
{
    public class SliderText : MonoBehaviour
    {
        [SerializeField] private TMP_Text _tmpText;
        [SerializeField] private Slider _slider;

        public void SetText()
        {
            _tmpText.text = _slider.value.ToString();
        }
    }
}