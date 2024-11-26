using System;
using System.Collections;
using System.Collections.Generic;
using Project.GameSystems.DataManagement;
using Project.GameSystems.UI;
using Unity.MLAgents;
using UnityEngine;

namespace Project.GameSystems.ML_Agents.Agent_Management.Managers {
    public class PlayerMazeManager : MazeManager {
        [Header("MultiAgent")] [SerializeField]
        private MazeAgent _playerTestAgent;

        [SerializeField] protected TrainingUI _trainingUI;

        [SerializeField, Min(1)] private int _numberAgents = 1;
        [SerializeField] private AgentTrainingData _agentTrainingData;
        private List<MazeAgent> _agentList;
        private Queue<float> _meanRewardQueue;
        public AgentTrainingData AgentTrainingData => _agentTrainingData;

        public override int NumberOfAgents => _numberAgents;

        private int _currentTrainingIndex;
        private int _cumulativeEpisodeCount;
        private int _agentSuccessCount;
        private int _agentFailedCount;
        private int _highestIteration;
        private float _meanCumulativeReward;
        private bool _finishedCumulativeEpisode;

        private List<MazeAgent> _highestIterationAgents;

        private void Start() {
            Application.runInBackground = true;
        }

        private void Update() {
            _trainingUI.UpdateUI(_agentTrainingData, _isPlaying);
        }

        protected override IEnumerator RunTimer() {
            yield return new WaitForSeconds(_maxEpisodeLength);
            _playerTestAgent.ProcessEndEpisode(false);
        }

        protected override void Initialise() {
            _playerDataStream.InitialiseStreams();

            _saveFileData = new SaveFileData {
                RunType = _runType.ToString(),
                MapShowcase = _mapShowcase.ToString(),
                MapName = _trainingEnvironment.gameObject.name,
                MazeTargetPosition = _trainingEnvironment.MazeTarget.localPosition
            };
            foreach (var maze in _trainingEnvironments) {
                maze.SetMazeManager(this);
                maze.InitialiseMazeTiles();
            }

            _trainingEnvironment.SetMazeManager(this);
            _trainingEnvironment.InitialiseMazeTiles();

            _highestIterationAgents = new List<MazeAgent>();
            _agentList = new List<MazeAgent>();
            _meanRewardQueue = new Queue<float>();

            if (_isPlayerControlled) {
                // Setup player
                _numberAgents = 1;
                _playerTestAgent.SetMazeEnvironment(_trainingEnvironment);
                _playerTestAgent.SetTrainingStatus(_isTraining);
                //_playerTestAgent.gameObject.SetActive(true);
                _playerTestAgent.SpawnAgent(0);
                _playerTestAgent.SetInputs(_playerInputController);
                _playerTestAgent.SetPlayerControlled(true);
                _playerTestAgent.SetManager(this);
                _playerTestAgent.gameObject.SetActive(true);
                _agentList.Add(_playerTestAgent);
            }
            else {
                // Setup each agent
                for (int i = 0; i < _numberAgents; i++) {
                    MazeAgent agent = Instantiate(_agentPrefab);
                    agent.SpawnAgent(i);
                    agent.SetManager(this);
                    agent.SetMazeEnvironment(_trainingEnvironment);
                    _agentList.Add(agent);
                }
            }

            _trainingEnvironment.SetPreviewCamera(_previewCamera);
            if (_isPlayerControlled) {
                _playerTestAgent.enabled = true;
            }
            else {
                foreach (var agent in _agentList) {
                    agent.enabled = true;
                }
            }

            StartCoroutine(RecordStepData());
        }

        public override void FinishedEpisode(MazeAgent ignoreAgent, bool isSuccess, int iterationCount) {
            CalculateMeanEpisodeReward(ignoreAgent.GetCumulativeReward());
            _cumulativeEpisodeCountTotal++;

            if (ignoreAgent == _playerTestAgent) {
                StartRunTimer();
                if (isSuccess) {
                    _successRunCount++;
                }
                else {
                    _unSuccessRunCount++;
                }

                if (_enableDataSaving) {
                    _saveFileData.MazeTargetPosition = _trainingEnvironment.MazeTarget.localPosition;
                    _saveFileData.Duration = Time.time - _runStartTime;
                    FileDataHandler.Save(_saveFileData, $"multi_agent_{_saveFileStartTime}",
                        $"run_{iterationCount}.json");
                    ResetSaveFile();
                }
            }

            if (_highestIteration < iterationCount) {
                foreach (var agent in _highestIterationAgents) {
                    agent.SetMaterial(_defaultAgentMaterial);
                }

                _highestIterationAgents.Clear();
                _highestIteration = iterationCount;
            }

            if (iterationCount == _highestIteration) {
                _highestIterationAgents.Add(ignoreAgent);
                ignoreAgent.SetMaterial(_highlightedAgentMaterial);
            }

            if (_highestIteration >= _numberEpisodesBeforeReset) {
                _finishedCumulativeEpisode = true;
            }

            if (isSuccess) {
                _agentSuccessCount++;
            }
            else {
                _agentFailedCount++;
            }

            if (!_finishedCumulativeEpisode) return;

            ResetTraining();
        }

