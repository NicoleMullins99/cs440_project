using System.Collections;
using Project.GameSystems.DataManagement;
using Project.GameSystems.UI;
using UnityEngine;

namespace Project.GameSystems.ML_Agents.Agent_Management.Managers {
    public class ReplayPlayerManager : MonoBehaviour {
        [SerializeField] private Transform _player;
        [SerializeField] private ReplayPlayerUI _ui;

        [Header("Navigation Mazes")] [SerializeField]
        private BaseTrainingMaze _maze1;

        [SerializeField] private BaseTrainingMaze _maze2;

        [Header("Way Finding Mazes")] [SerializeField]
        private BaseTrainingMaze _wfMaze;

        [Header("Save Data")] [SerializeField] private BaseTrainingMaze _currentMaze;
        [SerializeField] private float _playBackSpeed = 0.25f;

        private SaveFileDataBase _currentSaveFile;
        private IStepData _currentStepData;
        private Coroutine _playBackCoroutine;

        private int _currentStep = 0;
        private int _currentStepTotal;

        private bool _isNavigationMaze;

        public int CurrentStep => _currentStep;

        public void LoadSaveFile(string saveFilePath) {
            bool fileExists = FileDataHandler.FileExists(saveFilePath);

            if (!fileExists) {
                _ui.UpdateSaveFileStatus(SaveFileStatus.FileNotFound);
                return;
            }

            bool fileLoaded = FileDataHandler.Load<SaveFileData>(saveFilePath, out SaveFileData saveFileData);

            // Standard save file failed to load, try loading the WF save file
            if (!fileLoaded) {
                if (!FileDataHandler.Load<SaveFileDataWF>(saveFilePath, out SaveFileDataWF saveFileDataWF)) {
                    _ui.UpdateSaveFileStatus(SaveFileStatus.FailedToLoad);
                    return;
                }

                _currentSaveFile = saveFileDataWF;
            }
            else {
                _currentSaveFile = saveFileData;
            }


            _ui.SetSliderConstraints(_currentSaveFile.GetStepCount());
            _ui.UpdateSaveFileStatus(SaveFileStatus.Loaded);
        }

        public void PlaySaveFromBeginning() {
            _player.gameObject.SetActive(true);
            _currentStep = 0;
            _currentStepTotal = _currentSaveFile.GetStepCount();

            switch (_currentSaveFile.MapName) {
                case "Maze 1":
                    _currentMaze = _maze1;
                    _isNavigationMaze = true;
                    break;
                case "Maze 2":
                    _currentMaze = _maze2;
                    _isNavigationMaze = true;
                    break;
                case "Maze 1 WF":
                    _currentMaze = _wfMaze;
                    _isNavigationMaze = false;
                    break;
                default:
                    Debug.LogError($"Could not load maze: {_currentSaveFile.MapName}");
                    return;
            }

            _player.SetParent(_currentMaze.transform);

            _currentStepData = _currentSaveFile.GetStep(_currentStep);
            if (_isNavigationMaze) {
                SaveFileData saveFile = (SaveFileData)_currentSaveFile;
                _currentMaze.MazeTarget.localPosition = saveFile.MazeTargetPosition;
            }

            _ui.UpdateControlsUI(1);

            if (_playBackCoroutine != null) StopCoroutine(_playBackCoroutine);
            _playBackCoroutine = StartCoroutine(PlaySaveLoop(_playBackSpeed));
        }

        public void PauseSaveLoop() {
            if (_playBackCoroutine != null) {
                StopCoroutine(_playBackCoroutine);
                _playBackCoroutine = null;
            }
            else {
                _playBackCoroutine = StartCoroutine(PlaySaveLoop(_playBackSpeed));
            }
        }

        public void SetCurrentStep(int step) {
            if (_playBackCoroutine != null) {
                StopCoroutine(_playBackCoroutine);
                _playBackCoroutine = null;
            }

            _currentStep = step;
            _currentStepData = _currentSaveFile.GetStep(_currentStep);
            LoadCurrentStep(false);
        }

        private void LoadCurrentStep(bool updateControlUi = true) {
            _player.localPosition = _currentStepData.GetPlayerPosition();
            _player.localRotation = Quaternion.Euler(_currentStepData.GetPlayerRotation());

            _ui.UpdateStatsUI(_currentStepTotal, _currentStep, _currentStepData.GetTimeStamp(),
                _currentStepData.GetDistanceToTarget());
            if (updateControlUi) _ui.UpdateControlsUI(_currentStep + 1);
        }

        private IEnumerator PlaySaveLoop(float playRate) {
            WaitForSeconds waitTime = new WaitForSeconds(playRate);

            while (_currentStep < _currentSaveFile.GetStepCount() - 1) {
                yield return waitTime;
                _currentStep++;
                _currentStepData = _currentSaveFile.GetStep(_currentStep);
                LoadCurrentStep();
            }
        }
    }

    public enum SaveFileStatus {
        Empty,
        FailedToLoad,
        FileNotFound,
        Loaded
    }
}