using System.Collections.Generic;
using Project.GameSystems.ML_Agents.Training;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Project.GameSystems.ML_Agents.Basic_Maze_Agent {
    public class SimpleMazeAgent : Agent {
        [SerializeField] private Transform _targetTransform;
        [SerializeField] private Rigidbody _rigidbody;

        [SerializeField] private Transform _agentStartLocalPosition;
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private LayerMask _roadLayer;
        [SerializeField] private List<Transform> _exitSpawnPoints;

        private readonly List<GameObject> _visitedRewardPoints = new List<GameObject>();
        private readonly List<GameObject> _visitedRoadSegments = new List<GameObject>();

        [SerializeField] private GameObject _currentRoadSegment;
        [SerializeField] private float _currentReward;

        [Header("Training Phase")] [SerializeField]
        private bool _isPhase1;
        
        private Transform _lastExit;

        private float _wallCollisionFrameCount;
        private float _distToExit;
        private bool _collidedWithWall;
        private bool _reachedTarget;

        public override void CollectObservations(VectorSensor sensor) {
            // 6 float values passed
            sensor.AddObservation(transform.localPosition);
            sensor.AddObservation(_targetTransform.localPosition);
            sensor.AddObservation(_distToExit);
            _currentReward = GetCumulativeReward();
        }
        public override void OnActionReceived(ActionBuffers actions) {
            // Receive 2 continuous actions
            // 1 for x axis
            // 1 for z axis
            int moveX = actions.DiscreteActions[0];
            int moveZ = actions.DiscreteActions[1];
            
            // 0 = nothing, 1 = positive axis, 2 = negative axis
            if (moveX == 2) moveX = -1;
            if (moveZ == 2) moveZ = -1;
            
            _collidedWithWall = false;

            float newDist = Vector3.Distance(transform.localPosition, _targetTransform.localPosition);
            if (newDist < _distToExit) {
                AddReward(0.1f);
            }
            else {
                AddReward(-0.1f);
            }

            _distToExit = newDist;

            _rigidbody.Move(transform.position + (new Vector3(moveX, 0, moveZ) * _moveSpeed * Time.deltaTime),
                Quaternion.identity);
        }

        public override void OnEpisodeBegin() {
            base.OnEpisodeBegin();

            if (_isPhase1) {
                _agentStartLocalPosition.localPosition = new Vector3(Random.Range(-22.5f, 22.5f), 0.5f, Random.Range(-22.5f, 22.5f));
                _targetTransform.localPosition = new Vector3(Random.Range(-22.5f, 22.5f), 0.5f, Random.Range(-22.5f, 22.5f));
            }
            else {
                foreach (var visitedReward in _visitedRewardPoints) {
                    visitedReward.SetActive(true);
                }

                if (_lastExit == null) {
                    _lastExit = _exitSpawnPoints[Random.Range(0, _exitSpawnPoints.Count)];
                }

                if (_reachedTarget) {
                    for (int i = 0; i < 10; i++) {
                        Transform newExitTransform = _exitSpawnPoints[Random.Range(0, _exitSpawnPoints.Count)];
                        if (newExitTransform != _lastExit) {
                            _lastExit = newExitTransform;
                            break;
                        }
                    }

                    _targetTransform.position = _lastExit.position;
                }
                else {
                    _targetTransform.position = _lastExit.position;
                }
            }
            

            _reachedTarget = false;
            _collidedWithWall = false;
            _distToExit = Vector3.Distance(transform.localPosition, _targetTransform.localPosition);
            _currentReward = 0f;
            _currentRoadSegment = null;
            _visitedRewardPoints.Clear();
            _visitedRoadSegments.Clear();
            transform.localPosition = _agentStartLocalPosition.localPosition;
            _rigidbody.Move(transform.position, Quaternion.identity);
        }

        public void OnCollisionEnter(Collision other) {
            if (other.gameObject.CompareTag("Wall")) {
                AddReward(-10f);
                EndEpisode();
                if (!_collidedWithWall) _wallCollisionFrameCount = 0f;
                _collidedWithWall = true;
            }
            else if (other.gameObject.CompareTag("Target")) {
                AddReward(50f);
                _reachedTarget = true;
                EndEpisode();
            }
            else if (other.gameObject.CompareTag("Road")) {
                if (_currentRoadSegment == null) {
                    _currentRoadSegment = other.gameObject;
                }
                else if (_currentRoadSegment != null && other.gameObject != _currentRoadSegment) {
                    _currentRoadSegment = other.gameObject;
                }

                if (!_visitedRoadSegments.Contains(other.gameObject)) {
                    _visitedRoadSegments.Add(other.gameObject);
                    AddReward(1f);
                }
                else {
                    AddReward(-0.1f);
                }
            }
        }

        public void OnCollisionStay(Collision other) {
            if (_isPhase1) return;
            if (other.gameObject.CompareTag("Wall")) {
                AddReward(-0.01f);
                _wallCollisionFrameCount++;
                _collidedWithWall = true;

                if (_wallCollisionFrameCount > 200) {
                    AddReward(-10f);
                    EndEpisode();
                }
            }
            else if (other.gameObject == _currentRoadSegment) {
                AddReward(-0.01f);
            }
        }

        private void OnTriggerEnter(Collider other) {
            if (other.gameObject.TryGetComponent<TrainingReward>(out TrainingReward trainingReward) &&
                !_visitedRewardPoints.Contains(other.gameObject)) {
                AddReward(trainingReward.Reward);
                _visitedRewardPoints.Add(other.gameObject);
                other.gameObject.SetActive(false);
            }
        }
    }
}