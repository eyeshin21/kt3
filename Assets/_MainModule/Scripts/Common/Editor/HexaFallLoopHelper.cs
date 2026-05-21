using HexaFall.Gameplay.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class HexaFallLoopHelper
{
    [MenuItem("Assets/Create/Duplicate Material for ColorType", priority =1)]
    public static void DuplicateMaterialForColorType()
    {
        // Get the selected object in the Unity Editor
        UnityEngine.Object selectedObject = Selection.activeObject;

        // Check if the selected object is a material
        if (!(selectedObject is Material selectedMaterial))
        {
            Debug.LogError("Please select a material in the project folder.");
            return;
        }

        // Get the path of the selected material
        string materialPath = AssetDatabase.GetAssetPath(selectedMaterial);

        // Ensure the material is inside the project folder
        if (string.IsNullOrEmpty(materialPath))
        {
            Debug.LogError("The selected material must be inside the project folder.");
            return;
        }

        // Load the ColorCodeMaping asset
        ColorCodeMaping colorCodeMaping = AssetDatabase.LoadAssetAtPath<ColorCodeMaping>("Assets/_MainModule/Data/Configs/ColorCodeMaping.asset");
        if (colorCodeMaping == null)
        {
            Debug.LogError("ColorCodeMaping asset not found. Ensure it exists in the project.");
            return;
        }

        // Duplicate the material for each ColorType value
        foreach (ColorType colorType in Enum.GetValues(typeof(ColorType)))
        {
            if (colorType == ColorType.None) continue; // Skip the "None" type

            // Create a new material
            Material newMaterial = new Material(selectedMaterial);

            // Assign the color from ColorCodeMaping
            if (colorCodeMaping.colorTypeMaping.TryGetValue(colorType, out var mappedColor))
            {
                string colorCode = $"#{mappedColor}";

                if (ColorUtility.TryParseHtmlString(colorCode, out Color color))
                {
                    newMaterial.color = color; // Assuming the material has a "color" property
                }
            }
            else
            {
                Debug.LogWarning($"No color mapping found for {colorType}. Skipping.");
                continue;
            }

            // Save the new material in the same folder as the original
            string newMaterialPath = $"{System.IO.Path.GetDirectoryName(materialPath)}/{selectedMaterial.name}_{colorType}.mat";
            AssetDatabase.CreateAsset(newMaterial, newMaterialPath);
        }

        // Refresh the AssetDatabase to show the new materials
        AssetDatabase.Refresh();

        Debug.Log("Materials duplicated and colors assigned successfully.");
    }
}
