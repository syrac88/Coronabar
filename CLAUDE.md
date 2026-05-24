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
- _Project/Scripts/Minigames – MinigameBase, Minispiel01–07

🎮 4. Minispiele (Übersicht)
Alle Minispiele erben von MinigameBase und werden als Photon-Prefabs unter Resources/PhotonPrefabs/ instanziiert.
Registriert in GameRoomManager.minigamePrefabs (Reihenfolge = Index 1–7):
1. Minispiel01_PrefabRoot – Klick-Zähler (Button in der Mitte)
2. Minispiel02_PrefabRoot – Klick-Button wechselt X-Position (links/rechts)
3. Minispiel03_PrefabRoot – Klick-Button zufällige X-Position
4. Minispiel04_PrefabRoot – Klick-Button zufällige X/Y-Position
5. Minispiel05_PrefabRoot – Mathe-Duell (einstellige Zahlen)

Gemeinsamer Ablauf (MinigameBase.MinigameFlow):
Vor-Countdown (3 s) → Spielphase (20 s, countdownTime) → EndActualGame() → GetLocalPlayerScore() → SubmitScore (RPC an Master) → ShowResults → CloseMinigame.
Gewinner: Höchste Punktzahl (WinConditionType.HighestScoreWins), außer Child-Klasse überschreibt.

📄 5. Minispiel07 – Mathe-Duell (Dynamische Schriftgröße)
Skript: Assets/_Project/Scripts/Minigames/Minispiel07.cs
Prefab: Assets/_Project/Resources/PhotonPrefabs/Minispiel07_PrefabRoot.prefab

Spielkonzept:
- Rechnen unter Zeitdruck: Aufgabe in der Mitte (Zahlen 1–99, Operatoren +, -, x).
- 3 Antwort-Buttons mit je einer Zahl (eine richtig, zwei falsch).
- Richtig klicken: +1 Punkt. Falsch klicken: -1 Punkt.
- Nach jedem Klick sofort neue Aufgabe.
- Subtraktion: größere Zahl minus kleinere (keine negativen Ergebnisse).
- Am Ende: lokaler Score via GetLocalPlayerScore() an MasterClient (wie alle Minispiele).

UI-Besonderheiten (Runtime-Generierung, wenig Inspector-Zuweisung):
- Spiel-UI wird in EnsureGameUI() per Code unter minigamePanel erzeugt (Container „MatheDuellContainer“).
- Legacy-Elemente aus dem kopierten Prefab (Button, TextCounter) werden in HideLegacyPrefabElements() ausgeblendet.
- Layout von unten nach oben (Anker unten mittig):
  - 3 Antwort-Buttons nebeneinander, braun, 55 px vom unteren Kartenrand.
  - Aufgaben-Text 10 px über den Buttons (QuestionToButtonGap).
  - Punkte-Anzeige 24 px über der Aufgabe (ScoreToQuestionGap).
- SetGameplayUIVisible(bool): blendet Aufgabe, Punkte und alle 3 Buttons ein/aus.
- EndActualGame(): SetGameplayUIVisible(false) – Aufgabe, Punkte und Buttons verschwinden bei „Auswertung…“.
- StartActualGame() / SetupGame(): SetGameplayUIVisible(true).
- Dynamische Schriftgröße: Die Schriftgröße der Aufgaben wird automatisch angepasst, um lange Fragen in einer Zeile darzustellen.

Wichtige Methoden (Minispiel07):
| Methode | Beschreibung |
| GenerateNewQuestion() | Zufällige Aufgabe + 3 gemischte Antworten |
| OnAnswerButtonClicked() | Prüft Antwort, passt localScore an, neue Aufgabe |
| EnsureGameUI() | Einmaliges Erzeugen von Texten und Buttons |
| SetGameplayUIVisible() | Sichtbarkeit der Spiel-UI steuern |
| GetLocalPlayerScore() | Gibt localScore als float zurück |
| AdjustTextSize() | Passt die Schriftgröße dynamisch an die Länge der Frage an |

📄 6. Wichtige Skripte und ihre Funktionen
LobbyManager.cs: toggleMinigameMode – beim Raumerstellen in CustomRoomProperties (MinigameMode) speichern.
GameRoomManager.cs: Zentrale Instanz. Start() prüft Modus, blendet aufgabenfeldInstance oder masterMinigameButton. Steuert minigameIndex und StartMinigame() via Photon. minigamePrefabs enthält alle 7 Minispiel-Prefab-Namen.
MinigameBase.cs: Basis für Minispiele. CloseMinigame(): Arcade → masterMinigameButton + AddArcadeWinPoint; Normal → AssignNextTaskOwner + Aufgabenfeld.
DebugMinigameStarter.cs: Debug-Panel (Taste 'M'), Dropdown aus manager.minigamePrefabs, OnStartButtonClicked() startet gewähltes Minispiel (nur MasterClient).
InputFieldFocusHandler.cs: Behandelt den Fokus von InputFields, löscht Platzhaltertext bei Auswahl.

UI & Layout
PlayerFrameBehaviour.cs: Glas-Button (lokale Punkte), Spieler-Infos.
PlayerListAlternatingLayout.cs: Position der Spieler-Kacheln in der Bar.

💡 7. Besonderheiten & Arcade-Modus
Modus-Umschaltung: Spielmodus Lobby → GameRoom über CustomRoomProperties (MinigameMode).
Sicherheits-Logik: masterMinigameButton im Normal-Modus SetActive(false). MasterStartsNextMinigame() nur mit IsMasterClient.
Arcade-Gewinn: AddArcadeWinPoint-RPC für den Minispiel-Gewinner.

🛠 8. Entwickler-Tools & Debugging
Debug-Panel: Taste 'M' (Editor/Debug-Build). Dropdown listet alle Einträge aus minigamePrefabs – inkl. Minispiel 07.
Logik-Validierung: Modus-Check in GameRoomManager.Start().
Unity MCP: Console auslesen über Unity_GetConsoleLogs (Project Settings → AI → Unity MCP freigeben).

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

🔧 10. Konventionen für neue Minispiele
- Von MinigameBase erben, abstrakte Methoden implementieren.
- Prefab unter Resources/PhotonPrefabs/ mit PhotonView; Name in GameRoomManager.minigamePrefabs und GameRoom-Szene ergänzen.
- Prefab muss minigamePanel, countdownText, infoText, resultsPanel, resultsText, TextCloseCountdown zuweisen (MinigameBase-Felder).
- Score lokal zählen, am Ende nur GetLocalPlayerScore() – Synchronisation über MinigameBase.SubmitScore.
- Bei Runtime-UI: Legacy-Prefab-Elemente ausblenden; EndActualGame() Spiel-UI verstecken, wenn Ergebnis-Panel kommt.