using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class PlayerFrameBehaviour : MonoBehaviour
{
    public Button glassButton;
    public TMP_Text nameText;
    public TMP_Text pointsText;
    public TMP_Text totalPointsText;
    public Image imagePlayer;

    // Die ActorNumber des Spielers, den dieses PlayerFrame darstellt:
    [HideInInspector] public int actorNumber;

    private void Start()
    {
        // Glas-Button-Event nur für lokalen Spieler aktivieren
        if (glassButton != null)
        {
            // Prüfe: Ist das Frame von mir selbst? Dann Button aktivieren!
            glassButton.gameObject.SetActive(actorNumber == PhotonNetwork.LocalPlayer.ActorNumber);

            glassButton.onClick.AddListener(OnGlassClicked);
        }
    }

    void OnGlassClicked()
    {
        // Punkteerhöhung per RPC oder über einen Event an den GameRoomManager auslösen,
        // Hauptsache, der richtige Spieler bekommt die Punkte!
        if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            // Lokalen Spieler um 1 erhöhen (direkt oder besser per GameRoomManager)
            Player localPlayer = PhotonNetwork.LocalPlayer;
            int currentPoints = 0;
            localPlayer.CustomProperties.TryGetValue("Points", out object pointsObj);
            if (pointsObj != null) currentPoints = (int)pointsObj;

            int newPoints = currentPoints + 1;
            var props = new ExitGames.Client.Photon.Hashtable { { "Points", newPoints } };
            localPlayer.SetCustomProperties(props);
        }
    }
}
