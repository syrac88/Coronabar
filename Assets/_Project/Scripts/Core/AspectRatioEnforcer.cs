using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AspectRatioEnforcer : MonoBehaviour
{
    [Header("Target Aspect Ratio")]
    public float targetWidth = 16f;
    public float targetHeight = 9f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        UpdateCameraAspect();
    }

    // Wenn du das Skalieren live im Editor/Spiel testen willst, 
    // muss die Methode in der Update() stehen. 
    // Für das finale Spiel reicht oft auch Start() + ein Listener für Resolution-Changes.
    void Update()
    {
        UpdateCameraAspect();
    }

    private void UpdateCameraAspect()
    {
        // Ziel-Verhältnis (z.B. 16:9 = 1.777)
        float targetAspect = targetWidth / targetHeight;
        
        // Aktuelles Fenster-Verhältnis
        float windowAspect = (float)Screen.width / (float)Screen.height;
        
        // Skalierungsfaktor berechnen
        float scaleHeight = windowAspect / targetAspect;

        // Wenn das Fenster breiter ist als 16:9 (Letterbox - Balken oben/unten)
        if (scaleHeight < 1.0f)
        {
            Rect rect = cam.rect;
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
            cam.rect = rect;
        }
        // Wenn das Fenster schmaler ist als 16:9 (Pillarbox - Balken links/rechts)
        else
        {
            float scaleWidth = 1.0f / scaleHeight;
            Rect rect = cam.rect;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
            cam.rect = rect;
        }
    }
}