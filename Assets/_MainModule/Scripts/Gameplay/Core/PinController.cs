using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using HexaFall.Gameplay.Core;
using HexaFall.Gameplay.Data;
using UnityEngine;

namespace HexaFall.Gameplay.CoreController
{
    public sealed class PinController : MonoBehaviour
    {
        [SerializeField] private GameObject m_head;
        [SerializeField] private GameObject m_body;

        public string       PinId         { get; private set; }
        public PinDirection Direction     { get; private set; }
        public GridPosition HeadPosition  { get; private set; }
        public int          Length        { get; private set; }
        public bool         IsRemoved     { get; private set; }

        public GridPosition TriggerNeighborPosition { get; private set; }

        public IReadOnlyList<GridPosition> OccupiedPositions => occupiedPositions;

        private readonly List<GridPosition> occupiedPositions = new List<GridPosition>();

        public void Initialize(PinDefinition definition)
        {
            PinId        = definition.pinId;
            Direction    = definition.direction;
            HeadPosition = definition.headPosition;
            Length       = Mathf.Max(1, definition.length);
            IsRemoved    = false;

            occupiedPositions.Clear();
            for (int i = 0; i < Length; i++)
                occupiedPositions.Add(StepFrom(HeadPosition, Direction, i));

            TriggerNeighborPosition = StepFrom(HeadPosition, InverseOf(Direction), 1);
            
            if (m_body != null)
                m_body.transform.localScale = new Vector3(1, 1, Length -1);

            float angle = 0f;
            switch (Direction)
            {
                case PinDirection.RightToLeft: angle = 180f; break;
                case PinDirection.LeftToRight: angle = 0f; break;
                case PinDirection.DownToUp: angle = -90f; break;
                case PinDirection.UpToDown: angle = 90f; break;
            }
            transform.localRotation = Quaternion.Euler(0, angle, 0);
            m_head.transform.localScale = Vector3.one;
        }

        public void Remove()
        {
            IsRemoved = true;
        }

        public IEnumerator PlayBreak(float duration)
        {
            if (duration > 0f)
            {
                var seq = DOTween.Sequence();
                
                // Add a small punch scale to the root to make it pop initially
                seq.Insert(0, transform.DOPunchScale(new Vector3(0.1f, -0.1f, 0.1f), duration * 0.3f, 1, 0.5f));

                if (m_body != null)
                {
                    // Shrink body along X axis
                    seq.Insert(0, m_body.transform.DOScaleX(0, duration * 0.6f).SetEase(Ease.InBack));
                }

                if (m_head != null)
                {
                    // Rotate head while body shrinks
                    seq.Insert(0, m_head.transform.DOLocalRotate(new Vector3(0, 0, 360), duration * 0.6f, RotateMode.LocalAxisAdd).SetEase(Ease.InBack));
                    
                    // Head scales down after body shrinks
                    seq.Insert(duration * 0.6f, m_head.transform.DOScale(Vector3.zero, duration * 0.4f).SetEase(Ease.InBack));
                }

                yield return seq.WaitForCompletion();
            }
            gameObject.SetActive(false);
        }

        private static GridPosition StepFrom(GridPosition pos, PinDirection dir, int steps)
        {
            switch (dir)
            {
                case PinDirection.LeftToRight: return new GridPosition(pos.Row, pos.Column + steps);
                case PinDirection.RightToLeft: return new GridPosition(pos.Row, pos.Column - steps);
                case PinDirection.UpToDown:    return new GridPosition(pos.Row + steps, pos.Column);
                case PinDirection.DownToUp:    return new GridPosition(pos.Row - steps, pos.Column);
                default:                       return pos;
            }
        }

        private static PinDirection InverseOf(PinDirection dir)
        {
            switch (dir)
            {
                case PinDirection.LeftToRight: return PinDirection.RightToLeft;
                case PinDirection.RightToLeft: return PinDirection.LeftToRight;
                case PinDirection.UpToDown:    return PinDirection.DownToUp;
                case PinDirection.DownToUp:    return PinDirection.UpToDown;
                default:                       return dir;
            }
        }
    }
}