        private void ResetSaveFile() {
            _stepCount = 0;
            _saveFileData.Steps.Clear();
            _runStartTime = Time.time;
        }

        /// <summary>
        /// Reset maze systems, player, and agent.
        /// </summary>
        private void ResetTraining() {
            foreach (var agent in _agentList) {
                CalculateMeanEpisodeReward(agent.GetCumulativeReward());
                agent.ResetAgent();
                agent.EpisodeInterrupted();
                agent.SetMaterial(_defaultAgentMaterial);
            }

            ResetSaveFile();
            _saveFileStartTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();

            _highestIterationAgents.Clear();
            _highestIteration = 0;
            _trainingEnvironment.RefreshMaze();
            _agentSuccessCount = 0;
            _agentFailedCount = 0;
            _finishedCumulativeEpisode = false;
            _cumulativeEpisodeCount++;

            if (_cumulativeEpisodeCount == _runCount) {
                RunSummaryData data = new RunSummaryData {
                    RunType = _runType,
                    MapShowcase = _mapShowcase,
                    SuccessfulRuns = _successRunCount,
                    UnsuccessfulRuns = _unSuccessRunCount,
                    Duration = Time.time - _gameStartTime,
                    PlayerWins = 0,
                    AgentWins = 0,
                    ShowPlayerAgentWins = false
                };
                _playerInputController.SetControlsActive(false);
                _playerInputController.SetCursorState(true, CursorLockMode.None);
                _summaryUI.gameObject.SetActive(true);
                _summaryUI.SetSummaryData(data);
                _isPlaying = false;
                StopRunTimer();
            }
        }

        private void FixedUpdate() {
            if (!_isPlaying || !_hasStarted) return;
            _agentTrainingData.NumberOfAgents = _numberAgents;
            _agentTrainingData.AgentSuccessCount = _agentSuccessCount;
            _agentTrainingData.AgentFailedCount = _agentFailedCount;
            _agentTrainingData.CumulativeEpisodeCount = _cumulativeEpisodeCount;
            _agentTrainingData.CumulativeEpisodeCountTotal = _cumulativeEpisodeCountTotal;
            _agentTrainingData.HighestIteration = _highestIteration;
            _agentTrainingData.MeanCumulativeReward = _meanCumulativeReward;
            _agentTrainingData.Duration = Time.time - _gameStartTime;

            // Taking too long to finish the episode
            if (_numberAgents * 10 < _agentFailedCount) {
                ResetTraining();
            }

            // If curriculum learning is enabled, try get next maze.
            int currentLearningStep = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("maze1", 0f);
            if (_currentTrainingIndex != currentLearningStep) {
                Debug.Log($"Updated learningstep: {_currentTrainingIndex}");
                _currentTrainingIndex = currentLearningStep;
                _trainingEnvironment = _trainingEnvironments[_currentTrainingIndex];
                UpdateTrainingEnvironment();
            }
        }

        public void CalculateMeanEpisodeReward(float rewardValue) {
            if (_meanRewardQueue.Count > _numberAgents * 10) {
                _meanRewardQueue.Dequeue();
            }

            _meanRewardQueue.Enqueue(rewardValue);
            float meanReward = 0f;
            foreach (var reward in _meanRewardQueue) {
                meanReward += reward;
            }

            _meanCumulativeReward = meanReward / _meanRewardQueue.Count;
        }

        protected override void UpdateTrainingEnvironment() {
            _cumulativeEpisodeCountTotal = 0;
            _meanRewardQueue.Clear();
            Vector3 trainingEnvironmentPosition = _trainingEnvironment.transform.position;
            _overheadCameraTransform.position =
                new Vector3(trainingEnvironmentPosition.x, _overheadCameraTransform.position.y,
                    trainingEnvironmentPosition.z);
            _trainingEnvironment.InitialiseMazeTiles();
            foreach (var agent in _agentList) {
                agent.SetMazeEnvironment(_trainingEnvironment);
                agent.ResetAgent();
                agent.MaxStep = _trainingEnvironment.AgentTrainingSteps;
            }
        }

        /// <summary>
        /// Record the player data at a constant interval
        /// </summary>
        /// <returns></returns>
        private IEnumerator RecordStepData() {
            while (true) {
                _stepCount++;
                StepData step = new StepData {
                    StepCount = _stepCount,
                    DistanceToTarget = Vector3.Distance(_playerTestAgent.transform.localPosition,
                        _trainingEnvironment.MazeTarget.localPosition),
                    TimeStamp = DateTime.Now.ToString("HH:mm:ss.fffff"),
                    PlayerPosition = _playerTestAgent.transform.localPosition,
                    PlayerRotation = _playerTestAgent.transform.localRotation.eulerAngles
                };
                // Use the camera x rotation for the player x rotation
                var vector3 = step.PlayerRotation;
                vector3.x = _playerTestAgent.VirtualCamera.transform.localRotation.eulerAngles.x;
                step.PlayerRotation = vector3;
                _saveFileData.Steps.Add(step);
                _playerDataStream.StreamPlayerData(step);
                yield return new WaitForSeconds(_saveStepFrequency);
            }
        }
    }
}