using System.Collections;
using DG.Tweening;
using HexaFall.Gameplay.Core;
using HexaFall.Gameplay.Data;
using UnityEngine;

namespace HexaFall.Gameplay.CoreController
{
    public sealed class KeyController : MonoBehaviour
    {
        [SerializeField] private ColorMaterialMaping m_colorMaterialMapping;    
        [SerializeField] private MeshRenderer m_keyBody;
        [SerializeField] private Vector3 m_defaultRotation;
        [SerializeField] private Vector3 m_activeRotation;

        public string       KeyId    { get; private set; }
        public ColorType    Color    { get; private set; }
        public GridPosition Position { get; private set; }
        public bool         IsActivated { get; private set; }

        public void Initialize(KeyDefinition definition)
        {
            KeyId       = definition.keyId;
            Color       = definition.color;
            Position    = definition.position;
            IsActivated = false;
            gameObject.SetActive(true);
            transform.localRotation = Quaternion.Euler(m_defaultRotation);
            if (m_keyBody != null)
            {
                m_keyBody.materials = new Material[2] { m_colorMaterialMapping.materialKeyBody[Color], m_colorMaterialMapping.materialKeyBody[Color] };
            }   
        }

        public bool CanActivate(GridBoardController board)
        {
            if (IsActivated || board == null) return false;

            var pos = Position;
            var top = IsNeighborEmpty(board, pos.Row - 1, pos.Column, Color);
            var bot = IsNeighborEmpty(board, pos.Row + 1, pos.Column, Color);
            var left = IsNeighborEmpty(board, pos.Row, pos.Column - 1, Color);
            var right = IsNeighborEmpty(board, pos.Row, pos.Column + 1, Color);

            return top || bot || left || right;
        }

        public IEnumerator PlayActivate(Vector3 lockWorldPosition, float duration)
        {
            IsActivated = true;

            if (duration > 0f)
            {
                var seq = DG.Tweening.DOTween.Sequence();
                seq.Append(transform.DOMove(lockWorldPosition, duration).SetEase(DG.Tweening.Ease.InOutQuad));
                seq.Join(transform.DORotate(m_activeRotation, duration, DG.Tweening.RotateMode.FastBeyond360));
                seq.Append(transform.DORotate(m_activeRotation + Vector3.forward * 90, duration, RotateMode.FastBeyond360));
                yield return seq.WaitForCompletion();
            }

            gameObject.SetActive(false);
        }

        private static bool IsNeighborEmpty(GridBoardController board, int row, int col, ColorType keyColor)
        {
            var pos = new GridPosition(row, col);
            if (board.TryGetLock(keyColor, out var lk) && lk.Position == pos)
                return true; // Paired lock doesn't block its own key

            return board.IsCellEffectivelyEmpty(pos);
        }
    }
}
