using Lean.Pool;
using System.Collections;
using UnityEngine;

namespace HexaFall.Gameplay.CoreController
{
    public sealed partial class BoxController : MonoBehaviour
    {
        public bool TryReveal()
        {
            if (!IsHidden) return false;
            IsHidden = false;
            ApplyVisualState();
            return true;
        }

        public IEnumerator PlayReveal(float duration)
        {
            if (m_ResolveHiddenVfx != null)
            {
                var vfx = LeanPool.Spawn(m_ResolveHiddenVfx, transform);
                vfx.Play();
            }
            yield return new WaitForSeconds(duration);
        }
    }
}
