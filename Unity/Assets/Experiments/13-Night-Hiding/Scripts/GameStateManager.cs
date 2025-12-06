using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class GameStateManager : MonoBehaviour
{
    public enum GameState
    {
        Active,
        Paused,
        GameOver
    }

    // Singleton instance
    private static GameStateManager _instance;
    public static GameStateManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameStateManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameStateManager");
                    _instance = go.AddComponent<GameStateManager>();
                }
            }
            return _instance;
        }
    }

    // Static action for state change notifications
    public static event Action<GameState> OnGameStateChanged;

    [SerializeField] private PlayerInput playerInput;
    private GameState _currentState = GameState.Active;

    public GameState CurrentState
    {
        get => _currentState;
        private set
        {
            if (_currentState != value)
            {
                _currentState = value;
                OnGameStateChanged?.Invoke(_currentState);
                UpdateInputActionMap();
            }
        }
    }

    private void Awake()
    {
        // Singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Find PlayerInput if not assigned
        if (playerInput == null)
        {
            playerInput = FindObjectOfType<PlayerInput>();
        }
    }

    private void UpdateInputActionMap()
    {
        if (playerInput == null) return;

        switch (CurrentState)
        {
            case GameState.Active:
                playerInput.SwitchCurrentActionMap("Player");
                Time.timeScale = 1f;
                break;
            case GameState.Paused:
            case GameState.GameOver:
                playerInput.SwitchCurrentActionMap("UI");
                Time.timeScale = 0f;
                break;
        }
    }

    // Public methods to change state
    public void SetGameState(GameState newState)
    {
        CurrentState = newState;
    }

    public void SetActive()
    {
        CurrentState = GameState.Active;
    }

    public void SetPaused()
    {
        CurrentState = GameState.Paused;
    }

    public void SetGameOver()
    {
        CurrentState = GameState.GameOver;
    }
}