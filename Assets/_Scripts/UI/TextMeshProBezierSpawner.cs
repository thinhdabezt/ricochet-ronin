using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class TextMeshProBezierSpawner : MonoBehaviour
{
    [Header("Pool Setup")]
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private int poolSize = 20;

    [Header("Visual Configuration")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private float outlineWidth = 0.25f;
    [SerializeField] private Color outlineColor = Color.black;

    [Header("Time Pop-up Settings (Straight Vertical)")]
    [SerializeField] private Color timeColor = new Color(0f, 1f, 0.5f, 1f); // Neon Green
    [SerializeField] private float timeDuration = 0.4f;
    [SerializeField] private float timeTravelDistance = 2f;
    [SerializeField] private float timePunchScaleAmount = 0.2f;

    [Header("Score Pop-up Settings (Bezier Arc)")]
    [SerializeField] private Color scoreColor = new Color(1f, 0.84f, 0f, 1f); // Neon Yellow
    [SerializeField] private float scoreDuration = 0.5f;
    [SerializeField] private Vector2 scoreStartOffset = new Vector2(0.5f, 0f);
    [SerializeField] private float scoreArcHeight = 1.2f;
    [SerializeField] private float scoreArcDistance = 1.2f;
    [SerializeField] private float scoreTargetScale = 1.2f;

    private Queue<GameObject> poolQueue = new Queue<GameObject>();

    private void Awake()
    {
        InitializePool();
    }

    private void OnEnable()
    {
        GameEvents.OnScoreAndTimeGained += HandleScoreAndTimeGained;
    }

    private void OnDisable()
    {
        GameEvents.OnScoreAndTimeGained -= HandleScoreAndTimeGained;
    }

    private void InitializePool()
    {
        if (textPrefab == null)
        {
            Debug.LogWarning("TextMeshProBezierSpawner: Text Prefab is not assigned.");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(textPrefab, transform);
            obj.SetActive(false);
            
            // Programmatically ensure outline styling is applied at creation
            TMP_Text tmp = obj.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                if (fontAsset != null)
                {
                    tmp.font = fontAsset;
                }
                tmp.outlineColor = outlineColor;
                tmp.outlineWidth = outlineWidth;
            }

            poolQueue.Enqueue(obj);
        }
    }

    private GameObject GetPooledObject()
    {
        if (poolQueue.Count == 0)
        {
            if (textPrefab != null)
            {
                GameObject obj = Instantiate(textPrefab, transform);
                obj.SetActive(false);
                TMP_Text tmp = obj.GetComponentInChildren<TMP_Text>();
                if (tmp != null)
                {
                    if (fontAsset != null) tmp.font = fontAsset;
                    tmp.outlineColor = outlineColor;
                    tmp.outlineWidth = outlineWidth;
                }
                return obj;
            }
            return null;
        }

        GameObject pooledObj = poolQueue.Dequeue();
        if (pooledObj == null)
        {
            return GetPooledObject();
        }
        return pooledObj;
    }

    private void ReturnToPool(GameObject obj)
    {
        if (obj != null)
        {
            obj.SetActive(false);
            poolQueue.Enqueue(obj);
        }
    }

    private void HandleScoreAndTimeGained(Vector2 spawnPosition, int scoreGained, float timeGained)
    {
        if (timeGained > 0f)
        {
            SpawnTimeText(spawnPosition, timeGained);
        }

        if (scoreGained > 0)
        {
            SpawnScoreText(spawnPosition, scoreGained);
        }
    }

    private void SpawnTimeText(Vector2 spawnPosition, float timeValue)
    {
        GameObject textObj = GetPooledObject();
        if (textObj == null) return;

        textObj.SetActive(true);
        textObj.transform.position = spawnPosition;
        textObj.transform.localScale = Vector3.one;
        textObj.transform.rotation = Quaternion.identity;

        TMP_Text tmp = textObj.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            // Set text value dynamically (no Update loop concatenation)
            tmp.text = $"+{timeValue:0}s";
            tmp.color = timeColor;

            // Reset alpha
            Color c = tmp.color;
            c.a = 1f;
            tmp.color = c;

            // Setup DOTween Animation Sequence
            Sequence timeSequence = DOTween.Sequence();
            
            // 1. Move straight vertical
            timeSequence.Append(textObj.transform.DOMoveY(spawnPosition.y + timeTravelDistance, timeDuration).SetEase(Ease.OutQuad));
            
            // 2. Punch scale on spawn
            timeSequence.Join(textObj.transform.DOPunchScale(Vector3.one * timePunchScaleAmount, 0.2f, 10, 1f));
            
            // 3. Fade out towards the end
            timeSequence.Join(tmp.DOFade(0f, timeDuration).SetEase(Ease.InQuad));

            timeSequence.OnComplete(() => ReturnToPool(textObj));
        }
        else
        {
            // Fallback movement
            textObj.transform.DOMoveY(spawnPosition.y + timeTravelDistance, timeDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => ReturnToPool(textObj));
        }
    }

    private void SpawnScoreText(Vector2 spawnPosition, int scoreValue)
    {
        GameObject textObj = GetPooledObject();
        if (textObj == null) return;

        Vector2 startPos = spawnPosition + scoreStartOffset;

        textObj.SetActive(true);
        textObj.transform.position = startPos;
        textObj.transform.localScale = Vector3.zero;
        
        // Random tilt angle between -15 and 15 degrees
        float randomTilt = Random.Range(-15f, 15f);
        textObj.transform.rotation = Quaternion.Euler(0f, 0f, randomTilt);

        TMP_Text tmp = textObj.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = $"+{scoreValue}";
            tmp.color = scoreColor;

            // Reset alpha
            Color c = tmp.color;
            c.a = 1f;
            tmp.color = c;

            // Define Bezier arc points (Catmull-Rom path)
            Vector3[] path = new Vector3[] {
                new Vector3(startPos.x, startPos.y, 0f),
                new Vector3(startPos.x + scoreArcDistance * 0.4f, startPos.y + scoreArcHeight, 0f),
                new Vector3(startPos.x + scoreArcDistance, startPos.y + scoreArcHeight * 0.5f, 0f)
            };

            Sequence scoreSequence = DOTween.Sequence();

            // 1. Arc trajectory
            scoreSequence.Append(textObj.transform.DOPath(path, scoreDuration, PathType.CatmullRom).SetEase(Ease.OutQuad));
            
            // 2. Pop scale using Ease.OutBack
            scoreSequence.Join(textObj.transform.DOScale(scoreTargetScale, 0.2f).SetEase(Ease.OutBack));
            
            // 3. Fade out
            scoreSequence.Join(tmp.DOFade(0f, scoreDuration).SetEase(Ease.InQuad));

            scoreSequence.OnComplete(() => ReturnToPool(textObj));
        }
        else
        {
            // Fallback movement
            textObj.transform.DOScale(scoreTargetScale, 0.2f).SetEase(Ease.OutBack)
                .OnComplete(() => {
                    textObj.transform.DOMove(new Vector3(startPos.x + scoreArcDistance, startPos.y + scoreArcHeight * 0.5f, 0f), scoreDuration - 0.2f)
                        .OnComplete(() => ReturnToPool(textObj));
                });
        }
    }
}
