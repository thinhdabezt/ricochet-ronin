using UnityEngine;

[CreateAssetMenu(fileName = "WeaponSO", menuName = "Scriptable Objects/WeaponSO")]
public class WeaponDataSO : ScriptableObject
{
    [Header("Visuals")]
    public string weaponName;
    public Color weaponColor;

    [Header("Stats")]
    public float powerMultiplier = 1f;
    public float maxDragDistance = 5f;
}
