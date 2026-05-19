using UnityEngine;
using TMPro; // Required for TextMeshPro components
using UnityEngine.EventSystems;

public class InputFieldFocusHandler : MonoBehaviour, ISelectHandler
{
    public TMP_InputField inputField; // Changed to TMP_InputField
    public TMP_Text placeholderText; // Changed to TMP_Text

    void Start()
    {
        if (inputField != null)
        {
            // TMP_InputField does not have shouldActivateOnSelect directly.
            // Its activation is handled by its EventSystem interaction.
            // Ensure it's interactable.
            inputField.interactable = true;
        }
        else
        {
            Debug.LogError("TMP_InputField not assigned in InputFieldFocusHandler on " + gameObject.name);
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (placeholderText != null)
        {
            // For TMP_Text, the text property is directly accessible.
            placeholderText.text = "";
        }
        else
        {
            Debug.LogError("Placeholder Text (TMP_Text) not assigned in InputFieldFocusHandler on " + gameObject.name);
        }
    }
}