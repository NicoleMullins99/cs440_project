using Cinemachine;
using UnityEngine;

namespace Project.GameSystems.Player {
    public class StandardPlayer : MonoBehaviour {
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private CinemachineVirtualCamera _virtualCamera;

        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 5f;

        [Header("Player Control Components")] [SerializeField]
        private Transform _headCameraHolder;

        [SerializeField] private float _minEulerAngle = -80f;
        [SerializeField] private float _maxEulerAngle = 60f;

        private PlayerInputController _playerInputController;

        private int[] _discreteActions = new int[3];
        private Vector2 _moveValue;

        public void SetInputs(PlayerInputController inputs) {
            _playerInputController = inputs;
        }

        public void LookAt(Vector3 relativeDifference) {
            _virtualCamera.transform.localRotation = Quaternion.LookRotation(relativeDifference, new Vector3(0, 1, 0));
        }

        private void FixedUpdate() {
            ProcessInputs();
            ProcessActions();
        }

        public void ProcessActions() {
            // Receive 2 continuous actions
            // 0 for x axis
            // 1 for z axis
            // 2 for y rotation
            int moveX = _discreteActions[0];
            int moveZ = _discreteActions[1];
            int yRotation = _discreteActions[2];
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
            HandlePlayerRotation();


            Vector3 fixedVector = transform.right * moveX;
            fixedVector += transform.forward * moveZ;
            fixedVector *= _moveSpeed;

            // _rigidbody.Move(
            //     transform.position +
            //     (new Vector3(fixedVector.x, 0, fixedVector.z) * (_moveSpeed * Time.fixedDeltaTime)),
            //     _rigidbody.rotation);
            _rigidbody.velocity = new Vector3(fixedVector.x, _rigidbody.velocity.y, fixedVector.z);
        }

        public void ProcessInputs() {
            if (_playerInputController.PlayerInputs.MoveVector.x is > -0.2f and < 0.2f) {
                _discreteActions[0] = 0;
            }
            else if (_playerInputController.PlayerInputs.MoveVector.x > 0) {
                _discreteActions[0] = 1;
            }
            else {
                _discreteActions[0] = 2;
            }

            if (_playerInputController.PlayerInputs.MoveVector.y == 0) {
                _discreteActions[1] = 0;
            }
            else if (_playerInputController.PlayerInputs.MoveVector.y > 0) {
                _discreteActions[1] = 1;
            }
            else {
                _discreteActions[1] = 2;
            }

            if (_playerInputController.PlayerInputs.MouseVector.x is > -0.2f and < 0.2f) {
                _discreteActions[2] = 0;
            }
            else if (_playerInputController.PlayerInputs.MouseVector.x > 0) {
                if (_playerInputController.PlayerInputs.MouseVector.x < 0.5f) {
                    _discreteActions[2] = 1;
                }
                else {
                    _discreteActions[2] = 2;
                }
            }
            else {
                if (_playerInputController.PlayerInputs.MouseVector.x > -0.5f) {
                    _discreteActions[2] = 3;
                }
                else {
                    _discreteActions[2] = 4;
                }
            }
        }


        /// <summary>
        /// Logic for player rotation
        /// </summary>
        private void HandlePlayerRotation() {
            bool slowRotate = false;
            int moveValue;
            // Dead zone, don't move if the value isn't high enough
            if (_playerInputController.PlayerInputs.MouseVector.y is > -0.2f and < 0.2f) {
                moveValue = 0;
            }
            else if (_playerInputController.PlayerInputs.MouseVector.y > 0) {
                // Slowly rotate the player
                if (_playerInputController.PlayerInputs.MouseVector.y < 0.5f) {
                    slowRotate = true;
                }

                moveValue = 1;
            }
            else {
                if (_playerInputController.PlayerInputs.MouseVector.y > -0.5f) {
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
    }
}