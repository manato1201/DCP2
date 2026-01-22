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

    [SerializeField] private Image attackGaugeImage;

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

        MoveTextDisplay popup = popupObj.GetComponent<MoveTextDisplay>();
        if (popup != null)
        {
            popup.Setup(damage);
        }
    }

    /// <summary>
    /// ダメージゲージの表示を更新する
    /// </summary>
    /// <param name="current">現在の蓄積ダメージ</param>
    /// <param name="max">ダメージ上限値</param>
    public void UpdateAttackGauge(int current, int max)
    {
        if (attackGaugeImage == null) return;

        // 0.0 ～ 1.0 の割合に変換
        float ratio = (float)current / max;

        // fillAmountにセット (1.0を超えても見た目は1.0で止まるが、念のためClampしても良い)
        attackGaugeImage.fillAmount = Mathf.Clamp01(ratio);
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
