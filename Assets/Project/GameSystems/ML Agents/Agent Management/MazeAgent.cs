using Cinemachine;
using Project.GameSystems.ML_Agents.Agent_Management.Managers;
using Project.GameSystems.Player;
using Unity.MLAgents;
using UnityEngine;

namespace Project.GameSystems.ML_Agents.Agent_Management {
    public class MazeAgent : Agent {
        [Header("Agent Components")] [SerializeField]
        protected Rigidbody _rigidbody;

        [SerializeField] protected CinemachineVirtualCamera _virtualCamera;

        [SerializeField] protected MeshRenderer _meshRenderer;

        [SerializeField] protected ExtendedDecisionRequester _decisionRequester;

        [SerializeField] protected float _moveSpeed = 5f;
        [SerializeField] protected float _rotationSpeed = 5f;

        [SerializeField] protected int _agentIndex;

        [Header("Player Control Components")] [SerializeField]
        protected PlayerInputController _playerInputController;

        [SerializeField] protected Transform _headCameraHolder;
        [SerializeField] protected float _minEulerAngle = -80f;
        [SerializeField] protected float _maxEulerAngle = 60f;
        [SerializeField] protected bool _isPlayerControlled;
        [SerializeField] protected bool _isTraining;

        [Header("Maze Components")] [SerializeField]
        protected MazeManager _agentTrainingManager;

        [SerializeField] protected BaseTrainingMaze _currentTrainingEnvironment;

        [Header("Misc")] [SerializeField] protected float _currentReward;

        public BaseTrainingMaze Maze => _currentTrainingEnvironment;
        public CinemachineVirtualCamera VirtualCamera => _virtualCamera;
        public int AgentIndex => _agentIndex;

        public virtual void ResetAgent() {
            if (isActiveAndEnabled) {
                EpisodeInterrupted();
                StopAllCoroutines();
            }
            else {
                gameObject.SetActive(true);
                EnableDecisionRequester();
            }
        }

        public virtual void SpawnAgent(int index) {
            _agentIndex = index;
        }

        public virtual void DisableDecisionRequester() {
            _decisionRequester.IsEnabled = false;
        }

        public virtual void EnableDecisionRequester() {
            _decisionRequester.IsEnabled = true;
        }

        public virtual void SetMaterial(Material material) {
            _meshRenderer.materials = new[] { material };
        }

        public virtual void SetMazeEnvironment(BaseTrainingMaze maze) {
            _currentTrainingEnvironment = maze;
        }

        public virtual void SetPlayerControlled(bool status) {
            _isPlayerControlled = status;
        }

        public virtual void SetManager(MazeManager manager) {
            _agentTrainingManager = manager;
        }

        public virtual void SetTrainingStatus(bool state) {
            _isTraining = state;
        }

        public virtual void SetInputs(PlayerInputController playerInputController) {
            _playerInputController = playerInputController;
        }

        public virtual void ProcessEndEpisode(bool reachedTarget) {
            EndEpisode();
        }
    }
}