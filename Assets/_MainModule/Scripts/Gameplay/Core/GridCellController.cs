using AYellowpaper.SerializedCollections;
using HexaFall.Gameplay.Core;
using HexaFall.Gameplay.Data;
using System;
using UnityEngine;
using Lean.Pool;

namespace HexaFall.Gameplay.CoreController
{
    public enum BlockGridBorderType
    {
        None = -1,
        Top = 0,
        Bottom = 1,
        Left = 2,
        Right = 3,
        TopLeft = 4,
        TopRight = 5,
        BottomLeft = 6,
        BottomRight = 7,
        LeftTopRight = 8,
        TopRightBottom = 9,
        RightBottomLeft = 10,
        BottomLeftTop = 11,
        ClosedBorder = 12,
        Wall = 13,
        TopBottom = 14,
        LeftRight = 15,
    }

    public enum BlockGridCornerType
    {
        None = -1,
        TopLeftOut,
        TopRightOut,
        BottomLeftOut,
        BottomRightOut
    }

    public sealed class GridCellController : MonoBehaviour
    {
        [SerializeField] private BoxController m_boxPrefab;
        [SerializeField] private SerializedDictionary<BlockGridBorderType, GameObject> m_listBorder;
        [SerializeField] private SerializedDictionary<BlockGridCornerType, GameObject> m_listCorner;

        [SerializeField] private PinController    m_pinPrefab;
        [SerializeField] private KeyController    m_keyPrefab;
        [SerializeField] private LockController   m_lockPrefab;
        [SerializeField] private TunnelController m_tunnelPrefab;

        private BoxController    boxController;
        private PinController    pinController;
        private KeyController    keyController;
        private LockController   lockController;
        private TunnelController tunnelController;
        private GridCellBoardData gridCellBoardData;
        private GridPosition gridPosition;

        public GridPosition    GridPosition    => gridPosition;
        public BoxController   BoxController   => boxController;
        public PinController   PinController   => pinController;
        public KeyController   KeyController   => keyController;
        public LockController  LockController  => lockController;
        public TunnelController TunnelController => tunnelController;

        public void Initialize(GridPosition position, GridCellDefinition cellDefinition, GridCellBoardData boardData, Action<string> onTapped)
        {
            gridPosition      = position;
            gridCellBoardData = boardData;

            if (cellDefinition != null)
            {
                switch (cellDefinition.cellType)
                {
                    case GridCellType.StandardBox:
                    case GridCellType.MysteryBox:
                    case GridCellType.FrozenBox:
                        if (cellDefinition.box != null)
                        {
                            EnsureBoxController();
                            boxController.Initialize(cellDefinition.box, position, onTapped);
                        }
                        break;

                    default:
                        if (boxController != null)
                            boxController.gameObject.SetActive(false);
                        break;
                }
            }
            else if (boxController != null)
            {
                boxController.gameObject.SetActive(false);
            }

            CheckBorder();
        }

        public PinController SpawnPin(PinDefinition definition)
        {
            if (m_pinPrefab == null) { Debug.LogWarning($"[GridCellController] No PinPrefab assigned at {gridPosition}."); return null; }
            pinController = LeanPool.Spawn(m_pinPrefab, transform);
            pinController.Initialize(definition);
            return pinController;
        }

        public KeyController SpawnKey(KeyDefinition definition)
        {
            if (m_keyPrefab == null) { Debug.LogWarning($"[GridCellController] No KeyPrefab assigned at {gridPosition}."); return null; }
            keyController = LeanPool.Spawn(m_keyPrefab, transform);
            keyController.Initialize(definition);
            return keyController;
        }

        public LockController SpawnLock(LockDefinition definition)
        {
            if (m_lockPrefab == null) { Debug.LogWarning($"[GridCellController] No LockPrefab assigned at {gridPosition}."); return null; }
            lockController = LeanPool.Spawn(m_lockPrefab, transform);
            lockController.Initialize(definition);
            return lockController;
        }

        public TunnelController SpawnTunnel(TunnelDefinition definition)
        {
            if (m_tunnelPrefab == null) { Debug.LogWarning($"[GridCellController] No TunnelPrefab assigned at {gridPosition}."); return null; }
            tunnelController = LeanPool.Spawn(m_tunnelPrefab, transform);
            tunnelController.Initialize(definition);
            return tunnelController;
        }

        private void EnsureBoxController()
        {
            if (boxController != null || m_boxPrefab == null)
            {
                return;
            }

            boxController = LeanPool.Spawn(m_boxPrefab, transform);
        }

        public void SetBoxController(BoxController newBox)
        {
            boxController = newBox;
            if (boxController != null)
            {
                boxController.transform.SetParent(transform, true);
            }
        }

        public void DetachBoxController()
        {
            boxController = null;
        }

