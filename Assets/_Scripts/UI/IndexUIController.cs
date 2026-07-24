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
            int killCount = entry.Type == IndexType.Monster ? IndexManager.Instance.GetKillCount(entry.EntryID) : 0;

            if (label != null)
            {
                if (entry.Type == IndexType.Monster)
                {
                    label.text = killCount >= 1 ? entry.EntryName : "???";
                }
                else
                {
                    label.text = isUnlocked ? entry.EntryName : "???";
                }
            }

            if (img != null)
            {
                if (entry.Type == IndexType.Monster)
                {
                    if (killCount >= 10)
                    {
                        img.sprite = entry.Icon;
                        img.color = unlockedColor;
                    }
                    else if (killCount >= 1)
                    {
                        img.sprite = entry.Icon;
                        img.color = Color.black; // Silhouette
                    }
                    else
                    {
                        img.sprite = lockSprite;
                        img.color = lockedColor;
                    }
                }
                else
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

        int killCount = entry.Type == IndexType.Monster ? IndexManager.Instance.GetKillCount(entry.EntryID) : 0;

        if (detailsIcon != null)
        {
            detailsIcon.gameObject.SetActive(true);
            if (entry.Type == IndexType.Monster)
            {
                if (killCount >= 10)
                {
                    detailsIcon.sprite = entry.Icon;
                    detailsIcon.color = unlockedColor;
                }
                else if (killCount >= 1)
                {
                    detailsIcon.sprite = entry.Icon;
                    detailsIcon.color = Color.black; // Silhouette
                }
                else
                {
                    detailsIcon.sprite = lockSprite;
                    detailsIcon.color = lockedColor;
                }
            }
            else
            {
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
        }

        if (detailsName != null)
        {
            if (entry.Type == IndexType.Monster)
            {
                detailsName.text = killCount >= 1 ? entry.EntryName : "LOCKED ENTRY";
            }
            else
            {
                detailsName.text = isUnlocked ? entry.EntryName : "LOCKED ENTRY";
            }
        }

        if (detailsType != null)
        {
            detailsType.text = entry.Type == IndexType.Monster ? "TYPE: ENEMY" : "TYPE: UPGRADE";
            detailsType.color = entry.Type == IndexType.Monster ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.8f, 1f);
        }

        if (detailsDescription != null)
        {
            if (entry.Type == IndexType.Monster)
            {
                if (killCount >= 50)
                {
                    detailsDescription.text = $"{entry.Description}\n\nKills: {killCount} (Maxed)\n🔥 <color=#32cd32>Bonus: +5% Damage against this enemy type!</color>";
                }
                else if (killCount >= 10)
                {
                    detailsDescription.text = $"{entry.Description}\n\nKills: {killCount} / 50\n(Defeat 50 to unlock Lore & +5% Damage bonus)";
                }
                else if (killCount >= 1)
                {
                    detailsDescription.text = $"Kills: {killCount} / 50\n\n[Stats Locked]\nDefeat 10 of this enemy type to unlock statistics.\nDefeat 50 to unlock Lore & +5% Damage bonus.";
                }
                else
                {
                    detailsDescription.text = "Defeat this enemy in combat to unlock its entry details.";
                }
            }
            else
            {
                detailsDescription.text = isUnlocked 
                    ? entry.Description 
                    : "Select this upgrade in combat to unlock its entry details.";
            }
        }

        if (detailsLore != null)
        {
            if (entry.Type == IndexType.Monster)
            {
                detailsLore.text = killCount >= 50 ? $"\"{entry.LoreText}\"" : "???";
            }
            else
            {
                detailsLore.text = isUnlocked ? $"\"{entry.LoreText}\"" : "???";
            }
        }
    }
}
