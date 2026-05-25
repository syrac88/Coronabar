using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Minispiel02 : MinigameBase
{
    [Header("Spielspezifische UI")]
    public Button   clickButton;
    public TMP_Text TextCounter;

    [Header("Einstellungen")]
    [SerializeField] private float fixedX_A = -270f;
    [SerializeField] private float fixedX_B =  270f;

    private bool toggleX = false;

    protected override void SetupGame()
    {
        countdownTime = 20f;
        localScore    = 0;
        toggleX       = false;

        if (textBeschreibung != null)
            textBeschreibung.text = "Der Knopf springt links/rechts – klicke ihn so oft du kannst!";

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

        RectTransform rect = clickButton.GetComponent<RectTransform>();
        Vector2 pos        = rect.anchoredPosition;
        toggleX            = !toggleX;
        pos.x              = toggleX ? fixedX_A : fixedX_B;
        rect.anchoredPosition = pos;
    }
}
