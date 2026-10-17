using System.Collections.Generic;

/// <summary>
/// Quais posições do placar aparecem na tela, e onde entram as reticências
/// *(desenho do Raffael, 17/10/2026)*:
///
/// - **O topo inteiro** — as <see cref="TopRows"/> primeiras posições.
/// - **Reticências**, se houver gente entre o topo e o jogador.
/// - **O jogador com dois acima e dois abaixo** — onde ele está, comparado a
///   quem está logo ali.
/// - **Reticências de novo**, se houver gente depois — o "tem muito mais atrás".
///
/// Jogador no 10.024º de 20.000: 1 a 100, …, 10.022 a 10.026, …
///
/// É conta pura, sem placar de verdade: só posições. Fica separada da tela para
/// a verificação automática conferir os cantos sem montar UI nenhuma.
/// </summary>
public static class LeaderboardLayout
{
    /// <summary>Quantas posições do topo aparecem sempre.</summary>
    public const int TopRows = 100;

    /// <summary>Quantas posições acima e abaixo do jogador aparecem.</summary>
    public const int Neighbors = 2;

    /// <summary>Marca as reticências na lista que <see cref="Slots"/> devolve.</summary>
    public const int Gap = 0;

    /// <summary>
    /// As posições a mostrar, em ordem, com <see cref="Gap"/> onde vão as
    /// reticências. <paramref name="playerRank"/> 0 = o jogador não está no placar.
    /// </summary>
    public static List<int> Slots(int total, int playerRank, int top = TopRows, int neighbors = Neighbors)
    {
        var slots = new List<int>();
        if (total <= 0)
            return slots;

        int lastTop = total < top ? total : top;
        int last = lastTop;

        if (playerRank > 0 && playerRank <= total)
        {
            int from = playerRank - neighbors;
            int to = playerRank + neighbors > total ? total : playerRank + neighbors;

            // Reticências que esconderiam uma linha só mostram menos do que
            // a própria linha: aí o topo emenda direto no jogador.
            if (from > lastTop + 2)
            {
                AddRange(slots, 1, lastTop);
                slots.Add(Gap);
                AddRange(slots, from, to);
                last = to;
            }
            else
            {
                last = to > lastTop ? to : lastTop;
                AddRange(slots, 1, last);
            }
        }
        else
        {
            AddRange(slots, 1, lastTop);
        }

        if (last < total)
            slots.Add(Gap);

        return slots;
    }

    static void AddRange(List<int> slots, int from, int to)
    {
        for (int rank = from; rank <= to; rank++)
            slots.Add(rank);
    }
}
