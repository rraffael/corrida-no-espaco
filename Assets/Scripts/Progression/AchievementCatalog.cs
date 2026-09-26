using System;
using UnityEngine;

/// <summary>
/// A lista das conquistas, em <c>Assets/Resources/</c> — o mesmo arranjo dos
/// catálogos de fase e de nave.
/// </summary>
[CreateAssetMenu(fileName = "AchievementCatalog", menuName = "Corrida no Espaço/Catálogo de conquistas")]
public class AchievementCatalog : ScriptableObject
{
    public const string ResourcePath = "AchievementCatalog";

    [Tooltip("As conquistas, na ordem em que aparecem no menu.")]
    public Achievement[] achievements = Array.Empty<Achievement>();

    static AchievementCatalog cached;

    /// <summary>Sem catálogo não é erro: é só um jogo sem conquista ainda.</summary>
    public static AchievementCatalog Load()
    {
        if (cached == null)
            cached = Resources.Load<AchievementCatalog>(ResourcePath);

        return cached;
    }
}
