using UnityEngine;

/// <summary>
/// Com que nave a próxima corrida é jogada. O menu escolhe, a cena de jogo lê —
/// o mesmo papel que o <see cref="LevelSelection"/> faz para a fase.
///
/// **A escolha sobrevive a fechar o jogo**, ao contrário da fase. É a diferença
/// entre as duas: fase se escolhe a cada partida, nave é o que o jogador *é*.
/// Fazê-lo reescolher a nave toda vez que abrisse o app seria pedir que ele
/// repetisse uma decisão que já tomou.
/// </summary>
public static class ShipSelection
{
    const string PrefsKey = "cne.ship";

    static ShipDefinition chosen;
    static bool loaded;

    /// <summary>
    /// A nave escolhida. Sem escolha nenhuma — primeira vez, ou a nave salva
    /// saiu do catálogo — cai na primeira da lista, para o jogo nunca ficar sem
    /// nave por causa de uma preferência velha.
    /// </summary>
    public static ShipDefinition Ship
    {
        get
        {
            if (chosen != null)
                return chosen;

            var catalog = ShipCatalog.Load();
            if (catalog == null)
                return null;

            if (!loaded)
            {
                loaded = true;
                chosen = catalog.ByName(PlayerPrefs.GetString(PrefsKey, string.Empty));
            }

            return chosen ??= catalog.First();
        }
    }

    public static void Choose(ShipDefinition ship)
    {
        if (ship == null)
            return;

        chosen = ship;
        loaded = true;

        // Pelo nome do asset e não pelo displayName: o nome de tela é texto que
        // pode ser reescrito a qualquer momento, e a preferência do jogador não
        // pode se perder porque a "Nay" virou "Nayara".
        PlayerPrefs.SetString(PrefsKey, ship.name);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Esquece o que está em memória sem apagar o que está salvo. Existe para o
    /// Editor: com o recarregamento de domínio desligado, a nave escolhida numa
    /// rodada de teste sobreviveria à seguinte.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnLoad()
    {
        chosen = null;
        loaded = false;
    }
}
