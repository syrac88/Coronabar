using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using ExitGames.Client.Photon;


public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elemente")]
    public TMP_InputField inputRoomName;       // Eingabefeld für den Namen des Raums (Bar-Name)
    public TMP_InputField inputPlayerName;     // Eingabefeld für den Spielernamen
    public Button buttonEnterRoom;              // Button zum Betreten oder Erstellen des Raums

    [Header("UI Raum-Liste")]
    public Transform roomListParent;            // Parent-Objekt (Content der ScrollView), wo Buttons dynamisch erstellt werden
    public GameObject roomButtonPrefab;         // Prefab für die einzelnen Raum-Buttons in der Liste

    private string currentRoomName;              // Aktueller Raum-Name, der betreten/erstellt wird

    public List<Sprite> charakterSprites;   // Alle verfügbaren Sprites (im Inspector per Drag&Drop anfügen)
    public Image previewImage;               // Das Image im UI, das die Vorschau anzeigt
    private int selectedIndex = 0;

    // Cache für alle aktuellen Rauminfos, da Photon nur Updatelisten liefert (Deltas)
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    // Liste der momentan angezeigten UI-Buttons, zum Löschen vor Refresh
    private List<GameObject> currentRoomButtons = new List<GameObject>();

    /// <summary>
    /// Start wird beim Spielstart ausgeführt.
    /// Hier wird die Verbindung zum Photon Master Server aufgebaut
    /// und der Join-Room Button vorerst deaktiviert.
    /// Ein Listener auf den Button wird gesetzt.
    /// </summary>
    void Start()
    {
        buttonEnterRoom.interactable = false;
        PhotonNetwork.ConnectUsingSettings();
        buttonEnterRoom.onClick.AddListener(OnEnterRoomClicked);
        UpdatePreview();
    }

    /// <summary>
    /// Callback wenn Verbindung mit Photon Master Server hergestellt ist.
    /// Button zum Betreten eines Raums wird aktiviert.
    /// Der Client tritt der Lobby bei, um Raum-Updates (Liste) zu erhalten.
    /// </summary>
    public override void OnConnectedToMaster()
    {
        buttonEnterRoom.interactable = true;
        PhotonNetwork.JoinLobby();
    }

    /// <summary>
    /// Wird aufgerufen, wenn der Client die Lobby erfolgreich betreten hat.
    /// Hier könntest du UI-Status anzeigen, das ist optional.
    /// </summary>
    public override void OnJoinedLobby()
    {
        // Lobby ist nun aktiv und OnRoomListUpdate wird folgen.
    }

    /// <summary>
    /// Diese Methode wird ausgeführt, wenn Spieler den Button klickt.
    /// Sie liest den Spielernamen und Raumname aus, setzt den Photon Nickname
    /// und versucht, dem Raum beizutreten oder ihn zu erstellen.
    /// </summary>
    public void OnEnterRoomClicked()
    {
        // Spielername aus Input lesen oder Standard generieren
        string playerName = inputPlayerName.text.Trim();
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "Gast" + Random.Range(1000, 9999);
        }
        PhotonNetwork.NickName = playerName;

        // Raumname aus Input lesen und Validierung
        currentRoomName = inputRoomName.text.Trim();
        if (string.IsNullOrEmpty(currentRoomName))
        {
            // Raumname darf nicht leer sein
            return;
        }

        // Charakter Speichern
        ConfirmSelection();

        // Wenn verbunden, versuche Raum zu joinen
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinRoom(currentRoomName);
        }
    }

    /// <summary>
    /// Callback wenn einen Raum erfolgreich betreten wurde.
    /// Hier kannst du Übergang zur Spielszene starten (z.B. SceneManager.LoadScene).
    /// </summary>
    public override void OnJoinedRoom()
    {
        // Raum erfolgreich betreten - hier Szenenwechsel ggf. ausführen
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameRoom");
    }

    /// <summary>
    /// Wenn Beitritt zum Raum fehlschlägt (Raum existiert nicht),
    /// wird hier ein neuer Raum mit dem aktuellen Namen erstellt.
    /// </summary>
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        RoomOptions options = new RoomOptions { MaxPlayers = 8 };
        PhotonNetwork.CreateRoom(currentRoomName, options, TypedLobby.Default);
    }

    /// <summary>
    /// Callback nachdem ein Raum erfolgreich erstellt wurde.
    /// </summary>
    public override void OnCreatedRoom()
    {
        // Raum wurde erstellt
    }

    /// <summary>
    /// Falls Raumerstellung fehlschlägt, kann hier reagiert werden.
    /// </summary>
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        // Fehler bei Raumerstellung
    }

    /// <summary>
    /// Wird bei Änderungen in der Raumliste aufgerufen.
    /// Photon liefert nur Deltas, daher wird ein eigener Cache gepflegt.
    /// Anschließend wird die UI Raum-Liste aktualisiert.
    /// </summary>
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList)
            {
                cachedRoomList.Remove(info.Name);
            }
            else
            {
                cachedRoomList[info.Name] = info;
            }
        }
        UpdateRoomListUI();
    }

    /// <summary>
    /// Die Raum-UI-Liste wird vollständig neu aufgebaut basierend auf allen Einträgen aus dem Cache.
    /// </summary>
    private void UpdateRoomListUI()
    {
        ClearRoomListUI();

        foreach (var roomInfo in cachedRoomList.Values)
        {
            if (roomInfo.PlayerCount > 0 && !roomInfo.RemovedFromList)
                AddRoomToUI(roomInfo.Name, roomInfo.PlayerCount);
        }
    }

    /// <summary>
    /// Alle bisherigen Buttons in der Raumliste werden entfernt und aus dem Speicher gelöscht.
    /// </summary>
    private void ClearRoomListUI()
    {
        foreach (var go in currentRoomButtons)
            Destroy(go);
        currentRoomButtons.Clear();
    }

    /// <summary>
    /// Erzeugt einen Button für einen Raum im ScrollView Content.
    /// Fügt einen Listener hinzu, der bei Klick den Raumnamen in das Input-Feld schreibt und den Beitritt initiiert.
    /// </summary>
    /// <param name="roomName">Name des Raumes</param>
    /// <param name="playerCount">Anzahl der Spieler im Raum</param>
    private void AddRoomToUI(string roomName, int playerCount)
    {
        GameObject btn = Instantiate(roomButtonPrefab, roomListParent);
        btn.GetComponentInChildren<TMP_Text>().text = roomName + " (" + playerCount + ")";
        btn.GetComponent<Button>().onClick.AddListener(() => OnRoomButtonClicked(roomName));
        currentRoomButtons.Add(btn);
    }

    /// <summary>
    /// Wird aufgerufen, wenn ein Raum-Button in der Liste gedrückt wird.
    /// Setzt den Raumname ins Inputfeld und startet den Beitritt.
    /// </summary>
    /// <param name="roomName">Gewählter Raumname</param>
    public void OnRoomButtonClicked(string roomName)
    {
        inputRoomName.text = roomName;
        OnEnterRoomClicked();
    }

    public void NextCharakter()
    {
        selectedIndex = (selectedIndex + 1) % charakterSprites.Count;
        UpdatePreview();
    }

    public void PreviousCharakter()
    {
        selectedIndex = (selectedIndex - 1 + charakterSprites.Count) % charakterSprites.Count;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        previewImage.sprite = charakterSprites[selectedIndex];
    }

    public void ConfirmSelection()
    {
        // Angenommen, selectedIndex enthält deinen aktuellen Sprite/Charakter-Index

        // 1. Charakter-ID als Custom Property setzen
        Hashtable props = new Hashtable
        {
            { "CharakterID", selectedIndex }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

    }
}
