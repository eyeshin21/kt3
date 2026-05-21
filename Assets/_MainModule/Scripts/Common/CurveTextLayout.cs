using TMPro;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(TextMeshProUGUI))]
public class CurveTextLayout : MonoBehaviour
{
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 0);
    public float curveHeight = 10f;
    public float curveWidth = 100f;
    [SerializeField] private bool rotateCharactersToCurve = true;

    [SerializeField] private TMP_Text textMesh;
    private TMP_TextInfo textInfo;

    private void OnEnable()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        textMesh.ForceMeshUpdate();
        UpdateTextGeometry();
    }

    private void LateUpdate()
    {
        UpdateTextGeometry();
    }

    [ContextMenu("UpdateTextGeometry")]
    private void UpdateTextGeometry()
    {
        if (textMesh == null || textMesh.textInfo.characterCount <= 0)
        {
            return;
        }

        textMesh.ForceMeshUpdate();
        textInfo = textMesh.textInfo;

        var hasVisibleCharacter = false;
        float minX = 0f;
        float maxX = 0f;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible)
            {
                continue;
            }

            if (!hasVisibleCharacter)
            {
                minX = charInfo.bottomLeft.x;
                maxX = charInfo.topRight.x;
                hasVisibleCharacter = true;
                continue;
            }

            minX = Mathf.Min(minX, charInfo.bottomLeft.x);
            maxX = Mathf.Max(maxX, charInfo.topRight.x);
        }

        if (!hasVisibleCharacter)
        {
            return;
        }

        var totalWidth = Mathf.Max(maxX - minX, Mathf.Epsilon);

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible)
            {
                continue;
            }

            var vertexIndex = charInfo.vertexIndex;
            var materialIndex = charInfo.materialReferenceIndex;
            var vertices = textInfo.meshInfo[materialIndex].vertices;

            var charMidX = (vertices[vertexIndex].x + vertices[vertexIndex + 2].x) * 0.5f;
            var baselinePivot = new Vector3(charMidX, charInfo.baseLine, 0f);
            var normalizedX = Mathf.Clamp01((charMidX - minX) / totalWidth);
            var curveY = curve.Evaluate(normalizedX) * curveHeight;
            var curveAngle = GetCurveAngle(normalizedX, totalWidth);

            var offset = new Vector3(0f, curveY, 0f);
            var rotation = rotateCharactersToCurve
                ? Quaternion.Euler(0f, 0f, curveAngle)
                : Quaternion.identity;

            for (int j = 0; j < 4; j++)
            {
                var relativeVertex = vertices[vertexIndex + j] - baselinePivot;
                vertices[vertexIndex + j] = rotation * relativeVertex + baselinePivot + offset;
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textMesh.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    private float GetCurveAngle(float normalizedX, float totalWidth)
    {
        var sampleStep = Mathf.Max(1f / Mathf.Max(totalWidth, 1f), 0.0025f);
        var left = Mathf.Clamp01(normalizedX - sampleStep);
        var right = Mathf.Clamp01(normalizedX + sampleStep);

        if (Mathf.Approximately(left, right))
        {
            return 0f;
        }

        var y0 = curve.Evaluate(left) * curveHeight;
        var y1 = curve.Evaluate(right) * curveHeight;
        var x0 = left * totalWidth;
        var x1 = right * totalWidth;

        return Mathf.Atan2(y1 - y0, x1 - x0) * Mathf.Rad2Deg;
    }
}
