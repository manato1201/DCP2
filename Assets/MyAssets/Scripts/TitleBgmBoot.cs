using Cysharp.Threading.Tasks;
using UnityEngine;
using Sound;

public class TitleBgmBoot : MonoBehaviour
{
    [SerializeField] SoundManager sound; // Inspectorで参照

     void Start()
    {
       sound.PlayBGMAsync("BGM_Title", loop:true).Forget();

    }
}
