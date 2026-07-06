using UnityEngine;
using System;

/// <summary>
/// Attached to generated wall segments to forward 2D collision events back to OrthographicArenaConfiner.
/// </summary>
public class WallCollisionDetector : MonoBehaviour
{
    public Action<Collision2D> onCollisionEnter;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        onCollisionEnter?.Invoke(collision);
    }
}
