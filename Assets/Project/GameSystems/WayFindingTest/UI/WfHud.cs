using TMPro;
using UnityEngine;

namespace Project.GameSystems.WayFindingTest.UI {
    public class WfHud : MonoBehaviour {
        [SerializeField] private TMP_Text _timer;
        [SerializeField] private TMP_Text _target;

        private float _time;
        private float _remainingTime;
        private bool _timerEnabled;

        public void StartTimer(WayFindingTask.TaskSettings task) {
            _time = task.TaskTargetSettings.TaskTimer;
            _remainingTime = _time;
            _target.text = $"Target: {task.TaskTarget}";
            _timerEnabled = true;
        }

        public void StartTimer() {
            _time = 0;
            _remainingTime = 0;
            _timerEnabled = true;
        }

        public void SetTargetText(string text) {
            _target.text = $"Target: {text}";
        }
        
        public void ToggleTimer(bool state) {
            _timer.gameObject.SetActive(state);
        }

        public void StopTimer() {
            _timerEnabled = false;
        }

        public void ResetTimer() {
            _remainingTime = 0;
        }

        private void Update() {
            if (!_timerEnabled) return;

            _remainingTime += Time.deltaTime;
            _timer.text = $"Time: {Mathf.Max(Mathf.Round(_remainingTime * 100f) / 100f, 0)}";

            if (_remainingTime < 0) _timerEnabled = false;
        }
    }
}