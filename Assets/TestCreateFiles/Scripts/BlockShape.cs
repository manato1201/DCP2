using UnityEngine;

[CreateAssetMenu(fileName = "BlockShape", menuName = "Scriptable Objects/BlockShape")]
public class BlockShape : ScriptableObject
{
    [Header("形状データ")]
    public Vector2Int[] ShapeCoordinates;

    public enum BlockElement
    {
        red,
        green,
        blue,
        yellow,
        bluck,
    }
    public BlockElement blockElement = BlockElement.red;
}
