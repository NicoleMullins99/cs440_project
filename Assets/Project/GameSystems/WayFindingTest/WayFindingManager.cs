using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Project.GameSystems.DataManagement;
using Project.GameSystems.ML_Agents.Agent_Management;
using Project.GameSystems.Player;
using Project.GameSystems.WayFindingTest.UI;
using UnityEngine;

namespace Project.GameSystems.WayFindingTest {
    public class WayFindingManager : MonoBehaviour {
        [Header("Components")] [SerializeField]
        private PlayerInputController _playerInputController;

        [SerializeField] private MazeAgent _player;
        [SerializeField] private WayFindingMaze _maze;
        [SerializeField] private WayFindingTask _wayFindingTask;
        [SerializeField] private PlayerDataStream _playerDataStream;

        [Header("Task")] [SerializeField] private List<WF_TaskSO> _wfTaskList;

        [Header("UI")] [SerializeField] private WfTaskUi _taskUi;
        [SerializeField] private WfHud _hudUi;
        [SerializeField] private WfSummaryUi _summaryUi;

        [Header("Values")] [SerializeField] [Min(0.1f)]
        private float _failTime = 2f;

        [SerializeField] private string _failText = "Time’s up!";
        [SerializeField] private float _saveStepFrequency = 0.25f;

        private WayFindingTask.TaskSettings _currentTask;
        private Coroutine _taskCoroutine;
        private Coroutine _taskTimerCoroutine;
        private Coroutine _failTaskCoroutine;
        private Coroutine _saveDataCoroutine;

        private TaskTarget _taskTarget;

        private SaveFileDataWF _saveFileData;

        private float _gameStartTime;
        private int _saveFileStartTime;
        private int _currentTaskIndex;
        private int _stepCount;

        private bool _reachedTarget;
        private bool _isTrainingMode;
        private bool _isGameRunning;

        private int _successfulRounds;
        private int _unsuccessfulRounds;


        /// <summary>
        /// Load the game settings and start the game based on the settings
        /// </summary>
        public void LoadSettingsAndStart(bool isTraining, bool isMarkerStreamMode, int taskIndex) {
            _isGameRunning = true;
            _isTrainingMode = isTraining;
            _wayFindingTask.SetTask(_wfTaskList[taskIndex]);

            _playerDataStream.SetBehaviorDataStreamMarkerOnly(isMarkerStreamMode);
            _playerDataStream.InitialiseStreams();

            SetupAgent();
            _playerInputController.SetCursorState(false, CursorLockMode.Locked);

            _gameStartTime = Time.time;
            _saveFileStartTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();

            // If training mode is enabled, don't start the game
            // Player must exit via the pause menu
            if (isTraining) {
                TrainingSetup();
            }
            else {
                _saveFileData = new SaveFileDataWF {
                    RunType = "WayFinding Task",
                    MapName = "Maze 1 WF",
                };

                // TODO: Make sure coroutine isn't running
                StartCoroutine(MainTask());
            }

            if (_saveDataCoroutine != null) StopCoroutine(_saveDataCoroutine);
            _saveDataCoroutine = StartCoroutine(RecordStepData());
        }

        public void ReachedTarget() {
            _reachedTarget = true;
            _player.ProcessEndEpisode(true);
            _playerDataStream.StreamEventMarker(new[] { 5 });
        }

        private void TrainingSetup() {
            _wayFindingTask.TryGetTaskTarget(_wayFindingTask.SpawnPoint, out _taskTarget);

            _player.transform.localPosition = _taskTarget.transform.localPosition;
            _player.transform.localRotation = _taskTarget.transform.localRotation;
            RotatePlayerTowardsTarget(_taskTarget.SignTransform, new Vector3(0, -2, 0));
            _hudUi.SetTargetText("Training Mode");
            _hudUi.ToggleTimer(true);
            // Timer ticks up every frame
            _hudUi.StartTimer();

            _saveFileData = new SaveFileDataWF {
                RunType = "WayFinding Task Training",
                MapName = "Maze 1 WF",
            };
        }

        public void TrySaveTrainingData() {
            if (!_isGameRunning) return;
            _saveFileData.Duration = Time.time - _gameStartTime;
            _saveFileData.SuccessCount = _successfulRounds;
            _saveFileData.FailCount = _unsuccessfulRounds;
            StopCoroutine(_saveDataCoroutine);
            FileDataHandler.Save(_saveFileData, Path.Combine("WayFinding Task", $"game_task_{_saveFileStartTime}"),
                $"playerData_{_saveFileStartTime}.json");
        }

        #region Timers

        private IEnumerator MainTask() {
            _wayFindingTask.TryGetTaskTarget(_wayFindingTask.SpawnPoint, out _taskTarget);
            _reachedTarget = true;

            _player.transform.localPosition = _taskTarget.transform.localPosition;
            _player.transform.localRotation = _taskTarget.transform.localRotation;
            RotatePlayerTowardsTarget(_taskTarget.SignTransform, new Vector3(0, -2, 0));

            while (true) {
                if (!_wayFindingTask.TryGetTask(_currentTaskIndex, out WayFindingTask.TaskSettings task)) {
                    break;
                }

                _wayFindingTask.TryGetTaskTarget(task.TaskTarget, out _taskTarget);

                _currentTask = task;
                _taskTarget.EnableTarget();
                _taskTarget.OnReachedTarget += ReachedTarget;
                // Task loop
                yield return StartCoroutine(ProcessTask(task, _taskTarget));
            }

            _saveFileData.Duration = Time.time - _gameStartTime;
            _saveFileData.SuccessCount = _successfulRounds;
            _saveFileData.FailCount = _unsuccessfulRounds;
            StopCoroutine(_saveDataCoroutine);
            FileDataHandler.Save(_saveFileData, Path.Combine("WayFinding Task", $"game_task_{_saveFileStartTime}"),
                $"playerData_{_saveFileStartTime}.json");
            WfSummaryUi.WayFindingSummaryData summaryData = new WfSummaryUi.WayFindingSummaryData {
                DestinationCount = _wayFindingTask.TaskList.Count,
                SuccessfulRuns = _successfulRounds,
                UnsuccessfulRuns = _unsuccessfulRounds,
                Duration = _saveFileData.Duration,
            };
            _summaryUi.SetSummaryData(summaryData);
            _summaryUi.Toggle(true);
            _playerInputController.SetControlsActive(true);
            _playerInputController.SetCursorState(true, CursorLockMode.None);
        }

