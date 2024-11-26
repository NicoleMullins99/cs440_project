using Project.GameSystems.ML_Agents.Agent_Management;
using UnityEngine;

namespace Project.GameSystems.UI {
    public class PlayerVsAgentUI : MonoBehaviour {
        [SerializeField] private TrainingUI _playerUI;
        [SerializeField] private TrainingUI _agentUI;
        public void UpdateUI(AgentTrainingData playerData, AgentTrainingData agentData) {
            _playerUI.UpdateUI(playerData, false);
            _agentUI.UpdateUI(agentData, false);
        }
    }
}
