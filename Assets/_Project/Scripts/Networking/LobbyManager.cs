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
    public TMP_InputField inputRoomName;       // Eingabefeld f�r den Namen des Raums (Bar-Name)
    public TMP_InputField inputPlayerName;     // Eingabefeld f�r den Spielernamen
    public Button buttonEnterRoom;               // Button zum Betreten oder Erstellen des Raums

    [Header("UI Raum-Liste")]
    public Transform roomListParent;            // Parent-Objekt (Content der ScrollView), wo Buttons dynamisch erstellt werden
    public GameObject roomButtonPrefab;         // Prefab f�r die einzelnen Raum-Buttons in der Liste

    private string currentRoomName;              // Aktueller Raum-Name, der betreten/erstellt wird

    public List<Sprite> charakterSprites;   // Alle verf�gbaren Sprites (im Inspector per Drag&Drop anf�gen)
    public Image previewImage;               // Das Image im UI, das die Vorschau anzeigt
    private int selectedIndex = 0;
    public Toggle toggleMinigameMode; // Im Inspector zuweisen

    // Cache f�r alle aktuellen Rauminfos, da Photon nur Updatelisten liefert (Deltas)
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    // Liste der momentan angezeigten UI-Buttons, zum L�schen vor Refresh
    private List<GameObject> currentRoomButtons = new List<GameObject>();

    /// <summary>
    /// Start wird beim Spielstart ausgef�hrt.
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
    /// Hier k�nntest du UI-Status anzeigen, das ist optional.
    /// </summary>
    public override void OnJoinedLobby()
    {
        // Lobby ist nun aktiv und OnRoomListUpdate wird folgen.
    }

    /// <summary>
    /// Diese Methode wird ausgef�hrt, wenn Spieler den Button klickt.
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
    /// Hier kannst du �bergang zur Spielszene starten (z.B. SceneManager.LoadScene).
    /// </summary>
    public override void OnJoinedRoom()
    {
        // Raum erfolgreich betreten - hier Szenenwechsel ggf. ausf�hren
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameRoom");
    }

    /// <summary>
    /// Wenn Beitritt zum Raum fehlschl�gt (Raum existiert nicht),
    /// wird hier ein neuer Raum mit dem aktuellen Namen erstellt.
    /// </summary>
    public override void OnJoinRoomFailed(short returnCode, string message)
{
    // Hier wird der Modus gespeichert
    RoomOptions options = new RoomOptions { 
        MaxPlayers = 7,
        CustomRoomProperties = new Hashtable { { "MinigameMode", toggleMinigameMode.isOn } },
        CustomRoomPropertiesForLobby = new string[] { "MinigameMode" } // Damit andere den Modus sehen
    };
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
    /// Falls Raumerstellung fehlschl�gt, kann hier reagiert werden.
    /// </summary>
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        // Fehler bei Raumerstellung
    }

    /// <summary>
    /// Wird bei �nderungen in der Raumliste aufgerufen.
    /// Photon liefert nur Deltas, daher wird ein eigener Cache gepflegt.
    /// Anschlie�end wird die UI Raum-Liste aktualisiert.
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
    /// Die Raum-UI-Liste wird vollst�ndig neu aufgebaut basierend auf allen Eintr�gen aus dem Cache.
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
    /// Alle bisherigen Buttons in der Raumliste werden entfernt und aus dem Speicher gel�scht.
    /// </summary>
    private void ClearRoomListUI()
    {
        foreach (var go in currentRoomButtons)
            Destroy(go);
        currentRoomButtons.Clear();
    }

    /// <summary>
    /// Erzeugt einen Button f�r einen Raum im ScrollView Content.
    /// F�gt einen Listener hinzu, der bei Klick den Raumnamen in das Input-Feld schreibt und den Beitritt initiiert.
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
    /// Wird aufgerufen, wenn ein Raum-Button in der Liste gedr�ckt wird.
    /// Setzt den Raumname ins Inputfeld und startet den Beitritt.
    /// </summary>
    /// <param name="roomName">Gew�hlter Raumname</param>
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

    public void RandomCharakter()
    {
        if (charakterSprites.Count == 0)
            return;

        // Zuf�lligen Index w�hlen, der (falls du nicht den aktuellen willst) nicht selectedIndex sein muss:
        int randomIndex = Random.Range(0, charakterSprites.Count);
        selectedIndex = randomIndex;
        UpdatePreview();
    }


    private void UpdatePreview()
    {
        previewImage.sprite = charakterSprites[selectedIndex];
    }

    public void ConfirmSelection()
    {
        // Angenommen, selectedIndex enth�lt deinen aktuellen Sprite/Charakter-Index

        // 1. Charakter-ID als Custom Property setzen
        Hashtable props = new Hashtable
        {
            { "CharakterID", selectedIndex }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

    }

    public void BeendeSpiel()
    {
        // In Unity Editor stoppen
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
            // In einer normalen Build-Anwendung beenden
            Application.Quit();
    #endif
    }

    public void ToggleAudio()
    {
        if (Music.Instance == null)
        {
            return;
        }

        if (Music.Instance.isPlaying)
        {
            Music.Instance.PauseMusic();
        }
        else
        {
            Music.Instance.ResumeMusic();
        }
    }

}
