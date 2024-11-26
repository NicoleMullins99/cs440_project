using Project.GameSystems.DataManagement;
using Project.GameSystems.WayFindingTest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WfSetupUi : MonoBehaviour {
    [SerializeField] private WayFindingManager _wayFindingManager;
    [SerializeField] private PlayerDataStream _playerDataStream;

    [SerializeField] private Toggle _modeToggle;
    [SerializeField] private TMP_Dropdown _streamModeDropdown;
    [SerializeField] private TMP_Dropdown _taskDropdown;
    [SerializeField] private TMP_Text _streamStatus;

    private bool _isTrainingMode;
    private bool _isMarkerStreamMode;
    
    private int _taskIndex = 0;

    private void Awake() {
        _streamModeDropdown.value = PlayerPrefs.GetInt("WF_StreamMode", 0);
        _streamModeDropdown.RefreshShownValue();
    }

    private void Start() {
        UpdateStreamStatus(_playerDataStream.AreStreamsOpen);
    }

    public void StartGame() {
        SetMode();
        SetStreamMode();
        SetTask();
        _wayFindingManager.LoadSettingsAndStart(_isTrainingMode, _isMarkerStreamMode, _taskIndex);
        gameObject.SetActive(false);
    }

    public void SetMode() {
        _isTrainingMode = _modeToggle.isOn;
    }
    
    public void SetStreamMode() {
        _isMarkerStreamMode = _streamModeDropdown.value != 0;
        _playerDataStream.SetBehaviorDataStreamMarkerOnly(_isMarkerStreamMode);
        PlayerPrefs.SetInt("WF_StreamMode", _streamModeDropdown.value);
    }
    
    public void SetTask() {
        _taskIndex = _taskDropdown.value;
    }

    public void ToggleStreams() {
        if (!_playerDataStream.AreStreamsOpen) {
            _playerDataStream.InitialiseStreams();
        }
        
        UpdateStreamStatus(_playerDataStream.AreStreamsOpen);
    }
    
    private void UpdateStreamStatus(bool status) {
        if (status) {
            _streamStatus.text = "LSL stream status: <color=#44BD32>Open</color>";
        }
        else {
            _streamStatus.text = "LSL stream status: <color=#E84118>Closed</color>";
        }
    }
}