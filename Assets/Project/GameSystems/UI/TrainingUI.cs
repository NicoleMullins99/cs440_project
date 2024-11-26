using Project.GameSystems.ML_Agents.Agent_Management;
using TMPro;
using UnityEngine;

namespace Project.GameSystems.UI {
    public class TrainingUI : MonoBehaviour {
        [SerializeField] private TMP_Text _trainingStatus;
        [SerializeField] private TMP_Text _highestIteration;
        [SerializeField] private TMP_Text _agentCount;
        [SerializeField] private TMP_Text _agentSuccess;
        [SerializeField] private TMP_Text _agentFailed;
        [SerializeField] private TMP_Text _episodeCount;
        [SerializeField] private TMP_Text _meanReward;
        [SerializeField] private TMP_Text _timer;
        

        public void UpdateUI(AgentTrainingData data, bool isTraining) {
            string status = isTraining
                ? "<color=\"yellow\">Training"
                : "<color=\"yellow\"\">Not Training";
            _trainingStatus.text = $"Status: {status} </color>, {data.CumulativeEpisodeCount}";
            _highestIteration.text = $"Highest Iteration: {data.HighestIteration}";
            _agentCount.text = $"Agent Count: {data.NumberOfAgents}";
            _agentSuccess.text = $"Completed: <color=\"green\">{data.AgentSuccessCount}</color>";
            _agentFailed.text = $"Failed: <color=\"red\">{data.AgentFailedCount}</color>";
            _episodeCount.text =
                $"Episode Count: <color=\"blue\">{data.CumulativeEpisodeCountTotal}</color>";
            float meanCumulativeReward = data.MeanCumulativeReward;
            string meanRewardString = meanCumulativeReward >= 0 ? "\"green\"" : "\"red\"";
            _meanReward.text =
                $"Mean Reward: <color={meanRewardString}>{Mathf.Round(meanCumulativeReward * 100f) / 100f}</color>";
            _timer.text = $"Time: {Mathf.Round(data.Duration * 100f) / 100f}";
        }
    }
}