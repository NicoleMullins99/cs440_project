using System;
using System.Collections;
using Project.GameSystems.DataManagement;
using Project.GameSystems.UI;
using UnityEngine;

namespace Project.GameSystems.ML_Agents.Agent_Management.Managers {
    public class AgentVsPlayerManager : MazeManager {
        [Header("Agent vs Player")] [SerializeField]
        private MazeAgent _player;

        [SerializeField] private BaseTrainingMaze _playerMaze;
        [SerializeField] private BaseTrainingMaze _agentMaze;
        [SerializeField] private PlayerVsAgentUI _ui;
        [SerializeField] private Transform _mazeHolder;
        [SerializeField] private float _distBetweenMazes;

        private MazeAgent _agent;

        private AgentTrainingData _playerData;
        private AgentTrainingData _agentData;

        private int _highestIteration;
        private int _cumulativeEpisodeCount;

        private int _playerEpisodeCount;
        private int _playerWins;

        private int _agentSuccessCount;
        private int _agentFailCount;
        private int _agentEpisodeCount;
        private int _agentWins;

        private bool _finishedCumulativeEpisode;
        public override int NumberOfAgents => 2;

        private void Start() {
            Application.runInBackground = true;
        }

        private void Update() {
            _ui.UpdateUI(_playerData, _agentData);
        }

        private void FixedUpdate() {
            if (!_isPlaying || !_hasStarted) return;

            UpdateUI();
        }

        public override void FinishedEpisode(MazeAgent ignoreAgent, bool isSuccess, int iterationCount) {
            if (ignoreAgent == _player) {
                StartRunTimer();
                _playerEpisodeCount++;

                if (_enableDataSaving) {
                    _saveFileData.MazeTargetPosition = _playerMaze.MazeTarget.localPosition;
                    _saveFileData.Duration = Time.time - _runStartTime;
                    FileDataHandler.Save(_saveFileData, $"multi_agent_{_saveFileStartTime}",
                        $"run_{iterationCount}.json");
                    ResetSaveFile();
                }
            }
            else {
                _agentEpisodeCount++;
            }

            if (_highestIteration < iterationCount) {
                _highestIteration = iterationCount;
            }

            if (_highestIteration >= _numberEpisodesBeforeReset) {
                _finishedCumulativeEpisode = true;
            }

            if (isSuccess) {
                if (ignoreAgent == _agent) {
                    _agentSuccessCount++;
                }
                else {
                    _successRunCount++;
                }
            }
            else {
                if (ignoreAgent == _agent) {
                    _agentFailCount++;
                }
                else {
                    _unSuccessRunCount++;
                }
            }

            if (!_finishedCumulativeEpisode) return;

            ResetTraining(ignoreAgent);
        }

        protected override IEnumerator RunTimer() {
            yield return new WaitForSeconds(_maxEpisodeLength);
            _player.ProcessEndEpisode(false);
        }

        protected override void Initialise() {
            _playerDataStream.InitialiseStreams();
            
            _saveFileData = new SaveFileData {
                RunType = _runType.ToString(),
                MapShowcase = _mapShowcase.ToString(),
                MapName = _trainingEnvironment.gameObject.name,
                MazeTargetPosition = _trainingEnvironment.MazeTarget.localPosition
            };

            // Player Maze setup
            _playerMaze = Instantiate(_trainingEnvironment, _mazeHolder.position, Quaternion.identity, _mazeHolder);
            _playerMaze.SetMazeManager(this);
            _playerMaze.InitialiseMazeTiles();
            _playerMaze.SetRunType(_runType);

            // Agent Maze setup
            _agentMaze = Instantiate(_trainingEnvironment,
                _mazeHolder.position + new Vector3(1, 0, 1) * _distBetweenMazes, Quaternion.identity, _mazeHolder);
            _agentMaze.SetMazeManager(this);
            _agentMaze.InitialiseMazeTiles();
            _agentMaze.SetRunType(_runType);

            // Player setup
            _player.SpawnAgent(0);
            _player.VirtualCamera.gameObject.layer = LayerMask.NameToLayer("Player Camera");
            _player.SetInputs(_playerInputController);
            _player.SetPlayerControlled(true);
            _player.SetManager(this);
            _player.SetMazeEnvironment(_playerMaze);
            _player.SetTrainingStatus(_isTraining);
            _player.MaxStep = _playerMaze.AgentTrainingSteps;
            _player.gameObject.SetActive(true);

            // Agent setup
            _agent = Instantiate(_agentPrefab);
            _agent.SpawnAgent(1);
            _agent.VirtualCamera.gameObject.layer = LayerMask.NameToLayer("Agent Camera");
            _agent.SetManager(this);
            _agent.SetInputs(_playerInputController);
            _agent.SetMazeEnvironment(_agentMaze);
            _agent.SetTrainingStatus(_isTraining);
            _agent.MaxStep = _agentMaze.AgentTrainingSteps;

            // Move overhead camera to player maze position
            Vector3 trainingEnvironmentPosition = _trainingEnvironment.transform.position;
            _overheadCameraTransform.position =
                new Vector3(trainingEnvironmentPosition.x, _overheadCameraTransform.position.y,
                    trainingEnvironmentPosition.z);

            _trainingEnvironment.SetMazeManager(this);
            _playerMaze.SetPreviewCamera(_previewCamera);
            _player.enabled = true;
            _agent.enabled = true;
            StartCoroutine(RecordStepData());
        }

