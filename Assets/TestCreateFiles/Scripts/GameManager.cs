using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]PuzzleController controller;
    private void Update()
    {
        controller.OnControllerUpdate();
    }

    private void Start()
    {
        controller.OnControllerStart();
    }
}
