using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using HexaFall.Gameplay.Core;
using UnityEngine;
using Lean.Pool;

namespace HexaFall.Gameplay.CoreController
{
    /// <summary>
    /// View component and runtime state holder for one stack of hexa blocks.
    /// </summary>
    public sealed class StackController : MonoBehaviour
    {
        [SerializeField] private HexaBlock blockPrefab;
        [SerializeField] private List<Transform> m_listSlot;
        [SerializeField] private Transform blockRoot;
        [SerializeField] private float blockHeight = 0.18f;
        [SerializeField] private float flightArcHeight = 1f;
        [SerializeField] private GameObject m_hiddenVisual;
        [SerializeField] private ParticleSystem m_resolveHiddenVFX;

        [Header("Animation Feedback")]
        [SerializeField] private ParticleSystem m_jumpTakeoffVfx;
        [SerializeField] private AudioClip m_jumpSfx;

        private readonly List<ColorType> blocksBottomToTop = new List<ColorType>();
        private readonly List<HexaBlock> spawnedBlocks = new List<HexaBlock>();

        public GridPosition Position { get; private set; }
        public IReadOnlyList<ColorType> BlocksBottomToTop => blocksBottomToTop;
        public bool HasBlocks => blocksBottomToTop.Count > 0;
        public bool HasColor(ColorType color) => blocksBottomToTop.Contains(color);

        public bool IsHidden { get; private set; }
        public bool IsMoving { get; set; }

        public void Initialize(GridPosition position, IReadOnlyList<ColorType> blocks, bool isHidden = false)
        {
            Position = position;
            blocksBottomToTop.Clear();
            if (blocks != null)
            {
                blocksBottomToTop.AddRange(blocks);
            }
            IsHidden = isHidden;

            ApplyVisualState();
        }

        public void SetPosition(GridPosition position)
        {
            Position = position;
        }

        public HexaBlock PopBlockOfColor(ColorType color)
        {
            if (!HasBlocks) return null;
            
            int index = blocksBottomToTop.LastIndexOf(color);
            if (index == -1) return null;
            
            blocksBottomToTop.RemoveAt(index);
            
            var targetBlock = spawnedBlocks[index];
            spawnedBlocks.RemoveAt(index);

            // Animate blocks above the removed one sliding down
            for (int i = index; i < spawnedBlocks.Count; i++)
            {
                var block = spawnedBlocks[i];
                Transform targetParent = GetRoot();
                if (m_listSlot != null && i < m_listSlot.Count && m_listSlot[i] != null)
                {
                    targetParent = m_listSlot[i];
                }
                
                block.transform.SetParent(targetParent, true);
                block.transform.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.OutQuad);
                block.transform.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.OutQuad);
            }
            
