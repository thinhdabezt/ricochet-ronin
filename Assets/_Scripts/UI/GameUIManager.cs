using System;
using System.Collections.Generic;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject playingHUDPanel;
    [SerializeField] private GameObject upgradeSelectionPanel;
    [SerializeField] private GameObject gameOverPanel;

    private Dictionary<GameState, GameObject> panelMap;
    private GameState currentState = GameState.MainMenu;

    public GameState CurrentState => currentState;

    // Event-driven state changes
    public static event Action<GameState> OnStateChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Note: If you want it to persist across scenes, uncomment DontDestroyOnLoad
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePanels();
    }

    private void InitializePanels()
    {
        panelMap = new Dictionary<GameState, GameObject>
        {
            { GameState.MainMenu, mainMenuPanel },
            { GameState.Playing, playingHUDPanel },
            { GameState.UpgradeSelection, upgradeSelectionPanel },
            { GameState.GameOver, gameOverPanel }
        };
    }

    private void Start()
    {
        // Toggle default starting state
        TransitionToState(GameState.Playing); // Defaulting to Playing for active gameplay
    }

    public void TransitionToState(GameState newState)
    {
        currentState = newState;

        // Toggle panel visibilities based on event-driven approach
        foreach (var pair in panelMap)
        {
            if (pair.Value != null)
            {
                pair.Value.SetActive(pair.Key == newState);
            }
        }

        // Invoke state change event for modular observers (e.g. game over overlay animations)
        OnStateChanged?.Invoke(newState);
    }
}
