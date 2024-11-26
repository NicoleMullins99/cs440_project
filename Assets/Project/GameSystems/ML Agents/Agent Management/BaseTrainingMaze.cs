using System.Collections.Generic;
using Project.GameSystems.Maze;
using Project.GameSystems.ML_Agents.Agent_Management.Managers;
using Project.GameSystems.ML_Agents.Training;
using UnityEngine;

namespace Project.GameSystems.ML_Agents.Agent_Management {
    public abstract class BaseTrainingMaze : MonoBehaviour {
        [Header("Maze Specific Values")] [SerializeField]
        protected SO_Rewards _rewards;

        [SerializeField] protected Vector3 _previewCameraPosition = new Vector3(-15,200,0);
        [SerializeField] protected Vector3 _previewCameraRotation = new Vector3(90,0,0);
        [SerializeField, Min(0)] protected int _agentTrainingSteps = 2000;
        [SerializeField, Min(0.1f)] private float _tileStayTimer = 10f;
        [SerializeField, Range(0f, 1f)] protected float _minMazeExploreTarget = 0.5f;
        [SerializeField, Min(1)] protected int _maxIterations = 10;

        [SerializeField, Range(0.00001f, 0.01f)]
        protected float _stepPunishmentRatio;

        protected MazeManager _mazeManager;
        protected RunType _runType;

        public abstract Transform MazeTarget { get; }

        public abstract CollisionPunishmentLevel CollisionEnterPunishmentLevel { get; }
        public abstract CollisionPunishmentLevel CollisionStayPunishmentLevel { get; }

        public abstract int MazeTileCount { get; }
        public SO_Rewards Rewards => _rewards;
        public float MinMazeExploreTarget => _minMazeExploreTarget;
        public float TileStayTimer => _tileStayTimer;
        public int AgentTrainingSteps => _agentTrainingSteps;
        public int MaxIterations => _maxIterations;
        
        public float StepPunishmentRatio => _stepPunishmentRatio;

        public abstract MazeRoadTile GetRandomMazeTile();
        public abstract MazeRoadTile GetRandomMazeTile(MazeRoadTile ignoreTile);
        public abstract MazeRoadTile GetRandomMazeTile(List<MazeRoadTile> ignoreTiles);
        public abstract Quaternion GetRandomAgentSpawnRotation();

        public abstract MazeRoadTile GetCurrentEpisodeSpawnTile();
        public abstract MazeRoadTile GetCurrentEpisodeSpawnTile(int iterationCount);

        public abstract MazeRoadTile GetCurrentEpisodeExitTile(int iterationCount);
        public abstract void SetMazeExitPosition(int iterationCount);
        public abstract Quaternion GetCurrentEpisodeSpawnRotation();

        public abstract void RefreshMaze();

        public abstract void InitialiseMazeTiles();
        public abstract void SetEnvironmentVisibility(bool state);

        public void SetMazeManager(MazeManager manager) {
            _mazeManager = manager;
        }

        public void SetRunType(RunType runType)
        {
            _runType = runType;
        }

        public virtual void SetPreviewCamera(Transform cam)
        {
            cam.SetParent(transform, true);
            cam.localPosition = _previewCameraPosition;
            cam.localRotation = Quaternion.Euler(_previewCameraRotation);
        }

    }

    public enum CollisionPunishmentLevel {
        Light,
        Medium,
        Heavy
    }
}