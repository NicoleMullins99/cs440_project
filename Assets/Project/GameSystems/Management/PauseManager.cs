using Project.GameSystems.Player;
using UnityEngine;

namespace Project.GameSystems.Management {
    public class PauseManager : MonoBehaviour {
        private PauseAction _action;

        public bool IsPaused => _isPaused;
        [SerializeField] private PlayerInputController _playerInputController;
        [SerializeField] private GameObject _pauseMenu;
        [SerializeField] private GameObject _gameMenu;

        private bool _isPaused;

        private void Awake() {
            _action = new PauseAction();
            _isPaused = false;
        }

        private void OnEnable() {
            _action.Enable();
        }

        private void OnDisable() {
            _action.Disable();
        }

        private void Start() {
            // Subscribe the pause button to DeterminePause() method
            _action.Pause.PauseGame.performed += _ => DeterminePause();
        }

        private void DeterminePause() {
            if (_isPaused) {
                ResumeGame();
            }
            else {
                PauseGame();
            }
        }

        public void PauseGame(bool controlState = false) {
            // Player vs Menu controls
            if (!controlState) {
                _playerInputController.SetControlsActive(false);
                _playerInputController.SetCursorState(true, CursorLockMode.None);
            }
            else {
                _playerInputController.SetControlsActive(true);
                _playerInputController.SetCursorState(false, CursorLockMode.Locked);
            }

            // Freeze game physics
            Time.timeScale = 0;
            _pauseMenu.SetActive(true);
            _gameMenu.SetActive(false);
            _isPaused = true;
        }

        public void ResumeGame(bool controlState = true) {
            // Player vs Menu controls
            if (!controlState) {
                _playerInputController.SetControlsActive(false);
                _playerInputController.SetCursorState(true, CursorLockMode.None);
            }
            else {
                _playerInputController.SetControlsActive(true);
                _playerInputController.SetCursorState(false, CursorLockMode.Locked);
            }

            // Resume game physics
            Time.timeScale = 1;
            _pauseMenu.SetActive(false);
            _gameMenu.SetActive(true);
            _isPaused = false;
        }
    }
}