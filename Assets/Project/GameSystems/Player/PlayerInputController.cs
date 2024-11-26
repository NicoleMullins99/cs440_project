using UnityEngine;

namespace Project.GameSystems.Player {
    public class PlayerInputController : MonoBehaviour {
        private PlayerInputActions _playerInputActions;
        private PlayerControllerInputs _frameInputs;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private bool _enableControls;

        public PlayerControllerInputs PlayerInputs => _frameInputs;

        private void Awake() {
            _playerInputActions = new PlayerInputActions();
            if (_enableControls) {
                _playerInputActions.Enable();
            }
        }

        private void Update() {
            _frameInputs = new PlayerControllerInputs();
            if (!_enableControls) return;
            HandleMovementInput(ref _frameInputs);
            //_playerController.SetInputs(_frameInputs);
        }

        private void HandleMovementInput(ref PlayerControllerInputs inputs) {
            inputs.MoveVector = _playerInputActions.Movement.Move.ReadValue<Vector2>();
            inputs.MouseVector = _playerInputActions.Movement.Point.ReadValue<Vector2>();
        }

        public void SetCursorState(bool state, CursorLockMode cursorState) {
            Cursor.lockState = cursorState;
            Cursor.visible = state;
        }

        public void SetControlsActive(bool state) {
            _enableControls = state;
        }
    }

    public struct PlayerControllerInputs {
        public Vector2 MoveVector;
        public Vector2 MouseVector;
    }
}