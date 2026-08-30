using System;
using UnityEngine;

/// <summary>
/// Lista de todas as naves do jogo. Fica em <c>Assets/Resources/</c> para o menu
/// e a cena de jogo acharem sem referência de cena — o mesmo arranjo do
/// <see cref="LevelCatalog"/>, e pelo mesmo motivo: nave nova é um arquivo a mais
/// numa lista, não uma linha de código.
/// </summary>
[CreateAssetMenu(fileName = "ShipCatalog", menuName = "Corrida no Espaço/Catálogo de naves")]
public class ShipCatalog : ScriptableObject
{
    public const string ResourcePath = "ShipCatalog";

    [Tooltip("As naves, na ordem em que aparecem no menu.")]
    public ShipDefinition[] ships = Array.Empty<ShipDefinition>();

    static ShipCatalog cached;

    /// <summary>Carrega de Resources, uma vez por sessão.</summary>
    public static ShipCatalog Load()
    {
        if (cached == null)
            cached = Resources.Load<ShipCatalog>(ResourcePath);

        if (cached == null)
            Debug.LogError($"[Naves] Não achei o catálogo em Resources/{ResourcePath}.");

        return cached;
    }

    /// <summary>A primeira da lista, que é com a qual se joga sem ter escolhido nada.</summary>
    public ShipDefinition First()
    {
        foreach (var ship in ships)
        {
            if (ship != null)
                return ship;
        }

        return null;
    }

    public ShipDefinition ByName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        foreach (var ship in ships)
        {
            if (ship != null && ship.name == name)
                return ship;
        }

        return null;
    }
}
