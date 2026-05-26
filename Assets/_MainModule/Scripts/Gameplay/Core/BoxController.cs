using DG.Tweening;
using HexaFall.Gameplay.Core;
using HexaFall.Gameplay.Data;
using Lean.Pool;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace HexaFall.Gameplay.CoreController
{
    /// <summary>
    /// Raycast-clickable view and runtime state holder for one puzzle box.
    /// </summary>
    public sealed partial class BoxController : MonoBehaviour
    {
        [SerializeField] private ColorMaterialMaping m_listMaterial;
        [SerializeField] private MeshRenderer m_mesh;
        [SerializeField] private MeshRenderer m_meshFill;
        [SerializeField] private MeshRenderer m_meshBottom;
        [SerializeField] private UnityEvent<string> onInitialized;
        [SerializeField] private TextMeshPro m_textProgress;
        [SerializeField] private Transform m_targetBlockFly;

        [SerializeField] private MeshRenderer m_meshFrozenOverlay;
        [SerializeField] private TextMeshPro m_textMeltCount;
        [SerializeField] private GameObject m_hiddenVisual;

        [SerializeField] private float m_posYFlipOver = 1f;
        [SerializeField] private float m_posYFlipUp = 0f; 
        [SerializeField] private float m_maxFillPosY = 2.5f;
        [SerializeField] private float gridPathHopHeight = 0.2f;
        [SerializeField] private float slotJumpHeight = 1.2f;
        [SerializeField] private float heightLiftUpByBooster = 1;

        [Header("VFXs")]
        [SerializeField] private ParticleSystem m_iceBreakVfx;
        [SerializeField] private ParticleSystem m_ResolveHiddenVfx;
        [SerializeField] private ParticleSystem m_boxDisappearVfx;
        [SerializeField] private ParticleSystem m_boxLandVfx;

        [Header("Audio")]
        [SerializeField] private AudioClip m_landSfx;

        [SerializeField] private float flipDuration = 0.3f;

        private Action<string> onTapped;
        private Vector3 initialLocalScale;
        private Quaternion initialLocalRotation;
        private bool lastKnownPickable = false;
        private Tween flipTween;
        private Tween scaleTween;

        public void SetBoosterHighlight(bool active)
        {
            scaleTween?.Kill();
            var baseScale = initialLocalScale == Vector3.zero ? Vector3.one : initialLocalScale;
            if (active)
            {
                scaleTween = transform.DOScale(baseScale * 1.05f, 0.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }
            else
            {
                scaleTween = transform.DOScale(baseScale, 0.2f).SetEase(Ease.OutQuad);
            }
        }

        public string BoxId { get; private set; }
        public ColorType TargetColor { get; private set; }
        public int Capacity { get; private set; }
        public GridPosition GridPosition { get; private set; }
        public int FillCount { get; private set; }
        public bool IsPickable { get; set; }
        public bool IsInWaitingArea { get; set; }
        public bool IsArrivedInWaitingArea { get; private set; }
        public bool IsCleared { get; set; }
        public bool IsFull => FillCount >= Capacity;
        public Vector3 CollectionWorldPosition => m_targetBlockFly.transform.position;
        public Transform TargetBlockFly => m_targetBlockFly;

        public bool IsHidden         { get; private set; }
        public int  FrozenDurability { get; private set; }
        public bool IsFrozenLocked   { get; set; }

        private void Awake()
        {
            initialLocalScale = transform.localScale;
            initialLocalRotation = transform.localRotation;
        }

        public void Initialize(BoxDefinition definition, GridPosition position, Action<string> onTapped)
        {
            BoxId = definition.boxId;
            TargetColor = definition.targetColor;
            Capacity = definition.capacity;
            GridPosition = position;
            FillCount = 0;
            IsPickable = false;
            IsInWaitingArea = false;
            IsArrivedInWaitingArea = false;
            IsCleared = false;
            lastKnownPickable = false;
            IsHidden = definition.isHidden;
            FrozenDurability = definition.frozenDurability;
            IsFrozenLocked = FrozenDurability > 0;
            this.onTapped = onTapped;
            transform.localScale = initialLocalScale == Vector3.zero ? Vector3.one : initialLocalScale;
            transform.localRotation = initialLocalRotation;
            m_textProgress.gameObject.SetActive(false);
            gameObject.SetActive(true);
            onInitialized?.Invoke(BoxId);

            PlayFlip(IsPickable);

            ApplyVisualState();
        }

        public void AddBlock()
        {
            if (IsFull)
            {
                return;
            }

            FillCount++;
            UpdateProgressVisuals();
        }

        public void MarkArrivedInWaitingArea()
        {
            IsArrivedInWaitingArea = true;
            UpdateProgressVisuals();
        }

        public void ApplyVisualState()
        {
            gameObject.SetActive(!IsCleared);
            m_hiddenVisual.SetActive(IsHidden);
            m_textMeltCount.gameObject.SetActive(FrozenDurability > 0);

            if (IsHidden)
            {
                ApplyMaterial(ColorType.None);
                SetFrozenOverlay(false);
            }
            else if (FrozenDurability > 0)
            {
                ApplyMaterial(TargetColor);
                SetFrozenOverlay(true);
            }
            else
            {
                ApplyMaterial(TargetColor);
                SetFrozenOverlay(false);
            }

            // Flip only allowed when box is normal (not hidden, not frozen)
            bool canFlip = !IsHidden && FrozenDurability <= 0;
            if (canFlip && IsPickable != lastKnownPickable && !IsInWaitingArea)
            {
                lastKnownPickable = IsPickable;
                PlayFlip(IsPickable);
            }
            else if (!canFlip && lastKnownPickable)
            {
                // Force face-down if hidden/frozen becomes true after being revealed
                lastKnownPickable = false;
                PlayFlip(false);
            }

            UpdateProgressVisuals();
        }

        public IEnumerator PlayMoveAlongGridPathThenJump(IReadOnlyList<Vector3> pathWorldPositions, Vector3 targetWorldPosition, float duration)
        {
            gameObject.SetActive(true);

            var pathCount = pathWorldPositions == null ? 0 : pathWorldPositions.Count;
            var pathDuration = duration * 0.55f;
            var jumpDuration = duration * 0.45f;

            if (pathCount > 0)
            {
                var segmentDuration = pathDuration / pathCount;
                for (int i = 0; i < pathCount; i++)
                {
                    yield return PlayfulMoveArcToWorld(pathWorldPositions[i], segmentDuration, gridPathHopHeight, false, false).WaitForCompletion();
                }
            }

            yield return PlayfulMoveArcToWorld(targetWorldPosition, jumpDuration, slotJumpHeight * 1.5f, false, true).WaitForCompletion();
            MarkArrivedInWaitingArea();
        }

        public IEnumerator PlayEmergeFromTunnel(Vector3 sourceWorldPosition, float duration)
        {
            gameObject.SetActive(true);
            var targetPos = transform.position;
            transform.position = sourceWorldPosition;
            
            var originalScale = initialLocalScale == Vector3.zero ? Vector3.one : initialLocalScale;
            transform.localScale = Vector3.zero;

            Sequence seq = DOTween.Sequence();
            seq.Join(transform.DOJump(targetPos, gridPathHopHeight * 1.5f, 1, duration).SetEase(Ease.OutQuad));
            seq.Join(transform.DOScale(originalScale, duration).SetEase(Ease.OutBack));
            seq.Join(transform.DOLocalRotate(new Vector3(0, 360, 0), duration, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad));
            
            yield return seq.WaitForCompletion();
        }

        public IEnumerator PlayMoveAlongGridPathThenJump(IReadOnlyList<Vector3> pathWorldPositions, Transform targetSlot, float stepDuration, float jumToSlotDuration)
        {
            gameObject.SetActive(true);

            var pathCount = pathWorldPositions == null ? 0 : pathWorldPositions.Count;
            var jumpDuration = jumToSlotDuration;

            if (pathCount > 0)
            {
                var segmentDuration = stepDuration;
                for (int i = 0; i < pathCount; i++)
                {
                    yield return PlayfulMoveArcToWorld(pathWorldPositions[i], segmentDuration, gridPathHopHeight, false, false).WaitForCompletion();
                }
            }

            transform.SetParent(targetSlot, true);
            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOLocalJump(Vector3.zero, slotJumpHeight * 1.5f, 1, jumpDuration).SetEase(Ease.InOutSine));
            yield return seq.WaitForCompletion();
            
            var startScale = initialLocalScale == Vector3.zero ? Vector3.one : initialLocalScale;
            var squashScale = new Vector3(startScale.x * 1.15f, startScale.y * 0.85f, startScale.z * 1.15f);
            
            scaleTween?.Kill();
            Sequence bounceSeq = DOTween.Sequence();
            bounceSeq.Append(transform.DOScale(squashScale, 0.1f).SetEase(Ease.OutQuad));
            bounceSeq.Append(transform.DOScale(startScale, 0.15f).SetEase(Ease.OutBack));
            scaleTween = bounceSeq;
            
            MarkArrivedInWaitingArea();
        }

        public IEnumerator PlaySlideToWorld(Vector3 targetWorldPosition, float duration)
        {
            gameObject.SetActive(true);
            yield return MoveArcToWorld(targetWorldPosition, duration, 0.12f, false).WaitForCompletion();
        }

        public IEnumerator PlaySlideToLocal(Vector3 targetLocalPosition, float duration)
        {
            gameObject.SetActive(true);
            yield return MoveArcToLocal(targetLocalPosition, duration, 0.12f, false).WaitForCompletion();
        }

        public IEnumerator PlayCollectPulse(float duration)
        {
            if (m_boxLandVfx != null)
            {
                var vfx = LeanPool.Spawn(m_boxLandVfx, transform.position, Quaternion.identity, transform.parent);
                vfx.Play();
            }

            if (m_landSfx != null)
            {
                AudioSource.PlayClipAtPoint(m_landSfx, transform.position);
            }

            var startScale = initialLocalScale == Vector3.zero ? Vector3.one : initialLocalScale;
            
            var squashScale = new Vector3(startScale.x * 1.15f, startScale.y * 0.8f, startScale.z * 1.15f);
            var stretchScale = new Vector3(startScale.x * 0.9f, startScale.y * 1.1f, startScale.z * 0.9f);

            var halfDuration = Mathf.Max(0.01f, duration * 0.5f);
            var quarterDuration = halfDuration * 0.5f;

            scaleTween?.Kill();
            Sequence seq = DOTween.Sequence();
            
            seq.Append(transform.DOScale(squashScale, quarterDuration).SetEase(Ease.OutQuad));
            seq.Append(transform.DOScale(stretchScale, halfDuration).SetEase(Ease.InOutQuad));
            seq.Append(transform.DOScale(startScale, quarterDuration).SetEase(Ease.OutQuad));
            
            scaleTween = seq;

            yield return seq.WaitForCompletion();
        }

        public IEnumerator PlayClear(float duration)
        {
            if(m_boxDisappearVfx != null)
            {
                var vfxDisappear = LeanPool.Spawn(m_boxDisappearVfx, transform);
                vfxDisappear.Play();
            }
            scaleTween?.Kill();
            Sequence seq = DOTween.Sequence();
            seq.Join(transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack));
            seq.Join(transform.DOLocalRotate(new Vector3(0, 180, 0), duration, RotateMode.LocalAxisAdd).SetEase(Ease.InBack));
            yield return seq.WaitForCompletion();
            gameObject.SetActive(false);
        }

        public bool CanBeSelected => IsPickable && !IsInWaitingArea && !IsCleared && !IsHidden && FrozenDurability <= 0;

        private void UpdateProgressVisuals()
        {
            bool shouldDisplay = IsArrivedInWaitingArea && !IsCleared;

            bool frozenLabelActive = FrozenDurability > 0 && !IsArrivedInWaitingArea;

            if (m_textProgress != null)
            {
                if (frozenLabelActive)
                {
                    // Already set by SetFrozenOverlay — don't override
                }
                else
                {
                    m_textProgress.gameObject.SetActive(shouldDisplay);
                    if (shouldDisplay)
                        m_textProgress.text = $"{FillCount}/{Capacity}";
                }
            }

            if (m_meshFill != null)
            {
                m_meshFill.gameObject.SetActive(shouldDisplay);
                if (shouldDisplay)
                {
                    float pct = Capacity > 0 ? (float)FillCount / Capacity : 0f;
                    var lp = m_meshFill.transform.localPosition;
                    lp.y = Mathf.Lerp(0f, m_maxFillPosY, pct);
                    m_meshFill.transform.localPosition = lp;
                }
            }
        }

        private Tween PlayfulMoveArcToWorld(Vector3 targetWorldPosition, float duration, float arcHeight, bool flip, bool extraSpin)
        {
            var targetRotation = flip ? initialLocalRotation * Quaternion.Euler(-180f, 0f, 0f) : initialLocalRotation;

            if (duration <= 0f)
            {
                transform.position = targetWorldPosition;
                transform.localRotation = targetRotation;
                return DOVirtual.DelayedCall(0, delegate { });
            }

            Sequence seq = DOTween.Sequence();
            // Using OutQuad so it pops up quickly but falls slightly slower
            seq.Join(transform.DOJump(targetWorldPosition, arcHeight, 1, duration).SetEase(Ease.OutQuad));
            
            if (extraSpin)
            {
                seq.Join(transform.DOLocalRotate(new Vector3(0, 360, 0), duration, RotateMode.LocalAxisAdd).SetEase(Ease.InOutQuad));
            }
            else
            {
                seq.Join(transform.DOLocalRotateQuaternion(targetRotation, duration).SetEase(Ease.InOutQuad));
                // Add a playful squish/stretch jump effect
                seq.Join(transform.DOPunchScale(new Vector3(-0.15f, 0.15f, -0.15f), duration, 1, 0.5f));
            }
            return seq;
        }

        private Tween MoveArcToWorld(Vector3 targetWorldPosition, float duration, float arcHeight, bool flip)
        {
            var targetRotation = flip ? initialLocalRotation * Quaternion.Euler(-180f, 0f, 0f) : initialLocalRotation;

            if (duration <= 0f)
            {
                transform.position = targetWorldPosition;
                transform.localRotation = targetRotation;
                return DOVirtual.DelayedCall(0, delegate { });
            }

            Sequence seq = DOTween.Sequence();
            seq.Join(transform.DOJump(targetWorldPosition, arcHeight, 1, duration).SetEase(Ease.InOutQuad));
            seq.Join(transform.DOLocalRotateQuaternion(targetRotation, duration).SetEase(Ease.InOutQuad));
            return seq;
        }

        private Tween MoveArcToLocal(Vector3 targetLocalPosition, float duration, float arcHeight, bool flip)
        {
            var targetRotation = flip ? initialLocalRotation * Quaternion.Euler(-180f, 0f, 0f) : initialLocalRotation;

            if (duration <= 0f)
            {
                transform.localPosition = targetLocalPosition;
                transform.localRotation = targetRotation;
                return DOVirtual.DelayedCall(0, delegate { });
            }

            Sequence seq = DOTween.Sequence();
            seq.Join(transform.DOLocalJump(targetLocalPosition, arcHeight, 1, duration).SetEase(Ease.InOutQuad));
            seq.Join(transform.DOLocalRotateQuaternion(targetRotation, duration).SetEase(Ease.InOutQuad));
            return seq;
        }

        private Tween ScaleTo(Vector3 targetScale, float duration)
        {
            scaleTween?.Kill();

            if (duration <= 0f)
            {
                transform.localScale = targetScale;
                return DOVirtual.DelayedCall(0, delegate { });
            }

            scaleTween = transform.DOScale(targetScale, duration).SetEase(Ease.InOutQuad);
            return scaleTween;
        }

        private void PlayFlip(bool pickable)
        {
            flipTween?.Kill();

            m_mesh.gameObject.SetActive(IsPickable);
            if (m_meshBottom != null)
            {
                m_meshBottom.gameObject.SetActive(!IsPickable);
            }

            var targetRotation = pickable
                ? initialLocalRotation
                : initialLocalRotation * Quaternion.Euler(180f, 0f, 0f);

            if (flipDuration <= 0f)
            {
                m_mesh.transform.localRotation = targetRotation;
                var lp = transform.localPosition;
                lp.y = pickable ? 0 : 1;
                m_mesh.transform.localPosition = lp;
                return;
            }

            Sequence seq = DOTween.Sequence();
            seq.Join(m_mesh.transform.DOLocalRotateQuaternion(targetRotation, flipDuration).SetEase(Ease.InOutQuad));
            seq.Join(m_mesh.transform.DOLocalMoveY(pickable ? m_posYFlipUp : m_posYFlipOver, flipDuration).SetEase(Ease.InOutQuad));
            flipTween = seq;
        }

        public IEnumerator PlayBoosterActivation(float duration)
        {
            if (!IsPickable)
            {
                IsPickable = true;
                PlayFlip(true);
            }

            Sequence seq = DOTween.Sequence();
            
            seq.Append(transform.DOLocalMoveY(transform.localPosition.y + heightLiftUpByBooster, duration * 0.5f + flipDuration).SetEase(Ease.OutQuad));
            seq.Append(transform.DOLocalMoveY(transform.localPosition.y, duration * 0.5f).SetEase(Ease.InQuad));
            
            yield return new WaitForSeconds(duration);
        }

        public IEnumerator PlayShuffleAnticipation(float duration)
        {
            yield return transform.DOShakePosition(duration, strength: new Vector3(0.1f, 0f, 0.1f), vibrato: 20, randomness: 90).WaitForCompletion();
        }

        public IEnumerator PlayShuffleFlight(Vector3 targetWorldPos, float duration)
        {
            Sequence seq = DOTween.Sequence();
            
            seq.Join(transform.DOJump(targetWorldPos, jumpPower: 1.5f, numJumps: 1, duration).SetEase(Ease.InOutQuad));
            seq.Join(transform.DORotate(new Vector3(0f, 360f, 0f), duration, RotateMode.FastBeyond360).SetEase(Ease.InOutQuad));
            
            yield return seq.WaitForCompletion();
        }

        public IEnumerator PlayShuffleLanding(float duration)
        {
            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOScale(initialLocalScale * 1.2f, duration * 0.5f).SetEase(Ease.OutQuad));
            seq.Append(transform.DOScale(initialLocalScale, duration * 0.5f).SetEase(Ease.InBounce));
            yield return seq.WaitForCompletion();
        }

        public void SetGridPosition(GridPosition pos)
        {
            GridPosition = pos;
        }

        private void ApplyMaterial(ColorType targetColor)
        {
            if (m_mesh == null || m_listMaterial == null) return;

            if (m_listMaterial.materialBox.TryGetValue(targetColor, out var materialBox))
                m_mesh.sharedMaterial = materialBox;

            if (m_meshFill != null && m_listMaterial.materialHexaBlock.TryGetValue(targetColor, out var materialHexa))
                m_meshFill.sharedMaterial = materialHexa;

            if(m_meshBottom != null  && m_listMaterial.materialHexaBlock.TryGetValue(targetColor, out var materialBottom))
            {
                m_meshBottom.material = materialBottom;
            }
        }

        /// <summary>Creates a BoxDefinition snapshot of the current runtime state. Used by ShuffleBooster.</summary>
        public BoxDefinition SnapshotDefinition() => new BoxDefinition
        {
            boxId           = BoxId,
            targetColor     = TargetColor,
            capacity        = Capacity,
            isHidden        = IsHidden,
            frozenDurability = FrozenDurability,
        };
    }
}
