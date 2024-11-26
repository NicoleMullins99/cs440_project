using UnityEngine;

namespace Project.GameSystems.ML_Agents.Training {
    public class TrainingReward : MonoBehaviour {
        [SerializeField] private float _reward = 1f;

        public float Reward => _reward;
    }
}
