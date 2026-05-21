using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenExtension : Singleton<ScreenExtension>
{
    [HideInInspector]
    public float screenHeight;
    [HideInInspector]
    public float screenWidth;

    Rect safeArea;
    [HideInInspector]
    public Vector2 minAnchor;
    [HideInInspector]
    public Vector2 maxAnchor;
    [HideInInspector]
    public float safeAreaYBottom;
    [HideInInspector]
    public float safeAreaYTop;

    [HideInInspector] public float right;
    [HideInInspector] public float top;

    // Start is called before the first frame update
    void Start()
    {
        safeArea = Screen.safeArea;
        minAnchor = safeArea.position;
        maxAnchor = minAnchor + safeArea.size;

        minAnchor.x /= Screen.width;
        minAnchor.y /= Screen.height;
        maxAnchor.x /= Screen.width;
        maxAnchor.y /= Screen.height;

        float height = (float)Screen.height;
        float width = (float)Screen.width;

        float ratio = width / height;
        float ratioDefault = 720.0f / 1280.0f;
        if (ratio <= ratioDefault)
        {
            screenWidth = 1080.0f;
            screenHeight = Screen.height * (screenWidth / Screen.width);
        }
        else if (ratio > ratioDefault)
        {
            screenHeight = 1920.0f;
            screenWidth = Screen.width * (screenHeight / Screen.height);
        }

        safeAreaYBottom = minAnchor.y * screenHeight;
        safeAreaYTop = (1 - maxAnchor.y) * screenHeight;

        right = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0)).x;
        top = Camera.main.ScreenToWorldPoint(new Vector3(0, Screen.height, 0)).y;
    }
}