            return targetBlock;
        }

        public IEnumerator PlayTopBlockFlightTo(Vector3 targetWorldPosition, float duration)
        {
            if (!HasBlocks || blockPrefab == null)
            {
                yield break;
            }

            var color = blocksBottomToTop[blocksBottomToTop.Count - 1];
            var hiddenTopBlock = HideTopBlockVisual();
            var flyingBlock = LeanPool.Spawn(blockPrefab, GetRoot());
            flyingBlock.ApplyColor(color);
            flyingBlock.transform.position = GetTopBlockWorldPosition();
            flyingBlock.transform.localRotation = Quaternion.identity;

            if (duration <= 0f)
            {
                flyingBlock.transform.position = targetWorldPosition;
                flyingBlock.transform.localRotation = Quaternion.Euler(-180f, 0f, 0f);
            }
            else
            {
                var seq = DOTween.Sequence();
                seq.Join(flyingBlock.transform.DOJump(targetWorldPosition, flightArcHeight, 1, duration).SetEase(Ease.InOutQuad));
                seq.Join(flyingBlock.transform.DOLocalRotate(new Vector3(-180f, 0f, 0f), duration, RotateMode.FastBeyond360).SetEase(Ease.InOutQuad));
                yield return seq.WaitForCompletion();
            }

            LeanPool.Despawn(flyingBlock.gameObject);
            if (hiddenTopBlock != null)
            {
                hiddenTopBlock.SetActive(true);
            }
        }

        public IEnumerator PlayDetachedBlockFlight(ColorType color, Vector3 startWorldPosition, Transform targetTransform, float duration, HexaBlock flyingBlock = null)
        {
            if (flyingBlock == null)
            {
                if (blockPrefab == null) yield break;
                flyingBlock = LeanPool.Spawn(blockPrefab);
                flyingBlock.ApplyColor(color);
                flyingBlock.transform.position = startWorldPosition;
                flyingBlock.transform.rotation = Quaternion.identity;
            }
            else
            {
                flyingBlock.transform.SetParent(null, true);
            }

            flyingBlock.transform.SetParent(targetTransform, true);
            flyingBlock.transform.localScale = Vector3.one;

            if (duration <= 0f)
            {
                flyingBlock.transform.localPosition = Vector3.zero;
                flyingBlock.transform.localRotation = Quaternion.Euler(-180f, 90f, 0f);
            }
            else
            {
                if (m_jumpSfx != null)
                {
                    AudioSource.PlayClipAtPoint(m_jumpSfx, startWorldPosition);
                }
                if (m_jumpTakeoffVfx != null)
                {
                    var vfx = LeanPool.Spawn(m_jumpTakeoffVfx, startWorldPosition, Quaternion.identity);
                    vfx.Play();
                }

                var seq = DOTween.Sequence();
                
                float anticDuration = duration * 0.15f;
                float flightDuration = duration * 0.7f;
                float landDuration = duration * 0.15f;

                // 1. Anticipation (Squash + LookAt)
                Vector3 lookDir = targetTransform.position - startWorldPosition;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    flyingBlock.transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
                }
                
                seq.Append(flyingBlock.transform.DOScale(new Vector3(1.3f, 0.7f, 1.3f), anticDuration).SetEase(Ease.OutQuad));
                
                Sequence flightScale = DOTween.Sequence();
                flightScale.Append(flyingBlock.transform.DOScale(new Vector3(0.8f, 1.56f, 0.8f), flightDuration * 0.3f).SetEase(Ease.OutQuad));
                flightScale.Append(flyingBlock.transform.DOScale(Vector3.one, flightDuration * 0.4f).SetEase(Ease.InOutSine));
                flightScale.Append(flyingBlock.transform.DOScale(new Vector3(0.9f, 1.23f, 0.9f), flightDuration * 0.3f).SetEase(Ease.InQuad));
                
                float arcHeight = flightArcHeight + UnityEngine.Random.Range(-0.2f, 0.3f);
                seq.Append(flyingBlock.transform.DOLocalJump(Vector3.zero, Mathf.Max(0.2f, arcHeight), 1, flightDuration).SetEase(Ease.Linear));
                seq.Join(flightScale);
                
                seq.Join(flyingBlock.transform.DOLocalRotate(new Vector3(360f, 0f, 0f), flightDuration, RotateMode.LocalAxisAdd).SetEase(Ease.InOutSine));

                seq.Append(flyingBlock.transform.DOScale(new Vector3(1.5f, 0.4f, 1.5f), landDuration * 0.25f).SetEase(Ease.OutQuad));
                seq.Append(flyingBlock.transform.DOScale(new Vector3(1f, 1f, 1f), landDuration * 0.25f).SetEase(Ease.OutQuad));
                seq.Append(flyingBlock.transform.DOScale(Vector3.zero, landDuration * 0.2f).SetEase(Ease.InBack).SetDelay(landDuration * 0.3f));

                yield return seq.WaitForCompletion();
            }

            LeanPool.Despawn(flyingBlock.gameObject);
        }

        public IEnumerator PlaySlideToLocal(Vector3 targetLocalPosition, float duration)
        {
            if (duration <= 0f)
            {
                transform.localPosition = targetLocalPosition;
                yield break;
            }

            yield return transform.DOLocalMove(targetLocalPosition, duration).SetEase(Ease.InOutQuad).WaitForCompletion();
        }

        public void ApplyVisualState()
        {
            if (blockPrefab == null) return;

            // Despawn excess blocks
            while (spawnedBlocks.Count > blocksBottomToTop.Count)
            {
                var block = spawnedBlocks[spawnedBlocks.Count - 1];
                if (block != null)
                {
                    LeanPool.Despawn(block.gameObject);
                }
                spawnedBlocks.RemoveAt(spawnedBlocks.Count - 1);
            }

            // Spawn missing blocks
            for (int i = spawnedBlocks.Count; i < blocksBottomToTop.Count; i++)
            {
                Transform parent = GetRoot();
                int index = i%m_listSlot.Count;
                if (m_listSlot != null && m_listSlot[index] != null)
                {
                    parent = m_listSlot[index];
                }
                var block = LeanPool.Spawn(blockPrefab, parent);
                block.transform.localPosition = Vector3.zero;
                block.transform.localRotation = Quaternion.identity;
                spawnedBlocks.Add(block);
            }

            // Apply state to all current blocks
            for (int i = 0; i < blocksBottomToTop.Count; i++)
            {
                var block = spawnedBlocks[i];
                if (block != null)
                {
                    block.IsHidden = IsHidden;
                    block.ApplyColor(blocksBottomToTop[i]);
                }
            }

            if (m_hiddenVisual != null)
            {
                m_hiddenVisual.SetActive(IsHidden);
            }
        }

        public void SetHidden(bool hidden)
        {
            IsHidden = hidden;
            ApplyVisualState();
        }

        public IEnumerator PlayReveal(float duration)
        {
            IsHidden = false;
            if(m_resolveHiddenVFX != null)
            {
                var vfx = LeanPool.Spawn(m_resolveHiddenVFX, transform);
                vfx.Play();
            }
            ApplyVisualState();
            yield return null;
        }

        private GameObject HideTopBlockVisual()
        {
            if (spawnedBlocks.Count == 0) return null;
            var topBlock = spawnedBlocks[spawnedBlocks.Count - 1].gameObject;
            topBlock.SetActive(false);
            return topBlock;
        }

        public Vector3 GetTopBlockWorldPosition()
        {
            if (spawnedBlocks.Count > 0 && spawnedBlocks[spawnedBlocks.Count - 1] != null)
            {
                return spawnedBlocks[spawnedBlocks.Count - 1].transform.position;
            }
            return GetRoot().TransformPoint(Vector3.zero);
        }

        private Transform GetRoot()
        {
            return blockRoot == null ? transform : blockRoot;
        }

        public void ClearBlocks()
        {
            foreach (var block in spawnedBlocks)
            {
                if (block != null)
                {
                    LeanPool.Despawn(block.gameObject);
                }
            }
            spawnedBlocks.Clear();
        }
    }
}
