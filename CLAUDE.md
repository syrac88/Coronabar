# 🍻 Coronabar – Entwickler-Dokumentation

**Projekt-Repository:** https://github.com/syrac88/Coronabar
**Aktuelle Version:** 1.8 | **Steam App ID:** 3950630
**Engine:** Unity 2D (URP) | **Networking:** Photon PUN 2 | **Sprache:** C#

---

## 1. Spielkonzept & Modi

Coronabar ist ein digitales Multiplayer-Partyspiel (bis 7 Spieler), das Aufgaben-Karten mit Minispielen verbindet.

| Modus | Beschreibung |
|---|---|
| **Normal** | Aufgaben-Phase + automatische Minispiele nach X Aufgaben |
| **Arcade** | Kein Aufgabenfeld. MasterClient startet Minispiele manuell per Button |

Der Modus wird in der Lobby gewählt (`toggleMinigameMode`) und als Room Custom Property `"MinigameMode"` (bool) gespeichert. Im GameRoom liest `GameRoomManager.Start()` diese Property und konfiguriert die UI.

**Spieler-Daten (Photon Custom Properties):**
- `"CharakterID"` (int) – gewählter Charakter-Sprite-Index
- `"Points"` (int) – Rundenp unkte (werden durch Glas-Button erhöht)
- `"TotalPoints"` (int) – Gesamtpunkte über alle Runden / Arcade-Siege

---

## 2. Technische Architektur (PUN 2)

Zustandssynchronisation über **Room Custom Properties** und **RPCs**.

**Room Custom Properties:**
- `"MinigameMode"` – Arcade-Modus aktiv?
- `"TaskOwner"` – ActorNumber des aktuellen Aufgaben-Besitzers
- `"TaskIndex"` – Index der aktuellen Aufgabe (-1 = keine)
- `"TaskStatus"` – `"waiting"` / `"active"` / etc.

**Autorität:** MasterClient steuert Spielstatus, Minispiel-Loop und Arcade-Button. `StartMinigame()` und `MasterStartsNextMinigame()` prüfen `PhotonNetwork.IsMasterClient`.

---

## 3. Ordner- und Dateistruktur

```
Assets/
  _Project/
    Data/              ScriptableObjects (AufgabenDatenbank)
    Images/            Sprites, UI-Grafiken, Charakter-Sprites
    Prefabs/           Allgemeine Prefabs (PlayerFrame, Aufgabenfeld…)
    Resources/
      PhotonPrefabs/   Minispiel01–09_PrefabRoot (PhotonNetwork.Instantiate)
    Scenes/            MainMenu.unity, GameRoom.unity, Lobby.unity
    Scripts/
      Core/            GameRoomManager, LobbyManager, SceneLoader, Music, AspectRatioEnforcer
      Minigames/       MinigameBase, Minispiel01–09
      Networking/      LobbyManager
      UI/              PlayerFrameBehaviour, PlayerListAlternatingLayout,
                       TaskFieldManager, InputFieldFocusHandler
  StreamingAssets/     minispiel07_fragen.json, minispiel09_fragen.json
ProjectSettings/       ProjectSettings.asset (bundleVersion)
```

---

## 4. Minispiele – Übersicht

Alle Minispiele erben von `MinigameBase` und liegen als Photon-Prefabs unter `Resources/PhotonPrefabs/`.

| # | Prefab | Spielprinzip | Zeit |
|---|---|---|---|
| 01 | Minispiel01_PrefabRoot | Roter Knopf: so oft klicken wie möglich | 20 s |
| 02 | Minispiel02_PrefabRoot | Knopf wechselt nach Klick links↔rechts (±270 px) | 20 s |
| 03 | Minispiel03_PrefabRoot | Knopf springt zufällig auf X-Achse (±270 px) | 20 s |
| 04 | Minispiel04_PrefabRoot | Knopf springt zufällig auf X- und Y-Achse | 20 s |
| 05 | Minispiel05_PrefabRoot | Mathe-Duell: +/−/× mit 1–10 | 30 s |
| 06 | Minispiel06_PrefabRoot | Mathe-Duell: zweistellig + Division | 30 s |
| 07 | Minispiel07_PrefabRoot | Quiz Allgemeinwissen (numerische Antworten, JSON) | 30 s |
| 08 | Minispiel08_PrefabRoot | Stroop-Effekt: Tintenfarbe tippen | 30 s |
| 09 | Minispiel09_PrefabRoot | Wahr oder Falsch (Fakten-Aussagen, JSON) | 30 s |

