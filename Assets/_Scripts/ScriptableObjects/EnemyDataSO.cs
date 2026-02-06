using UnityEngine;

// Dòng này tạo file SO trực tiếp từ menu chuột phải trong Unity
[CreateAssetMenu(fileName = "EnemyDataSO", menuName = "Scriptable Objects/EnemyDataSO")]
public class EnemyDataSO : ScriptableObject
{
    public string enemyName;
    public int maxHealth;
    public Color enemyColor;
    public int scoreValue;
}
