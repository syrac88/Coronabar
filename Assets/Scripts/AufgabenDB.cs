using UnityEngine;
using System.Collections.Generic;
using System.IO;

[CreateAssetMenu(fileName = "AufgabenDatenbank", menuName = "Spiel/AufgabenDatenbank", order = 2)]
public class AufgabenDatenbank : ScriptableObject
{
    public List<Aufgabe> aufgabenListe = new List<Aufgabe>();

    [ContextMenu("Liste in JSON exportieren")]
    public void ExportToJson()
    {
        // Pfad: Assets/StreamingAssets/aufgaben.json
        string folderPath = Application.streamingAssetsPath;
        string filePath = Path.Combine(folderPath, "aufgaben.json");

        // Ordner erstellen, falls er nicht existiert
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Wrapper erstellen und in JSON umwandeln (true = schönes Format)
        AufgabenWrapper wrapper = new AufgabenWrapper { items = aufgabenListe };
        string jsonContent = JsonUtility.ToJson(wrapper, true);

        File.WriteAllText(filePath, jsonContent);
        
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh(); // Unity zeigen, dass eine neue Datei da ist
        #endif

        Debug.Log("<color=green>JSON erfolgreich exportiert nach: </color>" + filePath);
    }

    [ContextMenu("Aus JSON laden")]
    public void LoadFromJson()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "aufgaben.json");
        
        if (File.Exists(filePath))
        {
            string jsonContent = File.ReadAllText(filePath);
            AufgabenWrapper wrapper = JsonUtility.FromJson<AufgabenWrapper>(jsonContent);
            aufgabenListe = wrapper.items;
            Debug.Log("<color=blue>Aufgaben erfolgreich aus JSON geladen!</color>");
        }
        else
        {
            Debug.LogError("JSON Datei nicht gefunden unter: " + filePath);
        }
    }
}

// Hilfsklasse für das JSON-Format
[System.Serializable]
public class AufgabenWrapper
{
    public List<Aufgabe> items;
}