**Gemeinsamer Ablauf (`MinigameBase.MinigameFlow`):**
1. Vor-Countdown (3 s) – textBeschreibung ist bereits gesetzt, Buttons gesperrt
2. `StartActualGame()` – Buttons freischalten
3. Haupt-Countdown (`countdownTime`)
4. `EndActualGame()` – Buttons sperren, Spiel-UI ausblenden
5. `SubmitScore` RPC → MasterClient sammelt Scores (10 s Timeout)
6. Rangliste aufbauen, `ShowResults` RPC → alle Clients
7. Ergebnis-Anzeige (10 s), dann `CloseMinigame`
8. `NotifyWinnerToGameManager` RPC → VIP-Panel

**Score-Regeln:** +1 richtig / −1 falsch. Minispiel09: Untergrenze −20. Sieger = höchste Punktzahl (`WinConditionType.HighestScoreWins`).

---

## 5. Standardisiertes UI-Layout (Panel 880×535)

### Prefab-Objektnamen (exakt so benannt!)

| Objekt | Rolle |
|---|---|
| `TextMinispiel` | Spieltitel (fest im Prefab) |
| `TextBeschreibung` | Spielanweisung (Kind-Klasse setzt in `SetupGame()`) |
| `TextCountdown` | Countdown-Anzeige (MinigameBase verwaltet) |
| `TextScore` | „Punkte: X" (MinigameBase via `AddScore()`) |
| `Panelresults` | Overlay-Panel für Ergebnis-Anzeige |
| `TextResult` | Ergebnis-Text (in Panelresults) |
| `TextCloseCountdown` | „Schließt in X…" (im 60-px-Rahmen unten) |

### Header (center-anchor, center-pivot, 6 px Gap zwischen Elementen)

```
y=+242.5  h=50  fs=36                TextMinispiel      [485–535]
          6 px gap
y=+170.5  h=82  fs=20  autoSize=OFF  TextBeschreibung   [397–479]
          6 px gap
y= +98.5  h=50  fs=40  autoSize=OFF  TextCountdown      [341–391]  ← autoSize MUSS aus sein!
          6 px gap
y= +54.5  h=26  fs=22  autoSize=OFF  TextScore          [309–335]
──────────────────────────────────────────────── Header: 226 px
          Spielbereich               [309–60]  = 249 px
──────────────────────────────────────────────── BottomOffset: 60 px
          TextCloseCountdown         [60–0]    = Rahmen
```

**Wichtig:** `TextCountdown.enableAutoSizing = false` – sonst quillt der Text in benachbarte Elemente.

### ResultsPanel

- Objekt `Panelresults`: Stretch-Anker, füllt das gesamte Panel aus
- `TextResult`: `fontSize=18`, `enableAutoSizing=false`, `textWrappingMode=NoWrap`, `overflowMode=Overflow`
- Format (generiert in `MinigameBase.MinigameFlow`):
  ```
  -- ERGEBNISSE --

  1. Max: 14 Pkt
  2. Anna: 11 Pkt
  3. Tom: -2 Pkt
  ```
- **Keine Emojis** – der verwendete Pixel-Font unterstützt keine Unicode-Emojis

---

## 6. Score-System (`MinigameBase`)

```csharp
protected int   localScore = 0;                    // Punkte-Variable
protected void  AddScore(int delta)                // Punkte ändern + TextScore-UI
protected virtual float GetLocalPlayerScore()      // gibt localScore zurück (überschreibbar)
```

- `AddScore(+1/-1)` in Kind-Klassen aufrufen – kein eigenes `localScore`-Feld nötig
- `textScore` wird automatisch aktualisiert: „Punkte: X"
- **Kein** `UpdateScoreDisplay()` in Kind-Klassen nötig
- Minispiel01–04 spiegeln `localScore` zusätzlich im `TextCounter` (großer Zähler im Spielbereich)

---

## 7. MinigameBase – Serialized Fields (Prefab-Zuweisungen)

| C#-Feld | Prefab-Objekt | Hinweis |
|---|---|---|
| `minigamePanel` | `Minigame_0X` (Panel-Root) | |
| `textBeschreibung` | `TextBeschreibung` | `[FormerlySerializedAs("infoText")]` – alte Prefabs bleiben kompatibel |
| `countdownText` | `TextCountdown` | |
| `textScore` | `TextScore` | |
| `resultsPanel` | `Panelresults` | |
| `resultsText` | `TextResult` (in Panelresults) | |
| `TextCloseCountdown` | `TextCloseCountdown` | |

