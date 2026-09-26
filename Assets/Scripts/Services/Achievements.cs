using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O progresso das conquistas: quanto falta, quais completaram, quais já foram
/// resgatadas. Lê os números de <see cref="CareerStats"/> e guarda o estado de
/// cada uma no salvamento, pelo nome do asset.
///
/// **Completar e resgatar são dois passos** *(26/09/2026)*: a conquista completa
/// sozinha no fim da corrida, e a moeda só entra quando o jogador toca em
/// "Resgatar" no menu. É o que dá ao jogador o momento de receber — e é como a
/// maioria dos jogos faz, então ele já sabe usar.
/// </summary>
public static class Achievements
{
    public static IReadOnlyList<Achievement> All
    {
        get
        {
            var catalog = AchievementCatalog.Load();
            return catalog != null && catalog.achievements != null
                ? catalog.achievements
                : System.Array.Empty<Achievement>();
        }
    }

    /// <summary>
    /// O valor de agora do contador da conquista, na unidade da meta (distância
    /// em "un"). Conquista presa a uma nave lê só a carreira daquela nave.
    /// </summary>
    public static float Value(Achievement achievement)
    {
        if (achievement == null)
            return 0f;

        if (achievement.ship == null)
            return Value(achievement.counter);

        var entry = CareerStats.For(achievement.ship);
        if (entry == null)
            return 0f;

        switch (achievement.counter)
        {
            case CareerCounter.Races: return entry.races;
            case CareerCounter.Wins: return entry.wins;
            case CareerCounter.Kills: return entry.kills;
            case CareerCounter.Distance: return entry.distance * RaceSpeed.DisplayScale;
            case CareerCounter.ShipLevelUps: return entry.levelUps;
            default: return Value(achievement.counter);
        }
    }

    /// <summary>O valor de agora do contador, somando todas as naves.</summary>
    public static float Value(CareerCounter counter)
    {
        var career = CareerStats.Data;

        switch (counter)
        {
            case CareerCounter.Races: return career.races;
            case CareerCounter.Wins: return career.wins;
            case CareerCounter.EndlessRuns: return career.endlessRuns;
            case CareerCounter.Kills: return career.kills;
            case CareerCounter.Distance: return career.distance * RaceSpeed.DisplayScale;
            case CareerCounter.BestEndlessDistance: return career.bestEndlessDistance * RaceSpeed.DisplayScale;
            case CareerCounter.ShipLevelUps: return career.shipLevelUps;
            default: return 0f;
        }
    }

    /// <summary>De 0 a 1. Completa fica em 1 mesmo que o contador volte a zero num salvamento apagado.</summary>
    public static float Progress(Achievement achievement)
    {
        if (achievement == null)
            return 0f;

        if (IsCompleted(achievement))
            return 1f;

        return Mathf.Clamp01(Value(achievement) / Mathf.Max(1f, achievement.target));
    }

    public static bool IsCompleted(Achievement achievement)
    {
        var entry = Find(achievement, create: false);
        return entry != null && entry.completed;
    }

    public static bool IsClaimed(Achievement achievement)
    {
        var entry = Find(achievement, create: false);
        return entry != null && entry.claimed;
    }

    /// <summary>
    /// O jogador pode ver nome, descrição e progresso? Completa sempre pode. Antes
    /// disso: visível sim; oculta só com o requisito completo; secreta não.
    /// Escondida, a tela mostra "????????" — e a recompensa, sempre.
    /// </summary>
    public static bool IsRevealed(Achievement achievement)
    {
        if (achievement == null)
            return false;

        if (IsCompleted(achievement))
            return true;

        switch (achievement.visibility)
        {
            case Achievement.Visibility.Visivel:
                return true;
            case Achievement.Visibility.OcultaAteRequisito:
                // Sem requisito, se comporta como secreta: é o modo de falhar que
                // não entrega a surpresa por engano.
                return achievement.requirement != null && IsCompleted(achievement.requirement);
            default:
                return false;
        }
    }

    // ── Espelho no Google Play ──────────────────────────────────────────

    /// <summary>
    /// Manda ao Play toda conquista completa que ainda não foi — a fila. Roda no
    /// fim de cada corrida, e a integração da Fase 7 chama também ao logar.
    /// Sem o gancho ligado não faz nada, e a fila espera.
    /// </summary>
    public static void SyncPlayGames()
    {
        if (!PlayGamesMirror.Available)
            return;

        foreach (var achievement in All)
        {
            if (achievement == null || string.IsNullOrEmpty(achievement.playGamesId))
                continue;

            var entry = Find(achievement, create: false);
            if (entry == null || !entry.completed || entry.playSynced)
                continue;

            PlayGamesMirror.Unlock(achievement.playGamesId, ok =>
            {
                // Só sai da fila quando o Play confirma. Falhou — sem rede, login
                // caído —, fica para a próxima vez.
                if (!ok)
                    return;

                entry.playSynced = true;
                SaveGame.Save();
            });
        }
    }

    /// <summary>
    /// Marca como completas as que chegaram à meta. Devolve as que completaram
    /// agora — para quem quiser anunciar. Não grava: quem chama grava junto.
    /// </summary>
    public static List<Achievement> CheckAll()
    {
        var completedNow = new List<Achievement>();

        foreach (var achievement in All)
        {
            if (achievement == null || IsCompleted(achievement))
                continue;

            if (Value(achievement) < achievement.target)
                continue;

            Find(achievement, create: true).completed = true;
            completedNow.Add(achievement);
            Announcements.Enqueue(achievement);
        }

        return completedNow;
    }

    /// <summary>Paga a recompensa de uma conquista completa. Devolve se pagou.</summary>
    public static bool Claim(Achievement achievement)
    {
        if (achievement == null || !IsCompleted(achievement) || IsClaimed(achievement))
            return false;

        Find(achievement, create: true).claimed = true;

        // O Wallet grava o arquivo, e o "resgatado" vai junto na mesma gravação.
        if (achievement.reward > 0)
            Wallet.Add(achievement.reward);
        else
            SaveGame.Save();

        return true;
    }

    static AchievementEntry Find(Achievement achievement, bool create)
    {
        if (achievement == null)
            return null;

        var entries = SaveGame.Data.achievements;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].id == achievement.name)
                return entries[i];
        }

        if (!create)
            return null;

        var entry = new AchievementEntry { id = achievement.name };
        entries.Add(entry);
        return entry;
    }

    /// <summary>
    /// As que completaram e ainda não foram anunciadas na tela, em ordem. O
    /// <see cref="AchievementToast"/> do menu tira daqui uma de cada vez.
    ///
    /// **Fila, e não evento:** a conquista pode completar no fim de uma corrida,
    /// na cena de jogo, onde não há cartão; ela espera aqui e aparece quando o
    /// jogador volta ao menu. Fica só em memória — fechar o jogo antes de ver
    /// não perde nada, porque ela continua completa esperando o resgate.
    /// </summary>
    public static readonly Queue<Achievement> Announcements = new Queue<Achievement>();

    /// <summary>
    /// Confere as conquistas no fim de cada corrida e a cada número de carreira
    /// que mude fora dela (evoluir uma nave), antes de gravar — o que completar
    /// vai no mesmo arquivo. Pendurado ao carregar o jogo, e não num componente
    /// de cena: vale em qualquer cena, sem montagem.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        Announcements.Clear();
        CareerStats.RaceFinished += _ => Check();
        CareerStats.Updated += Check;
    }

    static void Check()
    {
        CheckAll();
        SyncPlayGames();
    }
}
