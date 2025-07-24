// 子オブジェクト用スクリプト
using UnityEngine;

public class ChildClickForwarder : MonoBehaviour
{
    private PieceController parentController;

    private void Start()
    {
        parentController = GetComponentInParent<PieceController>();
    }

    private void OnMouseDown()
    {
        parentController?.SendMessage("OnMouseDown", SendMessageOptions.DontRequireReceiver);
    }

    private void OnMouseUp()
    {
        parentController?.SendMessage("OnMouseUp", SendMessageOptions.DontRequireReceiver);
    }

    private void OnMouseDrag()
    {
        parentController?.SendMessage("OnMouseDrag", SendMessageOptions.DontRequireReceiver);
    }
}
