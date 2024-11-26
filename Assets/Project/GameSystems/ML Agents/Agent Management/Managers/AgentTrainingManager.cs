using System.Collections;
using System.Collections.Generic;
using Project.GameSystems.ML_Agents.Evaluation;
using Project.GameSystems.UI;
using Unity.MLAgents;
using UnityEngine;

namespace Project.GameSystems.ML_Agents.Agent_Management.Managers {
    public class AgentTrainingManager : MazeManager {
        [Header("Multi Environment Settings")] [SerializeField]
        private AgentEvaluation _agentEvaluation;
        [SerializeField] private bool _isSimultaneousEnvironments;
        [SerializeField] private bool _showEnvironment;
        [SerializeField] private Transform _mazeHolder;
        [SerializeField] private List<Vector2Int> _distBetweenMazes;
        [SerializeField] private TrainingUI _trainingUI;

        [SerializeField, Min(1)] private int _numberAgents = 20;
        [SerializeField] private AgentTrainingData _agentTrainingData;
        private List<MazeAgent> _agentList;
        private List<BaseTrainingMaze> _mazes;
        private Queue<float> _meanRewardQueue;
        public AgentTrainingData AgentTrainingData => _agentTrainingData;

        // 1 agent per maze
        public override int NumberOfAgents => 1;

        private int _currentTrainingIndex;
        private int _cumulativeEpisodeCount;
        private int _agentSuccessCount;
        private int _agentFailedCount;
        private float _meanCumulativeReward;
        private bool _finishedCumulativeEpisode;

        private void Start() {
            if (_isPlaying) {
                if (_isPlayerControlled) {
                    _playerInputController.SetCursorState(false, CursorLockMode.Locked);
                }
                else {
                    _playerInputController.SetCursorState(true, CursorLockMode.None);
                }

                Application.runInBackground = true;
                Initialise();
                _trainingEnvironment = _trainingEnvironments[_currentTrainingIndex];
                UpdateTrainingEnvironment();
            }
        }

        private void Update() {
            _trainingUI.UpdateUI(_agentTrainingData, _isPlaying);
        }

        protected override void Initialise() {
            _mazes = new List<BaseTrainingMaze>(_numberAgents);
            _agentList = new List<MazeAgent>();
            _meanRewardQueue = new Queue<float>();
            _gameStartTime = Time.time;
        }

        protected override IEnumerator RunTimer() {
            yield return new WaitForSeconds(_maxEpisodeLength);
        }

        public override void FinishedEpisode(MazeAgent ignoreAgent, bool isSuccess, int iterationCount) {
            CalculateMeanEpisodeReward(ignoreAgent.GetCumulativeReward());
            _cumulativeEpisodeCountTotal++;
            if (isSuccess) {
                if (iterationCount >= ignoreAgent.Maze.MaxIterations) {
                    _finishedCumulativeEpisode = true;
                }

                _agentSuccessCount++;
            }
            else {
                _agentFailedCount++;
            }

            if (_cumulativeEpisodeCountTotal >= _agentEvaluation.MaxEpisodeCount) {
                UpdateAgentTrainingData();
                _agentEvaluation.SetEvalData(_agentTrainingData);
                _agentEvaluation.SaveData();
            }

            if (!_finishedCumulativeEpisode) return;

            _finishedCumulativeEpisode = false;
            ignoreAgent.Maze.RefreshMaze();
            ignoreAgent.ResetAgent();
        }

        private void FixedUpdate() {
            if (!_isPlaying) return;
            UpdateAgentTrainingData();

            // Taking too long to finish the episode
            // if (_numberAgents * 10 < _agentFailedCount) {
            //     ResetTraining();
            // }

            int currentLearningStep = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("maze1", 0f);
            if (_currentTrainingIndex != currentLearningStep) {
                Debug.Log($"Updated learningstep: {_currentTrainingIndex}");
                _currentTrainingIndex = currentLearningStep;
                _trainingEnvironment = _trainingEnvironments[_currentTrainingIndex];
                _agentSuccessCount = 0;
                _agentFailedCount = 0;
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
            _numberEpisodesBeforeReset = _trainingEnvironment.MaxIterations;

            foreach (var agent in _agentList) {
                agent.transform.parent = null;
            }

            foreach (var maze in _mazes) {
                Destroy(maze.gameObject);
            }

            _mazes.Clear();

            int xStart = -(5 * _distBetweenMazes[_currentTrainingIndex].x);
            for (int i = 0; i < _numberAgents; i++) {
                int zStart = i / 10;
                int x = i % 10;
                Vector3 distBetweenMazes;
                if (_isSimultaneousEnvironments) {
                    int enviroIndex = i % _trainingEnvironments.Count;
                    distBetweenMazes = new Vector3(xStart + (x * _distBetweenMazes[enviroIndex].x), 300,
                        zStart * _distBetweenMazes[_currentTrainingIndex].x);
                    BaseTrainingMaze maze = Instantiate(_trainingEnvironments[enviroIndex], distBetweenMazes,
                        Quaternion.identity,
                        _mazeHolder);
                    maze.SetEnvironmentVisibility(_showEnvironment);
                    _mazes.Add(maze);
                }
                else {
                    distBetweenMazes = new Vector3(xStart + (x * _distBetweenMazes[_currentTrainingIndex].x), 300,
                        zStart * _distBetweenMazes[_currentTrainingIndex].x);
                    _mazes.Add(Instantiate(_trainingEnvironment, distBetweenMazes, Quaternion.identity, _mazeHolder));
                }

                _mazes[i].SetMazeManager(this);
                _mazes[i].InitialiseMazeTiles();
                MazeAgent agent;
                if (_agentList.Count <= i) {
                    agent = Instantiate(_agentPrefab);
                    _agentList.Add(agent);
                }
                else {
                    agent = _agentList[i];
                }

                agent.SetMazeEnvironment(_mazes[i]);
                agent.SpawnAgent(0);
                agent.SetManager(this);
                agent.ResetAgent();
                agent.MaxStep = _trainingEnvironment.AgentTrainingSteps;
                agent.enabled = true;
            }

            if (_isPlayerControlled && _numberAgents == 1) {
                _agentList[0].SetPlayerControlled(true);
                _overheadCameraTransform.gameObject.SetActive(false);
            }
        }

        private void UpdateAgentTrainingData() {
            _agentTrainingData.NumberOfAgents = _numberAgents;
            _agentTrainingData.AgentSuccessCount = _agentSuccessCount;
            _agentTrainingData.AgentFailedCount = _agentFailedCount;
            _agentTrainingData.CumulativeEpisodeCount = _cumulativeEpisodeCount;
            _agentTrainingData.CumulativeEpisodeCountTotal = _cumulativeEpisodeCountTotal;
            _agentTrainingData.MeanCumulativeReward = _meanCumulativeReward;
            _agentTrainingData.Duration = Time.time - _gameStartTime;
        }
    }
}