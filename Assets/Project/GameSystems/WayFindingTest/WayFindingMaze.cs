using System.Collections.Generic;
using Project.GameSystems.Maze;
using Project.GameSystems.ML_Agents.Agent_Management;
using UnityEditor;
using UnityEngine;

namespace Project.GameSystems.WayFindingTest {
    public class WayFindingMaze : BaseTrainingMaze {
        [Header("Maze Parameters")] [SerializeField]
        private GameObject _environmentHolder;

        [Header("Start up parameters")] [SerializeField]
        private LayerMask _roadLayerMask;

        [SerializeField] private List<MazeRoadTile> _roadTiles;
        [SerializeField] private CollisionPunishmentLevel _collisionEnterPunishmentLevel;
        [SerializeField] private CollisionPunishmentLevel _collisionStayPunishmentLevel;

        public override Transform MazeTarget => null;

        public override int MazeTileCount => _roadTiles.Count;
        public override CollisionPunishmentLevel CollisionEnterPunishmentLevel => _collisionEnterPunishmentLevel;
        public override CollisionPunishmentLevel CollisionStayPunishmentLevel => _collisionStayPunishmentLevel;

        // TODO: The maze class structure should be refactored to remove all of the below abstract methods that
        // are not used in this implementation. This will make the class easier to understand and maintain.
        public override MazeRoadTile GetRandomMazeTile() {
            throw new System.NotImplementedException();
        }

        public override MazeRoadTile GetRandomMazeTile(MazeRoadTile ignoreTile) {
            throw new System.NotImplementedException();
        }

        public override MazeRoadTile GetRandomMazeTile(List<MazeRoadTile> ignoreTiles) {
            throw new System.NotImplementedException();
        }

        public override Quaternion GetRandomAgentSpawnRotation() {
            throw new System.NotImplementedException();
        }

        public override MazeRoadTile GetCurrentEpisodeSpawnTile() {
            throw new System.NotImplementedException();
        }

        public override MazeRoadTile GetCurrentEpisodeSpawnTile(int iterationCount) {
            throw new System.NotImplementedException();
        }

        public override MazeRoadTile GetCurrentEpisodeExitTile(int iterationCount) {
            throw new System.NotImplementedException();
        }

        public override void SetMazeExitPosition(int iterationCount) {
            throw new System.NotImplementedException();
        }

        public override Quaternion GetCurrentEpisodeSpawnRotation() {
            throw new System.NotImplementedException();
        }

        public override void RefreshMaze() {
            throw new System.NotImplementedException();
        }

        public override void InitialiseMazeTiles() {
            foreach (var tile in _roadTiles) {
                tile.Initialise(1);
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