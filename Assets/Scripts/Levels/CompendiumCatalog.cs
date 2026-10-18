using System;
using UnityEngine;

/// <summary>
/// O que o Compêndio mostra além das naves: **toda** ficha de obstáculo e de
/// modificador de fase que existe, e não só as que estão em alguma fase. Fica em
/// <c>Assets/Resources/</c>, e quem preenche é o Montar, lendo as pastas — a
/// mesma arquitetura do catálogo de conquistas. As naves vêm do
/// <see cref="ShipCatalog"/>, que já é a lista do que existe.
///
/// Existe porque o Compêndio é a vitrine de "tudo o que há no jogo": uma ficha
/// fora de fase (o Rastro antes de 18/10, por exemplo) também tem de aparecer.
/// </summary>
[CreateAssetMenu(fileName = "CompendiumCatalog", menuName = "Corrida no Espaço/Catálogo do compêndio")]
public class CompendiumCatalog : ScriptableObject
{
    public const string ResourcePath = "CompendiumCatalog";

    [Tooltip("Obstáculos e inimigos, na ordem em que aparecem no Compêndio.")]
    public ObstacleStats[] obstacles = Array.Empty<ObstacleStats>();

    [Tooltip("Modificadores de fase, na ordem em que aparecem no Compêndio.")]
    public LevelModifier[] modifiers = Array.Empty<LevelModifier>();

    [Tooltip("Imagem de quem ainda não tem arte — as naves, hoje. Vai tingida com a cor da coisa.")]
    public Sprite placeholder;

    static CompendiumCatalog cached;

    /// <summary>Carrega de Resources, uma vez por sessão.</summary>
    public static CompendiumCatalog Load()
    {
        if (cached == null)
            cached = Resources.Load<CompendiumCatalog>(ResourcePath);

        if (cached == null)
            Debug.LogError($"[Compêndio] Não achei o catálogo em Resources/{ResourcePath}. Rode o Montar.");

        return cached;
    }
}
