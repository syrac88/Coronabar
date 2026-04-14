using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class AufgabenImporter : EditorWindow
{
    [TextArea(10, 20)]
    public string aufgabenTextListe;

    public string aufgabenDBAssetName = "AufgabenDB"; // Der Name deiner Datenbank-Datei

    [MenuItem("Tools/Aufgaben Importer")]
    public static void ShowWindow()
    {
        var window = GetWindow<AufgabenImporter>("Aufgaben Importer");
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Neue Aufgaben einfügen (pro Zeile eine Aufgabe):", EditorStyles.boldLabel);

        aufgabenTextListe = EditorGUILayout.TextArea(aufgabenTextListe, GUILayout.Height(200));

        aufgabenDBAssetName = EditorGUILayout.TextField("Datenbank Name", aufgabenDBAssetName);

        if (GUILayout.Button("In Datenbank importieren"))
        {
            ImportiereAufgabenInDB();
        }
    }

    void ImportiereAufgabenInDB()
    {
        if (string.IsNullOrEmpty(aufgabenTextListe)) return;

        // 1. Finde das Datenbank-Asset im Projekt
        string[] guids = AssetDatabase.FindAssets(aufgabenDBAssetName + " t:AufgabenDatenbank");
        if (guids.Length == 0)
        {
            Debug.LogError("Fehler: Die Datenbank '" + aufgabenDBAssetName + "' wurde nicht gefunden!");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        AufgabenDatenbank db = AssetDatabase.LoadAssetAtPath<AufgabenDatenbank>(path);

        if (db == null) return;

        // 2. Texte in Zeilen zerlegen
        string[] zeilen = aufgabenTextListe.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        // 3. Änderungen für Unity registrieren (damit man sie speichern kann)
        Undo.RecordObject(db, "Import Aufgaben");

        foreach (string zeile in zeilen)
        {
            string getrimmt = zeile.Trim();
            if (string.IsNullOrEmpty(getrimmt)) continue;

            // 4. Neue Aufgabe erstellen (ALS KLASSE, nicht mehr als Asset!)
            Aufgabe neueAufgabe = new Aufgabe();
            neueAufgabe.id = db.aufgabenListe.Count;
            neueAufgabe.textDE = getrimmt;
            neueAufgabe.textEN = ""; // Erstmal leer lassen

            // 5. Der entscheidende Punkt: HIER muss "db." davor stehen!
            db.aufgabenListe.Add(neueAufgabe);
        }

        // 6. Speichern
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        
        Debug.Log(zeilen.Length + " Aufgaben zur Datenbank hinzugefügt.");
        aufgabenTextListe = ""; // Feld leeren
    }
}