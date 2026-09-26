using UnityEngine;

/// <summary>
/// O nível de cada nave, e o ato de evoluir. Um int por nave em PlayerPrefs,
/// pela chave do nome do asset — o mesmo motivo do <see cref="ShipSelection"/>:
/// o nome de tela pode mudar, e o progresso do jogador não pode se perder por
/// isso.
///
/// Nave que nunca evoluiu está no nível 1. Não há lista de naves salva: nave
/// nova no catálogo começa no 1 sem migração nenhuma.
/// </summary>
public static class ShipProgress
{
    const string KeyPrefix = "cne.ship-level.";

    public static int Level(ShipDefinition ship)
    {
        if (ship == null)
            return 1;

        int saved = PlayerPrefs.GetInt(KeyPrefix + ship.name, 1);
        return Mathf.Clamp(saved, 1, ShipEvolution.MaxLevel(ship));
    }

    public static bool IsMaxed(ShipDefinition ship) => Level(ship) >= ShipEvolution.MaxLevel(ship);

    /// <summary>Quanto custa o próximo nível. Zero no nível máximo.</summary>
    public static int NextCost(ShipDefinition ship) =>
        IsMaxed(ship) ? 0 : ShipEvolution.Model(ship).CostToReach(Level(ship) + 1);

    public static bool CanEvolve(ShipDefinition ship) =>
        ship != null && !IsMaxed(ship) && Wallet.Balance >= NextCost(ship);

    /// <summary>Sobe um nível, pagando. Devolve se subiu.</summary>
    public static bool TryEvolve(ShipDefinition ship)
    {
        if (!CanEvolve(ship) || !Wallet.TrySpend(NextCost(ship)))
            return false;

        SetLevel(ship, Level(ship) + 1);
        return true;
    }

    /// <summary>
    /// Põe a nave num nível sem cobrar. Existe para teste — os botões de
    /// desenvolvimento da aba de naves —, e nada do jogo de verdade chama.
    /// </summary>
    public static void SetLevel(ShipDefinition ship, int level)
    {
        if (ship == null)
            return;

        PlayerPrefs.SetInt(KeyPrefix + ship.name, Mathf.Clamp(level, 1, ShipEvolution.MaxLevel(ship)));
        PlayerPrefs.Save();
    }
}
