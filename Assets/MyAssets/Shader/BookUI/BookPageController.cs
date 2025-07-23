using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class BookPageController : MonoBehaviour
{
    [SerializeField] private BookUI bookUI;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;

    [SerializeField] private int maxPageCount = 5;
    [SerializeField] private float pageChangeCooldown = 0.5f;

    private bool isProcessing = false;

    void Start()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextButtonClick);

        if (prevButton != null)
            prevButton.onClick.AddListener(OnPrevButtonClick);
    }

    public void OnNextButtonClick()
    {
        StartCoroutine(GoToPageCoroutine(1));
    }

    public void OnPrevButtonClick()
    {
        StartCoroutine(GoToPageCoroutine(-1));
    }

    private IEnumerator GoToPageCoroutine(int direction)
    {
        if (isProcessing)
            yield break;

        int nextPage = bookUI.CurrentPage + direction;

        if (!IsValidPage(nextPage))
            yield break;

        isProcessing = true;

        bookUI.CurrentPage = nextPage;

        yield return new WaitForSeconds(pageChangeCooldown);

        isProcessing = false;
    }

    private bool IsValidPage(int page)
    {
        return page >= 0 && page < maxPageCount;
    }
}