        private IEnumerator TaskTimer(float time) {
            float targetTime = time + Time.deltaTime;

            while (targetTime > 0) {
                // Exit early if the target is reached
                if (_reachedTarget) yield break;
                targetTime -= Time.deltaTime;
                yield return null;
            }

            if (!_reachedTarget) _playerDataStream.StreamEventMarker(new[] { 4 });
        }

        private IEnumerator FailTask(float time) {
            _reachedTarget = false;
            _playerInputController.SetControlsActive(false);
            _player.DisableDecisionRequester();
            _taskUi.SetTaskText(_failText);
            _taskUi.gameObject.SetActive(true);
            yield return new WaitForSeconds(time);
            _taskUi.gameObject.SetActive(false);
        }

        private IEnumerator ProcessTask(WayFindingTask.TaskSettings task, TaskTarget taskTarget) {
            _playerInputController.SetControlsActive(false);
            _player.DisableDecisionRequester();

            yield return new WaitForSeconds(task.TaskTargetSettings.ReachedTargetWaitTime);

            // Toggle UI
            _taskUi.SetTaskText(task.DisplayText);
            _taskUi.gameObject.SetActive(true);
            _playerDataStream.StreamEventMarker(new[] { 2 });
            yield return new WaitForSeconds(task.TaskTargetSettings.TextDuration);
            _taskUi.gameObject.SetActive(false);
            _playerDataStream.StreamEventMarker(new[] { 3 });
            _playerInputController.SetControlsActive(true);

            _reachedTarget = false;
            _player.EnableDecisionRequester();
            if (_taskTimerCoroutine != null) StopCoroutine(_taskTimerCoroutine);
            _hudUi.StartTimer(task);
            _taskTimerCoroutine = StartCoroutine(TaskTimer(task.TaskTargetSettings.TaskTimer));
            // Wait for timer to end, if interrupted than success
            yield return _taskTimerCoroutine;

            // Didn't reach the target
            if (!_reachedTarget) {
                _unsuccessfulRounds++;
                _player.ProcessEndEpisode(false);
                
                // Disable target before moving player
                if (taskTarget != null) {
                    taskTarget.DisableTarget();
                    taskTarget.OnReachedTarget -= ReachedTarget;
                }
                
                yield return StartCoroutine(FailTask(_failTime));
                _player.transform.localPosition = taskTarget.transform.localPosition;
                _player.transform.localRotation = taskTarget.transform.localRotation;

                _playerDataStream.StreamEventMarker(new[] { 1 });
                RotatePlayerTowardsTarget(_taskTarget.SignTransform, new Vector3(0, -2, 0));
            }
            else {
                if (taskTarget != null) {
                    taskTarget.DisableTarget();
                    taskTarget.OnReachedTarget -= ReachedTarget;
                }
                
                _successfulRounds++;
                _hudUi.StopTimer();
            }

            _currentTaskIndex++;
        }

        private IEnumerator RecordStepData() {
            while (true) {
                _stepCount++;
                float dist = _taskTarget != null
                    ? Vector3.Distance(_player.transform.localPosition,
                        _taskTarget.transform.localPosition)
                    : -1;
                StepDataWF step = new StepDataWF {
                    StepCount = _stepCount,
                    DistanceToTarget = dist,
                    TimeStamp = DateTime.Now.ToString("HH:mm:ss.fffff"),
                    PlayerPosition = _player.transform.localPosition,
                    PlayerRotation = _player.transform.localRotation.eulerAngles,
                    TaskTarget = _currentTask.TaskTarget.ToString(),
                };
                // Use the camera x rotation for the player x rotation
                var vector3 = step.PlayerRotation;
                vector3.x = _player.VirtualCamera.transform.localRotation.eulerAngles.x;
                step.PlayerRotation = vector3;

                _saveFileData.Steps.Add(step);
                _playerDataStream.StreamPlayerData(step);
                yield return new WaitForSeconds(_saveStepFrequency);
            }
        }

        #endregion

        private void RotatePlayerTowardsTarget(Transform targetTransform, Vector3 targetOffset) {
            Vector3 relativePos = targetTransform.localPosition + targetOffset -
                                  _player.transform.localPosition;
            _player.VirtualCamera.transform.localRotation = Quaternion.LookRotation(relativePos, new Vector3(0, 1, 0));
            Vector3 euler = _player.VirtualCamera.transform.localRotation.eulerAngles;
            _player.VirtualCamera.transform.localRotation = Quaternion.Euler(euler.x, 0, 0);
        }

        private void SetupAgent() {
            _player.SetInputs(_playerInputController);
            _player.gameObject.SetActive(true);
            _player.transform.SetParent(_wayFindingTask.transform);
            _maze.InitialiseMazeTiles();
            _player.SetMazeEnvironment(_maze);
            _player.SetPlayerControlled(true);

            _player.gameObject.SetActive(true);
            _player.enabled = true;
        }
    }
}