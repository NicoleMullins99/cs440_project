using System;
using UnityEngine;

namespace Project.GameSystems.Player {
    [Obsolete("Old player controller, not in use")]
    public class PlayerController : MonoBehaviour {
        private PlayerControllerInputs _inputs;
        private MoveState _currentMoveState;
        
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Transform _headCameraHolder;

        [Header("Move Parameters")]
        [SerializeField] private float _moveSpeed = 5f;

        [Header("Rotation Parameters")] [SerializeField]
        private float _rotationSpeed = 5f;
        [SerializeField] private float _minEulerAngle = -80f;
        [SerializeField] private float _maxEulerAngle = 60f;
        
        public void SetInputs(PlayerControllerInputs frameInputs) {
            _inputs = frameInputs;
            UpdateMoveState();
        }

        private void FixedUpdate() {
            ProcessMovementUpdates();
            ProcessRotationUpdates();
        }
        
        private void UpdateMoveState() {
            // No movement
            if (_inputs.MoveVector.magnitude == 0) {
                _currentMoveState = MoveState.Idle;
                return;
            }

            _currentMoveState = MoveState.Moving;
        }
        
        private void ProcessMovementUpdates() {
            Vector2 correctedMoveInput = _inputs.MoveVector * (_moveSpeed * Time.fixedDeltaTime);

            Vector3 currentPosition = _rigidbody.position;
            // Account for rotation
            Vector3 fixedPos = currentPosition + transform.right * correctedMoveInput.x;
            fixedPos += transform.forward * correctedMoveInput.y;

            Vector3 newPosition = new Vector3(fixedPos.x, currentPosition.y, fixedPos.z);
            _rigidbody.MovePosition(newPosition);
        }
        
        private void ProcessRotationUpdates() {
            if (_inputs.MouseVector == Vector2.zero) return;

            Vector2 rotationVector = _inputs.MouseVector * (_rotationSpeed * Time.fixedDeltaTime);
            Quaternion newRotation = _rigidbody.rotation *
                                     Quaternion.Euler(Vector3.up * Mathf.Clamp(rotationVector.x, -90f, 90f));
            // Set z rotation to 0
            newRotation = Quaternion.Euler(new Vector3(0, newRotation.eulerAngles.y, 0));
            _rigidbody.MoveRotation(newRotation);

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
    }
    
    public enum MoveState {
        Idle,
        Moving
    }
}
