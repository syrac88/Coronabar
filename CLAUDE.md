🍻 Projektbeschreibung: Coronabar
Projekt-Repository: https://github.com/syrac1988/Coronabar
Genre: Multiplayer-Party-/Trinkspiel
Engine: Unity 2D (Universal Render Pipeline - URP)
Networking: Photon Unity Networking 2 (PUN 2)
Sprache: C#

📖 1. Spielkonzept & Kernschleife (Core Loop)
"Coronabar" ist ein digitales Multiplayer-Partyspiel, das klassische Brett- und Trinkspielmechaniken mit Minispielen kombiniert.
Lobby: Spieler treten einem Raum bei. Hier kann der MasterClient den "Minispiel-Modus" (Arcade-Modus) per Checkbox aktivieren.
Modi:
- Normaler Modus: Klassischer Ablauf mit Aufgaben-Phase und automatischen Minispielen nach X Aufgaben.
- Arcade-Modus: Aufgaben und Aufgabenfeld sind deaktiviert. Der MasterClient steuert den Start der Minispiele manuell über einen speziellen Button.
GameRoom: Die Spieler sitzen in einer virtuellen Bar.
Auswertung: Gewinner werden auf einem "VIP Panel" präsentiert.

🛠 2. Technische Architektur & Networking (PUN 2)
Zustandssynchronisation erfolgt primär über Photon Custom Properties (Hashtables) und RPCs.
Room Custom Properties:
- TaskOwner, TaskIndex, TaskStatus: Steuern den Fortschritt im Normal-Modus.
- MinigameMode: Ein bool-Wert, der den Spielmodus festlegt und im GameRoomManager die UI-Logik steuert.
Autorität: Der MasterClient verwaltet den Spielstatus, den Minispiel-Loop und die Arcade-Button-Sichtbarkeit.

📂 3. Ordner- und Dateistruktur (Highlights)
- _Project/Data – ScriptableObjects (z. B. Aufgaben)
- _Project/Images – Sprites, UI-Grafiken
- _Project/Prefabs – allgemeine Prefabs
- _Project/Resources/PhotonPrefabs – Minispiel-Prefabs für PhotonNetwork.Instantiate
- _Project/Scenes – Lobby, GameRoom
- _Project/Scripts/Core – GameRoomManager, LobbyManager, SceneLoader
- _Project/Scripts/Minigames – MinigameBase, Minispiel01–08

🎮 4. Minispiele (Übersicht)
Alle Minispiele erben von MinigameBase und werden als Photon-Prefabs unter Resources/PhotonPrefabs/ instanziiert.
Registriert in GameRoomManager.minigamePrefabs (Reihenfolge = Index 1–8):
1. Minispiel01_PrefabRoot – Klick-Zähler (Button in der Mitte)
2. Minispiel02_PrefabRoot – Klick-Button wechselt X-Position (links/rechts)
3. Minispiel03_PrefabRoot – Klick-Button zufällige X-Position
4. Minispiel04_PrefabRoot – Klick-Button zufällige X/Y-Position
5. Minispiel05_PrefabRoot – Mathe-Duell (einstellige Zahlen)
6. Minispiel06_PrefabRoot – Mathe-Duell (zweistellig + Division)
7. Minispiel07_PrefabRoot – Quiz: Allgemeinwissen (numerische Antworten, JSON-Datenbank)
8. Minispiel08_PrefabRoot – Stroop-Effekt (Farbwort in anderer Farbe, Tintenfarbe tippen)

Gemeinsamer Ablauf (MinigameBase.MinigameFlow):
Vor-Countdown (3 s) → Spielphase (20 s, countdownTime) → EndActualGame() → GetLocalPlayerScore() → SubmitScore (RPC an Master) → ShowResults → CloseMinigame.
Gewinner: Höchste Punktzahl (WinConditionType.HighestScoreWins), außer Child-Klasse überschreibt.

📄 5. Minispiel07 – Quiz Allgemeinwissen (JSON-Datenbank)
Skript: Assets/_Project/Scripts/Minigames/Minispiel07.cs
Prefab: Assets/_Project/Resources/PhotonPrefabs/Minispiel07_PrefabRoot.prefab
Daten: Assets/StreamingAssets/minispiel07_fragen.json

Spielkonzept:
- Quiz-Fragen mit numerischen Antworten aus JSON-Datenbank.
- 3 Antwort-Buttons (eine richtig, zwei falsch).
- Richtig klicken: +1 Punkt. Falsch klicken: -1 Punkt.
- Seed-RPC-Pattern: MasterClient sendet Seed vor TriggerMinigameStart() → deterministische Fragereihenfolge auf allen Clients.

📄 6. Minispiel08 – Stroop-Effekt
Skript: Assets/_Project/Scripts/Minigames/Minispiel08.cs
Prefab: Assets/_Project/Resources/PhotonPrefabs/Minispiel08_PrefabRoot.prefab

