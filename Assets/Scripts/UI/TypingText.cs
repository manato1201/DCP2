using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TypingText : MonoBehaviour
{
    [Header("Text Display Settings")]
    [SerializeField] private List<TMP_Text> textComponents; // 複数のTextMeshProコンポーネント
    [SerializeField] private float typingSpeed = 0.05f; // 文字送りの速度

    private bool isTyping = false;        // 現在文字送り中か
    private bool skipRequested = false;   // スキップリクエストがあったか
    private Coroutine typingCoroutine;    // 現在動作中のコルーチン

    void Start()
    {
        if (textComponents == null || textComponents.Count == 0)
        {
            Debug.LogError("textComponents が設定されていません！");
        }
    }

    /// <summary>
    /// 指定した番号のテキストコンポーネントに文字送りを開始
    /// </summary>
    /// <param name="text">表示するテキスト</param>
    /// <param name="index">対象のTextMeshProコンポーネントのインデックス</param>
    public void StartTyping(string text, int index)
    {
        if (index < 0 || index >= textComponents.Count)
        {
            Debug.LogError("指定されたインデックスが範囲外です。");
            return;
        }

        TMP_Text targetTextComponent = textComponents[index];
        if (targetTextComponent == null)
        {
            Debug.LogError($"インデックス {index} に対応するTextMeshProコンポーネントが設定されていません。");
            return;
        }

        if (isTyping)
        {
            StopTyping(); // 現在の文字送りを停止
        }

        targetTextComponent.text = ""; // テキストをクリア
        isTyping = true;
        skipRequested = false;
        typingCoroutine = StartCoroutine(TypeText(text, targetTextComponent));
    }

    /// <summary>
    /// 指定したテキストコンポーネントで文字送りを実行するコルーチン
    /// </summary>
    private IEnumerator TypeText(string text, TMP_Text targetTextComponent)
    {
        foreach (char letter in text)
        {
            if (skipRequested)
            {
                targetTextComponent.text = text; // 全テキストを即時表示
                break;
            }

            targetTextComponent.text += letter; // 1文字ずつ追加
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false; // 文字送り終了
        typingCoroutine = null;
    }

    /// <summary>
    /// 文字送りを停止し、全テキストを即時表示
    /// </summary>
    public void StopTyping()
    {
        skipRequested = true;
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        // コルーチンが止まったタイミングで即時表示も行いたい場合、下記のように補完
        // ただしTypeText内のbreakで自動的に全表示されるので、不要な場合は省略OK
        // if (isTyping && targetTextComponent != null) targetTextComponent.text = ???;
        isTyping = false;
    }

    /// <summary>
    /// 現在の文字送り状態を取得
    /// </summary>
    /// <returns>文字送り中かどうか</returns>
    public bool IsTyping()
    {
        return isTyping;
    }
}