using System;
using UnityEditor;

namespace Internal
{
#if true

    [InitializeOnLoad]
    internal static class AutoFocusGameViewOnPlayModeStateChanged
    {
        private static readonly Type GAME_VIEW_TYPE;

        static AutoFocusGameViewOnPlayModeStateChanged()
        {
            GAME_VIEW_TYPE = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode) return;
            EditorWindow.FocusWindowIfItsOpen(GAME_VIEW_TYPE);
        }
    }
#endif
}