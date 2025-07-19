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
        // 'S'�L�[����������G��\�����A���b�Z�[�W���\��
        if (Input.GetKeyDown(KeyCode.S))
        {
            StartCoroutine(ShowEnemy());
        }

        // 'H'�L�[���������A�O���b�h���S�Ė��܂��Ă��鎞�ɔ�\���ɂ���
        if (Input.GetKeyDown(KeyCode.H) || (GridManager.Instance != null && GridManager.Instance.IsGridFull()))
        {
            HideEnemy();
        }
    }

    // �G��\�����A���b�Z�[�W���\������R���[�`��
    public IEnumerator ShowEnemy()
    {
        if (enemyObject != null)
        {
            Debug.Log("�G��\�����܂�");
            enemyObject.SetActive(true);

            // ���b�Z�[�W�\��
            yield return ShowMessage("Test Message");

            // 3�b��Ƀ��b�Z�[�W������
            yield return new WaitForSeconds(3f);
            HideMessage();
        }
    }

    public void HideEnemy()
    {
        if (enemyObject != null)
        {
            Debug.Log("�G�����ł����܂�");
            enemyObject.SetActive(false);
        }
    }

    public IEnumerator ShowMessage(string message)
    {
        messageBubble.SetActive(true); // �����o����\������
        messageText.text = message;
        messageText.gameObject.SetActive(true);
        yield return null;
    }

    public void HideMessage()
    {
        messageBubble.SetActive(false); // �����o�����\���ɂ���
        messageText.gameObject.SetActive(false);
    }
}