using System.Collections.Generic;
using System.Linq;
using Project.GameSystems.Maze;
using Project.GameSystems.ML_Agents.Agent_Management.Managers;
using Project.Utils;
using UnityEditor;
using UnityEngine;

namespace Project.GameSystems.ML_Agents.Agent_Management {
    public class TrainingMaze : BaseTrainingMaze {
        [Header("Maze Parameters")] [SerializeField]
        private List<Transform> _agentSpawnPoints;

        [SerializeField] private List<Transform> _targetSpawnPoints;

        [SerializeField] private Transform _mazeExit;
        [SerializeField] private GameObject _environmentHolder;

        [Header("Start up parameters")] [SerializeField]
        private bool _randomiseExitSpawns;

        [SerializeField] private LayerMask _roadLayerMask;
        [SerializeField] private List<MazeRoadTile> _roadTiles;
        [SerializeField] private CollisionPunishmentLevel _collisionEnterPunishmentLevel;
        [SerializeField] private CollisionPunishmentLevel _collisionStayPunishmentLevel;

        private MazeRoadTile[] _agentSpawnTiles;
        private MazeRoadTile[] _exitSpawnTiles;

        private Quaternion _currentSpawnRotation;
        private Transform _lastExit;

        public override Transform MazeTarget => _mazeExit;

        public override int MazeTileCount => _roadTiles.Count;
        public override CollisionPunishmentLevel CollisionEnterPunishmentLevel => _collisionEnterPunishmentLevel;
        public override CollisionPunishmentLevel CollisionStayPunishmentLevel => _collisionStayPunishmentLevel;

        public override MazeRoadTile GetRandomMazeTile() {
            // return _agentSpawnPoints[Random.Range(0, _agentSpawnPoints.Count)];
            return _roadTiles[Random.Range(0, _roadTiles.Count)];
        }

        public override MazeRoadTile GetRandomMazeTile(MazeRoadTile ignoreTile) {
            // Find index of the tile
            // Create a list of the index's of the road tiles
            // Remove the ignore tile index
            // Randomly select an index to pick from the road tile array
            int index = _roadTiles.IndexOf(ignoreTile);
            List<int> sequence = Enumerable.Range(0, _roadTiles.Count).ToList();
            sequence.Remove(index);
            return _roadTiles[sequence[Random.Range(0, sequence.Count)]];
        }

        public override MazeRoadTile GetRandomMazeTile(List<MazeRoadTile> ignoreTiles) {
            List<MazeRoadTile> roadList = _roadTiles.Except(ignoreTiles).ToList();
            return roadList[Random.Range(0, roadList.Count)];
        }

        public override Quaternion GetRandomAgentSpawnRotation() {
            return Quaternion.Euler(new Vector3(0,
                Constants.SpawnDirections[Random.Range(0, Constants.SpawnDirections.Length)], 0));
        }

        public override MazeRoadTile GetCurrentEpisodeSpawnTile() {
            return _agentSpawnTiles[0];
        }

        public override MazeRoadTile GetCurrentEpisodeSpawnTile(int iterationCount) {
            // Agent spawn point moves
            if (_runType == RunType.MovingSpawnMovingTarget || _runType == RunType.MovingSpawnStaticTarget) {
                return _agentSpawnTiles[iterationCount];
            }

            // Agent spawn is static, same spawn point
            return _agentSpawnTiles[0];
        }

        public override MazeRoadTile GetCurrentEpisodeExitTile(int iterationCount) {
            // Agent spawn point moves
            if (_runType == RunType.MovingSpawnMovingTarget || _runType == RunType.StaticSpawnMovingTarget) {
                return _exitSpawnTiles[iterationCount];
            }

            // Agent spawn is static, same spawn point
            return _exitSpawnTiles[0];
        }

        public override void SetMazeExitPosition(int iterationCount) {
            MazeRoadTile tile = GetCurrentEpisodeExitTile(iterationCount);
            _mazeExit.position = tile.transform.position + Vector3.up * 2;
        }

        public override Quaternion GetCurrentEpisodeSpawnRotation() {
            return _currentSpawnRotation;
        }

        public override void RefreshMaze() {
            _exitSpawnTiles = new MazeRoadTile[_mazeManager.NumberEpisodesBeforeReset];
            _agentSpawnTiles = new MazeRoadTile[_mazeManager.NumberEpisodesBeforeReset];
            for (int i = 0; i < _agentSpawnTiles.Length; i++) {
                _exitSpawnTiles[i] = GetRandomMazeTile();
                // Can't allow player to spawn in the exit
                _agentSpawnTiles[i] = GetRandomMazeTile(_exitSpawnTiles[i]);
            }

            _currentSpawnRotation = GetRandomAgentSpawnRotation();
        }

        public override void InitialiseMazeTiles() {
            RefreshMaze();
            foreach (var tile in _roadTiles) {
                tile.Initialise(_mazeManager.NumberOfAgents);
            }
        }

        public override void SetEnvironmentVisibility(bool state) {
            if (_environmentHolder != null) {
                _environmentHolder.SetActive(state);
            }   
        }

#if UNITY_EDITOR

        [ContextMenu("Setup Maze Tiles")]
        public void SetupMazeTiles() {
            _roadTiles = new List<MazeRoadTile>();
            _roadTiles.AddRange(transform.GetComponentsInChildren<MazeRoadTile>());

            foreach (MazeRoadTile tile in _roadTiles) {
                // Keeps the references
                EditorUtility.SetDirty(tile);
                tile.SetNeighbors(FindNeighborTiles(tile.transform));
            }
        }
#endif
        private MazeRoadTile[] FindNeighborTiles(Transform roadTransform) {
            MazeRoadTile[] returnArray = new MazeRoadTile[4];
            RaycastHit hit;
            if (Physics.Raycast(roadTransform.position + new Vector3(0, 10, 10), Vector3.down * 30, out hit, 30f,
                    _roadLayerMask)) {
                returnArray[0] = hit.transform.GetComponent<MazeRoadTile>();
            }

            if (Physics.Raycast(roadTransform.position + new Vector3(10, 10, 0), Vector3.down * 30, out hit, 30f,
                    _roadLayerMask)) {
                returnArray[1] = hit.transform.GetComponent<MazeRoadTile>();
            }

            if (Physics.Raycast(roadTransform.position + new Vector3(0, 10, -10), Vector3.down * 30, out hit, 30f,
                    _roadLayerMask)) {
                returnArray[2] = hit.transform.GetComponent<MazeRoadTile>();
            }

            if (Physics.Raycast(roadTransform.position + new Vector3(-10, 10, 0), Vector3.down * 30, out hit, 30f,
                    _roadLayerMask)) {
                returnArray[3] = hit.transform.GetComponent<MazeRoadTile>();
            }

            return returnArray;
        }
    }
}