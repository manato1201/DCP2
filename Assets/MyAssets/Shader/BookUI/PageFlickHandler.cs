using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI.Extensions;

public class PageFlickHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public BookUI bookUI;
    public float flickThreshold = 50f; // 最低移動距離（ピクセル）

    private Vector2 startPos;
    private bool isDragging = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        startPos = eventData.position;
        isDragging = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {


        if (!isDragging) return;

        Vector2 endPos = eventData.position;
        float deltaX = endPos.x - startPos.x;

        if (Mathf.Abs(deltaX) > flickThreshold)
        {
            if (deltaX < 0)
                bookUI.CurrentPage++;
            else
                bookUI.CurrentPage--;
        }
        if (Mathf.Abs(deltaX) > flickThreshold)
        {
            if (deltaX < 0)
            {
                bookUI.CurrentPage++;
                Debug.Log("Next Page");
            }
            else
            {
                bookUI.CurrentPage--;
                Debug.Log("Previous Page");
            }
        }
        isDragging = false;
    }
}