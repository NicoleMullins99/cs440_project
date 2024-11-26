using System;
using Project.GameSystems.DataManagement;
using Project.GameSystems.ML_Agents.Agent_Management;
using UnityEngine;

namespace Project.GameSystems.ML_Agents.Evaluation {
    public class AgentEvaluation : MonoBehaviour {
        [SerializeField] private int _maxEpisodeCount = 1000;
        [SerializeField] private string _saveFileName = "main";
        [SerializeField] private string _agentModel;

        [SerializeField] private bool _hasSavedData;
        
        [SerializeField] private AgentEvalData _agentEvalData;

        public int MaxEpisodeCount => _maxEpisodeCount;

        public void SetEvalData(AgentTrainingData data) {
            _agentEvalData = new AgentEvalData {
                AgentModelName = _agentModel,
                NumberOfAgents = data.NumberOfAgents,
                NumberOfEpisodes = data.CumulativeEpisodeCountTotal,
                SuccessfulEpisodes = data.AgentSuccessCount,
                UnsuccessfulEpisodes = data.AgentFailedCount,
                Duration = data.Duration
            };
        }
        
        public void SaveData() {
            if (_hasSavedData) return;

            _hasSavedData = true;
            FileDataHandler.Save<AgentEvalData>(_agentEvalData, "AgentEvaluation", $"{_saveFileName}.json");
        }
        
    }

    [Serializable]
    public struct AgentEvalData {
        public string AgentModelName;
        public int NumberOfAgents;
        public int NumberOfEpisodes;
        public int SuccessfulEpisodes;
        public int UnsuccessfulEpisodes;
        public float Duration;
    }
}
