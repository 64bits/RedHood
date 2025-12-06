using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class GameOverUIController : MonoBehaviour
{
    [SerializeField] private GameObject gameOverUI;

    private void OnEnable()
    {
        // Subscribe to state changes
        GameStateManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe from state changes
        GameStateManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void Start()
    {
        // Initialize UI state
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(false);
        }
    }

    private void OnGameStateChanged(GameStateManager.GameState newState)
    {
        if (gameOverUI == null) return;

        // Activate UI only when in GameOver state
        gameOverUI.SetActive(newState == GameStateManager.GameState.GameOver);
    }

    // Public method to restart game (call from UI button)
    public void RestartGame()
    {
        GameStateManager.Instance.SetActive();
        // Add your scene reload logic here if needed
        // UnityEngine.SceneManagement.SceneManager.LoadScene(
        //     UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    // Public method to quit game (call from UI button)
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}