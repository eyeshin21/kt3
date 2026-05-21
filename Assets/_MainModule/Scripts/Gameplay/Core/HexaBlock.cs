using HexaFall.Gameplay.Core;
using UnityEngine;

namespace HexaFall.Gameplay.CoreController
{
    public sealed class HexaBlock : MonoBehaviour
    {
        [SerializeField] private ColorMaterialMaping m_listMaterial;
        [SerializeField] private Renderer meshRenderer;
        [SerializeField] private Material m_hiddenMaterial;

        public bool IsHidden { get; set; }

        public void ApplyColor(ColorType colorId)
        {
            if (meshRenderer == null || m_listMaterial == null || !m_listMaterial.materialHexaBlock.TryGetValue(colorId, out var material))
            {
                return;
            }

            if (IsHidden && m_hiddenMaterial != null)
            {
                meshRenderer.material = m_hiddenMaterial;
                return;
            }

            meshRenderer.sharedMaterial = material;
        }
    }
}
