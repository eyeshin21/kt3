using UnityEngine;
using UnityEditor;
using HexaFall.Gameplay.Data;
using HexaFall.Gameplay.Validation;
using HexaFall.Gameplay.Core;
using System.Collections.Generic;
using HexaFall.Gameplay.Runtime;
using HexaFall.Gameplay.Config;
using System.Threading.Tasks;

namespace HexaFall.Gameplay.Editor
{
    public class LevelDesignToolWindow : EditorWindow
    {
        private LevelData currentLevel;
        private GridPosition selectedCellPos = new GridPosition(-1, -1);
        private GridPosition selectedStackPos = new GridPosition(-1, -1);
        private GridPosition dragSourceBoxPos = new GridPosition(-1, -1);
        private GridPosition dragSourceStackPos = new GridPosition(-1, -1);
        private Dictionary<GridPosition, Vector2> stackScrollPos = new Dictionary<GridPosition, Vector2>();
        private Vector2 mainStackBoardScrollPos;

        private Vector2 scrollPos;
        private LevelValidator validator = new LevelValidator();

        private string[] availableLevelPaths = new string[0];
        private string[] availableLevelNames = new string[0];
        private int selectedLevelIndex = -1;

        private void OnEnable()
        {
            RefreshLevelList();
        }

        private void OnFocus()
        {
            RefreshLevelList();
        }

        private void RefreshLevelList()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            
            System.Array.Sort(guids, (a, b) => 
            {
                string nameA = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(a));
                string nameB = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(b));
                
