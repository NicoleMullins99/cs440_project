using UnityEngine;

namespace Project.GameSystems.ML_Agents.Training {
    [CreateAssetMenu(fileName = "Maze Rewards", menuName = "ScriptableObjects/Maze Rewards", order = 100)]
    public class SO_Rewards : ScriptableObject {
        [Header("Rewards")]
        [SerializeField] protected float _targetCollisionReward = 1000;
        [SerializeField] protected float _maxTileVisitReward = 100f;
        [SerializeField] protected float _tileVisitReward = 100f;
        
        [Header("Punishments")]
        [SerializeField] protected float _timeStepPunishment = -0.00001f;
        [SerializeField] protected float _wallCollisionStayReward = -0.00001f;
        [SerializeField] protected float _maxNegativeReward = -1000f;
        [SerializeField] protected float _tileStayPunishment = -0.00001f;
        [SerializeField] protected float _wallCollisionReward = -20f;
        
        [Header("Options")] [SerializeField] protected bool _isConstantRewards;

        public float WallCollisionReward => _wallCollisionReward;
        public float TargetCollisionReward => _targetCollisionReward;
        public float WallCollisionStayReward => _wallCollisionStayReward;
        public float MaxNegativeReward => _maxNegativeReward;
        public float TileStayPunishment => _tileStayPunishment;
        public float MaxTileVisitReward => _maxTileVisitReward;
        public float TimeStepPunishment => _timeStepPunishment;
        public float TileVisitReward => _tileVisitReward;

        public bool IsConstantRewards => _isConstantRewards;
    }
}