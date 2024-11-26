using System;

namespace Project.GameSystems.ML_Agents.Agent_Management {
    [Serializable]
    public struct AgentTrainingData {
        public int NumberOfAgents;
        public int AgentSuccessCount;
        public int AgentFailedCount;
        public int CumulativeEpisodeCount;
        public int CumulativeEpisodeCountTotal;
        public int HighestIteration;
        public float MeanCumulativeReward;
        public float Duration;
    }
}