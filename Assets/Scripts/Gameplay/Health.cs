using System;
using UnityEngine;

/// <summary>
/// Vida de qualquer coisa que possa apanhar: a nave e os obstáculos usam o
/// mesmo componente. Quem causa dano não precisa saber no que está batendo.
/// </summary>
public class Health : MonoBehaviour
{
    [Tooltip("Vida cheia. Na nave, quem manda neste número é a ficha (ShipStats).")]
    [SerializeField, Min(1f)] float maxHealth = 100f;

    public float Max => maxHealth;
    public float Current { get; private set; }
    public bool IsAlive => Current > 0f;

    /// <summary>Fração de 0 a 1, para barra de HUD.</summary>
    public float Fraction => maxHealth > 0f ? Mathf.Clamp01(Current / maxHealth) : 0f;

    /// <summary>Disparado a cada mudança, com a vida nova.</summary>
    public event Action<float> Changed;

    /// <summary>Disparado uma única vez, quando a vida chega a zero.</summary>
    public event Action Died;

    bool announcedDeath;

    void Awake()
    {
        Current = maxHealth;
    }

    /// <summary>
    /// Define a vida cheia de fora. É como a ficha da nave impõe o número dela
    /// sem que este componente precise conhecer ficha nenhuma.
    /// </summary>
    public void Configure(float max, bool refill = true)
    {
        maxHealth = Mathf.Max(1f, max);

        if (refill)
            Current = maxHealth;
        else
            Current = Mathf.Min(Current, maxHealth);

        announcedDeath = false;
        Changed?.Invoke(Current);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || !IsAlive)
            return;

        Current = Mathf.Max(0f, Current - amount);
        Changed?.Invoke(Current);

        if (IsAlive || announcedDeath)
            return;

        // Uma morte só: dois tiros no mesmo frame não podem disparar dois
        // "fim de jogo".
        announcedDeath = true;
        Died?.Invoke();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || !IsAlive)
            return;

        Current = Mathf.Min(maxHealth, Current + amount);
        Changed?.Invoke(Current);
    }
}
