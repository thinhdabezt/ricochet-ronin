using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class UIComboSpawner : MonoBehaviour
{
    [Header("Pool Setup")]
    [SerializeField] private GameObject comboPrefab;
    [SerializeField] private int poolSize = 10;
    
    [Header("Color Configurations")]
    [SerializeField] private Color normalComboColor = Color.yellow;
    [SerializeField] private Color specialComboColor = new Color(1f, 0.5f, 0f); // Vibrant orange
    [SerializeField] private Color megaComboColor = Color.red;

    private Queue<GameObject> poolQueue = new Queue<GameObject>();

    private void Awake()
    {
        InitializePool();
    }

    private void OnEnable()
    {
        GameEvents.OnEnemyKilled += SpawnComboText;
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyKilled -= SpawnComboText;
    }

    private void InitializePool()
    {
        if (comboPrefab == null)
        {
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(comboPrefab, transform);
            obj.SetActive(false);
            poolQueue.Enqueue(obj);
        }
    }

    private GameObject GetPooledObject()
    {
        if (poolQueue.Count == 0)
        {
            if (comboPrefab != null)
            {
                GameObject obj = Instantiate(comboPrefab, transform);
                obj.SetActive(false);
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

    private void SpawnComboText(Vector2 killPosition, int comboCount)
    {
        GameObject textObj = GetPooledObject();
        if (textObj == null) return;

        textObj.SetActive(true);
        textObj.transform.position = killPosition;
        textObj.transform.localScale = Vector3.zero;

        // Apply a random tilt angle between -15 and 15 degrees
        float randomTilt = Random.Range(-15f, 15f);
        textObj.transform.rotation = Quaternion.Euler(0f, 0f, randomTilt);

        // Get the TMP component (supporting both standard TextMeshPro and TextMeshProUGUI)
        TMP_Text textComp = textObj.GetComponentInChildren<TMP_Text>();
        if (textComp != null)
        {
            string comboString;
            Color comboColor;

            if (comboCount < 2)
            {
                comboString = $"+{comboCount}X";
                comboColor = normalComboColor;
            }
            else if (comboCount == 2)
            {
                comboString = "DOUBLE KILL";
                comboColor = specialComboColor;
            }
            else if (comboCount == 3)
            {
                comboString = "TRIPLE KILL";
                comboColor = specialComboColor;
            }
            else
            {
                comboString = $"MEGA KILL +{comboCount}X";
                comboColor = megaComboColor;
            }

            textComp.text = comboString;
            textComp.color = comboColor;
            
            // Explicitly reset alpha for pooling
            Color resetColor = textComp.color;
            resetColor.a = 1f;
            textComp.color = resetColor;

            // Animate using DOTween sequence
            Sequence comboSequence = DOTween.Sequence();
            
            // 1. Scale from 0 to 1.4f using Ease.OutBack over 0.15s
            comboSequence.Append(textObj.transform.DOScale(1.4f, 0.15f).SetEase(Ease.OutBack));
            
            // 2. Hold for 0.2s
            comboSequence.AppendInterval(0.2f);
            
            // 3. Move up by 1.5 units and fade out over 0.3s
            float endY = textObj.transform.position.y + 1.5f;
            comboSequence.Append(textObj.transform.DOMoveY(endY, 0.3f).SetEase(Ease.OutQuad));
            comboSequence.Join(textComp.DOFade(0f, 0.3f).SetEase(Ease.OutQuad));
            
            // 4. Return to pool/deactivate immediately after completion
            comboSequence.OnComplete(() => ReturnToPool(textObj));
        }
        else
        {
            // Fallback animation if no text component is found
            textObj.transform.DOScale(1.4f, 0.15f).SetEase(Ease.OutBack)
                .OnComplete(() => {
                    textObj.transform.DOMoveY(textObj.transform.position.y + 1.5f, 0.3f)
                        .SetDelay(0.2f)
                        .OnComplete(() => ReturnToPool(textObj));
                });
        }
    }
}