---

## 8. Minispiel07 – Quiz Allgemeinwissen

**Dateien:**
- Script: `Assets/_Project/Scripts/Minigames/Minispiel07.cs`
- Prefab: `Assets/_Project/Resources/PhotonPrefabs/Minispiel07_PrefabRoot.prefab`
- Daten: `Assets/StreamingAssets/minispiel07_fragen.json`

**JSON-Format:**
```json
{ "fragen": [ { "frage": "...", "richtig": 42, "falsch": [10, 55] } ] }
```

**Besonderheiten:**
- 3 braune Antwort-Buttons (1 richtig, 2 falsch) – Reihenfolge per `Random` gemischt
- Seed-RPC-Pattern: `TriggerMinigameStart()` mit `override`, MasterClient sendet Seed → gleiche Reihenfolge auf allen Clients

---

## 9. Minispiel08 – Stroop-Effekt

**Dateien:**
- Script: `Assets/_Project/Scripts/Minigames/Minispiel08.cs`
- Prefab: `Assets/_Project/Resources/PhotonPrefabs/Minispiel08_PrefabRoot.prefab`

**Farben:** ROT, BLAU, GRÜN, GELB, LILA (5 Farben, 4 Buttons im 2×2-Grid)

**Layout-Formel (Spielbereich, bottom-anchor, pivot.y=0):**
```
Row1Y = 60    [Buttons untere Reihe]
Row2Y = 136   [Buttons obere Reihe]
WordY = 218   → Word-Oberkante bei 308 px < 309 px Spielfeld-Grenze ✓
WordHeight = 90 px
```

---

## 10. Minispiel09 – Wahr oder Falsch

**Dateien:**
- Script: `Assets/_Project/Scripts/Minigames/Minispiel09.cs`
- Prefab: `Assets/_Project/Resources/PhotonPrefabs/Minispiel09_PrefabRoot.prefab`
- Daten: `Assets/StreamingAssets/minispiel09_fragen.json`

**JSON-Format:**
```json
{ "fragen": [ { "aussage": "...", "antwort": true } ] }
```

**Besonderheiten:**
- WAHR-Button (grün) / FALSCH-Button (rot)
- Score-Untergrenze: −20 (kein weiterer Abzug darunter)
- Seed-RPC-Pattern wie Minispiel07

---

## 11. Wichtige Skripte

### Core & Manager

| Datei | Methode | Beschreibung |
|---|---|---|
| `GameRoomManager.cs` | `Start()` | Liest `MinigameMode`, init PlayerFrames, TaskOwner |
| `GameRoomManager.cs` | `StartMinigame(int index)` | Nur MasterClient; PhotonNetwork.Instantiate + TriggerMinigameStart() |
| `GameRoomManager.cs` | `MasterStartsNextMinigame()` | Rotiert `minigameIndex` 0–8, verbirgt Button danach |
| `GameRoomManager.cs` | `NotifyWinnerToGameManager(int)` | RPC: VIP-Panel anzeigen mit Name + Charakter-Sprite |
| `GameRoomManager.cs` | `AddArcadeWinPoint(int)` | RPC: +1 TotalPoints nur für den Gewinner |
| `GameRoomManager.cs` | `AssignNextTaskOwner()` | Coroutine: TaskOwner rotiert zur nächsten ActorNumber |
| `GameRoomManager.cs` | `SetAufgabenfeldVisible(bool)` | RPC: Aufgabenfeld ein/aus (inkl. Fallback per GameObject.Find) |
| `GameRoomManager.cs` | `AddRoundPointsToTotalForAll()` | RPC: „Points" → „TotalPoints" addieren, Points auf 0 |
| `LobbyManager.cs` | `OnEnterRoomClicked()` | Join-Versuch; bei Fehler → `OnJoinRoomFailed` |
| `LobbyManager.cs` | `OnJoinRoomFailed()` | Erstellt neuen Raum: MaxPlayers=7, MinigameMode-Property |
| `LobbyManager.cs` | `ConfirmSelection()` | Schreibt `CharakterID` in Player Custom Properties |
| `LobbyManager.cs` | `NextCharakter / PreviousCharakter / RandomCharakter` | Charakter-Auswahl in der Lobby |

### Minispiele

