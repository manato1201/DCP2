using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemyManager : MonoBehaviour
{
    public GameObject enemyObject;
    public GameObject messageBubble;
    public TextMeshProUGUI messageText;

    void Start()
    {
        enemyObject.SetActive(false);
        messageBubble.SetActive(false);
        messageText.gameObject.SetActive(false);
    }

    void Update()
    {
        // 'S'キーを押したら敵を表示し、メッセージも表示
        if (Input.GetKeyDown(KeyCode.S))
        {
            StartCoroutine(ShowEnemy());
        }

        // 'H'キーを押すか、グリッドが全て埋まっている時に非表示にする
        if (Input.GetKeyDown(KeyCode.H) || (GridManager.Instance != null && GridManager.Instance.IsGridFull()))
        {
            HideEnemy();
        }
    }

    // 敵を表示し、メッセージも表示するコルーチン
    public IEnumerator ShowEnemy()
    {
        if (enemyObject != null)
        {
            Debug.Log("敵を表示します");
            enemyObject.SetActive(true);

            // メッセージ表示
            yield return ShowMessage("Test Message");

            // 3秒後にメッセージを消す
            yield return new WaitForSeconds(3f);
            HideMessage();
        }
    }

    public void HideEnemy()
    {
        if (enemyObject != null)
        {
            Debug.Log("敵を消滅させます");
            enemyObject.SetActive(false);
        }
    }

    public IEnumerator ShowMessage(string message)
    {
        messageBubble.SetActive(true); // 吹き出しを表示する
        messageText.text = message;
        messageText.gameObject.SetActive(true);
        yield return null;
    }

    public void HideMessage()
    {
        messageBubble.SetActive(false); // 吹き出しを非表示にする
        messageText.gameObject.SetActive(false);
    }
}