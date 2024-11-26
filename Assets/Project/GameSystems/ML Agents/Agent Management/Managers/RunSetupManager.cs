using System.Collections.Generic;
using Project.GameSystems.DataManagement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.GameSystems.ML_Agents.Agent_Management.Managers
{
    public class RunSetupManager : MonoBehaviour
    {
        [SerializeField] private MazeManager _mazeManager;
        [SerializeField] private PlayerDataStream _playerDataStream;
        [SerializeField] private List<BaseTrainingMaze> _mazeList;

        [SerializeField] private BaseTrainingMaze _selectedMaze;
        [SerializeField] private RunType _runType;
        [SerializeField] private MapShowcase _mapShowcase;

        [SerializeField] private TMP_Dropdown _mapDropdown;
        [SerializeField] private TMP_Dropdown _runTypeDropdown;
        [SerializeField] private TMP_Dropdown _mapShowcaseDropdown;
        [SerializeField] private TMP_Dropdown _streamTypeDropdown;

        [SerializeField] private Slider _episodeCountInput;
        [SerializeField] private Slider _episodeRandomCountInput;
        [SerializeField] private Slider _episodeMaxLengthInput;
        [SerializeField] private Slider _mapPreviewInput;
        [SerializeField] private Toggle _dataSaveToggle;
        [SerializeField] private Toggle _runTimeToggle;

        [SerializeField] private Transform _episodeMaxLengthTransform;

        [SerializeField] [Min(1)] private int _runCount = 1;

        [SerializeField] [Min(1)] [Tooltip("How often should the target / player position change")]
        private int _episodeRandomisationFrequency = 1;

        [SerializeField] [Min(1)] private float _maxEpisodeLength = 60f;
        [SerializeField] [Min(0.5f)] private float _mapPreviewTime = 0.5f;
        [SerializeField] private bool _enableDataSaving = true;

        private bool _isMarkerStream;
        
        private void Awake() {
            _streamTypeDropdown.value = PlayerPrefs.GetInt("Nav_StreamMode", 0);
            _streamTypeDropdown.RefreshShownValue();
        }
        
        public void StartGame()
        {
            // Grab values from each UI option
            SetRunType();
            SetMapShowcase();
            SetEpisodeCount();
            SetEpisodeResetFrequency();
            SetMapPreviewTime();
            SetMaxEpisodeLength();
            SetDataSave();
            SetMapType();
            SetStreamType();
            
            RunSettings settings = new RunSettings
            {
                Maze = _selectedMaze,
                RunType = _runType,
                MapShowcase = _mapShowcase,
                RunCount = _runCount,
                EpisodeResetFreq = _episodeRandomisationFrequency,
                MaxEpisodeLength = _maxEpisodeLength,
                MapPreviewTime = _mapPreviewTime,
                EnableDataSaving = _enableDataSaving
            };

            _mazeManager.LoadSettingsAndStart(settings);
            gameObject.SetActive(false);
        }

        public void SetMapType() {
            _selectedMaze = _mazeList[_mapDropdown.value];
        }

        public void SetRunType()
        {
            _runType = (RunType)_runTypeDropdown.value;
        }

        public void SetMapShowcase()
        {
            _mapShowcase = (MapShowcase)_mapShowcaseDropdown.value;
        }

        public void SetEpisodeCount()
        {
            _runCount = (int)_episodeCountInput.value;
        }

        public void SetEpisodeResetFrequency()
        {
            _episodeRandomisationFrequency = (int)_episodeRandomCountInput.value;
        }

        public void SetMaxEpisodeLength() {
            _maxEpisodeLength = _runTimeToggle.isOn ? _episodeMaxLengthInput.value : float.MaxValue;
        }

        public void SetMapPreviewTime()
        {
            _mapPreviewTime = _mapPreviewInput.value;
        }

        public void SetDataSave()
        {
            _enableDataSaving = _dataSaveToggle.isOn;
        }

        public void SetTimeLimitToggle() {
            _episodeMaxLengthTransform.gameObject.SetActive(_runTimeToggle.isOn);
        }
        
        public void SetStreamType() {
            _isMarkerStream = _streamTypeDropdown.value != 0;
            _playerDataStream.SetBehaviorDataStreamMarkerOnly(_isMarkerStream);
            PlayerPrefs.SetInt("Nav_StreamMode", _streamTypeDropdown.value);
        }
    }

    public struct RunSettings {
        public BaseTrainingMaze Maze;
        public RunType RunType;
        public MapShowcase MapShowcase;
        public int RunCount;
        public int EpisodeResetFreq;
        public float MaxEpisodeLength;
        public float MapPreviewTime;
        public bool EnableDataSaving;
    }

    public enum RunType
    {
        StaticSpawnMovingTarget,
        StaticSpawnStaticTarget,
        MovingSpawnMovingTarget,
        MovingSpawnStaticTarget
    }

    public enum MapShowcase
    {
        None,
        ShowMapWithoutTarget,
        ShowMapWithTarget
    }
}