Spielkonzept:
- Ein Farbwort (z. B. „GELB”) erscheint in einer anderen Farbe (z. B. Rot).
- 4 Antwort-Buttons (2×2 Grid): jeder zeigt einen Farbnamen in der jeweiligen Farbe.
- Spieler muss die TINTENFARBE antippen, nicht die Wortbedeutung.
- Richtig: +1 Punkt. Falsch: -1 Punkt. Sofort neue Runde nach jedem Klick.
- 5 Farben: ROT, BLAU, GRÜN, GELB, LILA.

UI-Layout (Runtime-Generierung):
- Feste Prefab-Elemente (oben): Titel → Countdown → InfoText (kompakt, je ~36–54 px hoch)
- Runtime-Elemente (unten, Anker = Panel-Unterkante): Score → Stroop-Wort → Button-Reihe 2 → Button-Reihe 1
- BottomOffset = 60px (Pflichtabstand zum unteren Rand für den Balken unter der Karte)
- TextCloseCountdown: Unterkante bei 60px vom Rand → y_center = -Panel_Halbhöhe + 60 + TextHöhe/2

Layout-Formel (Y-Positionen, Anker unten-mitte, pivot.y=0):
  Row1Y  = BottomOffset            (= 60)
  Row2Y  = Row1Y + ButtonH + Gap   (= 136)
  WordY  = Row2Y + ButtonH + 20    (= 218)
  ScoreY = WordY + WordH + 14      (= 332)

📄 7. Wichtige Skripte und ihre Funktionen
LobbyManager.cs: toggleMinigameMode – beim Raumerstellen in CustomRoomProperties (MinigameMode) speichern.
GameRoomManager.cs: Zentrale Instanz. Start() prüft Modus, blendet aufgabenfeldInstance oder masterMinigameButton. Steuert minigameIndex und StartMinigame() via Photon. minigamePrefabs enthält alle 8 Minispiel-Prefab-Namen.
MinigameBase.cs: Basis für Minispiele. CloseMinigame(): Arcade → masterMinigameButton + AddArcadeWinPoint; Normal → AssignNextTaskOwner + Aufgabenfeld.
DebugMinigameStarter.cs: Debug-Panel (Taste 'M'), Dropdown aus manager.minigamePrefabs, OnStartButtonClicked() startet gewähltes Minispiel (nur MasterClient).
InputFieldFocusHandler.cs: Behandelt den Fokus von InputFields, löscht Platzhaltertext bei Auswahl.

UI & Layout
PlayerFrameBehaviour.cs: Glas-Button (lokale Punkte), Spieler-Infos.
PlayerListAlternatingLayout.cs: Position der Spieler-Kacheln in der Bar.

💡 8. Besonderheiten & Arcade-Modus
Modus-Umschaltung: Spielmodus Lobby → GameRoom über CustomRoomProperties (MinigameMode).
Sicherheits-Logik: masterMinigameButton im Normal-Modus SetActive(false). MasterStartsNextMinigame() nur mit IsMasterClient.
Arcade-Gewinn: AddArcadeWinPoint-RPC für den Minispiel-Gewinner.

🛠 9. Entwickler-Tools & Debugging
Debug-Panel: Taste 'M' (Editor/Debug-Build). Dropdown listet alle Einträge aus minigamePrefabs – inkl. Minispiel 08.
Logik-Validierung: Modus-Check in GameRoomManager.Start().
Unity MCP: Console auslesen über Unity_GetConsoleLogs (Project Settings → AI → Unity MCP freigeben).

🤖 10. Unity MCP – Prefab-Workflow (gelernt bei Minispiel08)
Prefabs können vollständig per Unity_RunCommand erstellt und bearbeitet werden – kein manuelles Klicken im Editor nötig.

Prefab duplizieren & Script tauschen:
  AssetDatabase.CopyAsset(srcPath, destPath);
  using (var scope = new PrefabUtility.EditPrefabContentsScope(destPath)) {
      var old = root.GetComponent<MinispielXX>();
      // Felder sichern, dann:
      Object.DestroyImmediate(old, true);
      var neu = root.AddComponent<MinispielYY>();
      neu.minigamePanel = ...; // gesicherte Felder zuweisen
  }
  AssetDatabase.SaveAssets();

Prefab-Elemente positionieren:
  using (var scope = new PrefabUtility.EditPrefabContentsScope(path)) {
      var rt = root.transform.Find("Minigame_04/TextMinispiel").GetComponent<RectTransform>();
      rt.anchoredPosition = new Vector2(0f, 240f);
      rt.sizeDelta = new Vector2(780f, 36f);
  }

GameRoom-Szene per Skript aktualisieren:
  var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
  // GameRoomManager finden, Array anpassen
  EditorSceneManager.MarkSceneDirty(scene);
  EditorSceneManager.SaveScene(scene);
  EditorSceneManager.CloseScene(scene, false);

