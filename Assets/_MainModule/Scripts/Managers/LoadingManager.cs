using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TigerForge;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    public Slider loadingBar;
    public GameObject vfx;
    public GameObject loadingObj;

    int amountOpen;

    public void Start()
    {
#if UNITY_EDITOR
        UnityEngine.Debug.unityLogger.logEnabled = true;
#else
        UnityEngine.Debug.unityLogger.logEnabled = false;
#endif

        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        //loadingObj.SetActive(false);
        //vfx.SetActive(true);

        LoadData();

    }

    private void LoadScene(bool adConsent, bool trackingConsent)
    {
        // Scene Loading
        LoadData();
    }

    bool showAOA = false;
    IEnumerator LoadSceneStart()
    {
        yield return new WaitForSeconds(2f);
        loadingObj.SetActive(true);
        //vfx.SetActive(false);

        //if (UserConfig.Instance.isNewUser)
        //{
        //    CGTeamBridge.instance.SetPropertyLevel("1");
        //    CGTeamBridge.instance.SetPropertyAppVersion();
        //}
        //CGTeamBridge.instance.LogEvent("open_app");


        //RemoteConfig.Instance.LoadLevelData();
        StartCoroutine(LoadAsyncGame(1));

        //if (!showAOA && amountOpen >= RemoteConfig.Instance.AOAShow)
        //{
        //    Debug.Log("Show AOA first time");
        //    showAOA = true;
        //    CGTeamBridge.instance.ShowAOA();
        //}
    }

    void LoadData()
    {
        // Ui Loading
        StartCoroutine(LoadSceneStart());
    }

    IEnumerator LoadAsyncGame(int scene)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(scene);
        operation.allowSceneActivation = false;
        while (!operation.isDone)
        {
            float progress = operation.progress;

            if (progress == 0.9f)
            {
                loadingBar.DOValue(1.0f, 2.0f).SetEase(Ease.Linear).OnComplete(() =>
                {
                    loadingBar.value = 1.0f;
                    operation.allowSceneActivation = true;
                });
            }

            yield return null;
        }
    }

    public void PressedRetryBtn()
    {
        //vfx.SetActive(true);
        LoadData();
    }
}
