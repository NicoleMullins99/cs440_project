using System;
using System.Collections;
using System.Collections.Generic;
using Project.GameSystems.Maze;
using Project.GameSystems.Player;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Project.GameSystems.ML_Agents.Agent_Management {
    [Obsolete("Outdated, DiscreteSimpleAgent is the current agent script")]
    public class SimpleAgent : MazeAgent {
        private delegate void ProcessOnCollideEnter(Collision other);

        private delegate void ProcessOnCollideStay(Collision other);

        private ProcessOnCollideEnter _processOnCollideEnter;
        private ProcessOnCollideStay _processOnCollideStay;

        [SerializeField] private List<MazeRoadTile> _visitedTiles;
        [SerializeField] private List<MazeRoadTile> _availableJunctionStack;
        [SerializeField] private int[] _neighborTilesVisitCount;

        [SerializeField] private MazeRoadTile _lastAvailableJunction;
        [SerializeField] private MazeRoadTile _currentTile;
        private Transform _currentTileTransform;

        private Coroutine _tileStayCoroutine;
        private Coroutine _tileVisitCoroutine;

        private float _visitedTilePercentage = 0f;
        private float _timeStepPunishment = 0f;
        [SerializeField] private float _tileVisitReward;

        [SerializeField] private int _mazeTileCount;
        [SerializeField] private int _visitedMazeTileCount;
        private int _episodeIteration;

        [SerializeField] private bool _tileStayPunish;
        [SerializeField] private bool _tileVisitThresholdPunish;
        [SerializeField] private bool _reachedDeadEnd;

        public override void ResetAgent() {
            _episodeIteration = 0;
            base.ResetAgent();
        }

        public void SetMaxStep(int steps) {
            MaxStep = steps;
        }

        public GameObject GetGameObject() {
            return gameObject;
        }

        private void CheckMaxNegativeReward() {
            if (GetCumulativeReward() < _currentTrainingEnvironment.Rewards.MaxNegativeReward) {
                SetReward(_currentTrainingEnvironment.Rewards.MaxNegativeReward);
                ProcessEndEpisode(false);
            }
        }

        #region MLAgents

        public override void Initialize() {
            base.Initialize();

            _mazeTileCount = _currentTrainingEnvironment.MazeTileCount;
            _visitedTilePercentage = 0f;
            _visitedMazeTileCount = 0;
            _tileVisitReward = _currentTrainingEnvironment.Rewards.IsConstantRewards
                ? _currentTrainingEnvironment.Rewards.TileVisitReward
                : _currentTrainingEnvironment.Rewards.MaxTileVisitReward / _mazeTileCount;
            _visitedTiles = new List<MazeRoadTile>();
            _availableJunctionStack = new List<MazeRoadTile>();
            _neighborTilesVisitCount = new int[4];
            MaxStep = _currentTrainingEnvironment.AgentTrainingSteps;
            _timeStepPunishment = _currentTrainingEnvironment.Rewards.IsConstantRewards
                ? _currentTrainingEnvironment.Rewards.TimeStepPunishment
                : (_currentTrainingEnvironment.Rewards.MaxNegativeReward / MaxStep) * 1.5f;

            transform.parent = _currentTrainingEnvironment.transform;

            _processOnCollideEnter = CollidePunishEnterHeavy;
            _processOnCollideStay = CollideStayPunishHeavy;

            if (_currentTrainingEnvironment.CollisionEnterPunishmentLevel == CollisionPunishmentLevel.Light) {
                _processOnCollideEnter = CollidePunishEnterLight;
                _processOnCollideStay = CollideStayPunishLight;
            }
            else if (_currentTrainingEnvironment.CollisionEnterPunishmentLevel == CollisionPunishmentLevel.Medium) {
                _processOnCollideEnter = CollidePunishEnterMedium;
                _processOnCollideStay = CollideStayPunishMedium;
            }
            else if (_currentTrainingEnvironment.CollisionEnterPunishmentLevel == CollisionPunishmentLevel.Heavy) {
                _processOnCollideEnter = CollidePunishEnterHeavy;
                _processOnCollideStay = CollideStayPunishHeavy;
            }
        }

        public override void CollectObservations(VectorSensor sensor) {
            // 6 float values passed
            //sensor.AddObservation(transform.localPosition);

            // Forward vector y value is always 0, useless observation
            Vector3 forwardVector = transform.forward;
            sensor.AddObservation(new Vector2(forwardVector.x, forwardVector.z));

            sensor.AddObservation(_visitedTilePercentage);
            sensor.AddObservation(_tileStayPunish);
            sensor.AddObservation(_reachedDeadEnd);
            // Visited tile neighbor observations
            sensor.AddObservation(_neighborTilesVisitCount[0]);
            sensor.AddObservation(_neighborTilesVisitCount[1]);
            sensor.AddObservation(_neighborTilesVisitCount[2]);
            sensor.AddObservation(_neighborTilesVisitCount[3]);
            _currentReward = GetCumulativeReward();
        }

        public override void OnActionReceived(ActionBuffers actions) {
            // Receive 2 continuous actions
            // 0 for x axis
            // 1 for z axis
            // 2 for y rotation
            float moveX = actions.ContinuousActions[0];
            float moveZ = actions.ContinuousActions[1];
            float yRotation = actions.ContinuousActions[2];

            // Ignore rotation if no input
            if (yRotation != 0f) {
                float rotationVector = yRotation * (_rotationSpeed * Time.fixedDeltaTime);
                Quaternion newRotation = _rigidbody.rotation *
                                         Quaternion.Euler(Vector3.up * Mathf.Clamp(rotationVector, -90f, 90f));
                // Set z rotation to 0
                newRotation = Quaternion.Euler(new Vector3(0, newRotation.eulerAngles.y, 0));
                _rigidbody.MoveRotation(newRotation);
            }

            // Handle first person camera movement
            if (_isPlayerControlled) {
                HandlePlayerRotation();
            }

            Vector3 fixedVector = transform.right * moveX;
            fixedVector += transform.forward * moveZ;

            _rigidbody.Move(
                transform.position +
                (new Vector3(fixedVector.x, 0, fixedVector.z) * (_moveSpeed * Time.fixedDeltaTime)),
                _rigidbody.rotation);
            AddReward(_timeStepPunishment);

            CheckMaxNegativeReward();
        }

        public override void Heuristic(in ActionBuffers actionsOut) {
            if (!_isPlayerControlled) return;
            ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
            PlayerControllerInputs actions = _agentTrainingManager.PlayerInputController.PlayerInputs;

            continuousActions[0] = Mathf.Clamp(actions.MoveVector.x, -1, 1);
            continuousActions[1] = Mathf.Clamp(actions.MoveVector.y, -1, 1);
            continuousActions[2] = Mathf.Clamp(actions.MouseVector.x, -1, 1);
        }

        public override void OnEpisodeBegin() {
            transform.parent = _currentTrainingEnvironment.transform;
            foreach (var tile in _visitedTiles) {
                tile.ResetTile(_agentIndex);
            }

            _visitedTiles.Clear();
            _availableJunctionStack.Clear();

            if (_tileVisitCoroutine != null) StopCoroutine(_tileVisitCoroutine);
            if (_tileStayCoroutine != null) StopCoroutine(_tileStayCoroutine);

            // _currentReward = 0f;
            // _visitedMazeTileCount = 0;
            // _visitedTilePercentage = 0f;
            // _mazeTileCount = _currentTrainingEnvironment.MazeTileCount;
            // _tileVisitReward = _currentTrainingEnvironment.Rewards.MaxTileVisitReward / _mazeTileCount;
            // _tileVisitThresholdPunish = false;
            // _tileStayPunish = false;
            // _timeStepPunishment = (_currentTrainingEnvironment.Rewards.MaxNegativeReward / MaxStep) * 1.5f;

            _currentReward = 0f;
            _visitedMazeTileCount = 0;
            _visitedTilePercentage = 0f;
            _mazeTileCount = _currentTrainingEnvironment.MazeTileCount;
            _tileVisitReward = _currentTrainingEnvironment.Rewards.IsConstantRewards
                ? _currentTrainingEnvironment.Rewards.TileVisitReward
                : _currentTrainingEnvironment.Rewards.MaxTileVisitReward / _mazeTileCount;

            _neighborTilesVisitCount = new int[4];
            _tileVisitThresholdPunish = false;
            _tileStayPunish = false;
            _timeStepPunishment = _currentTrainingEnvironment.Rewards.IsConstantRewards
                ? _currentTrainingEnvironment.Rewards.TimeStepPunishment
                : (_currentTrainingEnvironment.Rewards.MaxNegativeReward / MaxStep) * 1.5f;

            _currentTile = _currentTrainingEnvironment.GetCurrentEpisodeSpawnTile(_episodeIteration);
            transform.localPosition = _currentTile.transform.localPosition + Vector3.up * 2;
            transform.localRotation = _currentTrainingEnvironment.GetRandomAgentSpawnRotation();
        }

        #endregion

        private void OnCollisionEnter(Collision other) {
            if (!gameObject.activeSelf) return;
            if (other.gameObject.CompareTag("Road")) {
                MazeRoadTile tile = other.gameObject.GetComponent<MazeRoadTile>();
                // First time visiting tile
                if (!_visitedTiles.Contains(tile)) {
                    _visitedTiles.Add(tile);
                    _visitedMazeTileCount++;
                    if (_tileVisitCoroutine != null) {
                        StopCoroutine(_tileVisitCoroutine);
                    }

                    _tileVisitCoroutine = StartCoroutine(TileVisitTimer());
                    float newVisitedTilePercentage = _visitedMazeTileCount / (float) _mazeTileCount;
                    if (newVisitedTilePercentage > _visitedTilePercentage) {
                        AddReward(_tileVisitReward);
                    }

                    _visitedTilePercentage = newVisitedTilePercentage;
                }

                if (_currentTileTransform == null || other.transform != _currentTileTransform) {
                    EnteredNewTile(tile);
                }
            }

            CheckMaxNegativeReward();

            _processOnCollideEnter(other);
        }

        private void OnCollisionStay(Collision other) {
            if (other.gameObject.CompareTag("Road")) {
                // Still in same tile and can punish
                if ((other.transform == _currentTileTransform && _tileStayPunish) || _tileVisitThresholdPunish) {
                    AddReward(_currentTrainingEnvironment.Rewards.TileStayPunishment);

                    CheckMaxNegativeReward();
                }
            }

            _processOnCollideStay(other);
        }

        public override void ProcessEndEpisode(bool reachedTarget) {
            if (_visitedTilePercentage < _currentTrainingEnvironment.MinMazeExploreTarget && !reachedTarget) {
                // Didn't move much
                SetReward(_currentTrainingEnvironment.Rewards.MaxNegativeReward);
            }

            if (reachedTarget) _episodeIteration++;

            _agentTrainingManager.FinishedEpisode(this, reachedTarget, _episodeIteration);
            StopAllCoroutines();
            EndEpisode();
        }

        private void EnteredNewTile(MazeRoadTile tile) {
            // Already been to this tile and know its a dead
            // Had an unvisited tile that could have been visited instead
            if (tile.WasVisited(_agentIndex) && tile.HasVisitedAllNeighbors(_agentIndex) &&
                _currentTile.HasOtherUnvisitedNeighbors(_agentIndex, tile)) {
                AddReward(-_tileVisitReward * (1 + Mathf.Log(tile.GetVisitCount(_agentIndex))));
            }
            else // Reached a dead end and are currently backtracking
            if (_reachedDeadEnd && tile.WasVisited(_agentIndex) &&
                _currentTile.HasVisitedAllNeighbors(_agentIndex)) {
                // Reset the visit tile timer
                if (_tileVisitCoroutine != null) StopCoroutine(_tileVisitCoroutine);
                _tileVisitCoroutine = StartCoroutine(TileVisitTimer());
            }
            else // Back tracked to last available junction after reaching a dead end
            if (_reachedDeadEnd && !tile.HasVisitedAllNeighbors(_agentIndex)) {
                AddReward(_tileVisitReward);
            }

            tile.Visit(_agentIndex);
            if (_currentTile != null && _currentTile.HasVisitedAllNeighbors(_agentIndex)) {
                if (_availableJunctionStack.Contains(_currentTile)) {
                    _availableJunctionStack.Remove(_currentTile);
                }
            }

            _currentTile = tile;
            _currentTileTransform = tile.transform;

            MazeRoadTile[] neighbors = _currentTile.GetNeighbors();
            for (int i = 0; i < neighbors.Length; i++) {
                if (neighbors[i] == null) {
                    // No neighbor, can't move to
                    _neighborTilesVisitCount[i] = -1;
                }
                else {
                    _neighborTilesVisitCount[i] = neighbors[i].GetVisitCount(_agentIndex);
                }
            }

            if (!_currentTile.HasVisitedAllNeighbors(_agentIndex)) {
                // Available paths
                _reachedDeadEnd = false;
                _lastAvailableJunction = tile;
                if (!_availableJunctionStack.Contains(tile)) {
                    _availableJunctionStack.Add(tile);
                }
                else {
                    // Move to top of the stack
                    _availableJunctionStack.Remove(tile);
                    _availableJunctionStack.Add(tile);
                }
            }
            else {
                // No path, must back track
                _reachedDeadEnd = true;
                if (_availableJunctionStack.Contains(tile)) {
                    _availableJunctionStack.Remove(tile);
                }
            }

            if (_tileStayCoroutine != null) StopCoroutine(_tileStayCoroutine);
            _tileStayCoroutine = StartCoroutine(TileStayTimer());
        }

        private void HandlePlayerRotation() {
            PlayerControllerInputs playerControllerInputs =
                _agentTrainingManager.PlayerInputController.PlayerInputs;

            Vector2 rotationVector = playerControllerInputs.MouseVector * (_rotationSpeed * Time.fixedDeltaTime);
            Vector3 desiredRotation =
                new Vector3(_headCameraHolder.localRotation.eulerAngles.x + rotationVector.y, 0, 0);

            // Get values between -180 and 180 rather than 0 and 360
            float clampedX = desiredRotation.x;
            if (clampedX > 180) {
                clampedX -= 360;
            }

            if (clampedX > _maxEulerAngle) {
                desiredRotation = new Vector3(_maxEulerAngle, 0, 0);
            }
            else if (clampedX < _minEulerAngle) {
                desiredRotation = new Vector3(_minEulerAngle, 0, 0);
            }

            _headCameraHolder.localRotation = Quaternion.Euler(desiredRotation);
        }

        private IEnumerator TileStayTimer() {
            _tileStayPunish = false;
            yield return new WaitForSeconds(_currentTrainingEnvironment.TileStayTimer);
            _tileStayPunish = true;
        }

        private IEnumerator TileVisitTimer() {
            _tileVisitThresholdPunish = false;
            int tileVisitCount = _visitedTiles.Count;
            yield return new WaitForSeconds(_currentTrainingEnvironment.TileStayTimer);
            // Didn't increase tile visit
            if (_visitedTiles.Count <= tileVisitCount) {
                _tileVisitThresholdPunish = true;
            }
        }

        #region CollisionEnter Processing

        private void CollidePunishEnterHeavy(Collision other) {
            if (other.gameObject.CompareTag("Wall")) {
                SetReward(_currentTrainingEnvironment.Rewards.MaxNegativeReward);
                ProcessEndEpisode(false);
            }
            else if (other.gameObject.CompareTag("Target")) {
                SetReward(_currentTrainingEnvironment.Rewards.TargetCollisionReward);
                ProcessEndEpisode(true);
            }
        }

        private void CollidePunishEnterMedium(Collision other) {
            if (other.gameObject.CompareTag("Wall")) {
                AddReward(_currentTrainingEnvironment.Rewards.WallCollisionReward / 10f);
            }
            else if (other.gameObject.CompareTag("Target")) {
                SetReward(_currentTrainingEnvironment.Rewards.TargetCollisionReward);
                ProcessEndEpisode(true);
            }
        }

        private void CollidePunishEnterLight(Collision other) {
            if (other.gameObject.CompareTag("Wall")) {
                AddReward(_currentTrainingEnvironment.Rewards.WallCollisionReward / 100f);
            }
            else if (other.gameObject.CompareTag("Target")) {
                SetReward(_currentTrainingEnvironment.Rewards.TargetCollisionReward);
                ProcessEndEpisode(true);
            }
        }

        #endregion

        #region CollisionStay Processing

        private void CollideStayPunishLight(Collision other) {
            if (other.gameObject.CompareTag("Wall")) {
                AddReward(_currentTrainingEnvironment.Rewards.WallCollisionStayReward);
            }
        }

        private void CollideStayPunishMedium(Collision other) {
            if (other.gameObject.CompareTag("Wall")) {
                AddReward(_currentTrainingEnvironment.Rewards.WallCollisionStayReward);
            }
        }

        private void CollideStayPunishHeavy(Collision other) {
            if (other.gameObject.CompareTag("Wall")) {
                AddReward(_currentTrainingEnvironment.Rewards.WallCollisionReward);
            }
        }

        #endregion
    }
}