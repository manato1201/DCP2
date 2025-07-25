using UnityEngine;
using UnityEngine.UI;

public class ProposalLine : MonoBehaviour
{
    [SerializeField] private Image targetImage;     // 色を変えたいImage
    [SerializeField] private Color color1 = Color.white;
    [SerializeField] private Color color2 = Color.red;
    [SerializeField] private float duration = 1f;   // 往復にかかる時間

    private void Update()
    {
        if (targetImage == null) return;

        float t = Mathf.PingPong(Time.time / duration, 1f);
        targetImage.color = Color.Lerp(color1, color2, t);
    }
}