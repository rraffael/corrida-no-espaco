using UnityEngine;

/// <summary>
/// A moeda do jogo. **Provisória de ponta a ponta** *(pedido do Raffael,
/// 26/09/2026)*: o nome é de mentira, o saldo começa em zero e não há jeito de
/// ganhar. Existe para a evolução das naves ter o que cobrar — com o custo em
/// zero, dá para testar todas as evoluções agora. Como se ganha, quanto custa
/// e como se chama é a Parte 8.
/// </summary>
public static class Wallet
{
    /// <summary>Nome temporário. Trocar aqui troca em todas as telas.</summary>
    public const string CurrencyName = "Sucata";

    const string BalanceKey = "cne.wallet";

    public static int Balance => PlayerPrefs.GetInt(BalanceKey, 0);

    public static bool TrySpend(int amount)
    {
        if (amount < 0 || Balance < amount)
            return false;

        if (amount == 0)
            return true;

        PlayerPrefs.SetInt(BalanceKey, Balance - amount);
        PlayerPrefs.Save();
        return true;
    }

    public static void Add(int amount)
    {
        if (amount <= 0)
            return;

        PlayerPrefs.SetInt(BalanceKey, Balance + amount);
        PlayerPrefs.Save();
    }
}
