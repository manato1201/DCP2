using Unity.VisualScripting;
using UnityEngine;

public class BlockGroup : MonoBehaviour
{
    public BlockShape shape; //この塊の設計図
    public GameObject[] childBlocks; //実際に生成された欠片達

    public bool isPaletteBlock = false;

    //この塊を構成する欠片(子オブジェクト)を生成する
    public void Initialize(BlockShape shape, GameObject piecePrefab, Transform parent)
    {
        this.shape = shape;

        //設計図(shapeCoordinates)に基づいて欠片を生成
        childBlocks = new GameObject[shape.ShapeCoordinates.Length];
        for(int i = 0; i < shape.ShapeCoordinates.Length; i++)
        {
            Vector2Int coord = shape.ShapeCoordinates[i];

            //ワールド座標に変換
            Vector3 piecePos = new Vector3(coord.x, coord.y, 0);

            GameObject piece = Instantiate(piecePrefab, piecePos, Quaternion.identity, parent);

            piece.tag = "Block";

            //この塊の親オブジェクトに追従するように親子関係を設定
            piece.transform.SetParent(this.transform);
            //相対位置を補正
            piece.transform.localPosition = piecePos;
            childBlocks[i] = piece;
        }
    }

    public void NotifyChildDestroyed(GameObject childPiece)
    {
        int destroyedCount = 0;
        
        for(int i = 0; i < childBlocks.Length;i++)
        {
            if(childBlocks[i] == childPiece)
            {
                childBlocks[i] = null;
            }

            if (childBlocks[i] == null)
            {
                destroyedCount++;
            }

        }
        if (destroyedCount == childBlocks.Length)
        {
            Destroy(this.gameObject);
        }

    }
}
