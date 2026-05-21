using System;
using UnityEngine;

[Serializable]
public struct HeartState
{
    public int currentHearts;
    public int maxHearts;
    public bool isInfiniteLivesActive;
    public bool canPlay;
    public long secondsUntilNextHeart;
    public long secondsUntilInfiniteLivesEnd;
}

[DefaultExecutionOrder(-850)]
public class HeartManager : SingletonDontDestroyMono<HeartManager>
{
    [SerializeField] private int m_maxHearts = 5;
    [SerializeField] private int m_heartRefillMinutes = 20;
    [SerializeField] private float m_stateRefreshIntervalSeconds = 1f;

    public event Action<HeartState> OnLivesChanged;

    private HeartState lastHeartState;
    private bool hasHeartState;
    private float nextRefreshTime;

    public int CurrentHearts => UserManager.Instance.HeartCount;
    public int MaxHearts => UserManager.Instance.MaxHeartCount;
    public bool CanPlay => IsInfiniteLivesActive || CurrentHearts > 0;
    public bool IsInfiniteLivesActive => InfiniteLivesRemaining.TotalSeconds > 0;
    public TimeSpan RefillInterval => TimeSpan.FromMinutes(Mathf.Max(1, m_heartRefillMinutes));

    public TimeSpan TimeUntilNextHeart
    {
        get
        {
            SyncWithCurrentTime();

            if (CurrentHearts >= MaxHearts || UserManager.Instance.NextHeartRefillUtcTicks <= 0)
            {
                return TimeSpan.Zero;
            }

            DateTime nextRefillTime = new DateTime(UserManager.Instance.NextHeartRefillUtcTicks, DateTimeKind.Utc);
            TimeSpan remainingTime = nextRefillTime - DateTime.UtcNow;
            return remainingTime > TimeSpan.Zero ? remainingTime : TimeSpan.Zero;
        }
    }

    public TimeSpan InfiniteLivesRemaining
    {
        get
        {
            long endTicks = UserManager.Instance.InfiniteLivesEndUtcTicks;
            if (endTicks <= 0)
            {
                return TimeSpan.Zero;
            }

            DateTime endTime = new DateTime(endTicks, DateTimeKind.Utc);
            TimeSpan remainingTime = endTime - DateTime.UtcNow;
            return remainingTime > TimeSpan.Zero ? remainingTime : TimeSpan.Zero;
        }
    }

    //[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    //private static void Bootstrap()
    //{
    //    if (Instance != null)
    //    {
    //        return;
    //    }

