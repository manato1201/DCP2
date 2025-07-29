using System.Collections.Generic;
using UnityEngine;
public class GameController : MonoBehaviour
{
    // シングルトンインスタンス
    public static GameController Instance { get; private set; }

    [Header("ゲーム設定")]
    public List<GameObject> piecePrefabs; // ピースのPrefabリスト
    public Transform piecesContainer; // 配置ピースの親オブジェクト
    public int pieceCount; // ピースの個数

    private List<GameObject> spawnedPieces = new List<GameObject>();

    void Awake()
    {
        // シングルトン処理
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        pieceCount = piecePrefabs.Count;
        SpawnPieces();
    }

    // ピースを配置する
    void SpawnPieces()
    {
        // 既存のピースを全て削除
        foreach (var piece in spawnedPieces)
        {
            Destroy(piece);
        }
        spawnedPieces.Clear();

        // 新規生成
        float yOffset = 0f;
        foreach (var prefab in piecePrefabs)
        {
            Vector3 spawnPos = piecesContainer.position + new Vector3(0, yOffset, 0);
            GameObject newPiece = Instantiate(prefab, spawnPos, Quaternion.identity);
            spawnedPieces.Add(newPiece);

            // 各ピースのY座標をずらす（必要に応じて調整）
            yOffset -= 3.0f;
        }
    }

    // ゲームがクリアされたかチェックする
    public void CheckGameCompletion()
    {
        if (GridManager.Instance != null && GridManager.Instance.IsGridFull())
        {
            Debug.Log("パズルクリア（グリッド全埋め）");
            // ここにクリア画面を表示する処理を追加
            
        }
    }

    // ゲームをリセットする（ボタンなどから呼び出す用）
    public void ResetGame()
    {
        // グリッドの状態もリセットするためGridManagerの処理が必要だが、
        // 今はシーンをリロードしてリセットしている
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