        private void CheckBorder()
        {
            HideAllBorders();
            if (gridCellBoardData == null)
            {
                return;
            }

            var cellData = gridCellBoardData.GetCellAt(gridPosition);
            if (cellData == null || cellData.cellType != GridCellType.DeadCell)
            {
                return;
            }

            var col = gridPosition.Column;
            var row = gridPosition.Row;
            var checkLeft = col - 1 < 0 || gridCellBoardData.GetCellAt(new GridPosition(row, col - 1))?.cellType != GridCellType.DeadCell;
            var checkRight = col + 1 >= gridCellBoardData.width || gridCellBoardData.GetCellAt(new GridPosition(row, col + 1))?.cellType != GridCellType.DeadCell;
            var checkBot = row + 1 >= gridCellBoardData.height || gridCellBoardData.GetCellAt(new GridPosition(row + 1, col))?.cellType != GridCellType.DeadCell;
            var checkTop = row - 1 < 0 || gridCellBoardData.GetCellAt(new GridPosition(row - 1, col))?.cellType != GridCellType.DeadCell;

            var checkTopLeft = row - 1 < 0 || col - 1 < 0
                || gridCellBoardData.GetCellAt(new GridPosition(row - 1, col - 1))?.cellType != GridCellType.DeadCell;
            var checkTopRight = row - 1 < 0 || col + 1 >= gridCellBoardData.width
                || gridCellBoardData.GetCellAt(new GridPosition(row - 1, col + 1))?.cellType != GridCellType.DeadCell;
            var checkBotLeft = row + 1 >= gridCellBoardData.height || col - 1 < 0
                || gridCellBoardData.GetCellAt(new GridPosition(row + 1, col - 1))?.cellType != GridCellType.DeadCell;
            var checkBotRight = row + 1 >= gridCellBoardData.height || col + 1 >= gridCellBoardData.width
                || gridCellBoardData.GetCellAt(new GridPosition(row + 1, col + 1))?.cellType != GridCellType.DeadCell;


            SetBorder(ResolveBorder(checkLeft, checkRight, checkTop, checkBot));

            if(!checkTop && !checkLeft && checkTopLeft)
            {
                SetCorner(BlockGridCornerType.TopLeftOut);
            }

            if (!checkTop && !checkRight && checkTopRight)
            {
                SetCorner(BlockGridCornerType.TopRightOut);
            }

            if (!checkBot && !checkRight && checkBotRight)
            {
                SetCorner(BlockGridCornerType.BottomRightOut);
            }

            if (!checkBot && !checkLeft && checkBotLeft)
            {
                SetCorner(BlockGridCornerType.BottomLeftOut);
            }
        }

        private static BlockGridBorderType ResolveBorder(bool checkLeft, bool checkRight, bool checkTop, bool checkBot)
        {
            if (checkLeft && checkRight && checkTop && checkBot) return BlockGridBorderType.ClosedBorder;
            if (!checkLeft && !checkRight && !checkTop && !checkBot) return BlockGridBorderType.Wall;
            if (!checkLeft && checkRight && checkTop && checkBot) return BlockGridBorderType.TopRightBottom;
            if (checkLeft && !checkRight && checkTop && checkBot) return BlockGridBorderType.BottomLeftTop;
            if (checkLeft && checkRight && !checkTop && checkBot) return BlockGridBorderType.RightBottomLeft;
            if (checkLeft && checkRight && checkTop && !checkBot) return BlockGridBorderType.LeftTopRight;
            if (!checkLeft && checkRight && !checkTop && checkBot) return BlockGridBorderType.BottomRight;
            if (checkLeft && !checkRight && !checkTop && checkBot) return BlockGridBorderType.BottomLeft;
            if (!checkLeft && checkRight && checkTop && !checkBot) return BlockGridBorderType.TopRight;
            if (checkLeft && !checkRight && checkTop && !checkBot) return BlockGridBorderType.TopLeft;
            if (!checkLeft && !checkRight && checkTop && checkBot) return BlockGridBorderType.TopBottom;
            if (checkLeft && checkRight && !checkTop && !checkBot) return BlockGridBorderType.LeftRight;
            if (checkLeft) return BlockGridBorderType.Left;
            if (checkRight) return BlockGridBorderType.Right;
            if (checkTop) return BlockGridBorderType.Top;
            return BlockGridBorderType.Bottom;
        }

        private void HideAllBorders()
        {
            if (m_listBorder == null)
            {
                return;
            }

            foreach (var border in m_listBorder.Values)
            {
                if (border != null)
                {
                    border.SetActive(false);
                }
            }

            foreach (var border in m_listCorner.Values)
            {
                if (border != null)
                {
                    border.SetActive(false);
                }
            }
        }

        private void SetCorner(BlockGridCornerType blockGridCornerType)
        {
            if (m_listCorner == null || !m_listCorner.TryGetValue(blockGridCornerType, out var corner) || corner == null)
            {
                return;
            }

            corner.SetActive(true);
        }

        private void SetBorder(BlockGridBorderType blockGridBorderType)
        {
            if (m_listBorder == null || !m_listBorder.TryGetValue(blockGridBorderType, out var border) || border == null)
            {
                return;
            }

            border.SetActive(true);
        }

        public void Clear()
        {
            if (boxController != null) { LeanPool.Despawn(boxController.gameObject); boxController = null; }
            if (pinController != null) { LeanPool.Despawn(pinController.gameObject); pinController = null; }
            if (keyController != null) { LeanPool.Despawn(keyController.gameObject); keyController = null; }
            if (lockController != null) { LeanPool.Despawn(lockController.gameObject); lockController = null; }
            if (tunnelController != null) { LeanPool.Despawn(tunnelController.gameObject); tunnelController = null; }
        }
    }
}
