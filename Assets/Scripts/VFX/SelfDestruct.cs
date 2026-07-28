using UnityEngine;

/// <summary>
/// Destroys the object it is attached to after a fixed lifetime.
/// Used by one-shot visual effects, which have no other reason to stay alive.
/// </summary>
public class SelfDestruct : MonoBehaviour
{
    [Tooltip("Seconds until the object destroys itself")]
    public float lifetime = 3f;

    private void OnEnable()
    {
        Destroy(gameObject, lifetime);
    }
}
