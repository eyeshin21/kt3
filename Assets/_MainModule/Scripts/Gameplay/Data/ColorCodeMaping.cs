using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HexaFall.Gameplay.Core;

[CreateAssetMenu(fileName = "ColorCodeMaping", menuName = "ScriptableObjects/ColorCodeMaping", order = 1)]
public class ColorCodeMaping : ScriptableObject
{
    [SerializeField] public SerializedDictionary<string, ColorType> colorCodeMaping = new SerializedDictionary<string, ColorType>();
    [SerializeField] public SerializedDictionary<ColorType, string> colorTypeMaping = new SerializedDictionary<ColorType, string>();

    private void OnValidate()
    {
        //colorCodeMaping.Clear();
        //foreach(var key in colorTypeMaping.Keys)
        //{
        //    colorCodeMaping[colorTypeMaping[key]] = key; 
        //}

        //EditorUtility.SetDirty(this);
    }
}