| Datei | Methode | Beschreibung |
|---|---|---|
| `MinigameBase.cs` | `TriggerMinigameStart()` | RPC `RpcStartMinigame` an alle Clients |
| `MinigameBase.cs` | `RpcStartMinigame()` | Panel aktiv, localScore=0, SetupGame(), MinigameFlow() |
| `MinigameBase.cs` | `AddScore(int delta)` | localScore ändern + textScore-UI aktualisieren |
| `MinigameBase.cs` | `SubmitScore(RPC)` | Score von Clients → MasterClient |
| `MinigameBase.cs` | `ShowResults(RPC)` | Ergebnis-String + winnerId an alle Clients |
| `MinigameBase.cs` | `CloseMinigame()` | NotifyWinnerToGameManager → Arcade/Normal-Logik → Destroy |
| `Minispiel01–04.cs` | `OnClickButtonPressed()` | AddScore(1) + TextCounter aktualisieren |
| `Minispiel05–06.cs` | `GenerateNewQuestion()` | Neue Rechenaufgabe + 3 Antwort-Buttons |
| `Minispiel07/09.cs` | `TriggerMinigameStart()` | **override** – Seed-RPC senden, dann base aufrufen |
| `Minispiel07.cs` | `ShowCurrentQuestion()` | Frage + 3 zufällig gemischte Antworten anzeigen |
| `Minispiel08.cs` | `GenerateNewRound()` | Stroop-Wort + 4 Farb-Buttons zufällig neu setzen |
| `Minispiel09.cs` | `ShowCurrentQuestion()` | Aussage anzeigen; WAHR/FALSCH-Handler prüfen `antwort` |

### UI & Hilfsskripte

