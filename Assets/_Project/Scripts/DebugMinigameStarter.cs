using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.InputSystem;

public class DebugMinigameStarter : MonoBehaviour
{
    public TMP_Dropdown gameDropdown;
    public GameObject debugPanel;
    public int maxMinigames = 4; // Hier einfach die Anzahl deiner Spiele eintragen

    void Start()
    {
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
        gameDropdown.ClearOptions();
        List<string> options = new List<string>();

        for (int i = 1; i <= maxMinigames; i++)
        {
            options.Add("Minispiel " + i.ToString("D2"));
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

        // +1 weil Dropdown bei 0 startet, unsere Spiele aber bei 1
        int selectedIndex = gameDropdown.value + 1;
        
        GameRoomManager manager = FindAnyObjectByType<GameRoomManager>();
        if (manager != null)
        {
            manager.StartMinigame(selectedIndex);
        }
        
        debugPanel.SetActive(false);
    }
}