    //    var managerObject = new GameObject(nameof(HeartManager));
    //    managerObject.AddComponent<HeartManager>();
    //}

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            return;
        }

        if (UserManager.Instance.MaxHeartCount != m_maxHearts)
        {
            UserManager.Instance.SetMaxHeartCount(m_maxHearts);
        }

        SyncWithCurrentTime();
        EmitLivesChangedIfNeeded(true);
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + Mathf.Max(0.2f, m_stateRefreshIntervalSeconds);
        SyncWithCurrentTime();
        EmitLivesChangedIfNeeded();
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause)
        {
            SyncWithCurrentTime();
            EmitLivesChangedIfNeeded(true);
            return;
        }

        SyncWithCurrentTime();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            return;
        }

        SyncWithCurrentTime();
        EmitLivesChangedIfNeeded(true);
    }

    public void SyncWithCurrentTime()
    {
        DateTime now = DateTime.UtcNow;
        UserManager userManager = UserManager.Instance;

        if (userManager.MaxHeartCount != m_maxHearts)
        {
            userManager.SetMaxHeartCount(m_maxHearts);
        }

        if (userManager.InfiniteLivesEndUtcTicks > 0)
        {
            DateTime infiniteLivesEndTime = new DateTime(userManager.InfiniteLivesEndUtcTicks, DateTimeKind.Utc);
            if (now >= infiniteLivesEndTime)
            {
                userManager.SetInfiniteLivesEndUtcTicks(0);
            }
        }

        if (userManager.HeartCount >= userManager.MaxHeartCount)
        {
            if (userManager.NextHeartRefillUtcTicks != 0)
            {
                userManager.SetNextHeartRefillUtcTicks(0);
            }

            return;
        }

        if (userManager.NextHeartRefillUtcTicks <= 0)
        {
            userManager.SetNextHeartRefillUtcTicks(now.Add(RefillInterval).Ticks);
            return;
        }

        DateTime nextRefillTime = new DateTime(userManager.NextHeartRefillUtcTicks, DateTimeKind.Utc);
        if (now < nextRefillTime)
        {
            return;
        }

        double elapsedIntervals = Math.Floor((now - nextRefillTime).TotalSeconds / RefillInterval.TotalSeconds) + 1d;
        int refillCount = Mathf.Max(1, (int)elapsedIntervals);
        int nextHeartCount = Mathf.Min(userManager.MaxHeartCount, userManager.HeartCount + refillCount);
        userManager.SetHeartCount(nextHeartCount);

        if (nextHeartCount >= userManager.MaxHeartCount)
        {
            userManager.SetNextHeartRefillUtcTicks(0);
            return;
        }

        DateTime upcomingRefillTime = nextRefillTime.AddTicks(RefillInterval.Ticks * refillCount);
        userManager.SetNextHeartRefillUtcTicks(upcomingRefillTime.Ticks);
    }

    public bool RegisterLoss()
    {
        SyncWithCurrentTime();

        if (IsInfiniteLivesActive)
        {
            EmitLivesChangedIfNeeded(true);
            return true;
        }

        if (CurrentHearts <= 0)
        {
            EmitLivesChangedIfNeeded(true);
            return false;
        }

        UserManager userManager = UserManager.Instance;
        userManager.SetHeartCount(CurrentHearts - 1);

        if (userManager.HeartCount < userManager.MaxHeartCount && userManager.NextHeartRefillUtcTicks <= 0)
        {
            userManager.SetNextHeartRefillUtcTicks(DateTime.UtcNow.Add(RefillInterval).Ticks);
        }

        EmitLivesChangedIfNeeded(true);
        return true;
    }

    public void AddHearts(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SyncWithCurrentTime();
        UserManager userManager = UserManager.Instance;
        userManager.SetHeartCount(Mathf.Min(userManager.MaxHeartCount, userManager.HeartCount + amount));

        if (userManager.HeartCount >= userManager.MaxHeartCount)
        {
            userManager.SetNextHeartRefillUtcTicks(0);
        }
        else if (userManager.NextHeartRefillUtcTicks <= 0)
        {
            userManager.SetNextHeartRefillUtcTicks(DateTime.UtcNow.Add(RefillInterval).Ticks);
        }

        EmitLivesChangedIfNeeded(true);
    }

    public void FillToMax()
    {
        UserManager.Instance.SetHeartCount(MaxHearts);
        UserManager.Instance.SetNextHeartRefillUtcTicks(0);
        EmitLivesChangedIfNeeded(true);
    }

    public void StartInfiniteLives(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return;
        }

        SyncWithCurrentTime();

        DateTime now = DateTime.UtcNow;
        DateTime currentEndTime = IsInfiniteLivesActive
            ? now.Add(InfiniteLivesRemaining)
            : now;

        DateTime nextEndTime = currentEndTime.Add(duration);
        UserManager.Instance.SetInfiniteLivesEndUtcTicks(nextEndTime.Ticks);

        if (CurrentHearts < MaxHearts && UserManager.Instance.NextHeartRefillUtcTicks <= 0)
        {
            UserManager.Instance.SetNextHeartRefillUtcTicks(now.Add(RefillInterval).Ticks);
        }

        EmitLivesChangedIfNeeded(true);
    }

    public bool TryBuyHeartsWithCoins(int coinCost, int heartAmount)
    {
        if (heartAmount <= 0)
        {
            return false;
        }

        if (!UserManager.Instance.TrySpendCoins(Mathf.Max(0, coinCost)))
        {
            return false;
        }

        AddHearts(heartAmount);
        return true;
    }

    public bool TryBuyInfiniteLivesWithCoins(int coinCost, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return false;
        }

        if (!UserManager.Instance.TrySpendCoins(Mathf.Max(0, coinCost)))
        {
            return false;
        }

        StartInfiniteLives(duration);
        return true;
    }

    public HeartState GetCurrentState()
    {
        SyncWithCurrentTime();
        return BuildHeartState();
    }

    private HeartState BuildHeartState()
    {
        TimeSpan nextHeartTime = TimeUntilNextHeart;
        TimeSpan infiniteLivesTime = InfiniteLivesRemaining;

        return new HeartState
        {
            currentHearts = CurrentHearts,
            maxHearts = MaxHearts,
            isInfiniteLivesActive = infiniteLivesTime > TimeSpan.Zero,
            canPlay = CanPlay,
            secondsUntilNextHeart = Mathf.Max(0, Mathf.CeilToInt((float)nextHeartTime.TotalSeconds)),
            secondsUntilInfiniteLivesEnd = Mathf.Max(0, Mathf.CeilToInt((float)infiniteLivesTime.TotalSeconds))
        };
    }

    private void EmitLivesChangedIfNeeded(bool force = false)
    {
        HeartState currentState = BuildHeartState();
        if (!force && hasHeartState && AreStatesEqual(lastHeartState, currentState))
        {
            return;
        }

        hasHeartState = true;
        lastHeartState = currentState;
        OnLivesChanged?.Invoke(currentState);
    }

    private static bool AreStatesEqual(HeartState leftState, HeartState rightState)
    {
        return leftState.currentHearts == rightState.currentHearts
            && leftState.maxHearts == rightState.maxHearts
            && leftState.isInfiniteLivesActive == rightState.isInfiniteLivesActive
            && leftState.canPlay == rightState.canPlay
            && leftState.secondsUntilNextHeart == rightState.secondsUntilNextHeart
            && leftState.secondsUntilInfiniteLivesEnd == rightState.secondsUntilInfiniteLivesEnd;
    }
}
