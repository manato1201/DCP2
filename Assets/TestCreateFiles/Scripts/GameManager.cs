using UnityEngine;
using Cysharp.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    [SerializeField]PuzzleController controller;
    private void Update()
    {
        controller.OnControllerUpdate();
    }

    private void Awake()
    {
        
        controller.OnControllerStart();
    }
}
