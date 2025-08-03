using UnityEngine;

[CreateAssetMenu(fileName = "NeueAufgabe", menuName = "Spiel/Aufgabe", order = 1)]
public class Aufgabe : ScriptableObject
{
    [TextArea(2, 5)]  // Textbereich im Inspector mit mehrzeiliger Eingabe
    public string aufgabenText;  // Der Text der Aufgabe
}