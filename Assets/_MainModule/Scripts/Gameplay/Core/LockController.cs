using System.Collections;
using DG.Tweening;
using HexaFall.Gameplay.Core;
using HexaFall.Gameplay.Data;
using UnityEngine;

namespace HexaFall.Gameplay.CoreController
{
    public sealed class LockController : MonoBehaviour
    {
        [SerializeField] private ColorMaterialMaping m_colorMaterialMapping;
        [SerializeField] private Transform m_keyTargetPos;
        [SerializeField] private MeshRenderer m_lockBody;

        public string       LockId      { get; private set; }
        public ColorType    Color       { get; private set; }
        public GridPosition Position    { get; private set; }
        public bool         IsDestroyed { get; private set; }
        public Transform KeyTargetPos => m_keyTargetPos;

        public void Initialize(LockDefinition definition)
        {
            LockId      = definition.lockId;
            Color       = definition.color;
            Position    = definition.position;
            IsDestroyed = false;
            gameObject.SetActive(true);
            if (m_lockBody != null)
            {
                m_lockBody.material = m_colorMaterialMapping.materialLockBody[Color];
            }
        }

        public void MarkDestroyed()
        {
            IsDestroyed = true;
        }

        public IEnumerator PlayDestroy(float delay, float duration)
        {
            MarkDestroyed();
            yield return new WaitForSeconds(delay);
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
                var seq = DG.Tweening.DOTween.Sequence();
                seq.Append(transform.DORotate(new Vector3(0, 180, 0), duration, DG.Tweening.RotateMode.LocalAxisAdd));
                seq.Join(transform.DOScale(Vector3.zero, duration));
                yield return seq.WaitForCompletion();
            }
            gameObject.SetActive(false);
        }
    }
}
