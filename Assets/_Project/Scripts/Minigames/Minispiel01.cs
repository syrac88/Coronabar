using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Minispiel01 : MinigameBase
{
    [Header("Spielspezifische UI")]
    public Button    clickButton;
    public TMP_Text  TextCounter;    // Zeigt Klick-Anzahl im Spielbereich (groß)

    protected override void SetupGame()
    {
        countdownTime = 20f;
        localScore    = 0;

        if (textBeschreibung != null)
            textBeschreibung.text = "Klicke so oft du kannst auf den roten Knopf!";

        clickButton.gameObject.SetActive(false);
        if (TextCounter != null) { TextCounter.gameObject.SetActive(false); TextCounter.text = "0"; }

        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(OnClickButtonPressed);
    }

    protected override void StartActualGame()
    {
        clickButton.gameObject.SetActive(true);
        if (TextCounter != null) TextCounter.gameObject.SetActive(true);
    }

    protected override void EndActualGame()
    {
        clickButton.gameObject.SetActive(false);
        if (TextCounter != null) TextCounter.gameObject.SetActive(false);
    }

    private void OnClickButtonPressed()
    {
        if (!gameRunning) return;
        AddScore(1);
        if (TextCounter != null) TextCounter.text = localScore.ToString();
    }
}
