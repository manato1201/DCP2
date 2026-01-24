using UnityEngine;
// 1. APooledObject を継承する
public class BlockGroup : APooledObject
{
    public BlockShape shape;
    public GameObject[] childBlocks; 
    public bool isPaletteBlock = false;

    public int blockLevel = 1;  //ブロックのレベル
    // 内部で保持するピース（返却時に使う）
    private piece[] _cachedPieces;

    [SerializeField] private Transform gridCellParent;
    [SerializeField] private Transform blockParent;

    // 引数を変更：GameObjectのPrefabではなくピースのプールを受け取る
    public void Initialize(BlockShape shape, ObjectPool<piece> piecePool, Transform parent, float gridScale = 1.0f)
    {
        this.shape = shape;

        // 配列初期化
        int count = shape.ShapeCoordinates.Length;
        childBlocks = new GameObject[count];
        _cachedPieces = new piece[count]; // Release用にpieceコンポーネントも覚えておく

        for (int i = 0; i < count; i++)
        {
            Vector2Int coord = shape.ShapeCoordinates[i];
            Vector3 piecePos = new Vector3(coord.x * gridScale, coord.y * gridScale, 0);

            piece p = piecePool.Get();

            if (p != null)
            {
                p.tag = "Block";

                // 親子関係の設定
                p.transform.SetParent(this.transform);
                p.transform.localPosition = piecePos;
                p.transform.localRotation = Quaternion.identity;

                // 配列に保存
                childBlocks[i] = p.gameObject;
                _cachedPieces[i] = p;
            }
        }

        // BlockGroup自体の親を設定（パレットスロットなど）
        if (parent != null)
        {
            transform.SetParent(parent, false);
            transform.localPosition = Vector3.zero;
        }
    }

    //BlockGroupがプールに返される時、持っているピースも全部返す
    protected override void OnDespawn()
    {
        if (_cachedPieces != null)
        {
            foreach (var p in _cachedPieces)
            {
                if (p != null)
                {
                    // ピースをプールへ返却
                    p.Release();
                }
            }
            // 配列をクリア
            System.Array.Clear(childBlocks, 0, childBlocks.Length);
            System.Array.Clear(_cachedPieces, 0, _cachedPieces.Length);
        }

        // 必要なら自身のパラメータリセット
        isPaletteBlock = false;
    }
}
