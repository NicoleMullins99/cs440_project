using System;
using Project.GameSystems.ML_Agents.Agent_Management.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.GameSystems.UI {
    public class ReplayPlayerUI : MonoBehaviour {
        [SerializeField] private ReplayPlayerManager _manager;

        [Header("Stats")] [SerializeField] private TMP_Text _stepCount;
        [SerializeField] private TMP_Text _currentStep;
        [SerializeField] private TMP_Text _timeStamp;
        [SerializeField] private TMP_Text _distanceToTarget;

        [Header("Controls")] [SerializeField] private Slider _stepSlider;
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private TMP_Text _saveFileStatus;

        private void Awake() {
            UpdateSaveFileStatus(SaveFileStatus.Empty);
        }

        public void UpdateStatsUI(int stepCount, int currentStep, string timeStamp, float distToTarget) {
            _stepCount.text = $"Total Steps: <color=\"blue\">{stepCount}</color>";
            _currentStep.text = $"Current Step: <color=\"green\">{currentStep}</color>";
            _timeStamp.text = $"TimeStamp: <color=\"yellow\">{timeStamp}</color>";
            _distanceToTarget.text = $"Distance to Target: <color=\"blue\">{distToTarget}</color>";
        }

        public void UpdateControlsUI(int step) {
            _stepSlider.value = step;
        }

        public void UpdateSaveFileStatus(SaveFileStatus fileStatus) {
            switch (fileStatus) {
                case SaveFileStatus.Empty:
                    _saveFileStatus.SetText("Save Status: No file loaded");
                    break;
                case SaveFileStatus.Loaded:
                    _saveFileStatus.SetText("Save Status: <color=\"green\">Loaded file successfully</color>");
                    break;
                case SaveFileStatus.FailedToLoad:
                    _saveFileStatus.SetText(
                        "Save Status: <color=\"red\">Failed to load file \nIs the file path correct?</color>");
                    break;
                case SaveFileStatus.FileNotFound:
                    _saveFileStatus.SetText(
                        "Save Status: <color=\"red\">Failed to find file \nIs the file path correct?</color>");
                    break;
            }
        }

        public void SetSliderConstraints(int maxStep) {
            _stepSlider.maxValue = maxStep;
        }

        public void PauseLoop() {
            // Slider value increased, update the manager value
            if (_manager.CurrentStep + 1 != (int)_stepSlider.value) {
                _manager.SetCurrentStep((int)_stepSlider.value - 1);
            }
        }

        public void LoadSaveFile() {
            _manager.LoadSaveFile(_inputField.text);
        }
    }
}