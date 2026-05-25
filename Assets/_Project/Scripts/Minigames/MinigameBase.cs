using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Photon.Pun;
using Photon.Realtime;

public enum WinConditionType
{
    HighestScoreWins,
    LowestScoreWins
}

/// <summary>
/// Basis-Klasse für alle Minispiele.
///
/// LAYOUT (top-down, Panel 880×535):
///   TextMinispiel    50px  – Spieltitel, fest im Prefab gesetzt
///   TextBeschreibung 90px  – Spielanweisung, Kind-Klasse setzt in SetupGame()
///   CountdownText    54px  – Countdown-Anzeige (Base verwaltet)
///   TextScore        32px  – "Punkte: X" (Base verwaltet via AddScore())
///   ── 226px Header gesamt ──
///   SPIELBEREICH    ~249px – Kind-Klasse (BottomOffset=60 bleibt)
///   ── 60px Rahmen unten ──
///   TextCloseCountdown – auf dem Rahmen: "Schließt in X…"
///
/// SCORE-SYSTEM:
///   protected int localScore          – zentrale Punkte-Variable
///   protected void AddScore(int delta) – Punkte ändern + TextScore-UI updaten
///   virtual float GetLocalPlayerScore() – gibt localScore zurück (überschreibbar)
/// </summary>
public abstract class MinigameBase : MonoBehaviourPun
{
    // ── Header-Felder (alle im Prefab verankert) ─────────────────────────
    [Header("Basis UI Elemente")]
    public GameObject minigamePanel;

    /// <summary>
    /// Spielanweisung – von der Kind-Klasse in SetupGame() gesetzt.
    /// [FormerlySerializedAs] stellt sicher, dass alte Prefabs mit "infoText"
    /// weiterhin korrekt gebunden werden.
    /// </summary>
    [FormerlySerializedAs("infoText")]
    public TMP_Text textBeschreibung;

    public TMP_Text countdownText;

    /// <summary>
    /// "Punkte: X" – wird von AddScore() automatisch aktualisiert.
    /// Muss im Prefab als "TextScore"-Objekt unter minigamePanel liegen.
    /// </summary>
    public TMP_Text textScore;

    public GameObject resultsPanel;
    public TMP_Text   resultsText;
    public TMP_Text   TextCloseCountdown;

    // ── Interne Felder ────────────────────────────────────────────────────
    protected float countdownTime = 20f;
    protected bool  gameRunning   = false;
    protected int   localScore    = 0;

    private Dictionary<int, float> playerScores = new Dictionary<int, float>();
    private Transform canvasTransform;

    // ── Abstrakte Methoden ────────────────────────────────────────────────

    /// <summary>Vorbereitung: textBeschreibung setzen, Spiel-UI aufbauen.</summary>
    protected abstract void SetupGame();

    /// <summary>Interaktion freischalten (Buttons aktiv).</summary>
    protected abstract void StartActualGame();

    /// <summary>Interaktion sperren, Spiel-UI ausblenden.</summary>
    protected abstract void EndActualGame();

    /// <summary>Finaler Score. Standard gibt localScore zurück.</summary>
    protected virtual float GetLocalPlayerScore() => localScore;

    public virtual WinConditionType WinCondition => WinConditionType.HighestScoreWins;

    // ── Score-Hilfsmethode ────────────────────────────────────────────────

    /// <summary>
    /// Addiert delta zu localScore und aktualisiert das TextScore-Feld.
    /// Aufruf aus Kind-Klassen: AddScore(+1) oder AddScore(-1).
    /// </summary>
    protected void AddScore(int delta)
    {
        localScore += delta;
        if (textScore != null)
            textScore.text = $"Punkte: {localScore}";
    }

    // ── Unity-Lifecycle ───────────────────────────────────────────────────

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

    // ── Photon-Einstiegspunkt ─────────────────────────────────────────────

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

        // Header zurücksetzen
        localScore = 0;
        if (textScore        != null) textScore.text        = "Punkte: 0";
        if (textBeschreibung != null) textBeschreibung.text = "";
        if (countdownText    != null) countdownText.text    = "";

