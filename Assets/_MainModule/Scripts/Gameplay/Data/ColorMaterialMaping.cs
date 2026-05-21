using AYellowpaper.SerializedCollections;
using HexaFall.Gameplay.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorMaterialMaping", menuName = "ScriptableObjects/ColorMaterialMaping", order = 1)]
public class ColorMaterialMaping : ScriptableObject
{
    [SerializeField] public SerializedDictionary<ColorType, Material> materialBox = new SerializedDictionary<ColorType, Material>();
    [SerializeField] public SerializedDictionary<ColorType, Material> materialHexaBlock = new SerializedDictionary<ColorType, Material>();
    [SerializeField] public SerializedDictionary<ColorType, Material> materialLockBody = new SerializedDictionary<ColorType, Material>();
    [SerializeField] public SerializedDictionary<ColorType, Material> materialKeyBody = new SerializedDictionary<ColorType, Material>();
}
