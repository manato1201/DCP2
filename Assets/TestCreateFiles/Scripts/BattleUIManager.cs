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

    [SerializeField] private GameObject damageTextPrefab;
    public void SetHPSlider(float ratio)
    {
        if(hpSlider != null)
        {
            hpSlider.value = ratio;
        }
    }

    public void ShowDamage(int damage, Vector3 worldPosition)
    {
        GameObject popupObj = Instantiate(damageTextPrefab, worldPosition, Quaternion.identity);

        // 少し上にずらすなどの調整
        popupObj.transform.position += new Vector3(0, 1f, 0);

        DamgaeDisplay popup = popupObj.GetComponent<DamgaeDisplay>();
        if (popup != null)
        {
            popup.Setup(damage);
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
