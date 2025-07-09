using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    // �V���O���g���C���X�^���X
    public static GameController Instance { get; private set; }

    [Header("�Q�[���ݒ�")]
    public List<GameObject> piecePrefabs; // �s�[�X��Prefab���X�g
    public Transform piecesContainer; // �ҋ@�s�[�X��u���ꏊ
    public int pieceCount; // �s�[�X�̑���

    private List<GameObject> spawnedPieces = new List<GameObject>();

    void Awake()
    {
        // �V���O���g��
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        pieceCount = piecePrefabs.Count;
        SpawnPieces();
    }

    // �s�[�X�𐶐����đҋ@�ꏊ�ɔz�u����
    void SpawnPieces()
    {
        // �����̃s�[�X������΍폜
        foreach (var piece in spawnedPieces)
        {
            Destroy(piece);
        }
        spawnedPieces.Clear();

        // �V��������
        float yOffset = 0f;
        foreach (var prefab in piecePrefabs)
        {
            Vector3 spawnPos = piecesContainer.position + new Vector3(0, yOffset, 0);
            GameObject newPiece = Instantiate(prefab, spawnPos, Quaternion.identity);
            spawnedPieces.Add(newPiece);

            // ���̃s�[�X��Y���W�𒲐��i�K�X�ύX���Ă��������j
            yOffset -= 3.0f;
        }
    }

    // �Q�[�����N���A���ꂽ���`�F�b�N����
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
            Debug.Log("�p�Y���N���A");
            // �����ɃN���A��ʂ�\�����鏈����ǉ�
        }
    }

    // �Q�[�������Z�b�g����i�{�^������Ăяo���p�j
    public void ResetGame()
    {
        // �O���b�h�̏�Ԃ����Z�b�g���邽�߂�GridManager�̋@�\���K�v�����A
        // ����̓V���v���ɃV�[�����ēǂݍ��݂���̂���ԊȒP
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
