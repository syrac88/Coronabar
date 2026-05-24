using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using Hashtable = ExitGames.Client.Photon.Hashtable;

/// <summary>
/// Verwalten und Anzeigen aller Spieler im GameRoom.
/// Punkte werden mit Player Custom Properties synchronisiert.
/// </summary>
public class GameRoomManager : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    public Transform playerListParent;             // UI-Container mit Horizontal Layout Group für die PlayerFrames
    public GameObject playerFramePrefab;           // Prefab für die Spieler-Kachel (mit Namen, Punkten etc.)
    public GameObject aufgabenfeldPrefab;     // Dein Aufgabenfeld Prefab zum Instanziieren
    public TMP_Text textBarName;            // Barname - Schild
    public Sprite[] charakterSprites;
    GameObject aufgabenfeldInstance = null;

    //VIP
    [Header("VIP UI")]
    public GameObject vipPanel;           // VIP Panel GameObject (Inspector zuteilen)
    public Image vipCharacterImage;       // Image für Charakterbild im VIP Panel
    public TMP_Text vipNameText;          // Textfeld für Gewinnername VIP

    //Minigamestarten
    [Header("Minispiele (Namen der Prefabs im Resources/PhotonPrefabs Ordner)")]
    public string[] minigamePrefabs = new string[] {
        "Minispiel01_PrefabRoot",
        "Minispiel02_PrefabRoot",
        "Minispiel03_PrefabRoot",
        "Minispiel04_PrefabRoot",
        "Minispiel05_PrefabRoot",
        "Minispiel06_PrefabRoot"
    };

    private int minigameIndex = 0; // Wir starten jetzt logisch bei Index 0

    // Verknüpft jeden Spieler (über dessen ActorNumber) mit seinem PlayerFrame im UI
    private Dictionary<int, GameObject> playerFrames = new Dictionary<int, GameObject>();

    // NEU FÜR ARCADE MODUS
    public GameObject masterMinigameButton; 
    public bool isMinigameMode = false;

    /// <summary>
    /// Initialisiert alle aktuell anwesenden Spieler und verknüpft den Glas-Button.
    /// </summary>
    void Start()
    {
        textBarName.text = PhotonNetwork.CurrentRoom.Name;

        // Canvas finden
        var canvas = FindAnyObjectByType<Canvas>();
        
        // Modus aus Property lesen
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("MinigameMode", out object mode))
            isMinigameMode = (bool)mode;

        // Aufgabenfeld oder Button init
        if (!isMinigameMode)
        {
            // --- NORMALER MODUS ---
            if (canvas != null && aufgabenfeldPrefab != null)
                aufgabenfeldInstance = Instantiate(aufgabenfeldPrefab, canvas.transform);
            
            // WICHTIG: Hier den Button explizit ausschalten, falls er im Inspector an war
            if (masterMinigameButton != null)
                masterMinigameButton.SetActive(false);
        }
        else
        {
            // --- ARCADE MODUS ---
            if (masterMinigameButton != null)
                masterMinigameButton.SetActive(PhotonNetwork.IsMasterClient);
        }

        // 
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
        {
            Hashtable startProps = new Hashtable
            {
                { "TaskOwner", PhotonNetwork.PlayerList[0].ActorNumber },
                { "TaskIndex", -1 },
                { "TaskStatus", "waiting" }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(startProps);
        }

        // Restlich PlayerFrames initialisieren
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            AddPlayerFrame(player);
            UpdatePointsUI(player.ActorNumber, GetPlayerPoints(player));
        }

    }

    [PunRPC]
    public void SetAufgabenfeldVisible(bool visible)
    {
        if (aufgabenfeldInstance != null)
            aufgabenfeldInstance.SetActive(visible);
        else
        {
            // Fallback für "lost reference" – per Name suchen!
            var go = GameObject.Find("Aufgabenfeld(Clone)");
            if (go != null) go.SetActive(visible);
        }
    }

    public void AddRoundPointsToTotalForAll()
    {
        photonView.RPC(nameof(AddRoundPointsToTotalForAllRPC), RpcTarget.All);
    }

    [PunRPC]
    public void AddRoundPointsToTotalForAllRPC()
    {
        // Lokalen Spieler nehmen
        Player localPlayer = PhotonNetwork.LocalPlayer;

        int roundPoints = GetPlayerPoints(localPlayer);
        int totalPoints = GetPlayerTotalPoints(localPlayer);

        // Zur Gesamtsumme addieren
        int newTotal = totalPoints + roundPoints;

        // In die PlayerCustomProperties schreiben
        Hashtable props = new Hashtable
        {
            { "TotalPoints", newTotal },
            { "Points", 0 }
        };
        localPlayer.SetCustomProperties(props);

        // Optional: UI sofort updaten
        UpdateTotalPointsUI(localPlayer.ActorNumber, newTotal);
        UpdatePointsUI(localPlayer.ActorNumber, 0);
    }

    private int GetPlayerTotalPoints(Player player)
    {
        if (player.CustomProperties.TryGetValue("TotalPoints", out object totalPointsObj))
        {
            return (int)totalPointsObj;
        }
        return 0;
    }

    // UpdateTotalPointsUI analog zu UpdatePointsUI – siehe unten!
    private void UpdateTotalPointsUI(int actorNumber, int points)
    {
        if (playerFrames.TryGetValue(actorNumber, out GameObject frame))
        {
            var pointsText = frame.transform.Find("Text_PointsAll")?.GetComponent<TMP_Text>();
            if (pointsText != null)
                pointsText.text = points.ToString();
        }
    }


    /// <summary>
    /// Wird aufgerufen, wenn ein neuer Spieler dem Raum beitritt.
    /// </summary>
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        AddPlayerFrame(newPlayer);
        UpdatePointsUI(newPlayer.ActorNumber, GetPlayerPoints(newPlayer));
    }

    /// <summary>
    /// Wird aufgerufen, wenn ein Spieler den Raum verlässt.
    /// Entfernt dessen PlayerFrame.
    /// </summary>
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (playerFrames.TryGetValue(otherPlayer.ActorNumber, out GameObject frame))
        {
            Destroy(frame);
            playerFrames.Remove(otherPlayer.ActorNumber);
        }
    }

    /// <summary>
    /// Erstellt ein PlayerFrame-UI und setzt Spielername.
    /// </summary>
    /// <param name="player">Photon Player Objekt</param>
    private void AddPlayerFrame(Player player)
    {
        if (playerFrames.ContainsKey(player.ActorNumber))
            return; // Vermeide Doppel-Frames

        GameObject frame = Instantiate(playerFramePrefab, playerListParent);
        playerFrames[player.ActorNumber] = frame;

        // Zugriff auf das PlayerFrameBehaviour-Script im Prefab
        var frameBehaviour = frame.GetComponent<PlayerFrameBehaviour>();
        if (frameBehaviour != null)
        {
            // Setze ActorNumber für Erkennung durch PlayerFrameBehaviour (wichtig für lokalen Spieler)
            frameBehaviour.actorNumber = player.ActorNumber;

            // Name
            if (frameBehaviour.nameText != null)
                frameBehaviour.nameText.text = player.NickName;

            // Punktezähler
            if (frameBehaviour.pointsText != null)
                frameBehaviour.pointsText.text = "0";

            // Charakterbild
            if (player.CustomProperties.TryGetValue("CharakterID", out object charIDObj))
            {
                int charID = (int)charIDObj;
                if (frameBehaviour.imagePlayer != null && charakterSprites != null && charID >= 0 && charID < charakterSprites.Length)
                {
                    frameBehaviour.imagePlayer.sprite = charakterSprites[charID];
                }
            }
        }
        ArrangePlayerFrames();
    }


    /// <summary>
    /// Holt die Punkte eines Spielers aus dessen Custom Properties.
    /// Wenn nicht vorhanden, 0 zurück.
    /// </summary>
    /// <param name="player">Der Spieler</param>
    /// <returns>Punktezahl</returns>
    private int GetPlayerPoints(Player player)
    {
        if (player.CustomProperties.TryGetValue("Points", out object pointsObj))
        {
            return (int)pointsObj;
        }
        return 0;
    }

    /// <summary>
    /// Wird aufgerufen, wenn sich eine Spieler-Eigenschaft ändert.
    /// Hier wird die Punktestand-Anzeige für den Spieler aktualisiert.
    /// </summary>
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("Points"))
        {
            int newPoints = (int)changedProps["Points"];
            UpdatePointsUI(targetPlayer.ActorNumber, newPoints);
        }
        if (changedProps.ContainsKey("TotalPoints"))
        {
            int total = (int)changedProps["TotalPoints"];
            UpdateTotalPointsUI(targetPlayer.ActorNumber, total);
        }
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable startProps = new Hashtable
            {
                { "TaskOwner", PhotonNetwork.PlayerList[0].ActorNumber },
                { "TaskIndex", -1 },
                { "TaskStatus", "waiting" }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(startProps);
        }
    }

    // NEU: Master startet Minispiel
    public void MasterStartsNextMinigame()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        int gameId = (minigameIndex % minigamePrefabs.Length) + 1;
        minigameIndex++;
        StartMinigame(gameId);
        if(masterMinigameButton) masterMinigameButton.SetActive(false);
    }


    /// <summary>
    /// Aktualisiert das UI-Feld "Text_PointsRound" im PlayerFrame für den Spieler.
    /// </summary>
    private void UpdatePointsUI(int actorNumber, int points)
    {
        if (playerFrames.TryGetValue(actorNumber, out GameObject frame))
        {
            var pointsText = frame.transform.Find("Text_PointsRound")?.GetComponent<TMP_Text>();
            if (pointsText != null)
                pointsText.text = points.ToString();
        }
    }

    public AufgabenDatenbank aufgabenDatenbank;

    public Aufgabe HoleZufaelligeAufgabe()
    {
        if (aufgabenDatenbank == null || aufgabenDatenbank.aufgabenListe.Count == 0)
            return null;

        int index = Random.Range(0, aufgabenDatenbank.aufgabenListe.Count);
        return aufgabenDatenbank.aufgabenListe[index];
    }

    public void ArrangePlayerFrames()
    {
        float spacing = 250f; // Abstand zwischen den PlayerFrames
        int count = playerListParent.childCount;

        for (int i = 0; i < count; i++)
        {
            RectTransform rt = playerListParent.GetChild(i).GetComponent<RectTransform>();

            if (i == 0)
            {
                rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y); // Mitte
                continue;
            }

            int magnitude = (i + 1) / 2;
            int direction = (i % 2 == 1) ? 1 : -1; // ungerade = rechts, gerade = links

            float x = direction * magnitude * spacing;
            rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
        }
    }

    public void BeendeSpiel()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ToggleAudio()
    {
        if (Music.Instance == null) return;
        if (Music.Instance.isPlaying) Music.Instance.PauseMusic(); else Music.Instance.ResumeMusic();
    }

    public void StartMinigame(int index)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        int actualIndex = index - 1;
        if (actualIndex < 0 || actualIndex >= minigamePrefabs.Length) return;
        photonView.RPC(nameof(SetAufgabenfeldVisible), RpcTarget.All, false);
        string prefabName = $"PhotonPrefabs/{minigamePrefabs[actualIndex]}";
        var go = PhotonNetwork.Instantiate(prefabName, Vector3.zero, Quaternion.identity);
        var mainCanvas = GameObject.Find("Canvas");
        if (mainCanvas != null)
        {
            go.transform.SetParent(mainCanvas.transform, false);
            var minigame = go.GetComponent<MinigameBase>();
            if (minigame != null) minigame.TriggerMinigameStart();
        }
    }

    [PunRPC]
    public void NotifyWinnerToGameManager(int winnerActorId)
    {
        Player winner = PhotonNetwork.CurrentRoom.GetPlayer(winnerActorId);
        if (winner == null) return;
        vipPanel.SetActive(true);
        vipNameText.text = winner.NickName;
        if (winner.CustomProperties.TryGetValue("CharakterID", out object charIdObj) && charakterSprites != null)
            vipCharacterImage.sprite = charakterSprites[(int)charIdObj];
    }

    public void ResetTaskStatusForMinigame()
    {
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { "TaskOwner", -1 }, { "TaskIndex", -1 }, { "TaskStatus", "waiting" } });
    }

    public void StartMinigameAfterReset()
    {
        ResetTaskStatusForMinigame();
        if (PhotonNetwork.IsMasterClient)
        {
            StartMinigame(minigameIndex + 1);
            minigameIndex = (minigameIndex + 1) % minigamePrefabs.Length;
        }
    }

    public void AssignNextTaskOwner() => StartCoroutine(AssignNextOwnerDelayed());

    private System.Collections.IEnumerator AssignNextOwnerDelayed()
    {
        yield return null; 
        Player[] players = PhotonNetwork.PlayerList;
        if (players.Length == 0) yield break;
        Hashtable props = PhotonNetwork.CurrentRoom.CustomProperties;
        int currentOwner = props.ContainsKey("TaskOwner") ? (int)props["TaskOwner"] : -1;
        int ownerIndex = System.Array.FindIndex(players, p => p.ActorNumber == currentOwner);
        int nextOwner = players[(ownerIndex + 1) % players.Length].ActorNumber;
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { "TaskOwner", nextOwner }, { "TaskIndex", -1 }, { "TaskStatus", "waiting" } });
    }

    [PunRPC]
    public void AddArcadeWinPoint(int winnerActorId)
    {
        // Nur der Gewinner selbst (oder alle) erhöht seinen Total-Wert
        if (PhotonNetwork.LocalPlayer.ActorNumber == winnerActorId)
        {
            int currentTotal = GetPlayerTotalPoints(PhotonNetwork.LocalPlayer);
            int newTotal = currentTotal + 1; // 1 Punkt für den Sieg

            Hashtable props = new Hashtable { { "TotalPoints", newTotal } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            
            // UI sofort aktualisieren
            UpdateTotalPointsUI(PhotonNetwork.LocalPlayer.ActorNumber, newTotal);
        }
    }
}