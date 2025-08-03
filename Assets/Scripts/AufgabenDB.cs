using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AufgabenDatenbank", menuName = "Spiel/AufgabenDatenbank", order = 2)]
public class AufgabenDatenbank : ScriptableObject
{
    public List<Aufgabe> aufgabenListe;  // Liste aller Aufgaben
}
