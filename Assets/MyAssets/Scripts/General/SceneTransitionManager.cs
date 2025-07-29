using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DB;
using System.Collections.Generic;
using Sound;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("DB_Scene(ScriptableObject)")]
    [SerializeField] private DB_Scene dbScene; // Inspector不要ならEntity参照でもOK

    [Header("トランジションコントローラ")]
    [SerializeField] private TransitionEffectController transitionController;

    [Header("遷移用ボタン（任意個）")]
    [SerializeField] private List<Button> transitionButtons;

    [Header("トランジションマテリアルのインデックス (optional)")]
    [SerializeField] private int transitionMaterialIndex = 0;

    private void Awake()
    {
        

        transitionController.PlayTransitionOut();
        
        foreach (var btn in transitionButtons)
        {
            // ボタン名とDB_Scene.SceneName[].Nameを一致させて探す
            string targetName = btn.name; // ここはカスタムIDを持たせてもOK
            btn.onClick.AddListener(() => OnButtonClicked(targetName));
        }
        
    }
    

    private void OnButtonClicked(string sceneKey)
    {
        if (dbScene == null) dbScene = DB_Scene.Entity;
        if (dbScene.SceneName == null)
        {
            Debug.LogError("DB_Sceneにシーン情報がありません！");
            return;
        }
        // Nameが一致するSceneObjを探す
        var sceneObj = dbScene.SceneName.Find(x => x.Name == sceneKey);
        if (sceneObj == null)
        {
            Debug.LogError($"シーン名 \"{sceneKey}\" がDB_Sceneに見つかりません！");
            return;
        }
        SoundManager.Instance.PlaySE("TransitionSE", volume: 1.0f);
        StartSceneTransition(sceneObj.Name, transitionMaterialIndex);
    }

    public void StartSceneTransition(string sceneName, int materialIdx = 0)
    {
        
        if (transitionController != null)
        {
            
            transitionController.PlayTransitionIn(materialIdx, () =>
            {
                
                SceneManager.LoadScene(sceneName);
            });
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}