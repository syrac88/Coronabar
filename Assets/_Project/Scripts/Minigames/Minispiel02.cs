using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Minispiel02 : MinigameBase
{
    [Header("Spielspezifische UI")]
    public Button clickButton;
    public TMP_Text TextCounter;

    [Header("Einstellungen")]
    [SerializeField] private float fixedX_A = -270f;
    [SerializeField] private float fixedX_B = 270f;

    private int localClicks = 0;
    private bool toggleX = false;

    protected override void SetupGame()
    {
        localClicks = 0;
        toggleX = false;
        clickButton.gameObject.SetActive(false);

        if (TextCounter != null)
        {
            TextCounter.gameObject.SetActive(false);
            TextCounter.text = "0";
        }

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

    protected override float GetLocalPlayerScore()
    {
        return localClicks;
    }

    private void OnClickButtonPressed()
    {
        if (!gameRunning) return;

        localClicks++;
        if (TextCounter != null) TextCounter.text = localClicks.ToString();

        RectTransform buttonRect = clickButton.GetComponent<RectTransform>();
        Vector2 pos = buttonRect.anchoredPosition;

        toggleX = !toggleX;
        pos.x = toggleX ? fixedX_A : fixedX_B;

        buttonRect.anchoredPosition = pos;
    }
}