using UnityEngine;

/// <summary>
/// Quem está ocupando qual faixa, agora, na leva que ainda vem chegando. É o que
/// sustenta a **regra da fuga garantida**: nenhuma linha pode fechar todas as
/// faixas ao mesmo tempo — sempre sobra uma **completamente vazia**.
///
/// **Por que virou um lugar só, em 30/08/2026.** A regra morava dentro do
/// <see cref="ObstacleSpawner"/> e só conhecia obstáculo. Com modificadores
/// nascendo na pista, dois sorteadores passaram a povoar as mesmas faixas sem
/// saber um do outro, e cada um garantindo a fuga sozinho garantia coisa
/// nenhuma: obstáculo na 0 e modificador na 1 e na 2 é uma linha que os dois
/// aprovariam e que não deixa por onde passar.
///
/// **A faixa de fuga é vazia de tudo, e não só de perigo** *(decisão do Raffael,
/// 30/08/2026)*. Um Reforço na faixa livre a tornaria menos livre para quem só
/// quer passar, e a leitura de relance — a única que dá tempo de fazer — deixaria
/// de ser "onde não tem nada".
/// </summary>
public static class LaneOccupancy
{
    /// <summary>
    /// Marca em <paramref name="blocked"/> as faixas tomadas pela leva que ainda
    /// está chegando, e devolve quantas são.
    ///
    /// <paramref name="waveWindow"/> é a distância vertical dentro da qual duas
    /// coisas contam como a mesma leva. O que já passou dela não atrapalha a fuga
    /// de quem nasce agora.
    /// </summary>
    public static int Fill(bool[] blocked, float spawnY, float waveWindow)
    {
        for (int i = 0; i < blocked.Length; i++)
            blocked[i] = false;

        int count = 0;

        foreach (var obstacle in Obstacle.Active)
        {
            if (obstacle == null || spawnY - obstacle.transform.position.y > waveWindow)
                continue;

            for (int i = 0; i < blocked.Length; i++)
            {
                if (blocked[i] || !obstacle.Occupies(i))
                    continue;

                blocked[i] = true;
                count++;
            }
        }

        foreach (var pickup in LevelModifierPickup.Active)
        {
            if (pickup == null || spawnY - pickup.transform.position.y > waveWindow)
                continue;

            int lane = pickup.Lane;
            if (lane < 0 || lane >= blocked.Length || blocked[lane])
                continue;

            blocked[lane] = true;
            count++;
        }

        return count;
    }

    /// <summary>
    /// Sorteia uma faixa livre que **ainda deixe pelo menos uma vazia** depois de
    /// a coisa nascer. Devolve falso quando não existe posição assim — e aí quem
    /// chamou pula a vez, que é como o corredor respira.
    /// </summary>
    public static bool TryPickLane(int laneCount, int laneSpan, float spawnY, float waveWindow,
                                   out int lane)
    {
        lane = 0;

        if (laneSpan > laneCount || laneCount <= 0)
            return false;

        var blocked = new bool[laneCount];
        int blockedCount = Fill(blocked, spawnY, waveWindow);

        int chosen = -1;
        int seen = 0;

        for (int start = 0; start + laneSpan <= laneCount; start++)
        {
            bool free = true;
            for (int i = start; i < start + laneSpan && free; i++)
                free = !blocked[i];

            // Ocupar isto ainda tem de deixar uma faixa vazia sobrando.
            if (!free || blockedCount + laneSpan >= laneCount)
                continue;

            // Sorteio em passada única (amostragem de reservatório): evita alocar
            // uma lista de candidatos a cada nascimento, e nascimento acontece
            // várias vezes por segundo no fim de uma fase apertada.
            seen++;
            if (Random.Range(0, seen) == 0)
                chosen = start;
        }

        if (chosen < 0)
            return false;

        lane = chosen;
        return true;
    }
}
