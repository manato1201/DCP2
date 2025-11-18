using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutoScrollTMPro : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI text;

    [Tooltip("Viewport 高さと比較するための余白（px）")]
    [SerializeField] private float verticalPadding = 8f;

    public void SetText(string content)
    {
        if (text == null || scrollRect == null) return;

        text.text = content ?? string.Empty;

        // レイアウトを即時更新
        LayoutRebuilder.ForceRebuildLayoutImmediate(text.rectTransform);
        text.ForceMeshUpdate();

        float contentH = text.preferredHeight;
        float viewH = scrollRect.viewport.rect.height - verticalPadding;

        bool needScroll = contentH > viewH + 0.5f;
        scrollRect.vertical = needScroll;               // はみ出したときだけスクロールON
        scrollRect.verticalScrollbar?.gameObject.SetActive(needScroll);

        // 先頭表示（上に寄せる）
        scrollRect.normalizedPosition = new Vector2(0f, 1f);
    }
}