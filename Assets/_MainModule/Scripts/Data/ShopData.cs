using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopData", menuName = "ScriptableObjects/ShopData", order = 1)]
public class ShopData : ScriptableObject
{
    public List<ShopPack> listShopPack = new();
}

[Serializable]
public class ShopPack
{
    public eIAPKey iapKey;
    public string title;
    public string description;
    public List<RewardDataGame> listReward;
}

[Serializable]
public class RewardDataGame : RewardData
{
    public override void Claim()
    {
        switch (type)
        {
            case UserResourceType.Coin:
                UserManager.Instance.AddCoins(quantity);
                break;
            case UserResourceType.MagicWand:
                UserManager.Instance.AddBooster(BoosterType.MAGIC_WAND, quantity);
                break;
            case UserResourceType.Broom:
                UserManager.Instance.AddBooster(BoosterType.BROOM, quantity);
                break;
            case UserResourceType.Magnet:
                UserManager.Instance.AddBooster(BoosterType.MAGNET, quantity);
                break;
            default:
                break;
        }
    }
}