        SetupGame();
        StartCoroutine(MinigameFlow());
    }

    // ── Hauptablauf ───────────────────────────────────────────────────────

    private IEnumerator MinigameFlow()
    {
        playerScores.Clear();

        // 1. Vor-Countdown (3 s) — textBeschreibung ist jetzt bereits von SetupGame() gesetzt
        float preCountdown = 3f;
        while (preCountdown > 0)
        {
            countdownText.text  = Mathf.Ceil(preCountdown).ToString();
            preCountdown       -= Time.deltaTime;
            yield return null;
        }

        countdownText.text = countdownTime.ToString("F0");

        // 2. Spiel startet
        gameRunning = true;
        StartActualGame();

        // 3. Haupt-Countdown
        float timer = countdownTime;
        while (timer > 0)
        {
            timer              -= Time.deltaTime;
            countdownText.text  = Mathf.Ceil(timer).ToString();
            yield return null;
        }

        // 4. Spiel beenden
        gameRunning = false;
        EndActualGame();

        countdownText.text = "0";
        if (textBeschreibung != null)
            textBeschreibung.text = "Zeit ist um – Auswertung läuft…";

        // 5. Score an MasterClient senden
        float finalScore = GetLocalPlayerScore();
        photonView.RPC(nameof(SubmitScore), RpcTarget.MasterClient,
            PhotonNetwork.LocalPlayer.ActorNumber, finalScore);

        // 6. Auswertung (nur auf MasterClient)
        if (!PhotonNetwork.IsMasterClient) yield break;

        playerScores[PhotonNetwork.LocalPlayer.ActorNumber] = finalScore;

        float waitTimeout = 10f;
        float elapsed     = 0f;
        int   playerCount = PhotonNetwork.CurrentRoom.PlayerCount;

        while (playerScores.Count < playerCount && elapsed < waitTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (playerScores.Count == 0)
        {
            photonView.RPC(nameof(ShowResults), RpcTarget.All, "Keine Daten erhalten!", -1);
            yield break;
        }

        // Gewinner ermitteln
        float bestScore = (WinCondition == WinConditionType.HighestScoreWins)
            ? float.MinValue : float.MaxValue;
        int winnerId = -1;

        foreach (var kvp in playerScores)
        {
            bool isBetter = (WinCondition == WinConditionType.HighestScoreWins)
                ? kvp.Value > bestScore
                : kvp.Value < bestScore;
            if (isBetter) { bestScore = kvp.Value; winnerId = kvp.Key; }
        }

        // Sortierte Rangliste aufbauen
        var sorted = new List<KeyValuePair<int, float>>(playerScores);
        if (WinCondition == WinConditionType.HighestScoreWins)
            sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
        else
            sorted.Sort((a, b) => a.Value.CompareTo(b.Value));

        string resultStr = "🏆  Ergebnisse\n\n";
        for (int i = 0; i < sorted.Count; i++)
        {
            string pName = PhotonNetwork.CurrentRoom.GetPlayer(sorted[i].Key)?.NickName
                           ?? $"Spieler {sorted[i].Key}";
            int    pts   = Mathf.RoundToInt(sorted[i].Value);
            string medal = i == 0 ? "🥇" : (i == 1 ? "🥈" : "🥉");
            resultStr   += $"{medal}  {pName}:  {pts} Punkte\n";
        }

        photonView.RPC(nameof(ShowResults), RpcTarget.All, resultStr, winnerId);
    }

    // ── RPCs ──────────────────────────────────────────────────────────────

    [PunRPC]
    public void SubmitScore(int actorNumber, float score, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        playerScores[actorNumber] = score;
    }

    [PunRPC]
    public void ShowResults(string resultString, int winnerId)
    {
        // Header bereinigen
        if (textBeschreibung != null) textBeschreibung.text = "";
        if (textScore        != null) textScore.text        = "";
        countdownText.text = "";

        resultsText.text = resultString;
        resultsPanel.SetActive(true);

        StartCoroutine(CloseAfterDelay(10f, winnerId));
    }

    private IEnumerator CloseAfterDelay(float delay, int winnerId)
    {
        float t = delay;
        if (TextCloseCountdown != null) TextCloseCountdown.gameObject.SetActive(true);

        while (t > 0f)
        {
            if (TextCloseCountdown != null)
                TextCloseCountdown.text = $"Schließt in {Mathf.CeilToInt(t)}…";
            t -= Time.deltaTime;
            yield return null;
        }

        CloseMinigame(winnerId);
    }

    private void CloseMinigame(int winnerId)
    {
        minigamePanel.SetActive(false);
        resultsPanel.SetActive(false);

        if (!PhotonNetwork.IsMasterClient) return;

        var manager = FindAnyObjectByType<GameRoomManager>();
        if (manager == null)
        {
            PhotonNetwork.Destroy(gameObject);
            return;
        }

        // VIP-Panel bei allen Clients zeigen
        if (winnerId != -1)
            manager.photonView.RPC("NotifyWinnerToGameManager", RpcTarget.All, winnerId);

        if (manager.isMinigameMode)
        {
            if (winnerId != -1)
                manager.photonView.RPC("AddArcadeWinPoint", RpcTarget.All, winnerId);

            if (manager.masterMinigameButton != null)
                manager.masterMinigameButton.SetActive(true);
        }
        else
        {
            manager.AssignNextTaskOwner();
            manager.photonView.RPC("SetAufgabenfeldVisible", RpcTarget.All, true);
        }

        PhotonNetwork.Destroy(gameObject);
    }
}
