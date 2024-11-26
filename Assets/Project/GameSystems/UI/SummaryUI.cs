using System.Globalization;
using Project.GameSystems.ML_Agents.Agent_Management.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.GameSystems.UI {
    public class SummaryUI : MonoBehaviour {
        [SerializeField] private TMP_Text _runTypeText;
        [SerializeField] private TMP_Text _mapShowcaseText;
        [SerializeField] private TMP_Text _successText;
        [SerializeField] private TMP_Text _unsuccessText;
        [SerializeField] private TMP_Text _durationText;
        [SerializeField] private TMP_Text _playerWinsText;
        [SerializeField] private TMP_Text _agentWinsText;

        [SerializeField] private GameObject _playerWinsGameObject;
        [SerializeField] private GameObject _agentWinsGameObject;
        

        public void SetSummaryData(RunSummaryData data) {
            _runTypeText.text = data.RunType.ToString();
            _mapShowcaseText.text = data.MapShowcase.ToString();
            _successText.text = data.SuccessfulRuns.ToString();
            _unsuccessText.text = data.UnsuccessfulRuns.ToString();
            _durationText.text = $"{data.Duration.ToString(CultureInfo.InvariantCulture)} seconds";

            if (data.ShowPlayerAgentWins) {
                _playerWinsGameObject.SetActive(true);
                _agentWinsGameObject.SetActive(true);

                _playerWinsText.text = data.PlayerWins.ToString();
                _agentWinsText.text = data.AgentWins.ToString();
            }
            else {
                _playerWinsGameObject.SetActive(false);
                _agentWinsGameObject.SetActive(false);
            }
        }
        

        public void ReturnToMainMenu() {
            SceneManager.LoadScene("MainMenu");
        }
        
        public void ResetScene() {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public struct RunSummaryData {
        public RunType RunType;
        public MapShowcase MapShowcase;
        public int SuccessfulRuns;
        public int UnsuccessfulRuns;
        public int PlayerWins;
        public int AgentWins;
        public float Duration;
        public bool ShowPlayerAgentWins;
    }
}
