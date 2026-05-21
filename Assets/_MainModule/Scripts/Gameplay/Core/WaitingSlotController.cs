using UnityEngine;

public class WaitingSlotController : MonoBehaviour
{
    [SerializeField] private Renderer m_renderer;
    [SerializeField] private Color m_occupiedColor = Color.white;
    [SerializeField] private Color m_emptyColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color m_warningColor = new Color(1f, 0.75f, 0.15f, 1f);

    public void SetOccupied(bool isOccupied)
    {
        SetState(isOccupied, false);
    }

    public void SetState(bool isOccupied, bool isWarning)
    {
        if (m_renderer == null)
        {
            return;
        }

        m_renderer.material.color = isWarning ? m_warningColor : isOccupied ? m_occupiedColor : m_emptyColor;
    }
}
