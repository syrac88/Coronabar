using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Minispiel01 : MonoBehaviourPun
{
    [Header("UI Elemente")]
    public GameObject minigamePanel;
    public TMP_Text countdownText;
    public TMP_Text TextCounter;
    public Button clickButton;
    public TMP_Text infoText;
    public GameObject resultsPanel;
    public TMP_Text resultsText;

    private float countdownTime = 3f; // Spielzeit 3 Sekunden (kann angepasst werden)
    private bool gameRunning = false;
    private int localClicks = 0;
    private Dictionary<int, int> playerClicks = new Dictionary<int, int>();
    private Transform canvasTransform;  // Canvas als Parent

    private void Start()
    {
        clickButton.gameObject.SetActive(false);
        resultsPanel.SetActive(false);
        if (TextCounter != null)
            TextCounter.gameObject.SetActive(false);

        clickButton.onClick.AddListener(OnClickButtonPressed);

        if (TextCounter != null)
            TextCounter.text = "0";
    }

    public void TriggerMinigameStart()
    {
        photonView.RPC("RpcStartMinigame", RpcTarget.All);
    }

    [PunRPC]
    public void RpcStartMinigame()
    {
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        minigamePanel.SetActive(true);
        StartCoroutine(MinigameFlow());
    }

    private IEnumerator MinigameFlow()
    {
        gameRunning = true;
        localClicks = 0;
        playerClicks.Clear();

        minigamePanel.SetActive(true);
        resultsPanel.SetActive(false);
        clickButton.gameObject.SetActive(false);
        if (TextCounter != null)
            TextCounter.gameObject.SetActive(false);

        infoText.text = "Bereit... gleich geht's los!";
        if (TextCounter != null)
            TextCounter.text = "0";

        // 3 Sekunden Vor-Countdown
        float preCountdown = 3f;
        while (preCountdown > 0)
        {
            countdownText.text = Mathf.Ceil(preCountdown).ToString();
            preCountdown -= Time.deltaTime;
            yield return null;
        }

        countdownText.text = countdownTime.ToString();
        infoText.text = "Wer in 3 Sekunden am meisten klickt, gewinnt!";
        clickButton.gameObject.SetActive(true);
        if (TextCounter != null)
            TextCounter.gameObject.SetActive(true);

        float timer = countdownTime;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            countdownText.text = Mathf.Ceil(timer).ToString();
            yield return null;
        }

        clickButton.gameObject.SetActive(false);
        if (TextCounter != null)
            TextCounter.gameObject.SetActive(false);

        infoText.text = "Auswertung...";

        Debug.Log($"Sending click count {localClicks} from player {PhotonNetwork.LocalPlayer.ActorNumber}");

        // Klickzahl an MasterClient senden (einmalig)
        photonView.RPC("SendClickCount", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, localClicks);

        // MasterClient wertet aus und sendet Ergebnis zurück
        if (PhotonNetwork.IsMasterClient)
        {
            // Eigene Klickzahl speichern
            playerClicks[PhotonNetwork.LocalPlayer.ActorNumber] = localClicks;

            // Warte aktiv auf Klickzahlen aller Spieler oder Timeout
            float waitTimeout = 20f;
            float elapsed = 0f;
            int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;

            while (playerClicks.Count < playerCount && elapsed < waitTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (playerClicks.Count == 0)
            {
                infoText.text = "Keine Klickdaten erhalten!";
                photonView.RPC("ShowResults", RpcTarget.All, "Keine Klickdaten erhalten!");
            }
            else
            {
                // Gewinner ermitteln
                int maxClicks = -1;
                int winnerId = -1;
                foreach (var kvp in playerClicks)
                {
                    if (kvp.Value > maxClicks)
                    {
                        maxClicks = kvp.Value;
                        winnerId = kvp.Key;
                    }
                }

                string winnerName = PhotonNetwork.CurrentRoom.GetPlayer(winnerId)?.NickName ?? "Unbekannt";
                string resultStr = "Klick-Ergebnisse:\n";

                foreach (var kvp in playerClicks)
                {
                    string name = PhotonNetwork.CurrentRoom.GetPlayer(kvp.Key)?.NickName ?? $"Spieler {kvp.Key}";
                    resultStr += $"{name}: {kvp.Value}\n";
                }
                resultStr += $"\nGewinner: {winnerName}";

                photonView.RPC("ShowResults", RpcTarget.All, resultStr);
            }
        }
    }

    private void OnClickButtonPressed()
    {
        if (!gameRunning)
            return;

        localClicks++;

        if (TextCounter != null)
            TextCounter.text = localClicks.ToString();
    }

    [PunRPC]
    public void SendClickCount(int actorNumber, int clicks, PhotonMessageInfo info)
    {
        Debug.Log($"SendClickCount received on MasterClient: Player {actorNumber}, Clicks {clicks}");
        if (!PhotonNetwork.IsMasterClient) return;

        playerClicks[actorNumber] = clicks;
    }

    [PunRPC]
    public void ShowResults(string resultString)
    {
        infoText.text = "Minispiel beendet!";
        resultsText.text = resultString;
        resultsPanel.SetActive(true);
        countdownText.text = "";
        gameRunning = false;

        // Starte bei allen Clients das Schließen nach 3 Sekunden Wartezeit
        StartCoroutine(CloseAfterDelay(3f));
    }

    private IEnumerator CloseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CloseMinigame();
    }

    public void CloseMinigame()
    {
        minigamePanel.SetActive(false);
        resultsPanel.SetActive(false);
        clickButton.gameObject.SetActive(false);
        if (TextCounter != null)
            TextCounter.gameObject.SetActive(false);

        gameRunning = false;
        if (TextCounter != null)
            TextCounter.text = "0";

        if (PhotonNetwork.IsMasterClient)
        {
            // Gewinner ermitteln für VIP-Anzeige (evtl. doppelt)
            int maxClicks = -1;
            int winnerId = -1;
            foreach (var kvp in playerClicks)
            {
                if (kvp.Value > maxClicks)
                {
                    maxClicks = kvp.Value;
                    winnerId = kvp.Key;
                }
            }

            var gameRoomManager = FindFirstObjectByType<GameRoomManager>();
            if (gameRoomManager != null)
            {
                gameRoomManager.photonView.RPC("NotifyWinnerToGameManager", RpcTarget.All, winnerId);
                gameRoomManager.AssignNextTaskOwner();
                gameRoomManager.photonView.RPC("SetAufgabenfeldVisible", RpcTarget.All, true);
            }
            else
            {
                Debug.LogWarning("GameRoomManager nicht gefunden");
            }

            PhotonNetwork.Destroy(gameObject); // zerstört Minispiel-Instanz im Netzwerk
        }
    }

    private void Awake()
    {
        if (canvasTransform == null)
        {
            GameObject c = GameObject.Find("Canvas");
            if (c != null)
                canvasTransform = c.transform;
        }
        if (canvasTransform != null)
        {
            transform.SetParent(canvasTransform, false);
        }
        else
        {
            Debug.LogWarning("Canvas nicht gefunden, Parent konnte nicht gesetzt werden!");
        }
    }
}
