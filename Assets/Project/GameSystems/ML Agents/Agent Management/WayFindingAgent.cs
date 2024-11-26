using System;
using System.Collections;
using System.Collections.Generic;
using Project.GameSystems.Maze;
using Project.GameSystems.Player;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Project.GameSystems.ML_Agents.Agent_Management {
    public class WayFindingAgent : MazeAgent {
        private delegate void ProcessOnCollideEnter(Collision other);

        private delegate void ProcessOnCollideStay(Collision other);

        private ProcessOnCollideEnter _processOnCollideEnter;
        private ProcessOnCollideStay _processOnCollideStay;

        [SerializeField] private List<MazeRoadTile> _visitedTiles;
        [SerializeField] private MazeRoadTile[] _neighborTiles;

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

        [SerializeField] private float _moveValue_Forward;
        [SerializeField] private float _moveValue_Right;
        [SerializeField] private float _moveValue_Back;
        [SerializeField] private float _moveValue_Left;

        [Header("Debug")] [SerializeField] private Vector2 _moveValue;
        [SerializeField] private Vector3 _velocity;

        public override void ResetAgent() {
            _episodeIteration = 0;
            base.ResetAgent();
        }

        private void CheckMaxNegativeReward() {
            if (GetCumulativeReward() < _currentTrainingEnvironment.Rewards.MaxNegativeReward) {
                SetReward(_currentTrainingEnvironment.Rewards.MaxNegativeReward);
                if (_isTraining) ProcessEndEpisode(false);
            }
        }

        private void Start() {
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

        #region MLAgents

        public override void Initialize() {
            base.Initialize();

            _visitedTilePercentage = 0f;
            _visitedMazeTileCount = 0;
            _visitedTiles = new List<MazeRoadTile>();
            _neighborTiles = new MazeRoadTile[4];

            _processOnCollideEnter = CollidePunishEnterHeavy;
            _processOnCollideStay = CollideStayPunishHeavy;
        }

        public override void OnEpisodeBegin() {
            transform.SetParent(_currentTrainingEnvironment.transform);
            foreach (var tile in _visitedTiles) {
                tile.ResetTile(_agentIndex);
            }

            _visitedTiles.Clear();

            if (_tileVisitCoroutine != null) StopCoroutine(_tileVisitCoroutine);
            if (_tileStayCoroutine != null) StopCoroutine(_tileStayCoroutine);

            _currentReward = 0f;
            _visitedMazeTileCount = 0;
            _visitedTilePercentage = 0f;
            _mazeTileCount = _currentTrainingEnvironment.MazeTileCount;
            _tileVisitReward = _currentTrainingEnvironment.Rewards.IsConstantRewards
                ? _currentTrainingEnvironment.Rewards.TileVisitReward
                : _currentTrainingEnvironment.Rewards.MaxTileVisitReward / _mazeTileCount;

            _tileVisitThresholdPunish = false;
            _tileStayPunish = false;
            _timeStepPunishment = _currentTrainingEnvironment.Rewards.IsConstantRewards
                ? _currentTrainingEnvironment.Rewards.TimeStepPunishment
                : (_currentTrainingEnvironment.Rewards.MaxNegativeReward / _mazeTileCount) * 0.004f;
        }

        public override void CollectObservations(VectorSensor sensor) {
            // 6 float values passed
            //sensor.AddObservation(transform.localPosition);

            // Forward vector y value is always 0, useless observation
            Vector3 forwardVector = transform.forward;
            sensor.AddObservation(new Vector2(forwardVector.x, forwardVector.z));

            if (_currentTile != null) CalculateDirectionalMoveValues();

            sensor.AddObservation(_visitedTilePercentage);
            sensor.AddObservation(_tileStayPunish);
            sensor.AddObservation(_reachedDeadEnd);
            // Visited tile neighbor observations
            sensor.AddObservation(_moveValue_Forward);
            sensor.AddObservation(_moveValue_Back);
            sensor.AddObservation(_moveValue_Left);
            sensor.AddObservation(_moveValue_Right);
            _currentReward = GetCumulativeReward();
        }

        public override void OnActionReceived(ActionBuffers actions) {
            // Receive 2 continuous actions
            // 0 for x axis
            // 1 for z axis
            // 2 for y rotation
            int moveX = actions.DiscreteActions[0];
            int moveZ = actions.DiscreteActions[1];
            int yRotation = actions.DiscreteActions[2];
            bool slowRotate = yRotation == 1 || yRotation == 3;

            _moveValue = new Vector2(moveX, moveZ);

            // 0 = nothing, 1 = positive axis, 2 = negative axis
            if (moveX == 2) moveX = -1;
            if (moveZ == 2) moveZ = -1;
            // 0 = nothing, 1 slow,2 fast = positive axis, 3 slow,4 fast = negative axis
            if (yRotation == 2) {
                yRotation = 1;
            }
            else if (yRotation == 3 || yRotation == 4) {
                yRotation = -1;
            }

            // Ignore rotation if no input
            if (yRotation != 0) {
                float rotationVector = slowRotate
                    ? yRotation * (_rotationSpeed * 0.5f * Time.fixedDeltaTime)
                    : yRotation * (_rotationSpeed * Time.fixedDeltaTime);
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
            fixedVector *= _moveSpeed;

            // _rigidbody.Move(
            //     transform.position +
            //     (new Vector3(fixedVector.x, 0, fixedVector.z) * (_moveSpeed * Time.fixedDeltaTime)),
            //     _rigidbody.rotation);
            _rigidbody.velocity = new Vector3(fixedVector.x, _rigidbody.velocity.y, fixedVector.z);
            _velocity = _rigidbody.velocity;
            AddReward(_timeStepPunishment);

            CheckMaxNegativeReward();
        }

        public override void Heuristic(in ActionBuffers actionsOut) {
            if (!_isPlayerControlled) return;
            ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
            PlayerControllerInputs actions = _playerInputController.PlayerInputs;

            if (actions.MoveVector.x is > -0.2f and < 0.2f) {
                discreteActions[0] = 0;
            }
            else if (actions.MoveVector.x > 0) {
                discreteActions[0] = 1;
            }
            else {
                discreteActions[0] = 2;
            }

            if (actions.MoveVector.y == 0) {
                discreteActions[1] = 0;
            }
            else if (actions.MoveVector.y > 0) {
                discreteActions[1] = 1;
            }
            else {
                discreteActions[1] = 2;
            }

            if (actions.MouseVector.x is > -0.2f and < 0.2f) {
                discreteActions[2] = 0;
            }
            else if (actions.MouseVector.x > 0) {
                if (actions.MouseVector.x < 0.5f) {
                    discreteActions[2] = 1;
                }
                else {
                    discreteActions[2] = 2;
                }
            }
            else {
                if (actions.MouseVector.x > -0.5f) {
                    discreteActions[2] = 3;
                }
                else {
                    discreteActions[2] = 4;
                }
            }
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
                    float newVisitedTilePercentage = _visitedMazeTileCount / (float)_mazeTileCount;
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

        private void OnTriggerEnter(Collider other) {
            if (other.gameObject.CompareTag("Target")) {
                SetReward(_currentTrainingEnvironment.Rewards.TargetCollisionReward);
                ProcessEndEpisode(true);
            }
        }

        public override void ProcessEndEpisode(bool reachedTarget) {
            if (reachedTarget || !_isTraining) _episodeIteration++;

            StopAllCoroutines();
            EndEpisode();
        }

        private void EnteredNewTile(MazeRoadTile tile) {
            // Already been to this tile and know its a dead
            // Had an unvisited tile that could have been visited instead
            if (tile.WasVisited(_agentIndex) && tile.HasVisitedAllNeighbors(_agentIndex) &&
                _currentTile.HasOtherUnvisitedNeighbors(_agentIndex, tile)) {
                AddReward(-_tileVisitReward * (1 + (Mathf.Log(tile.GetVisitCount(_agentIndex))) * 2));
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

            _currentTile = tile;
            _currentTileTransform = tile.transform;

            MazeRoadTile[] neighbors = _currentTile.GetNeighbors();
            for (int i = 0; i < neighbors.Length; i++) {
                if (neighbors[i] == null) {
                    // No neighbor, can't move to
                    _neighborTiles[i] = null;
                }
                else {
                    _neighborTiles[i] = neighbors[i];
                }
            }

            if (!_currentTile.HasVisitedAllNeighbors(_agentIndex)) {
                // Available paths
                _reachedDeadEnd = false;
            }
            else {
                // No path, must back track
                _reachedDeadEnd = true;
            }

            if (_tileStayCoroutine != null) StopCoroutine(_tileStayCoroutine);
            _tileStayCoroutine = StartCoroutine(TileStayTimer());
        }

        /// <summary>
        /// Logic for player rotation
        /// </summary>
        private void HandlePlayerRotation() {
            PlayerControllerInputs playerControllerInputs =
                _playerInputController.PlayerInputs;
            bool slowRotate = false;
            int moveValue;
            // Dead zone, don't move if the value isn't high enough
            if (playerControllerInputs.MouseVector.y is > -0.2f and < 0.2f) {
                moveValue = 0;
            }
            else if (playerControllerInputs.MouseVector.y > 0) {
                // Slowly rotate the player
                if (playerControllerInputs.MouseVector.y < 0.5f) {
                    slowRotate = true;
                }

                moveValue = 1;
            }
            else {
                if (playerControllerInputs.MouseVector.y > -0.5f) {
                    slowRotate = true;
                }

                moveValue = -1;
            }

            float rotationValue = slowRotate
                ? moveValue * (_rotationSpeed * 0.5f * Time.fixedDeltaTime)
                : moveValue * (_rotationSpeed * Time.fixedDeltaTime);
            Vector3 desiredRotation =
                new Vector3(_headCameraHolder.localRotation.eulerAngles.x + rotationValue, 0, 0);

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

        /// <summary>
        /// Calculates which tiles are around the player relative to the player rotation.
        /// Assigns weights to each tile around the player used in observations.
        /// </summary>
        private void CalculateDirectionalMoveValues() {
            float rotation = transform.localRotation.eulerAngles.y;
            MazeRoadTile forwardTile;
            MazeRoadTile rightTile;
            MazeRoadTile backTile;
            MazeRoadTile leftTile;

            // Forward
            if (rotation is > 315 or <= 45) {
                forwardTile = _neighborTiles[0];
                rightTile = _neighborTiles[1];
                backTile = _neighborTiles[2];
                leftTile = _neighborTiles[3];
            } // Right
            else if (rotation is > 45 and <= 135) {
                forwardTile = _neighborTiles[1];
                rightTile = _neighborTiles[2];
                backTile = _neighborTiles[3];
                leftTile = _neighborTiles[0];
            } // Back 
            else if (rotation is > 135 and <= 180) {
                forwardTile = _neighborTiles[2];
                rightTile = _neighborTiles[3];
                backTile = _neighborTiles[0];
                leftTile = _neighborTiles[1];
            } // Left
            else {
                forwardTile = _neighborTiles[3];
                rightTile = _neighborTiles[0];
                backTile = _neighborTiles[1];
                leftTile = _neighborTiles[2];
            }

            _moveValue_Forward = NormaliseDirection(forwardTile);
            _moveValue_Right = NormaliseDirection(rightTile);
            _moveValue_Back = NormaliseDirection(backTile);
            _moveValue_Left = NormaliseDirection(leftTile);
        }

        /// <summary>
        /// Assigns a value to the tile relative to the players rotation:
        /// </summary>
        /// <param name="tile"></param>
        /// <returns>1= Tile hasn't been visited (Ideal solution).
        /// 0= Tile has been visited.
        /// -1= Tile can't be visited.</returns>
        private float NormaliseDirection(MazeRoadTile tile) {
            if (tile == null) return -1f;
            int value = tile.GetVisitCount(_agentIndex);
            bool hasVisitedAllDirections = _currentTile.HasVisitedAllNeighbors(_agentIndex);

            if (value <= -1) return -1f;
            if (value == 0) return 1f;

            // If all neighbor tiles have been visited, check if any neighbor tiles
            // have more than 1 connection (not a dead end)
            if (hasVisitedAllDirections) {
                int neighborCount = tile.GetNeighborCount();
                if (neighborCount > 1) {
                    value = (neighborCount - 1) / 4;
                    return value;
                }

                return -1;
            }

            if (value > 5) return -1f;

            return -(value / 5f);
        }

        /// <summary>
        /// Starts timer that when finished punishes an agent for staying on the same tile for too long
        /// </summary>
        /// <returns></returns>
        private IEnumerator TileStayTimer() {
            _tileStayPunish = false;
            yield return new WaitForSeconds(_currentTrainingEnvironment.TileStayTimer);
            _tileStayPunish = true;
        }

        /// <summary>
        /// Starts timer that when finished punishes an agent for not visiting a new tile
        /// </summary>
        /// <returns></returns>
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
                if (_isTraining) ProcessEndEpisode(false);
            }
        }

        private void CollidePunishEnterMedium(Collision other) {
            if (other.gameObject.CompareTag("Wall")) {
                AddReward(_currentTrainingEnvironment.Rewards.WallCollisionReward / 10f);
            }
        }

        private void CollidePunishEnterLight(Collision other) {
            if (other.gameObject.CompareTag("Wall")) {
                AddReward(_currentTrainingEnvironment.Rewards.WallCollisionReward / 100f);
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