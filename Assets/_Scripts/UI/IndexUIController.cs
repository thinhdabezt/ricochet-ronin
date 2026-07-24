using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class IndexUIController : MonoBehaviour
{
    [Header("Grid Setup")]
    [SerializeField] private Transform encountersGridContainer;
    [SerializeField] private Transform perksGridContainer;
    [SerializeField] private GameObject gridItemTemplate;

    [Header("Details Panel References")]
    [SerializeField] private Image detailsIcon;
    [SerializeField] private TextMeshProUGUI detailsName;
    [SerializeField] private TextMeshProUGUI detailsType;
    [SerializeField] private TextMeshProUGUI detailsDescription;
    [SerializeField] private TextMeshProUGUI detailsLore;

    [Header("Back Navigation")]
    [SerializeField] private Button backButton;

    [Header("Visual Resources")]
    [SerializeField] private Sprite lockSprite;
    [SerializeField] private Color lockedColor = new Color(0.15f, 0.15f, 0.2f, 0.8f);
    [SerializeField] private Color unlockedColor = Color.white;

    private void Start()
    {
        // Smooth entry transition (fade-in & zoom-in)
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = gameObject.AddComponent<CanvasGroup>();
        }
        cg.alpha = 0f;
        cg.DOFade(1f, 0.6f).SetEase(Ease.OutCubic).SetUpdate(true);

        transform.localScale = new Vector3(0.95f, 0.95f, 1f);
        transform.DOScale(1f, 0.6f).SetEase(Ease.OutCubic).SetUpdate(true);

        if (backButton != null)
        {
            backButton.onClick.AddListener(BackToMainMenu);
        }

        ClearDetails();
        PopulateGrid();
    }

    private void BackToMainMenu()
    {
        Time.timeScale = 1f;

        // Smooth exit transition (fade-out & zoom-out)
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            if (backButton != null) backButton.interactable = false;
            
            transform.DOScale(0.95f, 0.4f).SetEase(Ease.InCubic).SetUpdate(true);
            cg.DOFade(0f, 0.4f).SetEase(Ease.InCubic).SetUpdate(true).OnComplete(() =>
            {
                SceneManager.LoadScene("MainMenuScene");
            });
        }
        else
        {
            SceneManager.LoadScene("MainMenuScene");
        }
    }

    private void ClearDetails()
    {
        if (detailsIcon != null) detailsIcon.gameObject.SetActive(false);
        if (detailsName != null) detailsName.text = "SELECT AN ENTRY";
        if (detailsType != null) detailsType.text = "";
        if (detailsDescription != null) detailsDescription.text = "";
        if (detailsLore != null) detailsLore.text = "";
    }

    private void PopulateGrid()
    {
        if (encountersGridContainer == null || perksGridContainer == null || gridItemTemplate == null || IndexManager.Instance == null) return;

        gridItemTemplate.SetActive(false);

        // Clear both containers
        ClearContainer(encountersGridContainer);
        ClearContainer(perksGridContainer);

        var entries = IndexManager.Instance.AllEntries;
        foreach (var entry in entries)
        {
            if (entry == null) continue;

            Transform targetContainer = entry.Type == IndexType.Monster ? encountersGridContainer : perksGridContainer;

            GameObject itemGo = Instantiate(gridItemTemplate, targetContainer);
            itemGo.SetActive(true);

            Button btn = itemGo.GetComponent<Button>();
            Image img = itemGo.transform.Find("Icon")?.GetComponent<Image>();
            TextMeshProUGUI label = itemGo.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();

            bool isUnlocked = IndexManager.Instance.IsUnlocked(entry.EntryID);

            if (label != null)
            {
                label.text = isUnlocked ? entry.EntryName : "???";
            }

            if (img != null)
            {
                if (isUnlocked)
                {
                    img.sprite = entry.Icon;
                    img.color = unlockedColor;
                }
                else
                {
                    img.sprite = lockSprite;
                    img.color = lockedColor;
                }
            }

            if (btn != null)
            {
                btn.onClick.AddListener(() => ShowDetails(entry, isUnlocked));
            }
        }
    }

    private void ClearContainer(Transform container)
    {
        foreach (Transform child in container)
        {
            if (child.gameObject != gridItemTemplate)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void ShowDetails(IndexEntryData entry, bool isUnlocked)
    {
        if (entry == null) return;

        if (detailsIcon != null)
        {
            detailsIcon.gameObject.SetActive(true);
            if (isUnlocked)
            {
                detailsIcon.sprite = entry.Icon;
                detailsIcon.color = unlockedColor;
            }
            else
            {
                detailsIcon.sprite = lockSprite;
                detailsIcon.color = lockedColor;
            }
        }

        if (detailsName != null)
        {
            detailsName.text = isUnlocked ? entry.EntryName : "LOCKED ENTRY";
        }

        if (detailsType != null)
        {
            detailsType.text = entry.Type == IndexType.Monster ? "TYPE: ENEMY" : "TYPE: UPGRADE";
            detailsType.color = entry.Type == IndexType.Monster ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.8f, 1f);
        }

        if (detailsDescription != null)
        {
            detailsDescription.text = isUnlocked 
                ? entry.Description 
                : "Defeat this enemy or select this upgrade in combat to unlock its entry details.";
        }

        if (detailsLore != null)
        {
            detailsLore.text = isUnlocked ? $"\"{entry.LoreText}\"" : "???";
        }
    }
}