| Datei | Beschreibung |
|---|---|
| `PlayerFrameBehaviour.cs` | Kachel pro Spieler: `nameText`, `pointsText`, `totalPointsText`, `imagePlayer`. `glassButton` nur für lokalen Spieler sichtbar (+1 „Points") |
| `PlayerListAlternatingLayout.cs` | Ordnet Spieler-Kacheln alternierend (Mitte → rechts → links → …), Abstand 250 px |
| `TaskFieldManager.cs` | Holt zufällige Aufgabe aus `AufgabenDatenbank` ScriptableObject |
| `Music.cs` | Singleton-Hintergrundmusik: `PauseMusic()`, `ResumeMusic()`, `isPlaying` |
| `AspectRatioEnforcer.cs` | Camera-Komponente, erzwingt 16:9 per Letterbox/Pillarbox (Camera.rect) |
| `DebugMinigameStarter.cs` | Taste `M` (Editor/Debug-Build): Panel mit Dropdown aller Minispiele, Start nur für MasterClient |
| `InputFieldFocusHandler.cs` | Löscht Platzhaltertext im InputField bei Fokus |
| `SceneLoader.cs` | Szenenwechsel-Hilfsmethoden |

---

## 12. Konventionen für neue Minispiele

### Pflicht-Checkliste

1. **Klasse:** Von `MinigameBase` erben, `SetupGame()`, `StartActualGame()`, `EndActualGame()` implementieren
2. **Prefab:** Unter `Resources/PhotonPrefabs/MinispielXX_PrefabRoot` mit `PhotonView`
3. **Registrierung:** Name in `GameRoomManager.minigamePrefabs`-Array (Code) **und** in der GameRoom-Szene (Inspector)
4. **Serialized Fields binden:** Alle 7 MinigameBase-Felder im Inspector setzen (siehe Abschnitt 7)
5. **Score:** Nur `AddScore(+1/-1)` aufrufen – kein eigenes `localScore`-Feld, kein `UpdateScoreDisplay()`
6. **Beschreibung:** `textBeschreibung.text` in `SetupGame()` setzen (nicht in `StartActualGame()`)
7. **Spielzeit:** `countdownTime = 30f` in `SetupGame()` (Standard 05–09)
8. **Runtime-UI:** Legacy-Elemente `"Button"` und `"TextCounter"` mit `SetActive(false)` ausblenden; **kein** eigenes scoreText-Objekt erstellen

### Layout-Regeln Header (Unity MCP)

```csharp
// TextBeschreibung
rt.anchoredPosition = new Vector2(0f, 170.5f); rt.sizeDelta = new Vector2(780f, 82f);
tmp.enableAutoSizing = false; tmp.fontSize = 20f;

// TextCountdown  ← WICHTIG: autoSize=OFF, sonst Überlappung!
rt.anchoredPosition = new Vector2(0f, 98.5f);  rt.sizeDelta = new Vector2(400f, 50f);
tmp.enableAutoSizing = false; tmp.fontSize = 40f;

// TextScore
rt.anchoredPosition = new Vector2(0f, 54.5f);  rt.sizeDelta = new Vector2(400f, 26f);

// TextResult (in Panelresults)
tmp.enableAutoSizing = false; tmp.fontSize = 18f;
tmp.textWrappingMode = TextWrappingModes.NoWrap; tmp.overflowMode = TextOverflowModes.Overflow;
```

### Mit JSON-Datenbank (Seed-RPC-Pattern)

```csharp
[PunRPC] public void RpcSetSeed(int seed) => randomSeed = seed;

public override void TriggerMinigameStart()   // override, nicht new!
{
    if (PhotonNetwork.IsMasterClient)
    {
        int seed = UnityEngine.Random.Range(0, 99999);
        photonView.RPC(nameof(RpcSetSeed), RpcTarget.All, seed);
    }
    base.TriggerMinigameStart();
}
```

---

## 13. Unity MCP – Prefab-Workflow

Prefabs können vollständig per `Unity_RunCommand` erstellt und bearbeitet werden.

### Prefab duplizieren & Script tauschen
```csharp
AssetDatabase.CopyAsset(srcPath, destPath);
using (var scope = new PrefabUtility.EditPrefabContentsScope(destPath)) {
    var root = scope.prefabContentsRoot;
    var old = root.GetComponent<Minispiel07>();
    // Felder sichern, dann:
    Object.DestroyImmediate(old, true);
    var neu = root.AddComponent<Minispiel08>();
    neu.minigamePanel = /* gesichertes Feld */;
}
AssetDatabase.SaveAssets();
```

### Element per Name finden
```csharp
static GameObject FindDeep(GameObject root, string name) {
    if (root.name == name) return root;
    foreach (Transform child in root.transform) {
        var found = FindDeep(child.gameObject, name);
        if (found != null) return found;
    }
    return null;
}
```

### Alle 9 Prefabs in einem Loop anpassen
```csharp
for (int n = 1; n <= 9; n++) {
    string path = $"Assets/_Project/Resources/PhotonPrefabs/Minispiel0{n}_PrefabRoot.prefab";
    using (var scope = new PrefabUtility.EditPrefabContentsScope(path)) {
        var root = scope.prefabContentsRoot;
        // Änderungen...
    }
}
AssetDatabase.SaveAssets();
```

### GameRoom-Szene per Skript aktualisieren
```csharp
var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
// GameRoomManager finden und Array anpassen
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
EditorSceneManager.CloseScene(scene, false);
```

---

## 14. TMP Font – Umlaut-Support

- Alle TMP FontAssets wurden per `TryAddCharacters("äöüÄÖÜß")` erweitert
- `LiberationSans SDF` ist Default-Fallback und hat alle deutschen Zeichen
- Neue Fonts: `Unity_RunCommand` mit `font.TryAddCharacters("äöüÄÖÜß")` ausführen
- **Kein Emoji-Support** im verwendeten Font → nur ASCII/Latein verwenden

---

## 15. Steam Deploy (Workflow)

1. **Unity:** Version erhöhen (`PlayerSettings.bundleVersion`), Dev Build deaktivieren, Clean Build (`BuildOptions.CleanBuildCache`) nach `Build/CoronaBar.exe`
2. **Dateien kopieren:** `Build\` → `Build\steamworks_sdk_162\sdk\tools\ContentBuilder\content\GameBuild\` — **ohne** `CoronaBar_BurstDebugInformation_DoNotShip` und `steamworks_sdk_162`
3. **SteamPipeGUI:** `Build\steamworks_sdk_162\sdk\tools\SteamPipeGUI\SteamPipeGUI.exe`
   - App ID: `3950630`
   - Content Builder Path: `...\ContentBuilder`
   - Depot Path: `...\ContentBuilder\content\GameBuild`
   - „Generate VDF's" → „Upload" → Steam 2FA bestätigen
4. **SteamWorks:** [partner.steamgames.com/apps/builds/3950630](https://partner.steamgames.com/apps/builds/3950630) → Build als Default setzen

---

## 16. Git-Workflow

```
feature/fix → Commit → Push → PR → Squash-Merge → Tag vX.Y → GitHub Release
```

- Branch-Namensschema: `feat/`, `fix/`, `chore/`, `release/`
- Tag nach Deploy: `git tag -a vX.Y -m "..."` + `git push origin vX.Y`
- Commit-Suffix: `Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>`
