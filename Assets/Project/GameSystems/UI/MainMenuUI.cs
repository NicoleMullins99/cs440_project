using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.GameSystems.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        private void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void OpenPlayerScene()
        {
            SceneManager.LoadScene("MazeNav_Player");
        }
        
        public void OpenPlayerVsAgentScene()
        {
            SceneManager.LoadScene("MazeNav_PlayerVAgent");
        }

        public void OpenWayFindingTest() {
            SceneManager.LoadScene("WayfindingTest");
        }
        
        public void OpenReplayScene() {
            SceneManager.LoadScene("ReplayScene");
        }
        
        public void ExitGame()
        {
            Application.Quit();
        }
    }
}
