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
    public Button glassButton;                      // Button/Image für das Glas (zentral auf dem Bildschirm)
    public GameObject aufgabenfeldPrefab;     // Dein Aufgabenfeld Prefab zum Instanziieren


    // Verknüpft jeden Spieler (über dessen ActorNumber) mit seinem PlayerFrame im UI
    private Dictionary<int, GameObject> playerFrames = new Dictionary<int, GameObject>();

    /// <summary>
    /// Initialisiert alle aktuell anwesenden Spieler und verknüpft den Glas-Button.
    /// </summary>
    void Start()
    {
        // Canvas finden – automatisch oder über eine UI-"Tag"-Logik
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null && aufgabenfeldPrefab != null)
        {
            Instantiate(aufgabenfeldPrefab, canvas.transform);
        }
        else
        {
            Debug.LogError("Canvas oder Aufgabenfeld-Prefab fehlt!");
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

        if (glassButton != null)
            glassButton.onClick.AddListener(OnGlassClicked);
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

        // Name eintragen
        var nameText = frame.transform.Find("Text_Name")?.GetComponent<TMP_Text>();
        if (nameText != null)
            nameText.text = player.NickName;

        // Punktefeld initialisieren mit 0 - wird später via UpdatePointsUI aktualisiert
        var pointsText = frame.transform.Find("Text_PointsRound")?.GetComponent<TMP_Text>();
        if (pointsText != null)
            pointsText.text = "0";

        playerFrames[player.ActorNumber] = frame;
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
    /// Wird beim Klick auf das Glas ausgeführt.
    /// Erhöht den Punktestand des lokalen Spielers um 1.
    /// </summary>
    public void OnGlassClicked()
    {
        Player localPlayer = PhotonNetwork.LocalPlayer;

        int currentPoints = GetPlayerPoints(localPlayer);
        int newPoints = currentPoints + 1;

        // CustomProperty mit neuem Punktestand setzen, wird automatisch synchronisiert
        Hashtable props = new Hashtable { { "Points", newPoints } };
        localPlayer.SetCustomProperties(props);
    }

    /// <summary>
    /// Wird aufgerufen, wenn sich eine Spieler-Eigenschaft ändert.
    /// Hier wird die Punktestand-Anzeige für den Spieler aktualisiert.
    /// </summary>
    /// <param name="targetPlayer">Spieler dessen Eigenschaften geändert wurden</param>
    /// <param name="changedProps">Geänderte Eigenschaften</param>
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

            Debug.Log("Initial Room Properties gesetzt vom MasterClient.");
        }
    }


    /// <summary>
    /// Aktualisiert das UI-Feld "Text_PointsRound" im PlayerFrame für den Spieler.
    /// </summary>
    /// <param name="actorNumber">ActorNumber des Spielers</param>
    /// <param name="points">Neuer Punktestand</param>
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

}
