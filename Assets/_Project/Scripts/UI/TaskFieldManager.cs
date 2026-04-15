using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TaskFieldManager : MonoBehaviourPunCallbacks
{
    [Header("UI Referenzen")]
    public TMP_Text textBesitzer;      // Zeigt Namen des aktuellen Besitzers (oben)
    public Button buttonAufgabe;       // Knopf mit Text „LOS!“ oder Aufgabe (mit Kind-Text)
    public TMP_Text textAufgabe;       // Kindtext in buttonAufgabe, zeigt „LOS!“ oder Text Aufgabe
    public Button buttonErledigt;      // Button unten, nur für Besitzer sichtbar

    [Header("Aufgabendatenbank")]
    public AufgabenDatenbank aufgabenDatenbank;

    // Keys für Room Properties
    private const string KEY_OWNER = "TaskOwner";
    private const string KEY_INDEX = "TaskIndex";
    private const string KEY_STATUS = "TaskStatus";

    // 🔹 NEU: Zähler für erledigte Aufgaben
    private int erledigteAufgabenCounter = 0;

    // 🔹 NEU: Schwellwert bis Minispiel startet
    [SerializeField] private int aufgabenBisMinispiel = 1; // wird in Start überschrieben

    void Start()
    {
        // Button-Events registrieren
        buttonAufgabe.onClick.AddListener(OnLosClicked);
        buttonErledigt.onClick.AddListener(OnTaskDoneClicked);

        RefreshView();  // Initiale Ansicht erzeugen
        aufgabenBisMinispiel = 3; //Runden bis Minigame
    }

    // Wird immer bei Property-Updates aufgerufen
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        RefreshView();
    }

    // Updatet die UI nach aktuellen Room Properties
    // Updatet die UI nach aktuellen Room Properties
    void RefreshView()
    {
        // Sicherheitscheck: Sind wir überhaupt in einem Raum?
        if (PhotonNetwork.CurrentRoom == null) return;

        var props = PhotonNetwork.CurrentRoom.CustomProperties;

        int taskOwner = props.ContainsKey(KEY_OWNER) ? (int)props[KEY_OWNER] : -1;
        int taskIndex = props.ContainsKey(KEY_INDEX) ? (int)props[KEY_INDEX] : -1;
        string taskStatus = props.ContainsKey(KEY_STATUS) ? (string)props[KEY_STATUS] : "waiting";

        Player ownerPlayer = PhotonNetwork.CurrentRoom.GetPlayer(taskOwner);
        textBesitzer.text = (ownerPlayer != null ? ownerPlayer.NickName : "N/A") + "'s Aufgabe:";

        bool iAmOwner = PhotonNetwork.LocalPlayer.ActorNumber == taskOwner;

        if (taskStatus == "waiting")
        {
            // Noch keine Aufgabe gezogen -> Besitzer sieht „LOS!“, andere nicht
            buttonAufgabe.gameObject.SetActive(iAmOwner);
            buttonErledigt.gameObject.SetActive(false);

            // Frage soll nicht angezeigt werden, deshalb verstecken
            textAufgabe.gameObject.SetActive(false);
        }
        else if (taskStatus == "active" && taskIndex >= 0)
        {
            // Aufgabe aktiv, alle sehen sie, nur Besitzer sieht „Erledigt“-Button
            if (aufgabenDatenbank != null && taskIndex < aufgabenDatenbank.aufgabenListe.Count)
            {
                // Wir holen uns das Aufgaben-Objekt aus der Liste
                Aufgabe aufgabe = aufgabenDatenbank.aufgabenListe[taskIndex];

                // Sprachlogik: Hier wird entschieden, welcher Text angezeigt wird
                // (Später kannst du 'aktuelleSprache' über ein Menü steuern)
                string sprache = "DE"; 

                if (sprache == "EN") 
                {
                    textAufgabe.text = aufgabe.textEN;
                } 
                else 
                {
                    textAufgabe.text = aufgabe.textDE;
                }

                textAufgabe.gameObject.SetActive(true); // Text sichtbar machen
            }
            else
            {
                textAufgabe.text = "Aufgabe unbekannt";
                textAufgabe.gameObject.SetActive(true);
            }
            
            buttonAufgabe.gameObject.SetActive(false);
            buttonErledigt.gameObject.SetActive(iAmOwner);
        }
        else
        {
            // Keiner Aufgabe zuständig, alles ausblenden
            buttonAufgabe.gameObject.SetActive(false);
            buttonErledigt.gameObject.SetActive(false);
            textAufgabe.gameObject.SetActive(false);
        }
    }


    // Besitzer klickt auf „LOS!“ um Aufgabe zu ziehen
    void OnLosClicked()
    {
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        int taskOwner = props.ContainsKey(KEY_OWNER) ? (int)props[KEY_OWNER] : -1;
        string taskStatus = props.ContainsKey(KEY_STATUS) ? (string)props[KEY_STATUS] : "waiting";

        if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer.ActorNumber != taskOwner || taskStatus != "waiting")
            return;

        int count = aufgabenDatenbank?.aufgabenListe.Count ?? 0;
        if (count == 0)
            return;

        int rnd = Random.Range(0, count);

        Hashtable newProps = new Hashtable
    {
        { KEY_OWNER, taskOwner },
        { KEY_INDEX, rnd },
        { KEY_STATUS, "active" }
    };
    PhotonNetwork.CurrentRoom.SetCustomProperties(newProps);

    }


    // Besitzer klickt „Erledigt“, setzt nächsten Spieler als Besitzer und löscht Aufgabe
    void OnTaskDoneClicked()
    {
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        int taskOwner = props.ContainsKey(KEY_OWNER) ? (int)props[KEY_OWNER] : -1;
        string taskStatus = props.ContainsKey(KEY_STATUS) ? (string)props[KEY_STATUS] : "active";

        if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer.ActorNumber != taskOwner || taskStatus != "active")
            return;

        Player[] players = PhotonNetwork.PlayerList;
        if (players.Length == 0)
            return;

        int ownerIndex = System.Array.FindIndex(players, p => p.ActorNumber == taskOwner);
        int nextIndex = (ownerIndex + 1) % players.Length;
        int nextOwner = players[nextIndex].ActorNumber;

        Hashtable newProps = new Hashtable
        {
            { KEY_OWNER, nextOwner },
            { KEY_INDEX, -1 },
            { KEY_STATUS, "waiting" }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(newProps);

        FindAnyObjectByType<GameRoomManager>().AddRoundPointsToTotalForAll();

        // --- NEU: Aufgaben-Zähler erhöhen ---
        erledigteAufgabenCounter++;
        Debug.Log("erledigteAufgabenCounter:" + erledigteAufgabenCounter + "  (Aufgaben bis Minispiel: " + aufgabenBisMinispiel + ")");
        // Wenn wir mind. X Aufgaben erledigt haben → Minispiel starten
        if (erledigteAufgabenCounter >= aufgabenBisMinispiel)
        {
            // Reset für nächste Runde
            erledigteAufgabenCounter = 0;

            // Nur der MasterClient soll das Minispiel starten
            if (PhotonNetwork.IsMasterClient)
            {
                var gameRoomManager = FindAnyObjectByType<GameRoomManager>();
                if (gameRoomManager != null)
                {
                    gameRoomManager.StartMinigameAfterReset();
                }
            }
        }
    }
}
