using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    // シングルトンインスタンス
    public static GameController Instance { get; private set; }

    [Header("ゲーム設定")]
    public List<GameObject> piecePrefabs; // ピースのPrefabリスト
    public Transform piecesContainer; // 待機ピースを置く場所
    public int pieceCount; // ピースの総数

    private List<GameObject> spawnedPieces = new List<GameObject>();

    void Awake()
    {
        // シングルトン
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        pieceCount = piecePrefabs.Count;
        SpawnPieces();
    }

    // ピースを生成して待機場所に配置する
    void SpawnPieces()
    {
        // 既存のピースがあれば削除
        foreach (var piece in spawnedPieces)
        {
            Destroy(piece);
        }
        spawnedPieces.Clear();

        // 新しく生成
        float yOffset = 0f;
        foreach (var prefab in piecePrefabs)
        {
            Vector3 spawnPos = piecesContainer.position + new Vector3(0, yOffset, 0);
            GameObject newPiece = Instantiate(prefab, spawnPos, Quaternion.identity);
            spawnedPieces.Add(newPiece);

            // 次のピースのY座標を調整（適宜変更してください）
            yOffset -= 3.0f;
        }
    }

    // ゲームがクリアされたかチェックする
    public void CheckGameCompletion()
    {
        int placedCount = 0;
        foreach (var pieceObject in spawnedPieces)
        {
            PieceController piece = pieceObject.GetComponent<PieceController>();
            if (piece != null && piece.isPlaced)
            {
                placedCount++;
            }
        }

        if (placedCount == pieceCount)
        {
            Debug.Log("パズルクリア");
            // ここにクリア画面を表示する処理を追加
        }
    }

    // ゲームをリセットする（ボタンから呼び出す用）
    public void ResetGame()
    {
        // グリッドの状態をリセットするためにGridManagerの機能が必要だが、
        // 今回はシンプルにシーンを再読み込みするのが一番簡単
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