                int numA = ExtractNumber(nameA);
                int numB = ExtractNumber(nameB);
                if (numA != -1 && numB != -1) return numA.CompareTo(numB);
                return nameA.CompareTo(nameB);
            });

            availableLevelPaths = new string[guids.Length];
            availableLevelNames = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                availableLevelPaths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
                availableLevelNames[i] = System.IO.Path.GetFileNameWithoutExtension(availableLevelPaths[i]);
            }

            if (currentLevel != null)
            {
                string currentPath = AssetDatabase.GetAssetPath(currentLevel);
                selectedLevelIndex = System.Array.IndexOf(availableLevelPaths, currentPath);
            }
            else if (guids.Length > 0)
            {
                selectedLevelIndex = 0;
                currentLevel = AssetDatabase.LoadAssetAtPath<LevelData>(availableLevelPaths[0]);
            }
            else
            {
                selectedLevelIndex = -1;
            }
        }

        private static int ExtractNumber(string name)
        {
            var match = System.Text.RegularExpressions.Regex.Match(name, @"\d+");
            if (match.Success) return int.Parse(match.Value);
            return -1;
        }

        private static Texture2D whiteTexture;
        public static Texture2D WhiteTexture
        {
            get
            {
                if (whiteTexture == null)
                {
                    whiteTexture = new Texture2D(1, 1);
                    whiteTexture.SetPixel(0, 0, Color.white);
                    whiteTexture.Apply();
                }
                return whiteTexture;
            }
        }

        [MenuItem("Window/Hexa Fall/Level Design Tool")]
        public static void ShowWindow()
        {
            GetWindow<LevelDesignToolWindow>("Level Design Tool");
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawAssetManagement();

            if (currentLevel != null)
            {
                if (Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
                {
                    Undo.RecordObject(currentLevel, "Level Design Edit");
                }

                EditorGUILayout.Space();
                DrawGeneralSettings();

                EditorGUILayout.Space();
                DrawStackBoardEditor();

                EditorGUILayout.Space();
                DrawGridCellEditor();

                EditorGUILayout.Space();
                DrawStatistics();

                EditorGUILayout.Space();
                DrawValidationAndPlay();
            }

            Event e = Event.current;

            // Global mouse up to clear drag state and selection if dropped outside
            if (e.type == EventType.MouseUp)
            {
                dragSourceBoxPos = new GridPosition(-1, -1);
                dragSourceStackPos = new GridPosition(-1, -1);
                selectedCellPos = new GridPosition(-1, -1);
                selectedStackPos = new GridPosition(-1, -1);
                GUI.FocusControl(null);
                Repaint();
            }

            // Clear selection on Escape key
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                selectedCellPos = new GridPosition(-1, -1);
                selectedStackPos = new GridPosition(-1, -1);
                dragSourceBoxPos = new GridPosition(-1, -1);
                dragSourceStackPos = new GridPosition(-1, -1);
                GUI.FocusControl(null);
                Repaint();
                e.Use();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawAssetManagement()
        {
            GUILayout.Label("Level Management", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (availableLevelNames != null && availableLevelNames.Length > 0)
            {
                int newIndex = EditorGUILayout.Popup(selectedLevelIndex, availableLevelNames);
                if (newIndex != selectedLevelIndex || currentLevel == null)
                {
                    selectedLevelIndex = newIndex;
                    if (selectedLevelIndex >= 0 && selectedLevelIndex < availableLevelPaths.Length)
                    {
                        currentLevel = AssetDatabase.LoadAssetAtPath<LevelData>(availableLevelPaths[selectedLevelIndex]);
                    }
                }
            }
            else
            {
                GUILayout.Label("No Levels Found", GUILayout.Width(150));
            }
            
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                RefreshLevelList();
            }

            if (GUILayout.Button("Create New Level", GUILayout.Width(120)))
            {
                CreateNewLevel();
            }
            if (GUILayout.Button("Import JSON", GUILayout.Width(100)))
            {
                ImportJsonLevel();
            }
            if (GUILayout.Button("Save", GUILayout.Width(60)))
            {
                if (currentLevel != null)
                {
                    EditorUtility.SetDirty(currentLevel);
                    AssetDatabase.SaveAssets();
                    Debug.Log("Level saved!");
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void CreateNewLevel()
        {
            var newLevel = CreateInstance<LevelData>();
            
            var so = new SerializedObject(newLevel);
            so.FindProperty("level").intValue = 1;
            so.FindProperty("waitingSlots").intValue = 5;

            var path = EditorUtility.SaveFilePanelInProject("Create New Level", $"Level_{currentLevel.level+1}", "asset", "Save new level data", "Assets/_MainModule/Resources/Data/Levels");
            if (string.IsNullOrEmpty(path)) return;

            AssetDatabase.CreateAsset(newLevel, path);
            AssetDatabase.SaveAssets();

            // Load it back to let Unity initialize the [Serializable] classes
            currentLevel = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            
            RefreshLevelList();

            currentLevel.gridCellBoardData.width = 10;
            currentLevel.gridCellBoardData.height = 10;
            currentLevel.gridCellBoardData.gridCells = new List<GridCellDefinition>();
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 10; c++)
                {
                    currentLevel.gridCellBoardData.gridCells.Add(new GridCellDefinition()
                    {
                        position = new GridPosition(r, c),
                        cellType = GridCellType.DeadCell,
                        box = new BoxDefinition() { capacity = 24, boxId = $"box_{r}_{c}", targetColor = ColorType.None }
                    });
                }
            }

            // Set stackboard default 7x6
            var stackBoardSO = new SerializedObject(currentLevel);
            stackBoardSO.FindProperty("stackBoard.columns").intValue = 7;
            stackBoardSO.FindProperty("stackBoard.rows").intValue = 6;
            stackBoardSO.ApplyModifiedProperties();
            
            // clear selected
            selectedCellPos = new GridPosition(-1, -1);
            selectedStackPos = new GridPosition(-1, -1);

            EditorUtility.SetDirty(currentLevel);
            AssetDatabase.SaveAssets();
        }

        private void DrawGeneralSettings()
        {
            GUILayout.Label("General Settings", EditorStyles.boldLabel);
            var so = new SerializedObject(currentLevel);
            EditorGUILayout.PropertyField(so.FindProperty("level"));
            EditorGUILayout.PropertyField(so.FindProperty("waitingSlots"));
            so.ApplyModifiedProperties();
        }

        private void DrawGridCellEditor()
        {
            GUILayout.Label("Box Grid Board (Top-Down View)", EditorStyles.boldLabel);
            if (currentLevel.gridCellBoardData == null) return;

            int oldWidth = currentLevel.gridCellBoardData.width;
            int oldHeight = currentLevel.gridCellBoardData.height;

            EditorGUILayout.BeginHorizontal();
            int newWidth = EditorGUILayout.DelayedIntField("Width", oldWidth);
            int newHeight = EditorGUILayout.DelayedIntField("Height", oldHeight);
            EditorGUILayout.EndHorizontal();

            if (newWidth != oldWidth || newHeight != oldHeight)
            {
                ResizeGridCells(newWidth, newHeight);
            }

            EditorGUILayout.Space();

            GUIStyle cellStyle = new GUIStyle(GUI.skin.box);
            cellStyle.normal.background = WhiteTexture;
            cellStyle.fixedWidth = 40;
            cellStyle.fixedHeight = 40;
            cellStyle.alignment = TextAnchor.MiddleCenter;
            cellStyle.fontStyle = FontStyle.Bold;

            for (int r = 0; r < currentLevel.gridCellBoardData.height; r++) // Draw top to bottom (row 0 is top)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < currentLevel.gridCellBoardData.width; c++)
                {
                    var cell = currentLevel.gridCellBoardData.GetCellAt(new GridPosition(r, c));
                    Color bgColor = Color.gray;
                    Color textColor = Color.white;
                    string label = "";

                    if (cell != null)
                    {
                        if (cell.cellType == GridCellType.DeadCell)
                        {
                            bgColor = Color.black;
                            label = "X";
                        }
                        else if (cell.cellType == GridCellType.StandardBox || cell.cellType == GridCellType.MysteryBox || cell.cellType == GridCellType.FrozenBox)
                        {
                            bgColor = GetColor(cell.box.targetColor);
                            textColor = GetTextColor(cell.box.targetColor);
                            if (cell.cellType == GridCellType.MysteryBox)
                                label = $"M{cell.box.capacity}";
                            else if (cell.cellType == GridCellType.FrozenBox)
                                label = $"F{cell.box.capacity}";
                            else
                                label = cell.box.capacity.ToString();
                        }
                        else if (cell.cellType == GridCellType.KeyCell)
                        {
                            var keyDef = currentLevel.keys?.Find(k => k.position.Row == r && k.position.Column == c);
                            bgColor = keyDef != null ? GetColor(keyDef.color) : Color.white;
                            textColor = keyDef != null ? GetTextColor(keyDef.color) : Color.black;
                            label = "Ky";
                        }
                        else if (cell.cellType == GridCellType.LockCell)
                        {
                            var lockDef = currentLevel.locks?.Find(k => k.position.Row == r && k.position.Column == c);
                            bgColor = lockDef != null ? GetColor(lockDef.color) : Color.white;
                            textColor = lockDef != null ? GetTextColor(lockDef.color) : Color.black;
                            label = "Lk";
                        }
                        else if (cell.cellType == GridCellType.TunnelCell)
                        {
                            bgColor = new Color(0.6f, 0.3f, 0f);
                            label = "Tn";
                        }
                        else if (cell.cellType == GridCellType.PinCell)
                        {
                            var pinDef = currentLevel.pins?.Find(p => p.headPosition.Row == r && p.headPosition.Column == c);
                            bgColor = new Color(0.8f, 0.8f, 0.8f);
                            textColor = Color.black;
                            string dir = "";
                            if (pinDef != null) {
                                if (pinDef.direction == PinDirection.LeftToRight) dir = "→";
                                else if (pinDef.direction == PinDirection.RightToLeft) dir = "←";
                                else if (pinDef.direction == PinDirection.UpToDown) dir = "↓";
                                else if (pinDef.direction == PinDirection.DownToUp) dir = "↑";
                            }
                            label = "Pn" + dir;
                        }
                        else if (cell.cellType == GridCellType.PinTailCell)
                        {
                            var pinDef = currentLevel.pins?.Find(p => p.pinId == cell.pinId);
                            bgColor = new Color(0.5f, 0.5f, 0.5f);
                            textColor = Color.black;
                            string dir = "";
                            if (pinDef != null) {
                                if (pinDef.direction == PinDirection.LeftToRight) dir = "→";
                                else if (pinDef.direction == PinDirection.RightToLeft) dir = "←";
                                else if (pinDef.direction == PinDirection.UpToDown) dir = "↓";
                                else if (pinDef.direction == PinDirection.DownToUp) dir = "↑";
                            }
                            label = "pt" + dir;
                        }
                        else if (cell.cellType == GridCellType.Empty)
                        {
                            bgColor = Color.gray;
                        }
                        else
                        {
                            bgColor = Color.white;
                            textColor = Color.black;
                            label = "?";
                        }
                    }

                    var oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = bgColor;
                    cellStyle.normal.textColor = textColor;
                    
                    bool isSelected = (selectedCellPos.Row == r && selectedCellPos.Column == c);
                    if (isSelected) GUI.backgroundColor = Color.magenta;

                    GUILayout.Box(label, cellStyle);
                    GUI.backgroundColor = oldColor;

                    var cellRect = GUILayoutUtility.GetLastRect();
                    
                    // Handle drag and swap
                    if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                    {
                        dragSourceBoxPos = new GridPosition(r, c);
                        GUI.FocusControl(null);
                        Event.current.Use();
                    }
                    else if (Event.current.type == EventType.MouseUp && cellRect.Contains(Event.current.mousePosition))
                    {
                        if (dragSourceBoxPos.Row != -1 && (dragSourceBoxPos.Row != r || dragSourceBoxPos.Column != c))
                        {
                            var dragSourceCell = currentLevel.gridCellBoardData.GetCellAt(dragSourceBoxPos);
                            if (dragSourceCell != null && dragSourceCell.cellType == GridCellType.PinCell)
                            {
                                ConfigurePinFromDrag(dragSourceBoxPos, new GridPosition(r, c));
                            }
                            else
                            {
                                SwapBoxCells(dragSourceBoxPos, new GridPosition(r, c));
                            }
                        }
                        selectedCellPos = new GridPosition(r, c);
                        selectedStackPos = new GridPosition(-1, -1); // deselect stack
                        dragSourceBoxPos = new GridPosition(-1, -1);
                        Event.current.Use();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            DrawSelectedCellInspector();
        }

        private void SwapBoxCells(GridPosition a, GridPosition b)
        {
            var cellA = currentLevel.gridCellBoardData.GetCellAt(a);
            var cellB = currentLevel.gridCellBoardData.GetCellAt(b);
            if (cellA != null && cellB != null)
            {
                // Swap grid positions directly to preserve all fields (isHidden, frozenDurability, pinId, etc.)
                cellA.position = b;
                cellB.position = a;

                // Update mechanics coordinates
                if (currentLevel.tunnels != null)
                {
                    foreach (var t in currentLevel.tunnels)
                    {
                        if (t.position == a) t.position = b;
                        else if (t.position == b) t.position = a;
                    }
                }
                if (currentLevel.pins != null)
                {
                    foreach (var p in currentLevel.pins)
                    {
                        if (p.headPosition == a) p.headPosition = b;
                        else if (p.headPosition == b) p.headPosition = a;
                    }
                }
                if (currentLevel.keys != null)
                {
                    foreach (var k in currentLevel.keys)
                    {
                        if (k.position == a) k.position = b;
                        else if (k.position == b) k.position = a;
                    }
                }
                if (currentLevel.locks != null)
                {
                    foreach (var l in currentLevel.locks)
                    {
                        if (l.position == a) l.position = b;
                        else if (l.position == b) l.position = a;
                    }
                }

                EditorUtility.SetDirty(currentLevel);
            }
        }

        private void ConfigurePinFromDrag(GridPosition start, GridPosition end)
        {
            var cellStart = currentLevel.gridCellBoardData.GetCellAt(start);
            if (cellStart == null || cellStart.cellType != GridCellType.PinCell) return;
            
            var pinDef = currentLevel.pins?.Find(p => p.headPosition.Row == start.Row && p.headPosition.Column == start.Column);
            if (pinDef == null) return;
            
            int dRow = end.Row - start.Row;
            int dCol = end.Column - start.Column;
            
            if (Mathf.Abs(dRow) > 0 && Mathf.Abs(dCol) > 0)
            {
                if (Mathf.Abs(dRow) > Mathf.Abs(dCol)) dCol = 0;
                else dRow = 0;
            }
            
            if (dRow > 0) { pinDef.direction = PinDirection.UpToDown; pinDef.length = dRow + 1; }
            else if (dRow < 0) { pinDef.direction = PinDirection.DownToUp; pinDef.length = -dRow + 1; }
            else if (dCol > 0) { pinDef.direction = PinDirection.LeftToRight; pinDef.length = dCol + 1; }
            else if (dCol < 0) { pinDef.direction = PinDirection.RightToLeft; pinDef.length = -dCol + 1; }
            
            foreach(var cell in currentLevel.gridCellBoardData.gridCells)
            {
                if (cell.cellType == GridCellType.PinTailCell && cell.pinId == pinDef.pinId)
                {
                    cell.cellType = GridCellType.Empty;
                    cell.pinId = "";
                }
            }
            
            for (int i = 1; i < pinDef.length; i++)
            {
                int r = start.Row + (dRow != 0 ? (dRow > 0 ? i : -i) : 0);
                int c = start.Column + (dCol != 0 ? (dCol > 0 ? i : -i) : 0);
                var tailCell = currentLevel.gridCellBoardData.GetCellAt(new GridPosition(r, c));
                if (tailCell != null && tailCell.cellType != GridCellType.DeadCell)
                {
                    tailCell.cellType = GridCellType.PinTailCell;
                    tailCell.pinId = pinDef.pinId;
                }
            }
            
            EditorUtility.SetDirty(currentLevel);
        }

        private void ResizeGridCells(int newWidth, int newHeight)
        {
            var newList = new List<GridCellDefinition>();
            int oldWidth = currentLevel.gridCellBoardData.width;

            var existingMap = new Dictionary<GridPosition, GridCellDefinition>();
            foreach (var cell in currentLevel.gridCellBoardData.gridCells)
            {
                existingMap[cell.position] = cell;
            }
            
            for (int r = 0; r < newHeight; r++)
            {
                for (int c = 0; c < newWidth; c++)
                {
                    int old_c = c - (newWidth / 2) + (oldWidth / 2);
                    int old_r = r;
                    var oldPos = new GridPosition(old_r, old_c);
                    
                    if (existingMap.TryGetValue(oldPos, out var existing))
                    {
                        existing.position = new GridPosition(r, c);
                        newList.Add(existing);
                        
                        // Update mechanics positions!
                        if (existing.cellType == GridCellType.TunnelCell)
                        {
                            var t = currentLevel.tunnels?.Find(x => x.tunnelId == existing.tunnelId);
                            if (t != null) t.position = existing.position;
                        }
                        else if (existing.cellType == GridCellType.PinCell)
                        {
                            var p = currentLevel.pins?.Find(x => x.pinId == existing.pinId);
                            if (p != null) p.headPosition = existing.position;
                        }
                        else if (existing.cellType == GridCellType.KeyCell)
                        {
                            var k = currentLevel.keys?.Find(x => x.keyId == existing.keyId);
                            if (k != null) k.position = existing.position;
                        }
                        else if (existing.cellType == GridCellType.LockCell)
                        {
                            var l = currentLevel.locks?.Find(x => x.lockId == existing.lockId);
                            if (l != null) l.position = existing.position;
                        }
                    }
                    else
                    {
                        newList.Add(new GridCellDefinition()
                        {
                            position = new GridPosition(r, c),
                            cellType = GridCellType.DeadCell,
                            box = new BoxDefinition() { capacity = 24, boxId = $"box_{r}_{c}", targetColor = ColorType.None }
                        });
                    }
                }
            }
            currentLevel.gridCellBoardData.width = newWidth;
            currentLevel.gridCellBoardData.height = newHeight;
            currentLevel.gridCellBoardData.gridCells = newList;
            
            CleanupObsoleteMechanics();
            
            EditorUtility.SetDirty(currentLevel);
        }

        private void CleanupObsoleteMechanics()
        {
            if (currentLevel.tunnels != null)
            {
                for (int i = currentLevel.tunnels.Count - 1; i >= 0; i--)
                {
                    var tunnel = currentLevel.tunnels[i];
                    var cell = currentLevel.gridCellBoardData.GetCellAt(tunnel.position);
                    if (cell == null || cell.cellType != GridCellType.TunnelCell || cell.tunnelId != tunnel.tunnelId)
                    {
                        currentLevel.tunnels.RemoveAt(i);
                    }
                }
            }

            if (currentLevel.pins != null)
            {
                for (int i = currentLevel.pins.Count - 1; i >= 0; i--)
                {
                    var pin = currentLevel.pins[i];
                    var cell = currentLevel.gridCellBoardData.GetCellAt(pin.headPosition);
                    if (cell == null || cell.cellType != GridCellType.PinCell || cell.pinId != pin.pinId)
                    {
                        currentLevel.pins.RemoveAt(i);
                    }
                }
            }

            if (currentLevel.keys != null)
            {
                for (int i = currentLevel.keys.Count - 1; i >= 0; i--)
                {
                    var key = currentLevel.keys[i];
                    var cell = currentLevel.gridCellBoardData.GetCellAt(key.position);
                    if (cell == null || cell.cellType != GridCellType.KeyCell || cell.keyId != key.keyId)
                    {
                        currentLevel.keys.RemoveAt(i);
                    }
                }
            }

            if (currentLevel.locks != null)
            {
                for (int i = currentLevel.locks.Count - 1; i >= 0; i--)
                {
                    var loc = currentLevel.locks[i];
                    var cell = currentLevel.gridCellBoardData.GetCellAt(loc.position);
                    if (cell == null || cell.cellType != GridCellType.LockCell || cell.lockId != loc.lockId)
                    {
                        currentLevel.locks.RemoveAt(i);
                    }
                }
            }
        }

        private void CleanupCellData(GridCellDefinition cell, GridCellType oldType)
        {
            if (oldType == GridCellType.TunnelCell)
            {
                currentLevel.tunnels?.RemoveAll(t => t.position.Row == cell.position.Row && t.position.Column == cell.position.Column);
                cell.tunnelId = "";
            }
            else if (oldType == GridCellType.KeyCell)
            {
                currentLevel.keys?.RemoveAll(k => k.position.Row == cell.position.Row && k.position.Column == cell.position.Column);
                cell.keyId = "";
            }
            else if (oldType == GridCellType.LockCell)
            {
                currentLevel.locks?.RemoveAll(l => l.position.Row == cell.position.Row && l.position.Column == cell.position.Column);
                cell.lockId = "";
            }
            else if (oldType == GridCellType.PinCell || oldType == GridCellType.PinTailCell)
            {
                string pId = cell.pinId;
                if (!string.IsNullOrEmpty(pId))
                {
                    currentLevel.pins?.RemoveAll(p => p.pinId == pId);
                    foreach (var c in currentLevel.gridCellBoardData.gridCells)
                    {
                        if (c.pinId == pId && (c.cellType == GridCellType.PinCell || c.cellType == GridCellType.PinTailCell))
                        {
                            c.cellType = GridCellType.Empty;
                            c.pinId = "";
                        }
                    }
                }
            }

            if (cell.box != null)
            {
                if (oldType == GridCellType.MysteryBox) cell.box.isHidden = false;
                if (oldType == GridCellType.FrozenBox) cell.box.frozenDurability = 0;
            }
        }

        private void DrawSelectedCellInspector()
        {
            if (selectedCellPos.Row == -1) return;

            var cell = currentLevel.gridCellBoardData.GetCellAt(selectedCellPos);
            if (cell == null) return;

            EditorGUILayout.Space();
            GUILayout.Label($"Selected Box Cell: Row {selectedCellPos.Row}, Col {selectedCellPos.Column}", EditorStyles.boldLabel);

            GridCellType oldType = cell.cellType;
            EditorGUI.BeginChangeCheck();
            GridCellType newType = (GridCellType)EditorGUILayout.EnumPopup("Cell Type", cell.cellType);

            if (newType != oldType)
            {
                CleanupCellData(cell, oldType);
                cell.cellType = newType;
            }

            if (currentLevel.keys == null) currentLevel.keys = new System.Collections.Generic.List<KeyDefinition>();
            if (currentLevel.locks == null) currentLevel.locks = new System.Collections.Generic.List<LockDefinition>();
            if (currentLevel.tunnels == null) currentLevel.tunnels = new System.Collections.Generic.List<TunnelDefinition>();
            if (currentLevel.pins == null) currentLevel.pins = new System.Collections.Generic.List<PinDefinition>();

            if (cell.cellType == GridCellType.StandardBox || cell.cellType == GridCellType.MysteryBox || cell.cellType == GridCellType.FrozenBox)
            {
                if (cell.box == null) cell.box = new BoxDefinition();
                cell.box.boxId = EditorGUILayout.TextField("Box ID", cell.box.boxId);
                cell.box.targetColor = (ColorType)EditorGUILayout.EnumPopup("Target Color", cell.box.targetColor);
                cell.box.capacity = EditorGUILayout.IntField("Capacity", cell.box.capacity);
                
                if (cell.cellType == GridCellType.MysteryBox)
                    cell.box.isHidden = EditorGUILayout.Toggle("Is Hidden", cell.box.isHidden);
                
                if (cell.cellType == GridCellType.FrozenBox)
                    cell.box.frozenDurability = EditorGUILayout.IntField("Frozen Durability", cell.box.frozenDurability);
            }
            else if (cell.cellType == GridCellType.KeyCell)
            {
                var keyDef = currentLevel.keys.Find(k => k.position.Row == cell.position.Row && k.position.Column == cell.position.Column);
                if (keyDef == null)
                {
                    keyDef = new KeyDefinition { position = cell.position, keyId = $"key_{cell.position.Row}_{cell.position.Column}", color = ColorType.Red };
                    currentLevel.keys.Add(keyDef);
                }
                keyDef.keyId = EditorGUILayout.TextField("Key ID", keyDef.keyId);
                keyDef.color = (ColorType)EditorGUILayout.EnumPopup("Key Color", keyDef.color);
                cell.keyId = keyDef.keyId;
            }
            else if (cell.cellType == GridCellType.LockCell)
            {
                var lockDef = currentLevel.locks.Find(k => k.position.Row == cell.position.Row && k.position.Column == cell.position.Column);
                if (lockDef == null)
                {
                    lockDef = new LockDefinition { position = cell.position, lockId = $"lock_{cell.position.Row}_{cell.position.Column}", color = ColorType.Red };
                    currentLevel.locks.Add(lockDef);
                }
                lockDef.lockId = EditorGUILayout.TextField("Lock ID", lockDef.lockId);
                lockDef.color = (ColorType)EditorGUILayout.EnumPopup("Lock Color", lockDef.color);
                cell.lockId = lockDef.lockId;
            }
            else if (cell.cellType == GridCellType.TunnelCell)
            {
                var tunnelDef = currentLevel.tunnels.Find(t => t.position.Row == cell.position.Row && t.position.Column == cell.position.Column);
                if (tunnelDef == null)
                {
                    tunnelDef = new TunnelDefinition { position = cell.position, tunnelId = $"tunnel_{cell.position.Row}_{cell.position.Column}", direction = FacingDirection.Down, contents = new System.Collections.Generic.List<BoxDefinition>() };
                    currentLevel.tunnels.Add(tunnelDef);
                }
                tunnelDef.tunnelId = EditorGUILayout.TextField("Tunnel ID", tunnelDef.tunnelId);
                tunnelDef.direction = (FacingDirection)EditorGUILayout.EnumPopup("Direction", tunnelDef.direction);
                cell.tunnelId = tunnelDef.tunnelId;

                var so = new SerializedObject(currentLevel);
                var tunnelsProp = so.FindProperty("tunnels");
                for (int i = 0; i < tunnelsProp.arraySize; i++)
                {
                    var tProp = tunnelsProp.GetArrayElementAtIndex(i);
                    if (tProp.FindPropertyRelative("position.row").intValue == cell.position.Row &&
                        tProp.FindPropertyRelative("position.column").intValue == cell.position.Column)
                    {
                        EditorGUILayout.PropertyField(tProp.FindPropertyRelative("contents"), new GUIContent("Tunnel Contents"), true);
                        so.ApplyModifiedProperties();
                        
                        if (tunnelDef.contents != null)
                        {
                            bool modified = false;
                            for (int j = 0; j < tunnelDef.contents.Count; j++)
                            {
                                var b = tunnelDef.contents[j];
                                if (b != null)
                                {
                                    string expectedId = $"tunnel_{cell.position.Row}_{cell.position.Column}_box_{j}";
                                    if (b.boxId != expectedId)
                                    {
                                        b.boxId = expectedId;
                                        modified = true;
                                    }
                                }
                            }
                            if (modified) EditorUtility.SetDirty(currentLevel);
                        }
                        
                        break;
                    }
                }
            }
            else if (cell.cellType == GridCellType.PinCell)
            {
                var pinDef = currentLevel.pins.Find(p => p.headPosition.Row == cell.position.Row && p.headPosition.Column == cell.position.Column);
                if (pinDef == null)
                {
                    pinDef = new PinDefinition { headPosition = cell.position, pinId = $"pin_{cell.position.Row}_{cell.position.Column}", direction = PinDirection.LeftToRight, length = 2 };
                    currentLevel.pins.Add(pinDef);
                }
                pinDef.pinId = EditorGUILayout.TextField("Pin ID", pinDef.pinId);
                pinDef.direction = (PinDirection)EditorGUILayout.EnumPopup("Direction", pinDef.direction);
                pinDef.length = EditorGUILayout.IntField("Length", pinDef.length);
                cell.pinId = pinDef.pinId;
            }
            else if (cell.cellType == GridCellType.PinTailCell)
            {
                cell.pinId = EditorGUILayout.TextField("Pin ID (Link)", cell.pinId);
                EditorGUILayout.LabelField("Tail cell, linked by Pin ID.", EditorStyles.wordWrappedLabel);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(currentLevel);
            }
        }

        private void DrawStackBoardEditor()
        {
            GUILayout.Label("Stack Board (Top-Down View)", EditorStyles.boldLabel);
            if (currentLevel.stackBoard == null) return;

            int cols = currentLevel.stackBoard.Columns;
            int rows = currentLevel.stackBoard.Rows;

            EditorGUILayout.BeginHorizontal();
            int newCols = EditorGUILayout.DelayedIntField("Columns", cols);
            int newRows = EditorGUILayout.DelayedIntField("Rows", rows);
            EditorGUILayout.EndHorizontal();

            if (newCols != cols || newRows != rows)
            {
                var so = new SerializedObject(currentLevel);
                so.FindProperty("stackBoard.columns").intValue = newCols;
                so.FindProperty("stackBoard.rows").intValue = newRows;
                so.ApplyModifiedProperties();
                
                ResizeStackGrid(cols, rows, newCols, newRows);
                cols = newCols;
                rows = newRows;
            }

            mainStackBoardScrollPos = EditorGUILayout.BeginScrollView(mainStackBoardScrollPos, GUILayout.Height(400));

            for (int r = rows - 1; r >= 0; r--) // Draw top to bottom
            {
                EditorGUILayout.BeginHorizontal();
                int colsInRow = (r % 2 == 1) ? cols - 1 : cols;
                if (r % 2 == 1)
                {
                    GUILayout.Space(22);
                }

                for (int c = 0; c < colsInRow; c++)
                {
                    var stack = GetStackAt(r, c);
                    
                    var prevColor = GUI.backgroundColor;
                    if (selectedStackPos.Row == r && selectedStackPos.Column == c)
                        GUI.backgroundColor = Color.magenta;
                    else
                        GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

                    EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(44), GUILayout.Height(70));
                    GUI.backgroundColor = prevColor;

                    if (stack != null && stack.blocksBottomToTop.Count > 0)
                    {
                        var groups = new List<(ColorType color, int count)>();
                        foreach(var col in stack.blocksBottomToTop)
                        {
                            if (groups.Count > 0 && groups[groups.Count - 1].color == col)
                                groups[groups.Count - 1] = (col, groups[groups.Count - 1].count + 1);
                            else
                                groups.Add((col, 1));
                        }
                        
                        var pos = new GridPosition(r, c);
                        if (!stackScrollPos.TryGetValue(pos, out var sPos)) sPos = Vector2.zero;
                        
                        if (stack.isHidden)
                        {
                            GUIStyle mysteryStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                            mysteryStyle.normal.textColor = Color.yellow;
                            GUILayout.Label("?", mysteryStyle, GUILayout.Height(14));
                        }

                        // We use a scrollview without scrollbars to fit everything cleanly
                        sPos = EditorGUILayout.BeginScrollView(sPos, GUIStyle.none, GUIStyle.none);

                        for (int i = groups.Count - 1; i >= 0; i--)
                        {
                            var prevBg = GUI.backgroundColor;
                            GUI.backgroundColor = GetColor(groups[i].color);
                            GUIStyle blockStyle = new GUIStyle(GUI.skin.box);
                            blockStyle.normal.background = WhiteTexture;
                            blockStyle.normal.textColor = GetTextColor(groups[i].color);
                            blockStyle.fontStyle = FontStyle.Bold;
                            blockStyle.margin = new RectOffset(0, 0, 0, 0);
                            GUILayout.Box(groups[i].count.ToString(), blockStyle, GUILayout.Width(36), GUILayout.Height(16));
                            GUI.backgroundColor = prevBg;
                        }

                        EditorGUILayout.EndScrollView();
                        stackScrollPos[pos] = sPos;
                    }
                    else
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("0", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
                        GUILayout.FlexibleSpace();
                    }

                    EditorGUILayout.EndVertical();

                    var cellRect = GUILayoutUtility.GetLastRect();
                    if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                    {
                        dragSourceStackPos = new GridPosition(r, c);
                        GUI.FocusControl(null);
                        Event.current.Use();
                    }
                    else if (Event.current.type == EventType.MouseUp && cellRect.Contains(Event.current.mousePosition))
                    {
                        if (dragSourceStackPos.Row != -1 && (dragSourceStackPos.Row != r || dragSourceStackPos.Column != c))
                        {
                            SwapStackCells(dragSourceStackPos, new GridPosition(r, c));
                        }
                        selectedStackPos = new GridPosition(r, c);
                        selectedCellPos = new GridPosition(-1, -1);
                        dragSourceStackPos = new GridPosition(-1, -1);
                        Event.current.Use();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            DrawSelectedStackInspector();
        }

        private void SwapStackCells(GridPosition a, GridPosition b)
        {
            var stackA = GetStackAt(a.Row, a.Column);
            var stackB = GetStackAt(b.Row, b.Column);

            if (stackA == null) stackA = CreateStackAt(a.Row, a.Column);
            if (stackB == null) stackB = CreateStackAt(b.Row, b.Column);

            stackA.position = b;
            stackB.position = a;

            EditorUtility.SetDirty(currentLevel);
        }

        private StackDefinition CreateStackAt(int r, int c)
        {
            var so = new SerializedObject(currentLevel);
            var stacksProp = so.FindProperty("stackBoard.stacks");
            int index = stacksProp.arraySize;
            stacksProp.InsertArrayElementAtIndex(index);
            var newStackProp = stacksProp.GetArrayElementAtIndex(index);
            newStackProp.FindPropertyRelative("useExplicitPosition").boolValue = true;
            newStackProp.FindPropertyRelative("position.row").intValue = r;
            newStackProp.FindPropertyRelative("position.column").intValue = c;
            newStackProp.FindPropertyRelative("blocksBottomToTop").ClearArray();
            so.ApplyModifiedProperties();
            return GetStackAt(r, c);
        }

        private void ResizeStackGrid(int oldCols, int oldRows, int newCols, int newRows)
        {
            var so = new SerializedObject(currentLevel);
            var stacksProp = so.FindProperty("stackBoard.stacks");
            
            var existingStacks = new Dictionary<GridPosition, StackDefinition>();
            foreach (var stack in currentLevel.stackBoard.Stacks)
            {
                if (stack.useExplicitPosition)
                {
                    existingStacks[stack.position] = stack;
                }
            }

            stacksProp.ClearArray();

            for (int r = 0; r < newRows; r++)
            {
                int colsInRow = (r % 2 == 1) ? newCols - 1 : newCols;
                for (int c = 0; c < colsInRow; c++)
                {
                    int old_c = c - (newCols / 2) + (oldCols / 2);
                    int old_r = r;
                    var oldPos = new GridPosition(old_r, old_c);

                    if (existingStacks.TryGetValue(oldPos, out var existingStack))
                    {
                        int index = stacksProp.arraySize;
                        stacksProp.InsertArrayElementAtIndex(index);
                        var newStackProp = stacksProp.GetArrayElementAtIndex(index);
                        newStackProp.FindPropertyRelative("useExplicitPosition").boolValue = true;
                        newStackProp.FindPropertyRelative("position.row").intValue = r;
                        newStackProp.FindPropertyRelative("position.column").intValue = c;
                        
                        var blocksProp = newStackProp.FindPropertyRelative("blocksBottomToTop");
                        blocksProp.ClearArray();
                        for (int i = 0; i < existingStack.blocksBottomToTop.Count; i++)
                        {
                            blocksProp.InsertArrayElementAtIndex(i);
                            blocksProp.GetArrayElementAtIndex(i).intValue = (int)existingStack.blocksBottomToTop[i];
                        }
                    }
                }
            }
            so.ApplyModifiedProperties();
        }

        private StackDefinition GetStackAt(int r, int c)
        {
            if (currentLevel.stackBoard.Stacks == null) return null;
            foreach (var s in currentLevel.stackBoard.Stacks)
            {
                if (s.useExplicitPosition && s.position.Row == r && s.position.Column == c)
                    return s;
            }
            return null;
        }

        private void DrawSelectedStackInspector()
        {
            if (selectedStackPos.Row == -1) return;

            var stack = GetStackAt(selectedStackPos.Row, selectedStackPos.Column);
            
            EditorGUILayout.Space();
            GUILayout.Label($"Selected Stack: Row {selectedStackPos.Row}, Col {selectedStackPos.Column}", EditorStyles.boldLabel);

            if (stack == null)
            {
                if (GUILayout.Button("Create Stack Here"))
                {
                    var so = new SerializedObject(currentLevel);
                    var stacksProp = so.FindProperty("stackBoard.stacks");
                    int index = stacksProp.arraySize;
                    stacksProp.InsertArrayElementAtIndex(index);
                    var newStackProp = stacksProp.GetArrayElementAtIndex(index);
                    
                    newStackProp.FindPropertyRelative("useExplicitPosition").boolValue = true;
                    newStackProp.FindPropertyRelative("position.row").intValue = selectedStackPos.Row;
                    newStackProp.FindPropertyRelative("position.column").intValue = selectedStackPos.Column;
                    newStackProp.FindPropertyRelative("blocksBottomToTop").ClearArray();
                    so.ApplyModifiedProperties();
                }
                return;
            }

            // Edit stack properties
            var so2 = new SerializedObject(currentLevel);
            var stacksProp2 = so2.FindProperty("stackBoard.stacks");
            for(int i=0; i<stacksProp2.arraySize; i++)
            {
                var sProp = stacksProp2.GetArrayElementAtIndex(i);
                if (sProp.FindPropertyRelative("position.row").intValue == selectedStackPos.Row &&
                    sProp.FindPropertyRelative("position.column").intValue == selectedStackPos.Column)
                {
                    EditorGUILayout.PropertyField(sProp.FindPropertyRelative("isHidden"));
                    so2.ApplyModifiedProperties();
                    break;
                }
            }
            
            // Quick add colors
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Add Block:", GUILayout.Width(80));
            foreach (ColorType col in System.Enum.GetValues(typeof(ColorType)))
            {
                if (col == ColorType.None) continue;
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = GetColor(col);
                if (GUILayout.Button(col.ToString(), GUILayout.Width(50)))
                {
                    var so = new SerializedObject(currentLevel);
                    var stacksProp = so.FindProperty("stackBoard.stacks");
                    for(int i=0; i<stacksProp.arraySize; i++)
                    {
                        var sProp = stacksProp.GetArrayElementAtIndex(i);
                        if (sProp.FindPropertyRelative("position.row").intValue == selectedStackPos.Row &&
                            sProp.FindPropertyRelative("position.column").intValue == selectedStackPos.Column)
                        {
                            var tunningConfig = AssetDatabase.LoadAssetAtPath<GameplayTuningConfig>("Assets/_MainModule/Data/Configs/GameplayTuningConfig.asset");
                            int maxBlocks = tunningConfig != null ? tunningConfig.MaximumStackBlocks : 10;
                            var blocksProp = sProp.FindPropertyRelative("blocksBottomToTop");
                            if (blocksProp.arraySize >= maxBlocks)
                            {
                                EditorUtility.DisplayDialog("Limit Reached", $"A stack cannot exceed {maxBlocks} blocks.", "OK");
                                break;
                            }
                            blocksProp.InsertArrayElementAtIndex(blocksProp.arraySize);
                            blocksProp.GetArrayElementAtIndex(blocksProp.arraySize - 1).enumValueIndex = (int)col;
                            break;
                        }
                    }
                    so.ApplyModifiedProperties();
                }
                GUI.backgroundColor = oldColor;
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Delete Stack"))
            {
                var so = new SerializedObject(currentLevel);
                var stacksProp = so.FindProperty("stackBoard.stacks");
                for(int i=0; i<stacksProp.arraySize; i++)
                {
                    var sProp = stacksProp.GetArrayElementAtIndex(i);
                    if (sProp.FindPropertyRelative("position.row").intValue == selectedStackPos.Row &&
                        sProp.FindPropertyRelative("position.column").intValue == selectedStackPos.Column)
                    {
                        stacksProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
                so.ApplyModifiedProperties();
                return; // deleted, nothing more to draw
            }

            // Draw block list
            EditorGUILayout.LabelField("Blocks (Bottom to Top):");
            for (int i = 0; i < stack.blocksBottomToTop.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"[{i}]", GUILayout.Width(30));
                
                var oldBg = GUI.backgroundColor;
                GUI.backgroundColor = GetColor(stack.blocksBottomToTop[i]);
                GUIStyle blockStyle = new GUIStyle(GUI.skin.box);
                blockStyle.normal.background = WhiteTexture;
                blockStyle.normal.textColor = GetTextColor(stack.blocksBottomToTop[i]);
                blockStyle.fontStyle = FontStyle.Bold;
                GUILayout.Box(stack.blocksBottomToTop[i].ToString(), blockStyle, GUILayout.Width(100));
                GUI.backgroundColor = oldBg;

                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    var so = new SerializedObject(currentLevel);
                    var stacksProp = so.FindProperty("stackBoard.stacks");
                    for(int sIdx=0; sIdx<stacksProp.arraySize; sIdx++)
                    {
                        var sProp = stacksProp.GetArrayElementAtIndex(sIdx);
                        if (sProp.FindPropertyRelative("position.row").intValue == selectedStackPos.Row &&
                            sProp.FindPropertyRelative("position.column").intValue == selectedStackPos.Column)
                        {
                            var blocksProp = sProp.FindPropertyRelative("blocksBottomToTop");
                            blocksProp.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }
                    so.ApplyModifiedProperties();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private LevelValidationResult lastValidationResult;
        private Vector2 validationScrollPos;
        private float validationLogHeight = 150f;
        private bool isResizingLog = false;

        private void DrawValidationAndPlay()
        {
            GUILayout.Label("Validation & Play", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate Level", GUILayout.Height(30)))
            {
                var tunningConfig = AssetDatabase.LoadAssetAtPath<GameplayTuningConfig>("Assets/_MainModule/Data/Configs/GameplayTuningConfig.asset");
                lastValidationResult = validator.Validate(currentLevel, tunningConfig);
            }

            if (GUILayout.Button("Auto Fix", GUILayout.Height(30)))
            {
                AutoFixLevel();
            }

            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Play Level", GUILayout.Height(30)))
            {
                PlayLevel();
            }
            GUI.backgroundColor = oldColor;
            EditorGUILayout.EndHorizontal();

            if (lastValidationResult != null)
            {
                EditorGUILayout.Space();
                if (lastValidationResult.IsValid)
                {
                    EditorGUILayout.HelpBox("Level is valid! No errors found.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox($"Found {lastValidationResult.Errors.Count} error(s):", MessageType.Error);
                    
                    // Draggable resize handle at the top
                    var resizeRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(5), GUILayout.ExpandWidth(true));
                    EditorGUI.DrawRect(resizeRect, new Color(0.3f, 0.3f, 0.3f, 1f));
                    EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeVertical);

                    Event e = Event.current;
                    if (e.type == EventType.MouseDown && resizeRect.Contains(e.mousePosition))
                    {
                        isResizingLog = true;
                        e.Use();
                    }
                    if (isResizingLog && e.type == EventType.MouseDrag)
                    {
                        // Dragging down (positive Y) increases height
                        validationLogHeight += e.delta.y;
                        validationLogHeight = Mathf.Max(50f, validationLogHeight);
                        Repaint();
                        e.Use();
                    }
                    if (e.type == EventType.MouseUp)
                    {
                        isResizingLog = false;
                    }

                    validationScrollPos = EditorGUILayout.BeginScrollView(validationScrollPos, GUI.skin.box, GUILayout.Height(validationLogHeight));
                    foreach (var err in lastValidationResult.Errors)
                    {
                        EditorGUILayout.LabelField($"- {err}", EditorStyles.wordWrappedLabel);
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void AutoFixLevel()
        {
            if (currentLevel == null) return;
            var tuning = AssetDatabase.LoadAssetAtPath<GameplayTuningConfig>("Assets/_MainModule/Data/Configs/GameplayTuningConfig.asset");
            if (tuning == null) return;

            Undo.RecordObject(currentLevel, "Auto Fix Level");
            bool changed = false;

            // Fix waiting slots
            if (currentLevel.waitingSlots < tuning.MinimumWaitingSlots) { currentLevel.waitingSlots = tuning.MinimumWaitingSlots; changed = true; }
            if (currentLevel.waitingSlots > tuning.MaximumWaitingSlots) { currentLevel.waitingSlots = tuning.MaximumWaitingSlots; changed = true; }

            // Fix grid boxes
            if (currentLevel.gridCellBoardData != null && currentLevel.gridCellBoardData.gridCells != null)
            {
                foreach (var cell in currentLevel.gridCellBoardData.gridCells)
                {
                    if (cell.box != null)
                    {
                        if (cell.box.capacity <= 0) { cell.box.capacity = tuning.DefaultBoxCapacity; changed = true; }
                        if (cell.box.capacity > tuning.MaximumBoxCapacity) { cell.box.capacity = tuning.MaximumBoxCapacity; changed = true; }
                        
                        if (cell.cellType == GridCellType.FrozenBox && cell.box.frozenDurability < 1)
                        {
                            cell.box.frozenDurability = 1; changed = true;
                        }

                        if (cell.cellType != GridCellType.MysteryBox && cell.box.isHidden)
                        {
                            cell.box.isHidden = false; changed = true;
                        }
                        if (cell.cellType != GridCellType.FrozenBox && cell.box.frozenDurability > 0)
                        {
                            cell.box.frozenDurability = 0; changed = true;
                        }
                    }
                }
            }

            // Fix tunnel boxes
            if (currentLevel.tunnels != null)
            {
                foreach (var t in currentLevel.tunnels)
                {
                    if (t.contents != null)
                    {
                        foreach (var b in t.contents)
                        {
                            if (b.capacity <= 0) { b.capacity = tuning.DefaultBoxCapacity; changed = true; }
                            if (b.capacity > tuning.MaximumBoxCapacity) { b.capacity = tuning.MaximumBoxCapacity; changed = true; }
                        }
                    }
                }
            }

            // Fix stacks
            if (currentLevel.stackBoard != null)
            {
                var so = new SerializedObject(currentLevel);
                var stacksProp = so.FindProperty("stackBoard.stacks");
                if (stacksProp != null)
                {
                    for (int i = stacksProp.arraySize - 1; i >= 0; i--)
                    {
                        var stackProp = stacksProp.GetArrayElementAtIndex(i);
                        var blocksProp = stackProp.FindPropertyRelative("blocksBottomToTop");

                        if (blocksProp != null)
                        {
                            for (int b = blocksProp.arraySize - 1; b >= 0; b--)
                            {
                                if (blocksProp.GetArrayElementAtIndex(b).enumValueIndex == (int)ColorType.None)
                                {
                                    blocksProp.DeleteArrayElementAtIndex(b);
                                    changed = true;
                                }
                            }

                            while (blocksProp.arraySize > tuning.MaximumStackBlocks)
                            {
                                blocksProp.DeleteArrayElementAtIndex(blocksProp.arraySize - 1);
                                changed = true;
                            }

                            if (blocksProp.arraySize == 0)
                            {
                                stacksProp.DeleteArrayElementAtIndex(i);
                                changed = true;
                            }
                        }
                        else
                        {
                            stacksProp.DeleteArrayElementAtIndex(i);
                            changed = true;
                        }
                    }
                    if (changed)
                    {
                        so.ApplyModifiedProperties();
                    }
                }
            }

            // Fill missing colors
            if (currentLevel.gridCellBoardData != null && currentLevel.stackBoard != null)
            {
                var boxCapacity = new Dictionary<ColorType, int>();
                var stackCounts = new Dictionary<ColorType, int>();

                // Count Boxes
                foreach (var cell in currentLevel.gridCellBoardData.gridCells)
                {
                    if ((cell.cellType == GridCellType.StandardBox || cell.cellType == GridCellType.MysteryBox || cell.cellType == GridCellType.FrozenBox) && cell.box != null)
                    {
                        var col = cell.box.targetColor;
                        if (col == ColorType.None) continue;
                        if (!boxCapacity.ContainsKey(col)) boxCapacity[col] = 0;
                        boxCapacity[col] += cell.box.capacity;
                    }
                }
                
                if (currentLevel.tunnels != null)
                {
                    foreach (var tunnel in currentLevel.tunnels)
                    {
                        if (tunnel.contents != null)
                        {
                            foreach (var box in tunnel.contents)
                            {
                                var col = box.targetColor;
                                if (col == ColorType.None) continue;
                                if (!boxCapacity.ContainsKey(col)) boxCapacity[col] = 0;
                                boxCapacity[col] += box.capacity;
                            }
                        }
                    }
                }

                // Count Stacks
                foreach (var stack in currentLevel.stackBoard.Stacks)
                {
                    if (stack.blocksBottomToTop == null) continue;
                    foreach (var col in stack.blocksBottomToTop)
                    {
                        if (col == ColorType.None) continue;
                        if (!stackCounts.ContainsKey(col)) stackCounts[col] = 0;
                        stackCounts[col]++;
                    }
                }

                var so = new SerializedObject(currentLevel);
                var stacksProp = so.FindProperty("stackBoard.stacks");

                if (stacksProp != null)
                {
                    var allColors = new HashSet<ColorType>(boxCapacity.Keys);
                    allColors.UnionWith(stackCounts.Keys);

                    foreach (var col in allColors)
                    {
                        var cap = boxCapacity.ContainsKey(col) ? boxCapacity[col] : 0;
                        var count = stackCounts.ContainsKey(col) ? stackCounts[col] : 0;
                        var deficit = cap - count;

                        while (deficit > 0)
                        {
                            bool added = false;
                            
                            // Try to add to existing non-full stacks (prefer stacks that aren't empty)
                            for (int i = 0; i < stacksProp.arraySize; i++)
                            {
                                var stackProp = stacksProp.GetArrayElementAtIndex(i);
                                var blocksProp = stackProp.FindPropertyRelative("blocksBottomToTop");
                                if (blocksProp != null && blocksProp.arraySize > 0 && blocksProp.arraySize < tuning.MaximumStackBlocks)
                                {
                                    blocksProp.InsertArrayElementAtIndex(blocksProp.arraySize);
                                    blocksProp.GetArrayElementAtIndex(blocksProp.arraySize - 1).enumValueIndex = (int)col;
                                    deficit--;
                                    added = true;
                                    changed = true;
                                    break; 
                                }
                            }

                            if (!added)
                            {
                                // Try to find empty slot to create new stack
                                int rows = so.FindProperty("stackBoard.rows").intValue;
                                int cols = so.FindProperty("stackBoard.columns").intValue;
                                
                                var occupied = new HashSet<GridPosition>();
                                for(int i=0; i<stacksProp.arraySize; i++)
                                {
                                    var sp = stacksProp.GetArrayElementAtIndex(i);
                                    if (sp.FindPropertyRelative("useExplicitPosition").boolValue)
                                    {
                                        occupied.Add(new GridPosition(
                                            sp.FindPropertyRelative("position.row").intValue,
                                            sp.FindPropertyRelative("position.column").intValue
                                        ));
                                    }
                                }

                                GridPosition? emptyPos = null;
                                for (int r = 0; r < rows; r++)
                                {
                                    int colsInRow = (r % 2 == 1) ? cols - 1 : cols;
                                    for (int c = 0; c < colsInRow; c++)
                                    {
                                        var p = new GridPosition(r, c);
                                        if (!occupied.Contains(p))
                                        {
                                            emptyPos = p;
                                            break;
                                        }
                                    }
                                    if (emptyPos.HasValue) break;
                                }

                                if (emptyPos.HasValue)
                                {
                                    int newIdx = stacksProp.arraySize;
                                    stacksProp.InsertArrayElementAtIndex(newIdx);
                                    var newStackProp = stacksProp.GetArrayElementAtIndex(newIdx);
                                    newStackProp.FindPropertyRelative("useExplicitPosition").boolValue = true;
                                    newStackProp.FindPropertyRelative("position.row").intValue = emptyPos.Value.Row;
                                    newStackProp.FindPropertyRelative("position.column").intValue = emptyPos.Value.Column;
                                    var blocksProp = newStackProp.FindPropertyRelative("blocksBottomToTop");
                                    blocksProp.ClearArray();
                                    blocksProp.InsertArrayElementAtIndex(0);
                                    blocksProp.GetArrayElementAtIndex(0).enumValueIndex = (int)col;
                                    
                                    deficit--;
                                    changed = true;
                                }
                                else
                                {
                                    // Board is full, expand it to make room for new stacks!
                                    var rowsProp = so.FindProperty("stackBoard.rows");
                                    rowsProp.intValue = rowsProp.intValue + 1;
                                    continue;
                                }
                            }
                        }

                        while (deficit < 0)
                        {
                            bool removed = false;
                            
                            // Try to remove from existing stacks
                            for (int i = 0; i < stacksProp.arraySize; i++)
                            {
                                var stackProp = stacksProp.GetArrayElementAtIndex(i);
                                var blocksProp = stackProp.FindPropertyRelative("blocksBottomToTop");
                                if (blocksProp != null && blocksProp.arraySize > 0)
                                {
                                    for (int b = blocksProp.arraySize - 1; b >= 0; b--)
                                    {
                                        if (blocksProp.GetArrayElementAtIndex(b).enumValueIndex == (int)col)
                                        {
                                            blocksProp.DeleteArrayElementAtIndex(b);
                                            deficit++;
                                            removed = true;
                                            changed = true;
                                            
                                            if (blocksProp.arraySize == 0)
                                            {
                                                stacksProp.DeleteArrayElementAtIndex(i);
                                            }
                                            break; 
                                        }
                                    }
                                    if (removed) break; 
                                }
                            }

                            if (!removed)
                            {
                                break;
                            }
                        }
                    }
                    if (changed)
                    {
                        so.ApplyModifiedProperties();
                    }
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(currentLevel);
                AssetDatabase.SaveAssets();
                lastValidationResult = validator.Validate(currentLevel, tuning);
                EditorUtility.DisplayDialog("Auto Fix", "Level was successfully auto-fixed and re-validated.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Auto Fix", "No auto-fixable errors were found.", "OK");
            }
        }

        private void DrawStatistics()
        {
            GUILayout.Label("Level Statistics", EditorStyles.boldLabel);
            
            var boxCounts = new Dictionary<ColorType, int>();
            var boxCapacity = new Dictionary<ColorType, int>();
            var stackCounts = new Dictionary<ColorType, int>();

            // Count Boxes
            if (currentLevel.gridCellBoardData?.gridCells != null)
            {
                foreach (var cell in currentLevel.gridCellBoardData.gridCells)
                {
                    if ((cell.cellType == GridCellType.StandardBox || cell.cellType == GridCellType.MysteryBox || cell.cellType == GridCellType.FrozenBox) && cell.box != null)
                    {
                        var col = cell.box.targetColor;
                        if (col == ColorType.None) continue;
                        if (!boxCounts.ContainsKey(col))
                        {
                            boxCounts[col] = 0;
                            boxCapacity[col] = 0;
                        }
                        boxCounts[col]++;
                        boxCapacity[col] += cell.box.capacity;
                    }
                }
                
                if (currentLevel.tunnels != null)
                {
                    foreach (var tunnel in currentLevel.tunnels)
                    {
                        if (tunnel.contents != null)
                        {
                            foreach (var box in tunnel.contents)
                            {
                                var col = box.targetColor;
                                if (col == ColorType.None) continue;
                                if (!boxCounts.ContainsKey(col))
                                {
                                    boxCounts[col] = 0;
                                    boxCapacity[col] = 0;
                                }
                                boxCounts[col]++;
                                boxCapacity[col] += box.capacity;
                            }
                        }
                    }
                }
            }

            // Count Stacks
            if (currentLevel.stackBoard?.Stacks != null)
            {
                foreach (var stack in currentLevel.stackBoard.Stacks)
                {
                    if (stack.blocksBottomToTop == null) continue;
                    foreach (var col in stack.blocksBottomToTop)
                    {
                        if (col == ColorType.None) continue;
                        if (!stackCounts.ContainsKey(col)) stackCounts[col] = 0;
                        stackCounts[col]++;
                    }
                }
            }

            var allColors = new HashSet<ColorType>(boxCounts.Keys);
            allColors.UnionWith(stackCounts.Keys);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Color", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("Box Count", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("Box Cap.", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("Stack Blks", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("Diff (Cap-Blks)", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            foreach (var col in allColors)
            {
                EditorGUILayout.BeginHorizontal();
                
                var oldBg = GUI.backgroundColor;
                GUI.backgroundColor = GetColor(col);
                GUIStyle colStyle = new GUIStyle(GUI.skin.box);
                colStyle.normal.background = WhiteTexture;
                colStyle.normal.textColor = GetTextColor(col);
                colStyle.fontStyle = FontStyle.Bold;
                GUILayout.Box(col.ToString(), colStyle, GUILayout.Width(80));
                GUI.backgroundColor = oldBg;

                int bCount = boxCounts.ContainsKey(col) ? boxCounts[col] : 0;
                int bCap = boxCapacity.ContainsKey(col) ? boxCapacity[col] : 0;
                int sCount = stackCounts.ContainsKey(col) ? stackCounts[col] : 0;
                int diff = bCap - sCount;

                GUILayout.Label(bCount.ToString(), GUILayout.Width(80));
                GUILayout.Label(bCap.ToString(), GUILayout.Width(80));
                GUILayout.Label(sCount.ToString(), GUILayout.Width(80));
                
                var oldContentColor = GUI.contentColor;
                if (diff < 0) GUI.contentColor = Color.red;
                else if (diff > 0) GUI.contentColor = Color.yellow;
                else GUI.contentColor = Color.green;
                
                GUILayout.Label(diff.ToString(), EditorStyles.boldLabel, GUILayout.Width(100));
                GUI.contentColor = oldContentColor;

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void PlayLevel()
        {
            if (currentLevel != null)
            {
                EditorUtility.SetDirty(currentLevel);
                AssetDatabase.SaveAssets();

                EditorPrefs.SetString("HexaFall_TestLevelPath", AssetDatabase.GetAssetPath(currentLevel));
                EditorApplication.isPlaying = true;

                if (Application.isPlaying)
                {
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.GetPanel<UIHome>().Hide();
                        UIManager.Instance.GetPanel<UIGamePlay>().Show();
                    }

                    if (GameController.Instance != null)
                    {
                        GameController.Instance.CurrentLevel = currentLevel.level;
                        GameController.Instance.StartCurrentLevel();
                    }
                }
            }
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnPlayModeStart()
        {
            if (EditorPrefs.HasKey("HexaFall_TestLevelPath"))
            {
                string path = EditorPrefs.GetString("HexaFall_TestLevelPath");
                EditorPrefs.DeleteKey("HexaFall_TestLevelPath");
                
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level != null)
                {
                    var gc = FindObjectOfType<GameController>();
                    if (gc != null)
                    {
                        gc.CurrentLevelData = level;
                        gc.StartLevel(level.level);
                    }
                    else
                    {
                        var lc = FindObjectOfType<LevelController>();
                        if (lc != null) lc.SetData(level);
                    }
                }
            }
        }
#endif

        private Color GetColor(ColorType col)
        {
            switch (col)
            {
                case ColorType.Red: return new Color(1f, 0.3f, 0.3f);
                case ColorType.Blue: return new Color(0.3f, 0.5f, 1f);
                case ColorType.Green: return new Color(0.3f, 0.8f, 0.3f);
                case ColorType.Yellow: return new Color(1f, 0.9f, 0.2f);
                case ColorType.Purple: return new Color(0.7f, 0.3f, 0.9f);
                case ColorType.Orange: return new Color(1f, 0.6f, 0.2f);
                case ColorType.Pink: return new Color(1f, 0.5f, 0.8f);
                case ColorType.Cyan: return new Color(0.2f, 0.9f, 0.9f);
                case ColorType.White: return new Color(0.9f, 0.9f, 0.9f);
                case ColorType.Black: return new Color(0.2f, 0.2f, 0.2f);
                case ColorType.DarkBlue: return new Color(0.1f, 0.1f, 0.6f);
                case ColorType.Gray: return new Color(0.6f, 0.6f, 0.6f);
                case ColorType.CamoGreen: return new Color(0.47f, 0.53f, 0.29f);
                default: return Color.gray;
            }
        }

        private Color GetTextColor(ColorType col)
        {
            if (col == ColorType.Yellow || col == ColorType.White || col == ColorType.Cyan || col == ColorType.Pink || col == ColorType.Green || col == ColorType.Gray || col == ColorType.CamoGreen)
                return Color.black;
            return Color.white;
        }

        private void ImportJsonLevel()
        {
            if (currentLevel == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign or create a LevelData first to overwrite.", "OK");
                return;
            }

            string path = EditorUtility.OpenFilePanel("Select Legacy JSON Level", "", "json");
            if (string.IsNullOrEmpty(path)) return;

            string json = System.IO.File.ReadAllText(path);
            var legacy = JsonUtility.FromJson<LegacyLevelData>(json);
            
            if (legacy == null || legacy.collectorArea == null || legacy.hexStackArea == null)
            {
                EditorUtility.DisplayDialog("Error", "Failed to parse legacy JSON.", "OK");
                return;
            }

            int gW = legacy.collectorArea.gridWidth;
            int gH = legacy.collectorArea.gridHeight;
            currentLevel.gridCellBoardData = new GridCellBoardData { width = gW, height = gH, gridCells = new List<GridCellDefinition>() };
            
            for (int r = 0; r < gH; r++)
            {
                for (int c = 0; c < gW; c++)
                {
                    currentLevel.gridCellBoardData.gridCells.Add(new GridCellDefinition
                    {
                        position = new GridPosition(r, c),
                        cellType = GridCellType.Empty,
                        box = new BoxDefinition { boxId = $"box_{r}_{c}", capacity = 24 }
                    });
                }
            }

            if (legacy.collectorArea.deadCells != null)
            {
                foreach (var dc in legacy.collectorArea.deadCells)
                {
                    var cell = currentLevel.gridCellBoardData.GetCellAt(new GridPosition(dc.y, dc.x));
                    if (cell != null) cell.cellType = GridCellType.DeadCell;
                }
            }

            if (legacy.collectorArea.singleBlockCollectors != null)
            {
                foreach (var sbc in legacy.collectorArea.singleBlockCollectors)
                {
                    var cell = currentLevel.gridCellBoardData.GetCellAt(new GridPosition(sbc.y, sbc.x));
                    if (cell != null)
                    {
                        cell.cellType = GridCellType.StandardBox;
                        cell.box.targetColor = MapLegacyColor(sbc.color);
                        cell.box.capacity = 24;
                    }
                }
            }

            if (legacy.collectorArea.mysteryCollectors != null)
            {
                foreach (var m in legacy.collectorArea.mysteryCollectors)
                {
                    var cell = currentLevel.gridCellBoardData.GetCellAt(new GridPosition(m.y, m.x));
                    if (cell != null)
                    {
                        cell.cellType = GridCellType.MysteryBox;
                        cell.box.targetColor = MapLegacyColor(m.hiddenColor);
                        cell.box.capacity = 24;
                        cell.box.isHidden = true;
                    }
                }
            }

            if (legacy.collectorArea.woodBoxCollectors != null)
            {
                foreach (var w in legacy.collectorArea.woodBoxCollectors)
                {
                    var cell = currentLevel.gridCellBoardData.GetCellAt(new GridPosition(w.y, w.x));
                    if (cell != null)
                    {
                        cell.cellType = GridCellType.MysteryBox;
                        cell.box.targetColor = MapLegacyColor(w.hiddenColor);
                        cell.box.capacity = 24;
                        cell.box.isHidden = true;
                    }
                }
            }

            if (legacy.collectorArea.iceCollectors != null)
            {
                foreach (var ice in legacy.collectorArea.iceCollectors)
                {
                    var cell = currentLevel.gridCellBoardData.GetCellAt(new GridPosition(ice.y, ice.x));
                    if (cell != null)
                    {
                        cell.cellType = GridCellType.FrozenBox;
                        cell.box.targetColor = MapLegacyColor(ice.hiddenColor);
                        cell.box.capacity = ice.iceCapacity > 0 ? ice.iceCapacity : 24;
                        cell.box.frozenDurability = 1;
                    }
                }
            }
            
            currentLevel.tunnels = new List<TunnelDefinition>();
            currentLevel.pins = new List<PinDefinition>();
            currentLevel.keys = new List<KeyDefinition>();
            currentLevel.locks = new List<LockDefinition>();

            if (legacy.collectorArea.tunnels != null)
            {
                foreach (var t in legacy.collectorArea.tunnels)
                {
                    var cell = currentLevel.gridCellBoardData.GetCellAt(new GridPosition(t.y, t.x));
                    if (cell != null)
                    {
                        cell.cellType = GridCellType.TunnelCell;
                        string tId = $"tunnel_{t.y}_{t.x}";
                        cell.tunnelId = tId;

                        FacingDirection fd = FacingDirection.Down;
                        if (t.direction == "Right") fd = FacingDirection.Right;
                        else if (t.direction == "Left") fd = FacingDirection.Left;
                        else if (t.direction == "Up") fd = FacingDirection.Up;

                        var contents = new List<BoxDefinition>();
                        if (t.collectorQueue != null)
                        {
                            for (int i = 0; i < t.collectorQueue.Length; i++)
                            {
                                contents.Add(new BoxDefinition { boxId = $"{tId}_box_{i}", capacity = 24, targetColor = MapLegacyColor(t.collectorQueue[i].color) });
                            }
                        }

                        currentLevel.tunnels.Add(new TunnelDefinition
                        {
                            tunnelId = tId,
                            position = new GridPosition(t.y, t.x),
                            direction = fd,
                            contents = contents
                        });
                    }
                }
            }

            if (legacy.collectorArea.pinBlockers != null)
            {
                int pinIdx = 0;
                foreach (var p in legacy.collectorArea.pinBlockers)
                {
                    var cell = currentLevel.gridCellBoardData.GetCellAt(new GridPosition(p.y, p.x));
                    if (cell != null)
                    {
                        cell.cellType = GridCellType.PinCell;
                        string pId = $"pin_{pinIdx++}";
                        cell.pinId = pId;

                        PinDirection pd = PinDirection.LeftToRight;
                        if (p.direction == "Left") pd = PinDirection.RightToLeft;
                        else if (p.direction == "Right") pd = PinDirection.LeftToRight;
                        else if (p.direction == "Up") pd = PinDirection.DownToUp;
                        else if (p.direction == "Down") pd = PinDirection.UpToDown;

                        currentLevel.pins.Add(new PinDefinition
                        {
                            pinId = pId,
                            headPosition = new GridPosition(p.y, p.x),
                            direction = pd,
                            length = p.blockCount + 1
                        });

                        int dr = 0, dc = 0;
                        if (pd == PinDirection.LeftToRight) dc = 1;
                        if (pd == PinDirection.RightToLeft) dc = -1;
                        if (pd == PinDirection.UpToDown) dr = 1;
                        if (pd == PinDirection.DownToUp) dr = -1;

                        for (int i = 1; i <= p.blockCount; i++)
                        {
                            var tailCell = currentLevel.gridCellBoardData.GetCellAt(new GridPosition(p.y + dr * i, p.x + dc * i));
                            if (tailCell != null)
                            {
                                tailCell.cellType = GridCellType.PinTailCell;
                                tailCell.pinId = pId;
                            }
                        }
                    }
                }
            }

            int sW = (legacy.hexStackArea.gridWidth + 1) / 2;
            int sH = legacy.hexStackArea.gridHeight;
            var stacksList = new List<StackDefinition>();
            
            if (legacy.hexStackArea.stacks != null)
            {
                foreach (var st in legacy.hexStackArea.stacks)
                {
                    var def = new StackDefinition { useExplicitPosition = true, position = new GridPosition(st.y, st.x / 2), blocksBottomToTop = new List<ColorType>() };
                    if (st.colors != null)
                    {
                        foreach (var c in st.colors)
                        {
                            def.blocksBottomToTop.Add(MapLegacyColor(c));
                        }
                    }
                    stacksList.Add(def);
                }
            }

            currentLevel.stackBoard = new StackBoardData(sW, sH, stacksList);

            EditorUtility.SetDirty(currentLevel);
            AssetDatabase.SaveAssets();

            selectedCellPos = new GridPosition(-1, -1);
            selectedStackPos = new GridPosition(-1, -1);

            EditorUtility.DisplayDialog("Success", "Legacy JSON imported successfully!", "OK");
        }

        private ColorType MapLegacyColor(string colorCode)
        {
            if (string.IsNullOrEmpty(colorCode)) return ColorType.None;
            colorCode = colorCode.ToLower();
            switch (colorCode)
            {
                case "b": return ColorType.Blue;
                case "y": return ColorType.Yellow;
                case "o": return ColorType.Orange;
                case "pk": return ColorType.Pink;
                case "w": return ColorType.White;
                case "r": return ColorType.Red;
                case "g": return ColorType.Green;
                case "dr": return ColorType.Cyan;
                case "db": return ColorType.DarkBlue;
                case "gr" : return ColorType.Gray;
                case "p" : return ColorType.Purple;
                case "og" : return ColorType.CamoGreen;
                case "dg": return ColorType.Black;
                default:
                    Debug.LogWarning("Unknown legacy color code: " + colorCode);
                    return ColorType.Red;
            }
        }
        private ColorType GetRandomColorType()
        {
            var values = System.Enum.GetValues(typeof(ColorType));
            ColorType randomColor;
            do
            {
                randomColor = (ColorType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
            } while (randomColor == ColorType.None); // Exclude 'None'
            return randomColor;
        }
    }
}
