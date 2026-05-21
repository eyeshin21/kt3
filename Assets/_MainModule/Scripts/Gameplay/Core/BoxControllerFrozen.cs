using System.Collections;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

namespace HexaFall.Gameplay.CoreController
{
    public sealed partial class BoxController : MonoBehaviour
    {
        public bool ReduceFrozen()
        {
            if (FrozenDurability <= 0) return false;

            if (m_iceBreakVfx != null)
            {
                var frozenVfx = LeanPool.Spawn(m_iceBreakVfx, transform);
                frozenVfx.Play();
            }

            FrozenDurability--;
            ApplyVisualState();

            if (FrozenDurability > 0)
            {
                PlayFrozenDamage();
            }

            return FrozenDurability == 0;
        }

        private void PlayFrozenDamage()
        {
            if (m_meshFrozenOverlay != null)
            {
                // Give the frozen overlay a shatter-like jolt
                m_meshFrozenOverlay.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.3f, 5, 0.5f);
            }
            // Shake the entire box
            transform.DOPunchRotation(new Vector3(5f, 5f, 5f), 0.3f, 5, 0.5f);
        }

        public IEnumerator PlayThaw(float duration)
        {
            if (m_meshFrozenOverlay != null)
            {
                // Play a shattering or melting away effect
                var seq = DG.Tweening.DOTween.Sequence();
                seq.Join(m_meshFrozenOverlay.transform.DOScale(Vector3.zero, duration).SetEase(DG.Tweening.Ease.InBack));
                seq.Join(m_meshFrozenOverlay.transform.DOLocalRotate(new Vector3(0, 180, 0), duration, DG.Tweening.RotateMode.LocalAxisAdd).SetEase(DG.Tweening.Ease.InBack));
                seq.Insert(0, transform.DOPunchScale(new Vector3(0.1f, -0.1f, 0.1f), duration * 0.5f, 1, 0.5f));
                yield return seq.WaitForCompletion();
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }
        }

        private void SetFrozenOverlay(bool active)
        {
            if (m_meshFrozenOverlay != null)
                m_meshFrozenOverlay.gameObject.SetActive(active);
            if (active && m_textProgress != null && !IsArrivedInWaitingArea)
            {
                m_textMeltCount.gameObject.SetActive(true);
                m_textMeltCount.text = $"{FrozenDurability}";
            }
        }
    }
}