        protected override void UpdateTrainingEnvironment() {
            _cumulativeEpisodeCountTotal = 0;
            Vector3 trainingEnvironmentPosition = _trainingEnvironment.transform.position;
            _overheadCameraTransform.position =
                new Vector3(trainingEnvironmentPosition.x, _overheadCameraTransform.position.y,
                    trainingEnvironmentPosition.z);
            _playerMaze.InitialiseMazeTiles();
            _agentMaze.InitialiseMazeTiles();

            _agent.SetMazeEnvironment(_agentMaze);
            _agent.ResetAgent();
            _agent.MaxStep = _playerMaze.AgentTrainingSteps;

            _player.SetMazeEnvironment(_playerMaze);
            _player.ResetAgent();
            _player.MaxStep = _agentMaze.AgentTrainingSteps;
        }

        private void ResetSaveFile() {
            _stepCount = 0;
            _saveFileData.Steps.Clear();
            _runStartTime = Time.time;
        }

        private void ResetTraining(MazeAgent ignoreAgent) {
            ignoreAgent.SetMaterial(_defaultAgentMaterial);
            ignoreAgent.ResetAgent();

            _player.ResetAgent();
            _playerMaze.RefreshMaze();
            _agent.ResetAgent();
            _agentMaze.RefreshMaze();

            if (ignoreAgent == _player) {
                _agent.EpisodeInterrupted();
            }
            else {
                _agent.EpisodeInterrupted();
            }

            _highestIteration = 0;
            _finishedCumulativeEpisode = false;

            if (ignoreAgent == _player) {
                _playerWins++;
            }
            else {
                _agentWins++;
            }

            ResetSaveFile();
            _saveFileStartTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
            _cumulativeEpisodeCount++;


            if (_cumulativeEpisodeCount == _runCount) {
                RunSummaryData data = new RunSummaryData {
                    RunType = _runType,
                    MapShowcase = _mapShowcase,
                    SuccessfulRuns = _successRunCount,
                    UnsuccessfulRuns = _unSuccessRunCount,
                    Duration = Time.time - _gameStartTime,
                    PlayerWins = _playerWins,
                    AgentWins = _agentWins,
                    ShowPlayerAgentWins = true
                };
                _playerInputController.SetControlsActive(false);
                _playerInputController.SetCursorState(true, CursorLockMode.None);
                _agent.DisableDecisionRequester();
                _summaryUI.gameObject.SetActive(true);
                _summaryUI.SetSummaryData(data);
                _isPlaying = false;
                StopRunTimer();
            }
        }

        private IEnumerator RecordStepData() {
            while (true) {
                _stepCount++;
                StepData step = new StepData {
                    StepCount = _stepCount,
                    DistanceToTarget = Vector3.Distance(_player.transform.localPosition,
                        _trainingEnvironment.MazeTarget.localPosition),
                    TimeStamp = DateTime.Now.ToString("HH:mm:ss.fffff"),
                    PlayerPosition = _player.transform.localPosition,
                    PlayerRotation = _player.transform.localRotation.eulerAngles
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

        private void UpdateUI() {
            float duration = Time.time - _gameStartTime;

            _playerData.NumberOfAgents = 1;
            _playerData.AgentSuccessCount = _successRunCount;
            _playerData.AgentFailedCount = _unSuccessRunCount;
            _playerData.CumulativeEpisodeCount = _cumulativeEpisodeCount;
            _playerData.CumulativeEpisodeCountTotal = _playerEpisodeCount;
            _playerData.MeanCumulativeReward = _player.GetCumulativeReward();
            _playerData.Duration = duration;

            _agentData.NumberOfAgents = 1;
            _agentData.AgentSuccessCount = _agentSuccessCount;
            _agentData.AgentFailedCount = _agentFailCount;
            _agentData.CumulativeEpisodeCount = _cumulativeEpisodeCount;
            _agentData.CumulativeEpisodeCountTotal = _agentEpisodeCount;
            _agentData.MeanCumulativeReward = _agent.GetCumulativeReward();
            _agentData.Duration = duration;
        }
    }
}