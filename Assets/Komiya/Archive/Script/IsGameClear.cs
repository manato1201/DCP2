using NUnit.Framework;
using TMPro;
using UnityEngine;
using Value;

public class IsGameClear : MonoBehaviour
{
    [SerializeField] ValueManagement valueManagement;
    [SerializeField] TextMeshProUGUI resultText;

    [SerializeField] string clearText = "GameClear!";
    [SerializeField] string gameoverText = "GameOver...";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetResultText();
    }

    private void SetResultText()
    {
        if (valueManagement.isGameClear)
        {
            resultText.text = clearText;
        }
        else
        {
             resultText.text = gameoverText;
        }
    }
}
