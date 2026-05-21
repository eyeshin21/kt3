using System.Collections;
using System.Collections.Generic;
using HexaFall.Gameplay.Core;
using HexaFall.Gameplay.Data;
using TMPro;
using UnityEngine;
using Lean.Pool;
using DG.Tweening;

namespace HexaFall.Gameplay.CoreController
{
    public sealed class TunnelController : MonoBehaviour
    {
        [SerializeField] private GameObject m_visual;
        [SerializeField] private BoxController boxPrefab;
        [SerializeField] private TextMeshPro m_textQuantity;

        public string         TunnelId           { get; private set; }
        public GridPosition   Position           { get; private set; }
        public FacingDirection Direction         { get; private set; }
        public bool           HasContent         => contentQueue.Count > 0;
        public GridPosition   ReleaseTargetPosition { get; private set; }
        public bool           IsRemoved          { get; private set; }

        private readonly Queue<BoxDefinition> contentQueue = new Queue<BoxDefinition>();

        public void Initialize(TunnelDefinition definition)
        {
            TunnelId  = definition.tunnelId;
            Position  = definition.position;
            Direction = definition.direction;
            IsRemoved = false;

            contentQueue.Clear();
            if (definition.contents != null)
            {
                foreach (var box in definition.contents)
                {
                    if (box != null)
                        contentQueue.Enqueue(box);
                }
            }

            ReleaseTargetPosition = ComputeReleaseTarget(Position, Direction);
            m_textQuantity.text = HasContent ? contentQueue.Count.ToString() : string.Empty;
            gameObject.SetActive(true);

            float angle = 0f;
            switch (Direction)
            {
                case FacingDirection.Left: angle = 90f; break;
                case FacingDirection.Right: angle = -90f; break;
                case FacingDirection.Up: angle = 180f; break;
                case FacingDirection.Down: angle = 0f; break;
            }
            if (m_visual != null)
                m_visual.transform.localRotation = Quaternion.Euler(0, angle, 0);
        }

        public BoxController TryRelease(GridBoardController board, Transform cellParent, System.Action<string> onBoxTapped)
        {
            if (!HasContent || board == null)
                return null;

            if (!board.IsCellEffectivelyEmpty(ReleaseTargetPosition))
                return null;

            if (boxPrefab == null)
            {
                contentQueue.Dequeue();
                return null;
            }

            var definition = contentQueue.Dequeue();
            m_textQuantity.text = HasContent ? contentQueue.Count.ToString() : string.Empty;

            if (!HasContent)
            {
                IsRemoved = true;
                gameObject.SetActive(false);
            }

            var parent = cellParent != null ? cellParent : transform;
            var newBox  = LeanPool.Spawn(boxPrefab, parent);
            newBox.Initialize(definition, ReleaseTargetPosition, onBoxTapped);
            return newBox;
        }

        public IEnumerator PlayRelease(float duration)
        {
            if (duration > 0f)
            {
                if (m_visual != null)
                {
                    yield return m_visual.transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0.15f), duration, 2, 0.5f).WaitForCompletion();
                }
                else
                {
                    yield return new WaitForSeconds(duration);
                }
            }
        }

        private static GridPosition ComputeReleaseTarget(GridPosition pos, FacingDirection dir)
        {
            switch (dir)
            {
                case FacingDirection.Left:  return new GridPosition(pos.Row, pos.Column - 1);
                case FacingDirection.Right: return new GridPosition(pos.Row, pos.Column + 1);
                case FacingDirection.Up:    return new GridPosition(pos.Row - 1, pos.Column);
                case FacingDirection.Down:  return new GridPosition(pos.Row + 1, pos.Column);
                default:                   return pos;
            }
        }
    }
}
