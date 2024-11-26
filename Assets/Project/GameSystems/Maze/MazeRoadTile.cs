using UnityEngine;

namespace Project.GameSystems.Maze {
    public class MazeRoadTile : MonoBehaviour {
        // 0 North, 1 East, 2 South, 3 West
        [SerializeField] private MazeRoadTile[] _neighborTileConnections = new MazeRoadTile[4];


        private bool[] _visitedArray;
        private int[] _visitCountArray;


        public void Initialise(int numAgents) {
            _visitedArray = new bool[numAgents];
            _visitCountArray = new int[numAgents];
        }

        public void SetNeighbors(MazeRoadTile[] neighbors) {
            _neighborTileConnections = neighbors;
        }

        public MazeRoadTile[] GetNeighbors() {
            return _neighborTileConnections;
        }

        public void Visit(int index) {
            _visitedArray[index] = true;
            _visitCountArray[index]++;
        }

        public void CompleteResetTile() {
            for (int i = 0; i < _visitedArray.Length; i++) {
                _visitedArray[i] = false;
                _visitCountArray[i] = 0;
            }
        }

        public void ResetTile(int agentIndex) {
            _visitedArray[agentIndex] = false;
            _visitCountArray[agentIndex] = 0;
        }

        public bool HasVisitedAllNeighbors(int index) {
            foreach (var tile in _neighborTileConnections) {
                if (tile == null) {
                    continue;
                }

                if (!tile.WasVisited(index)) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check if a tiles neighbors haven't been visited, excluding a tile
        /// </summary>
        /// <param name="index">Agent index</param>
        /// <param name="tile">Ignore tile</param>
        /// <returns></returns>
        public bool HasOtherUnvisitedNeighbors(int index, MazeRoadTile tile) {
            foreach (var t in _neighborTileConnections) {
                if (t == tile) {
                    continue;
                }

                MazeRoadTile t1 = t;
                if (t1 != null && !t1.WasVisited(index)) {
                    return true;
                }
            }

            return false;
        }

        public bool WasVisited(int index) {
            return _visitedArray[index];
        }

        public int GetVisitCount(int index) {
            return _visitCountArray[index];
        }

        public int GetNeighborCount() {
            int count = 0;
            for (int i = 0; i < _neighborTileConnections.Length; i++) {
                if (_neighborTileConnections[i] != null) {
                    count++;
                }
            }

            return count;
        }
    }
}