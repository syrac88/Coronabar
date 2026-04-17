using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.InputSystem;

public class DebugMinigameStarter : MonoBehaviour
{
    public TMP_Dropdown gameDropdown;
    public GameObject debugPanel;
    
    private GameRoomManager manager;

    void Start()
    {
        manager = FindAnyObjectByType<GameRoomManager>();
        SetupDropdown();
        debugPanel.SetActive(false);
    }

    void Update()
    {
        // 'M' zum Öffnen/Schließen
        if (Keyboard.current.mKey.wasPressedThisFrame && (Debug.isDebugBuild || Application.isEditor))
        {
            if (debugPanel != null)
                debugPanel.SetActive(!debugPanel.activeSelf);
        }
    }

    void SetupDropdown()
    {
        if (manager == null || gameDropdown == null) return;

        gameDropdown.ClearOptions();
        List<string> options = new List<string>();

        // Zieht sich automatisch die Anzahl der Spiele aus dem Manager!
        for (int i = 0; i < manager.minigamePrefabs.Length; i++)
        {
            options.Add($"Minispiel {(i + 1):D2} ({manager.minigamePrefabs[i]})");
        }

        gameDropdown.AddOptions(options);
    }

    public void OnStartButtonClicked()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogError("Nur der MasterClient darf das Minispiel forcen!");
            return;
        }

        if (manager != null)
        {
            int selectedIndex = gameDropdown.value + 1;
            manager.StartMinigame(selectedIndex);
        }
        
        debugPanel.SetActive(false);
    }
}