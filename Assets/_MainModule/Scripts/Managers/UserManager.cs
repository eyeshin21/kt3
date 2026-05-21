using System;
using UnityEngine;

public enum UserResourceType
{
    Coin,
    Heart,
    MagicWand,
    Broom,
    Magnet
}

[Serializable]
public class UserData
{
    public int currentLevel = 1;
    public int coins = 0;
    public int heartCount = 5;
    public int maxHeartCount = 5;
    public long nextHeartRefillUtcTicks = 0;
    public long infiniteLivesEndUtcTicks = 0;
    public int magicWandCount = 0;
    public int broomCount = 0;
    public int magnetCount = 0;
    public bool hadPurchasedRemoveAds = false;

    public void Normalize()
    {
        currentLevel = Mathf.Max(1, currentLevel);
        coins = Mathf.Max(0, coins);
        maxHeartCount = Mathf.Max(1, maxHeartCount);
        heartCount = Mathf.Clamp(heartCount, 0, maxHeartCount);
        magicWandCount = Mathf.Max(0, magicWandCount);
        broomCount = Mathf.Max(0, broomCount);
        magnetCount = Mathf.Max(0, magnetCount);

        if (heartCount >= maxHeartCount)
        {
            nextHeartRefillUtcTicks = 0;
        }
    }
}

[DefaultExecutionOrder(-900)]
public class UserManager : SingletonDontDestroyMono<UserManager>
{
    private const string UserDataKey = "UserData";

    public event Action<UserResourceType, int, int> OnResourceChanged;
    public event Action OnResourcesChanged;
    public event Action<int, int> OnLevelChanged;

    private UserData userData;

    public int CurrentLevel
    {
        get
        {
            EnsureLoaded();
            return userData.currentLevel;
        }
    }

    public int Coins
    {
        get
        {
            EnsureLoaded();
            return userData.coins;
        }
    }

    public int HeartCount
    {
        get
        {
            EnsureLoaded();
            return userData.heartCount;
        }
    }

    public int MaxHeartCount
    {
        get
        {
            EnsureLoaded();
            return userData.maxHeartCount;
        }
    }

    public long NextHeartRefillUtcTicks
    {
        get
        {
            EnsureLoaded();
            return userData.nextHeartRefillUtcTicks;
        }
    }

    public long InfiniteLivesEndUtcTicks
    {
        get
        {
            EnsureLoaded();
            return userData.infiniteLivesEndUtcTicks;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        var managerObject = new GameObject(nameof(UserManager));
        managerObject.AddComponent<UserManager>();
    }

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            return;
        }

