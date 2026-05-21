using UnityEditor;
using UnityEngine;

public class LevelImagePostprocessor : AssetPostprocessor
{
    private const string TargetFolder = "Assets/_MainModule/Textures/Levels";

    private void OnPostprocessTexture(Texture2D texture)
    {
        // Get the imported asset's path
        string assetPath = assetImporter.assetPath;

        // Check if the asset is in the target folder
        if (assetPath.StartsWith(TargetFolder))
        {
            // Extract the file name without extension
            string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            // Check if the file name matches the "levelx" pattern
            if (fileName.StartsWith("lv", System.StringComparison.OrdinalIgnoreCase))
            {
                // Extract the number part from the file name
                string numberPart = fileName.Substring(2); // Skip "level"

                // Construct the new file name
                string newFileName = $"{numberPart}.png";

                // Get the full path of the asset
                string directory = System.IO.Path.GetDirectoryName(assetPath);
                string newAssetPath = System.IO.Path.Combine(directory, newFileName);

                // Rename the asset
                AssetDatabase.RenameAsset(assetPath, newFileName);
                Debug.Log($"Renamed imported image from '{fileName}.png' to '{newFileName}'");
            }
        }
    }
}