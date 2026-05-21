using AYellowpaper.SerializedCollections;
using HexaFall.Gameplay.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "ScriptableObjects/GameConfig")]
public class GameConfig : ScriptableObject
{
    public SerializedDictionary<LevelType, int> coinEarnByLevels;
}
