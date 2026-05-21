using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public abstract class RewardData
{
    public UserResourceType type;
    public int quantity;

    public abstract void Claim();
}
