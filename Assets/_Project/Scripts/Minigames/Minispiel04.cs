using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Minispiel04 : MonoBehaviourPun
{
    [Header("UI Elemente")]
    public GameObject minigamePanel;
    public TMP_Text countdownText;
    public TMP_Text TextCounter;
    public Button clickButton;
    public TMP_Text infoText;
    public GameObject resultsPanel;
    public TMP_Text resultsText;
    public TMP_Text TextCloseCountdown;


    // Feste X-Positionen
    [SerializeField] private float fixedX_A = -350f;
    [SerializeField] private float fixedX_B = 350f;

    [SerializeField] private float fixedY_A = -30f;
    [SerializeField] private float fixedY_B = -195f;


    // Spielzeit in Sekunden
    private float countdownTime = 20f;
    private bool gameRunning = false;
    private int localClicks = 0;

    // Speichert die Klickzahlen der Spieler (gesammelt vom MasterClient)
    private Dictionary<int, int> playerClicks = new Dictionary<int, int>();

    private Transform canvasTransform;

    private void Start()
    {
        // UI initial verbergen
        clickButton.gameObject.SetActive(false);
        resultsPanel.SetActive(false);
        if (TextCounter != null)
            TextCounter.gameObject.SetActive(false);

        clickButton.onClick.AddListener(OnClickButtonPressed);

        if (TextCounter != null)
            TextCounter.text = "0";
    }

    /// <summary>
    /// Startet das Minispiel per RPC bei allen Clients
    /// </summary>
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

    /// <summary>
    /// Ablauf des Minispiels: Countdown, Klicks sammeln, Auswertung
    /// </summary>
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

        // Vor-Countdown 3 Sekunden
        float preCountdown = 10f;
        while (preCountdown > 0)
        {
            countdownText.text = Mathf.Ceil(preCountdown).ToString();
            preCountdown -= Time.deltaTime;
            yield return null;
        }

        countdownText.text = countdownTime.ToString();
        infoText.text = $"Die meisten Klicks in {countdownTime} Sekunden gewinnen!";
        clickButton.gameObject.SetActive(true);
        if (TextCounter != null)
            TextCounter.gameObject.SetActive(true);

        // Haupt-Countdown
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

        // Klickzahlen an MasterClient senden (einmalig)
        photonView.RPC("SendClickCount", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, localClicks);

        if (PhotonNetwork.IsMasterClient)
        {
            // Eigene Klickzahl im Dictionary speichern
            playerClicks[PhotonNetwork.LocalPlayer.ActorNumber] = localClicks;

            // Warte aktiv, bis alle Klickzahlen eingegangen sind oder Timeout (20s)
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
                string resultStr = "Ergebnisse:\n";

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

    /// <summary>
    /// Z�hlt auf lokalen Klick
    /// </summary>
    private void OnClickButtonPressed()
    {
        if (!gameRunning)
            return;

        localClicks++;

        if (TextCounter != null)
            TextCounter.text = localClicks.ToString();

        RectTransform buttonRect = clickButton.GetComponent<RectTransform>();

        // Zuf�llige Werte zwischen den Grenzen
        float randomX = Random.Range(fixedX_A, fixedX_B);
        float randomY = Random.Range(fixedY_A, fixedY_B);

        // Neue Position setzen
        buttonRect.anchoredPosition = new Vector2(randomX, randomY);
    }



    /// <summary>
    /// RPC zum Empfangen von Klickzahlen beim MasterClient
    /// </summary>
    [PunRPC]
    public void SendClickCount(int actorNumber, int clicks, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        playerClicks[actorNumber] = clicks;
    }

    /// <summary>
    /// RPC zum Anzeigen des Ergebnisses an alle Clients
    /// </summary>
    [PunRPC]
    public void ShowResults(string resultString)
    {
        infoText.text = "Minispiel beendet!";
        resultsText.text = resultString;
        resultsPanel.SetActive(true);
        countdownText.text = "";
        gameRunning = false;

        // Nach 3 Sekunden Minispiel schlie�en
        StartCoroutine(CloseAfterDelay(20f));
    }

    /// <summary>
    /// Verz�gerter Aufruf zur Minispiel-Schlie�ung
    /// </summary>
    private IEnumerator CloseAfterDelay(float delay)
    {
        float t = delay;
        // Optional: Zeige "Minispiel schlie�t in X..." o.�.
        if (TextCloseCountdown != null)
            TextCloseCountdown.gameObject.SetActive(true);

        while (t > 0f)
        {
            if (TextCloseCountdown != null)
                TextCloseCountdown.text = $"Minispiel endet in {Mathf.CeilToInt(t)}...";

            t -= Time.deltaTime;
            yield return null;
        }

        if (TextCloseCountdown != null)
        {
            TextCloseCountdown.text = "";           // Optional, um Text zu l�schen
            TextCloseCountdown.gameObject.SetActive(false);
        }

        CloseMinigame();
    }

    /// <summary>
    /// Schlie�t Minispiel-UI und informiert GameRoomManager �ber Gewinner
    /// </summary>
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
            // Gewinner bestimmen (doppelt sicherheitshalber)
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

            var gameRoomManager = FindAnyObjectByType<GameRoomManager>();
            if (gameRoomManager != null)
            {
                // Gewinner an GameRoomManager melden
                gameRoomManager.photonView.RPC("NotifyWinnerToGameManager", RpcTarget.All, winnerId);
                // N�chsten Task-Owner zuweisen
                gameRoomManager.AssignNextTaskOwner();
                // Aufgabenfeld wieder sichtbar machen
                gameRoomManager.photonView.RPC("SetAufgabenfeldVisible", RpcTarget.All, true);
            }

            // Netzwerk-Instanz zerst�ren
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void Awake()
    {
        // F�ge Minispiel zur Canvas hinzu
        if (canvasTransform == null)
        {
            GameObject c = GameObject.Find("Canvas");
            if (c != null)
                canvasTransform = c.transform;
        }
        if (canvasTransform != null)
            transform.SetParent(canvasTransform, false);
    }
}
