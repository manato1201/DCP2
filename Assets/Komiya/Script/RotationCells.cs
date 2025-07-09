using UnityEngine;

namespace Cell
{
    //==========================================
    //�S����:���{��
    //�@�\:�N���b�N�������Ƀu���b�N����]
    //==========================================

    public class RotationCells : MonoBehaviour
    {
        Piece Piece_;
        private Vector2 mouseDownPos;
        private bool isMouseDown = false;

        // �h���b�O�Ƃ݂Ȃ��ŏ��ړ������i���̋����ȏ㓮������h���b�O�j
        private float dragThreshold = 0.1f;

        private void Start()
        {
            Piece_ = GetComponentInParent<Piece>();
        }

        private void Update()
        {
            // �}�E�X�������ꂽ�u�Ԃ̈ʒu���L�^
            if (Input.GetMouseButtonDown(0))
            {
                isMouseDown = true;
                mouseDownPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }

            // �}�E�X�������ꂽ�Ƃ��A�N���b�N���h���b�O���𔻒f
            if (Input.GetMouseButtonUp(0) && isMouseDown)
            {
                isMouseDown = false;
                Vector2 mouseUpPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                float distance = Vector2.Distance(mouseDownPos, mouseUpPos);

                if (distance < dragThreshold)
                {
                    // �N���b�N�Ɣ��f
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