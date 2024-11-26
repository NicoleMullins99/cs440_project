using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WfSummaryUi : MonoBehaviour {
    [SerializeField] private TMP_Text _destinationCountText;
    [SerializeField] private TMP_Text _successText;
    [SerializeField] private TMP_Text _unsuccessText;
    [SerializeField] private TMP_Text _durationText;
    
    
    public void SetSummaryData(WayFindingSummaryData data) {
        _destinationCountText.text = data.DestinationCount.ToString();
        _successText.text = data.SuccessfulRuns.ToString();
        _unsuccessText.text = data.UnsuccessfulRuns.ToString();
        _durationText.text = $"{data.Duration.ToString()} seconds";
    }
    
    public void Toggle(bool state) {
        gameObject.SetActive(state);
    }

    public struct WayFindingSummaryData {
        public int DestinationCount;
        public int SuccessfulRuns;
        public int UnsuccessfulRuns;
        public float Duration;
    }

    public void ReturnToMainMenu() {
        SceneManager.LoadScene("MainMenu");
    }
        
    public void ResetScene() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
