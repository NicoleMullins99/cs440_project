using System;
using System.Collections;
using System.Collections.Generic;
using Project.GameSystems.DataManagement;
using Project.GameSystems.Player;
using Project.GameSystems.UI;
using UnityEngine;

namespace Project.GameSystems.ML_Agents.Agent_Management.Managers
{
    /// <summary>
    /// Main entry point for most game logic. Manages the maze, player, and agent systems.
    /// </summary>
    public abstract class MazeManager : MonoBehaviour
    {
        [Header("Maze Components")] [SerializeField]
        protected BaseTrainingMaze _trainingEnvironment;

        [SerializeField] protected List<BaseTrainingMaze> _trainingEnvironments;
        [SerializeField] protected Transform _overheadCameraTransform;
        [SerializeField] protected Transform _previewCamera;
        
        [Header("UI Components")]
        [SerializeField] protected Transform _previewImageTransform;
        [SerializeField] protected SummaryUI _summaryUI;

        [Header("Prefabs / Components")] [SerializeField]
        protected PlayerInputController _playerInputController;

        [SerializeField] protected Material _defaultAgentMaterial;
        [SerializeField] protected Material _highlightedAgentMaterial;

        [SerializeField] protected MazeAgent _agentPrefab;

        [Header("Toggles")] [SerializeField] protected bool _isPlaying;
        [SerializeField] protected bool _isTraining;
        [SerializeField] protected bool _isPlayerControlled;

        [Header("Run Settings")] [SerializeField]
        protected RunType _runType;
        [SerializeField] protected MapShowcase _mapShowcase;
        [SerializeField] [Min(1)] protected int _runCount = 3;

        [SerializeField, Min(1)]
        protected int _numberEpisodesBeforeReset = 10;

        [SerializeField] [Min(1)] protected float _maxEpisodeLength = 60f;
        [SerializeField] [Min(0.5f)] protected float _mapPreviewTime = 0.5f;
        [SerializeField] protected bool _enableDataSaving = true;

        [Header("Save Data")]
        [SerializeField] protected float _saveStepFrequency = 0.25f;
        [SerializeField] protected PlayerDataStream _playerDataStream;

        protected SaveFileData _saveFileData;
        protected Coroutine _runTimerCoroutine;

        protected bool _hasStarted;
        protected float _gameStartTime;
        protected float _runStartTime;
        
        protected int _saveFileStartTime;
        protected int _successRunCount;
        protected int _unSuccessRunCount;
        protected int _stepCount;
        protected int _cumulativeEpisodeCountTotal;


        public PlayerInputController PlayerInputController => _playerInputController;

        public bool IsPlaying => _isPlaying;
        public bool IsPlayerControlled => _isPlayerControlled;

        public int NumberEpisodesBeforeReset => _numberEpisodesBeforeReset;

        public abstract int NumberOfAgents { get; }

        /// <summary>
        /// Called from an agent when it finishes an episode
        /// </summary>
        /// <param name="ignoreAgent">Agent that finished its episode</param>
        /// <param name="isSuccess">Did it reach the target?</param>
        /// <param name="iterationCount">Run iteration</param>
        public abstract void FinishedEpisode(MazeAgent ignoreAgent, bool isSuccess, int iterationCount);

        /// <summary>
        /// Load the game settings and start the game based on the settings
        /// </summary>
        /// <param name="settings">Game settings</param>
        public virtual void LoadSettingsAndStart(RunSettings settings)
        {
            _runType = settings.RunType;
            _mapShowcase = settings.MapShowcase;
            _runCount = settings.RunCount;
            _numberEpisodesBeforeReset = settings.EpisodeResetFreq;
            _mapPreviewTime = settings.MapPreviewTime;
            _maxEpisodeLength = settings.MaxEpisodeLength;
            _enableDataSaving = settings.EnableDataSaving;
            if (settings.Maze != null) {
                _trainingEnvironment = settings.Maze;
            }
            

            if (_isPlayerControlled)
            {
                _playerInputController.SetCursorState(false, CursorLockMode.Locked);
            }

            _trainingEnvironment.SetRunType(_runType);
            
            Initialise();
            UpdateTrainingEnvironment();

            if (_mapShowcase == MapShowcase.None) {
                StartGame();
                StartRunTimer();
            } else if (_mapShowcase == MapShowcase.ShowMapWithoutTarget) {
                StartCoroutine(PreviewMap(true));
            }
            else {
                StartCoroutine(PreviewMap(false));
            }
            
        }

        protected virtual void StartGame() {
            _hasStarted = true;
            _gameStartTime = Time.time;
            _runStartTime = _gameStartTime;
            _saveFileStartTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
        }
        
        /// <summary>
        /// Enable the map render texture game object for a period of time. Freeze player controls.
        /// </summary>
        /// <param name="hideTarget"></param>
        /// <returns></returns>
        protected IEnumerator PreviewMap(bool hideTarget) {
            _previewImageTransform.gameObject.SetActive(true);
            _playerInputController.SetControlsActive(false);
            _trainingEnvironment.MazeTarget.gameObject.SetActive(!hideTarget);
            
            yield return new WaitForSeconds(_mapPreviewTime);
            
            _playerInputController.SetControlsActive(true);
            _previewImageTransform.gameObject.SetActive(false);
            _trainingEnvironment.MazeTarget.gameObject.SetActive(true);
            StartGame();
            StartRunTimer();
        }

        protected void StartRunTimer() {
            if (_runTimerCoroutine != null) StopCoroutine(_runTimerCoroutine);
            _runTimerCoroutine = StartCoroutine(RunTimer());
        }

        protected void StopRunTimer() {
            if (_runTimerCoroutine != null) StopCoroutine(_runTimerCoroutine);
        }

        protected abstract IEnumerator RunTimer();

        /// <summary>
        /// Setup the maze logic
        /// </summary>
        protected abstract void Initialise();
        protected abstract void UpdateTrainingEnvironment();

    }
}