Core & Manager
| Datei | Funktion | Beschreibung |
| GameRoomManager.cs | Start() | Initialisiert Bar, Modus-Prüfung, Spieler-UI |
| GameRoomManager.cs | MasterStartsNextMinigame() | Nächstes Minispiel (Arcade), rotiert Index 0–6 |
| GameRoomManager.cs | StartMinigame(int index) | Instanziiert PhotonPrefabs/MinispielXX_PrefabRoot |
| GameRoomManager.cs | SetAufgabenfeldVisible() | RPC: Aufgabenfeld ein/aus |
| GameRoomManager.cs | NotifyWinnerToGameManager() | RPC: VIP-Panel mit Sieger |
| GameRoomManager.cs | AssignNextTaskOwner() | Coroutine: TaskOwner wechseln |
| GameRoomManager.cs | AddRoundPointsToTotalForAll() | RPC: Rundenpunkte ins Gesamtkonto |
| TaskFieldManager.cs | HoleZufaelligeAufgabe() | Zufällige Aufgabe aus ScriptableObject |
| LobbyManager.cs | OnEnterRoomClicked() | Raum beitreten/erstellen |
| LobbyManager.cs | OnJoinRoomFailed() | Neuer Raum mit MinigameMode-Property |
| LobbyManager.cs | ConfirmSelection() | CharakterID in Player Properties |
| LobbyManager.cs | UpdatePreview() | Charakter-Vorschaubild |
| SceneLoader.cs | LoadLobbyScene() | Szenenwechsel |

Minigames (Base & Logic)
| Datei | Funktion | Beschreibung |
| MinigameBase.cs | TriggerMinigameStart() | RPC RpcStartMinigame an alle Clients |
| MinigameBase.cs | RpcStartMinigame() | Panel aktiv, SetupGame(), MinigameFlow() |
| MinigameBase.cs | MinigameFlow() | Countdown → Spiel → Auswertung → Ergebnisse |
| MinigameBase.cs | SubmitScore() | RPC: Score an MasterClient |
| MinigameBase.cs | ShowResults() | RPC: Ergebnis-Tabelle |
| MinigameBase.cs | CloseMinigame() | Arcade-Button oder nächste Aufgabe; Destroy |
| Minispiel01–04.cs | SetupGame / StartActualGame / EndActualGame / GetLocalPlayerScore | Klick-Minispiele mit Inspector-UI (Button, TextCounter) |
| Minispiel05.cs | EnsureGameUI() | Runtime-UI: Aufgabe, Punkte, 3 braune Antwort-Buttons |
| Minispiel05.cs | GenerateNewQuestion() | Aufgabe + Antworten; sofort nach Klick erneuern |
| Minispiel05.cs | SetGameplayUIVisible() | Aufgabe, Punkte, Buttons bei Spielende ausblenden |


UI & Hilfsklassen
| Datei | Funktion | Beschreibung |
| PlayerFrameBehaviour.cs | – | ActorNumber, Glas-Button, Spieler-UI |
| PlayerListAlternatingLayout.cs | ArrangePlayerFrames() | Layout der Spieler-Avatare |
| Music.cs | ToggleAudio() | Singleton Hintergrundmusik |
| DebugMinigameStarter.cs | OnStartButtonClicked() | Forciert Minispiel-Start (Master) |
| InputFieldFocusHandler.cs | OnSelect() | Löscht Platzhaltertext bei Fokus des InputFields |

🔧 11. Konventionen für neue Minispiele
- Von MinigameBase erben, abstrakte Methoden implementieren.
- Prefab unter Resources/PhotonPrefabs/ mit PhotonView; Name in GameRoomManager.minigamePrefabs (Code-Default) UND in der GameRoom-Szene (Inspector) ergänzen.
- Prefab muss minigamePanel, countdownText, infoText, resultsPanel, resultsText, TextCloseCountdown zuweisen (MinigameBase-Felder).
- Score lokal zählen, am Ende nur GetLocalPlayerScore() – Synchronisation über MinigameBase.SubmitScore.
- Bei Runtime-UI: Legacy-Prefab-Elemente ausblenden; EndActualGame() Spiel-UI verstecken, wenn Ergebnis-Panel kommt.

Layout-Regeln für Runtime-UI (gelernt Minispiel05–08):
- WICHTIG: Unterer Rand = 60px freilassen (physischer Balken unterhalb der Spielkarte).
- TextCloseCountdown-Formel: y_center = -(Panel_Halbhöhe) + 60 + (TextHöhe/2)
  → Bei Panel 535px hoch: y = -267.6 + 60 + 20 = -188
- Feste Prefab-Elemente (Titel, Countdown, InfoText) OBEN kompakt anordnen (Mitte bei y=240/183/125).
  Dadurch bleibt der untere 2/3-Bereich für Runtime-Elemente frei.
- Runtime-Elemente von unten nach oben aufbauen (anchorMin/Max=(0.5,0), pivot=(0.5,0)):
  BottomOffset=60 → Row1 → Row2 → Spielelement → Score
- InfoText in StartActualGame() überschreiben (nicht SetupGame, dort überschreibt die Base-Klasse es wieder).
- Prefab per Unity MCP erstellen (siehe Abschnitt 10) – kein manuelles Klicken nötig.