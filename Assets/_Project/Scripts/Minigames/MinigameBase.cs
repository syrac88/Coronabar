using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public enum WinConditionType
{
    HighestScoreWins,
    LowestScoreWins // Z.B. für Rennen auf Zeit
}

public abstract class MinigameBase : MonoBehaviourPun
{
    [Header("Basis UI Elemente")]
    public GameObject minigamePanel;
    public TMP_Text countdownText;
    public TMP_Text infoText;
    public GameObject resultsPanel;
    public TMP_Text resultsText;
    public TMP_Text TextCloseCountdown;

    protected float countdownTime = 20f;
    protected bool gameRunning = false;
    private Dictionary<int, float> playerScores = new Dictionary<int, float>();
    private Transform canvasTransform;

    // ----- ABSTRAKTE METHODEN (Müssen von den Spielen überschrieben werden) -----
    protected abstract void SetupGame();
    protected abstract void StartActualGame();
    protected abstract void EndActualGame();
    protected abstract float GetLocalPlayerScore();

    // Standardmäßig gewinnt die höchste Punktzahl. Für Rennspiele in der Child-Klasse überschreiben!
    public virtual WinConditionType WinCondition => WinConditionType.HighestScoreWins;

    protected virtual void Awake()
    {
        if (canvasTransform == null)
        {
            GameObject c = GameObject.Find("Canvas");
            if (c != null) canvasTransform = c.transform;
        }
        if (canvasTransform != null)
            transform.SetParent(canvasTransform, false);
    }

    public virtual void TriggerMinigameStart()
    {
        photonView.RPC(nameof(RpcStartMinigame), RpcTarget.All);
    }

    [PunRPC]
    public void RpcStartMinigame()
    {
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        minigamePanel.SetActive(true);
        resultsPanel.SetActive(false);
        
        SetupGame(); // Child-Klasse bereitet sich vor
        StartCoroutine(MinigameFlow());
    }

    private IEnumerator MinigameFlow()
    {
        playerScores.Clear();
        infoText.text = "Bereit... gleich geht's los!";
        countdownText.text = "";

        // 1. Vor-Countdown (3 Sekunden)
        float preCountdown = 3f; // Reduziert von 10f auf 3f (10 war in deinem Code, oft zu lang für Minispiele)
        while (preCountdown > 0)
        {
            countdownText.text = Mathf.Ceil(preCountdown).ToString();
            preCountdown -= Time.deltaTime;
            yield return null;
        }

        infoText.text = (WinCondition == WinConditionType.HighestScoreWins) 
            ? $"Die meisten Punkte in {countdownTime} Sekunden gewinnen!" 
            : $"Die schnellste Zeit gewinnt!";
            
        countdownText.text = countdownTime.ToString();

        // 2. Spiel startet lokal
        gameRunning = true;
        StartActualGame(); // Child-Klasse schaltet z.B. Buttons frei

        // 3. Haupt-Countdown
        float timer = countdownTime;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            countdownText.text = Mathf.Ceil(timer).ToString();
            yield return null;
        }

        // 4. Spiel beenden lokal
        gameRunning = false;
        EndActualGame(); // Child-Klasse blockiert Eingaben
        
        infoText.text = "Auswertung...";
        countdownText.text = "0";

        // 5. Punkte an MasterClient senden
        float finalScore = GetLocalPlayerScore();
        photonView.RPC(nameof(SubmitScore), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, finalScore);

        // 6. MasterClient wertet aus
        if (PhotonNetwork.IsMasterClient)
        {
            playerScores[PhotonNetwork.LocalPlayer.ActorNumber] = finalScore;

            float waitTimeout = 10f;
            float elapsed = 0f;
            int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
            
            while (playerScores.Count < playerCount && elapsed < waitTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (playerScores.Count == 0)
            {
                photonView.RPC(nameof(ShowResults), RpcTarget.All, "Keine Daten erhalten!", -1);
            }
            else
            {
                float bestScore = (WinCondition == WinConditionType.HighestScoreWins) ? float.MinValue : float.MaxValue;
                int winnerId = -1;

                foreach (var kvp in playerScores)
                {
                    bool isBetter = (WinCondition == WinConditionType.HighestScoreWins) ? kvp.Value > bestScore : kvp.Value < bestScore;
                    if (isBetter)
                    {
                        bestScore = kvp.Value;
                        winnerId = kvp.Key;
                    }
                }

                string winnerName = PhotonNetwork.CurrentRoom.GetPlayer(winnerId)?.NickName ?? "Unbekannt";
                string resultStr = "Ergebnisse:\n";

                foreach (var kvp in playerScores)
                {
                    string name = PhotonNetwork.CurrentRoom.GetPlayer(kvp.Key)?.NickName ?? $"Spieler {kvp.Key}";
                    resultStr += $"{name}: {Mathf.RoundToInt(kvp.Value)}\n"; // Aktuell Runden wir auf ganze Zahlen für Anzeige
                }
                resultStr += $"\nGewinner: {winnerName}";

                photonView.RPC(nameof(ShowResults), RpcTarget.All, resultStr, winnerId);
            }
        }
    }

    [PunRPC]
    public void SubmitScore(int actorNumber, float score, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        playerScores[actorNumber] = score;
    }

    [PunRPC]
    public void ShowResults(string resultString, int winnerId)
    {
        infoText.text = "Minispiel beendet!";
        resultsText.text = resultString;
        resultsPanel.SetActive(true);
        countdownText.text = "";
        
        StartCoroutine(CloseAfterDelay(10f, winnerId)); // 10 Sek Anzeige reicht meistens
    }

    private IEnumerator CloseAfterDelay(float delay, int winnerId)
    {
        float t = delay;
        if (TextCloseCountdown != null) TextCloseCountdown.gameObject.SetActive(true);

        while (t > 0f)
        {
            if (TextCloseCountdown != null) TextCloseCountdown.text = $"Schließt in {Mathf.CeilToInt(t)}...";
            t -= Time.deltaTime;
            yield return null;
        }

        CloseMinigame(winnerId);
    }

    private void CloseMinigame(int winnerId)
    {
        minigamePanel.SetActive(false);
        resultsPanel.SetActive(false);

        if (PhotonNetwork.IsMasterClient)
        {
            var manager = FindAnyObjectByType<GameRoomManager>();
            if (manager != null)
            {
                // VIP-Panel mit Gewinner anzeigen (bei allen Clients)
                if (winnerId != -1)
                    manager.photonView.RPC("NotifyWinnerToGameManager", RpcTarget.All, winnerId);

                if (manager.isMinigameMode)
                {
                    // Gewinner einen Punkt im Arcade-Modus geben
                    if (winnerId != -1)
                        manager.photonView.RPC("AddArcadeWinPoint", RpcTarget.All, winnerId);

                    // Button wieder zeigen
                    if (manager.masterMinigameButton != null)
                        manager.masterMinigameButton.SetActive(true);
                }
                else
                {
                    manager.AssignNextTaskOwner();
                    manager.photonView.RPC("SetAufgabenfeldVisible", RpcTarget.All, true);
                }
            }
            PhotonNetwork.Destroy(gameObject);
        }
    }
}