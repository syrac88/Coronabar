using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;   // Für LINQ: Distinct

public class AufgabenImporter : EditorWindow
{
    [TextArea(10, 20)]
    public string aufgabenTextListe;

    public string saveFolder = "Assets/Aufgaben";
    public string aufgabenDBAssetName = "AufgabenDB"; // Name ohne .asset

    [MenuItem("Tools/Aufgaben Importer")]
    public static void ShowWindow()
    {
        var window = GetWindow<AufgabenImporter>("Aufgaben Importer");
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Füge hier deine Aufgaben ein (jede Aufgabe eine neue Zeile):");

        aufgabenTextListe = EditorGUILayout.TextArea(aufgabenTextListe, GUILayout.Height(200));

        saveFolder = EditorGUILayout.TextField("Speicherordner", saveFolder);

        aufgabenDBAssetName = EditorGUILayout.TextField("AufgabenDB Name", aufgabenDBAssetName);

        if (GUILayout.Button("Aufgaben importieren und Assets anlegen"))
        {
            ImportiereAufgaben();
        }
    }

    void ImportiereAufgaben()
    {
        if (string.IsNullOrEmpty(aufgabenTextListe))
        {
            Debug.LogError("Keine Aufgaben gefunden!");
            return;
        }

        string[] aufgaben = aufgabenTextListe
            .Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();

        if (!AssetDatabase.IsValidFolder(saveFolder))
        {
            Debug.Log($"Ordner {saveFolder} existiert nicht, erstelle...");
            Directory.CreateDirectory(saveFolder);
            AssetDatabase.Refresh();
        }

        // Liste der erzeugten Aufgaben für spätere Zuweisung
        var neuErstellteAufgaben = new System.Collections.Generic.List<Aufgabe>();
        int startCount = 0;
        // Prüfe bestehende Dateien, damit keine Indexüberschneidungen
        while (File.Exists($"{saveFolder}/Aufgabe_{startCount}.asset")) startCount++;

        int count = 0;
        foreach (string aufgabeText in aufgaben)
        {
            Aufgabe neueAufgabe = ScriptableObject.CreateInstance<Aufgabe>();
            neueAufgabe.aufgabenText = aufgabeText;

            string assetPath = $"{saveFolder}/Aufgabe_{startCount + count}.asset";
            AssetDatabase.CreateAsset(neueAufgabe, assetPath);
            neuErstellteAufgaben.Add(neueAufgabe);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // --- AufgabenDB laden und Aufgabenliste erweitern ---
        // Suche Pfad zur AufgabenDB-Datei
        string[] dbPfadArray = AssetDatabase.FindAssets(aufgabenDBAssetName + " t:AufgabenDatenbank", new[] { saveFolder });
        if (dbPfadArray.Length > 0)
        {
            string dbAssetPath = AssetDatabase.GUIDToAssetPath(dbPfadArray[0]);
            var aufgabenDB = AssetDatabase.LoadAssetAtPath<AufgabenDatenbank>(dbAssetPath);
            if (aufgabenDB != null)
            {
                // Neue Aufgaben anhängen
                aufgabenDB.aufgabenListe.AddRange(neuErstellteAufgaben);
                // Optional: Doppelte vermeiden, falls du das willst!
                aufgabenDB.aufgabenListe = aufgabenDB.aufgabenListe.Distinct().ToList();
                EditorUtility.SetDirty(aufgabenDB);
                AssetDatabase.SaveAssets();
                Debug.Log($"{count} Aufgaben importiert und AufgabenDB aktualisiert ({aufgabenDB.aufgabenListe.Count} Aufgaben insgesamt).");
            }
            else
            {
                Debug.LogWarning("AufgabenDB nicht gefunden!");
            }
        }
        else
        {
            Debug.LogWarning("Kein AufgabenDB Asset im Zielordner gefunden – AufgabenDB muss im selben Ordner liegen oder passe 'saveFolder' entsprechend an.");
        }
    }
}
