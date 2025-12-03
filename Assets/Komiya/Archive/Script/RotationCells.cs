using UnityEngine;

namespace Cell
{
    //==========================================
    //担当者:小宮純
    //機能:クリックした時にブロックを回転
    //==========================================

    public class RotationCells : MonoBehaviour
    {
        Piece Piece_;
        private Vector2 mouseDownPos;
        private bool isMouseDown = false;

        // ドラッグとみなす最小移動距離（この距離以上動いたらドラッグ）
        private float dragThreshold = 0.1f;

        private void Start()
        {
            Piece_ = GetComponentInParent<Piece>();
        }

        private void Update()
        {
            // マウスが押された瞬間の位置を記録
            if (Input.GetMouseButtonDown(0))
            {
                isMouseDown = true;
                mouseDownPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }

            // マウスが離されたとき、クリックかドラッグかを判断
            if (Input.GetMouseButtonUp(0) && isMouseDown)
            {
                isMouseDown = false;
                Vector2 mouseUpPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                float distance = Vector2.Distance(mouseDownPos, mouseUpPos);

                if (distance < dragThreshold)
                {
                    // クリックと判断
                    Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

                    if (hit.collider != null && hit.collider.gameObject == this.gameObject)
                    {
                        Piece_.Rotate(true);
                    }
                }
            }
        }
    }
}