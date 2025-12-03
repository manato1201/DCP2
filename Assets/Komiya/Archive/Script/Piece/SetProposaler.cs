using UnityEngine;
using Value;
using TMPro;

public class SetProposaler : MonoBehaviour
{
    [SerializeField] ValueManagement ValueManagement_;
    [SerializeField] TMP_Text text;
    [SerializeField] private bool isParentl;
    

    private void Start()
    {
        if (isParentl)
        {
            text.text = ValueManagement_.ParentMission.ToString();
        }
        if (!isParentl)
        {
            text.text = ValueManagement_.ChildMission.ToString();
        }
    }
}
