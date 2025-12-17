using UnityEngine;
using UnityEngine.Events;

public class UnitStatus : MonoBehaviour
{
    [Header("Status Settings")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int currentHP = 10;

    public UnityEvent<float> OnHPChanged;

    public UnityEvent OnDead;

    public void OnUIStart()
    {
        currentHP = maxHP;
        UpdateHPView();
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        if(currentHP <= 0)
        {
            currentHP = 0;
            OnDead?.Invoke();
        }

        UpdateHPView();
    }

    private void UpdateHPView()
    {
        float ratio = (float)currentHP / maxHP;
        OnHPChanged?.Invoke(ratio);
    }
}
