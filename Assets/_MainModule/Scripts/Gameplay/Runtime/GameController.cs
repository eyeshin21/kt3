using HexaFall.Gameplay.Config;
using HexaFall.Gameplay.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HexaFall.Gameplay.Runtime
{
    public enum GameState
    {
        INITIALIZING,
        PLAYING,
        PAUSING,
        ENDING,
        LOSING
    }

    public partial class GameController : SingletonMono<GameController> 
    {
        [SerializeField] private LevelController m_levelController;
        [SerializeField] private int maxLevel = 100;
        [SerializeField] private BoosterManager m_boosterManager;
        [SerializeField] private TutorialManager m_tutorialManager;
        [SerializeField] private GameplayTuningConfig m_gameTuningConfig;

        [SerializeField] private GameConfig m_gameConfig;

        public GameConfig GameConfig => m_gameConfig;

        public GameplayTuningConfig GameplayTuningConfig => m_gameTuningConfig;

        public GameState CurrentState { get; private set; }

        public int CurrentLevel
        {
            get => UserManager.Instance.CurrentLevel;
            set
            {
                //UserManager.Instance.SetCurrentLevel(Mathf.Clamp(value, 0, maxLevel));
                if (value > maxLevel)
                {
                    UserManager.Instance.SetCurrentLevel(value % maxLevel);
                }
                else
                {
                    UserManager.Instance.SetCurrentLevel(value);
                }
            }
        }

        public int IndexLevel
        {
            get => PlayerPrefs.GetInt("IndexLevel", 0);
            set => PlayerPrefs.SetInt("IndexLevel", value);
        }


        public float CurrentRemainingTime { get; set; }

        public BoosterManager BoosterManager => m_boosterManager;

        private LevelData currentLevelData;

        public LevelData CurrentLevelData
        {
            get
            {
                if (currentLevelData == null || currentLevelData.level != CurrentLevel)
                {
                    if (IndexLevel == 0)
                    {
                        currentLevelData = LoadLevel(CurrentLevel);
                    }
                    else
                    {
                        currentLevelData = LoadLevel(CurrentLevel, IndexLevel);
                    }
                }
                return currentLevelData;
            }
            set
            {
                currentLevelData = value;
            }
        }

        public void StartCurrentLevel()
        {
            StartLevel(CurrentLevel);

            //m_tutorialManager.CheckShowTutorial();
        }

        public void StartLevel(int level)
        {
            HeartManager.Instance.SyncWithCurrentTime();
            //if (!HeartManager.Instance.CanPlay)
            //{
            //    Debug.LogWarning("No hearts available. Wait for refill or activate infinite lives.");
            //    return false;
            //}

            CurrentState = GameState.PLAYING;
            CurrentLevel = level;
            m_levelController.SetData(CurrentLevelData);
            UIManager.Instance.GetPanel<UIGamePlay>().Show();

            CGTeamBridge.Instance.OnGameStarted(CurrentLevel, CurrentLevelData.levelType.ToString());
        }

        public void NextLevel()
        {
            CurrentLevel++;
        }

        internal void RestartLevel()
        {
            CGTeamBridge.Instance.OnGameAbandoned(CurrentLevel, m_levelController.GetCurrentLevelProgress());
            StartCurrentLevel();
        }

        internal void LoseGame()
        {
            Debug.Log("Lose Game");
            CurrentState = GameState.LOSING;
            HeartManager.Instance.RegisterLoss();
            UIManager.Instance.GetPanel<UILose>().Show();
            UIManager.Instance.GetPanel<UILose>().SetRetryAction(RestartLevel);
            CGTeamBridge.Instance.OnGameFinished(false, m_levelController.GetCurrentLevelProgress(), CurrentLevel);
        }

        public void WinLevel()
        {
            CurrentState = GameState.ENDING;
            UIManager.Instance.GetPanel<UIWin>().Show();

            CGTeamBridge.Instance.OnGameFinished(true, 1, CurrentLevel, m_gameConfig.coinEarnByLevels[CurrentLevelData.levelType]);
        }

        public LevelData LoadLevel(int level, int index)
        {
            string levelPath = $"Data/Levels/{index}/Level_{level}";
            var levelData = Resources.Load<LevelData>(levelPath);
            if (levelData == null)
            {
                Debug.LogError($"Level data not found at path: {levelPath}");
                levelPath = $"Data/Levels/Level_{1}";
                levelData = Resources.Load<LevelData>(levelPath);
            }

            return levelData;
        }

        public LevelData LoadLevel(int level)
        {
            string levelPath = $"Data/Levels/Level_{level}";
            var levelData = Resources.Load<LevelData>(levelPath);
            if (levelData == null)
            {
                Debug.LogError($"Level data not found at path: {levelPath}");
                levelPath = $"Data/Levels/Level_{1}";
                levelData = Resources.Load<LevelData>(levelPath);
            }

            return levelData;
        }

        internal void PauseGame()
        {
            CurrentState = GameState.PAUSING;
            UIManager.Instance.GetPanel<UISettingGamePlay>().Show();
        }

        public void ResumeGame()
        {
            CurrentState = GameState.PLAYING;
        }

        public void EndGame()
        {
            CurrentState = GameState.ENDING;
            BoosterManager.CancelActiveBooster();
        }

        public int GetCurrentLevelCoinReward()
        {
            return m_gameConfig.coinEarnByLevels[CurrentLevelData.levelType];
        }
    }
}
