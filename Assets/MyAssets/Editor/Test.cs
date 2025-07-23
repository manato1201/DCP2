using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class Test : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        void OnUpdate()
        {
            if (BuildPipeline.isBuildingPlayer) return;

            EditorApplication.update -= OnUpdate;

            Debug.Log("ビルド終了");
        }

        EditorApplication.update += OnUpdate;

        Debug.Log("ビルド開始");
    }
}