        EnsureLoaded();
    }

    public void Load()
    {
        string rawData = PlayerPrefs.GetString(UserDataKey, string.Empty);
        if (string.IsNullOrEmpty(rawData))
        {
            userData = CreateDefaultUserData();
            Save();
            return;
        }

        try
        {
            userData = JsonUtility.FromJson<UserData>(rawData);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to load user data. Resetting save. {exception.Message}");
            userData = CreateDefaultUserData();
        }

        if (userData == null)
        {
            userData = CreateDefaultUserData();
        }

        userData.Normalize();
        Save();
    }

    public void Save()
    {
        EnsureLoaded();
        userData.Normalize();
        PlayerPrefs.SetString(UserDataKey, JsonUtility.ToJson(userData));
        PlayerPrefs.Save();
    }

    public int GetBoosterCount(BoosterType boosterType)
    {
        EnsureLoaded();

        return boosterType switch
        {
            BoosterType.MAGIC_WAND => userData.magicWandCount,
            BoosterType.BROOM => userData.broomCount,
            BoosterType.MAGNET => userData.magnetCount,
            _ => 0
        };
    }

    public void SetCurrentLevel(int level)
    {
        EnsureLoaded();

        level = Mathf.Max(1, level);
        if (userData.currentLevel == level)
        {
            return;
        }

        int previousLevel = userData.currentLevel;
        userData.currentLevel = level;
        Save();
        OnLevelChanged?.Invoke(previousLevel, level);
    }

    public void AddCoins(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        SetCoins(Coins + amount);
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (Coins < amount)
        {
            return false;
        }

        SetCoins(Coins - amount);
        return true;
    }

    public void SetBoosterCount(BoosterType boosterType, int amount)
    {
        EnsureLoaded();

        amount = Mathf.Max(0, amount);
        int previousAmount = GetBoosterCount(boosterType);
        if (previousAmount == amount)
        {
            return;
        }

        switch (boosterType)
        {
            case BoosterType.MAGIC_WAND:
                userData.magicWandCount = amount;
                NotifyResourceChanged(UserResourceType.MagicWand, previousAmount, amount);
                break;
            case BoosterType.BROOM:
                userData.broomCount = amount;
                NotifyResourceChanged(UserResourceType.Broom, previousAmount, amount);
                break;
            case BoosterType.MAGNET:
                userData.magnetCount = amount;
                NotifyResourceChanged(UserResourceType.Magnet, previousAmount, amount);
                break;
        }

        Save();
    }

    public void AddBooster(BoosterType boosterType, int amount)
    {
        if (amount == 0)
        {
            return;
        }

        SetBoosterCount(boosterType, GetBoosterCount(boosterType) + amount);
    }

    public bool TrySpendBooster(BoosterType boosterType, int amount = 1)
    {
        if (amount <= 0)
        {
            return true;
        }

        int currentAmount = GetBoosterCount(boosterType);
        if (currentAmount < amount)
        {
            return false;
        }

        SetBoosterCount(boosterType, currentAmount - amount);
        return true;
    }

    internal void SetHeartCount(int amount)
    {
        EnsureLoaded();

        amount = Mathf.Clamp(amount, 0, MaxHeartCount);
        int previousAmount = userData.heartCount;
        if (previousAmount == amount)
        {
            return;
        }

        userData.heartCount = amount;
        NotifyResourceChanged(UserResourceType.Heart, previousAmount, amount);
        Save();
    }

    internal void SetMaxHeartCount(int amount)
    {
        EnsureLoaded();

        amount = Mathf.Max(1, amount);
        if (userData.maxHeartCount == amount)
        {
            return;
        }

        userData.maxHeartCount = amount;
        if (userData.heartCount > userData.maxHeartCount)
        {
            userData.heartCount = userData.maxHeartCount;
        }

        Save();
        OnResourcesChanged?.Invoke();
    }

    internal void SetNextHeartRefillUtcTicks(long ticks)
    {
        EnsureLoaded();

        if (userData.nextHeartRefillUtcTicks == ticks)
        {
            return;
        }

        userData.nextHeartRefillUtcTicks = ticks;
        Save();
        OnResourcesChanged?.Invoke();
    }

    internal void SetInfiniteLivesEndUtcTicks(long ticks)
    {
        EnsureLoaded();

        if (userData.infiniteLivesEndUtcTicks == ticks)
        {
            return;
        }

        userData.infiniteLivesEndUtcTicks = ticks;
        Save();
        OnResourcesChanged?.Invoke();
    }

    private void SetCoins(int amount)
    {
        EnsureLoaded();

        amount = Mathf.Max(0, amount);
        int previousAmount = userData.coins;
        if (previousAmount == amount)
        {
            return;
        }

        userData.coins = amount;
        NotifyResourceChanged(UserResourceType.Coin, previousAmount, amount);
        Save();
    }

    private void EnsureLoaded()
    {
        if (userData == null)
        {
            Load();
        }
    }

    private void NotifyResourceChanged(UserResourceType resourceType, int previousAmount, int currentAmount)
    {
        OnResourceChanged?.Invoke(resourceType, previousAmount, currentAmount);
        OnResourcesChanged?.Invoke();
    }

    private UserData CreateDefaultUserData()
    {
        return new UserData
        {
            currentLevel = Mathf.Max(1, PlayerPrefs.GetInt("CurrentLevel", 1))
        };
    }

    public bool IsPurchasedRemoveAds => userData.hadPurchasedRemoveAds;

    public void SetPurchasedRemoveAds(bool isPurchased)
    {
        userData.hadPurchasedRemoveAds = isPurchased;
        Save();
    }

    private void OnApplicationPause(bool pause)
    {
        if(pause)
        {
            Save();
        }
    }
}
