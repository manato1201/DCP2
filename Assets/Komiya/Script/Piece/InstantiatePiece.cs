using System.Collections.Generic;
using UnityEngine;

public class InstantiatePiece : MonoBehaviour
{
    [SerializeField] List<GameObject> PiecePrefab = new List<GameObject>();

    public void instantiateObject(int i)
    {
        GameObject piece = Instantiate(PiecePrefab[i]);
        piece.transform.localPosition = Vector3.zero;
    }
}
