using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BattleUIManager: MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private List<SpriteRenderer> playerHP;

    [SerializeField] private Sprite trueHP;
    [SerializeField] private Sprite falseHP;

    public void SetHPSlider(float ratio)
    {
        if(hpSlider != null)
        {
            hpSlider.value = ratio;
        }
    }

    public void SetHPUI(int limitHP)
    {
        switch(limitHP)
        {
            case 0:
                playerHP[0].sprite = falseHP;
                playerHP[1].sprite = falseHP;
                playerHP[2].sprite = falseHP;
            break;
            case 1:
                playerHP[0].sprite = trueHP;
                playerHP[1].sprite = falseHP;
                playerHP[2].sprite = falseHP;
                break;
            case 2:
                playerHP[0].sprite = trueHP;
                playerHP[1].sprite = trueHP;
                playerHP[2].sprite = falseHP;
                break;
            case 3:
                playerHP[0].sprite = trueHP;
                playerHP[1].sprite = trueHP;
                playerHP[2].sprite = trueHP;
                break;
            default:
                Debug.LogError("範囲外の体力を指定しています。");
                break;
        }
    }

}
