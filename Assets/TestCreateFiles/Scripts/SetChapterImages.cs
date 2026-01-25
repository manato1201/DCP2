using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class SetChapterImages : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private List<Sprite> enemyImages = new List<Sprite>();
    [SerializeField] private List<Sprite> GB = new List<Sprite>();

    private Sprite targetEnemy;
    private Sprite targetGB;

    [SerializeField] private GameObject objectGB;
    [SerializeField] private GameObject objectEnemy;


    public void SetImagesForChapter(string chapter = "CHAP1")
    {
        if(chapter == "CHAP1" || chapter == "")
        {
            targetEnemy = enemyImages[0];
            targetGB = GB[0];
        }
        if(chapter == "CHAP2")
        {
            targetEnemy = enemyImages[1];
            targetGB = GB[1];
        }

        SetImages();
    }

    public void SetImages()
    {
        SpriteRenderer Esr = objectEnemy.GetComponent<SpriteRenderer>();
        SpriteRenderer GBsr = objectGB.GetComponent<SpriteRenderer>();

        Esr.sprite = targetEnemy;
        GBsr.sprite = targetGB;
    }
}
