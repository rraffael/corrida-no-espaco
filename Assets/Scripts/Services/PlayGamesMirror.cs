using System;

/// <summary>
/// O **espelho das conquistas no Google Play** *(desenho de 26/09/2026)*: Corrida
/// no Espaço → conquista do jogo com recompensa → conquista do Play. O jogo é o
/// dono da verdade; o Play só registra o que o jogo já decidiu.
///
/// **Hoje é um gancho vazio, e é de propósito.** O plugin do Play Games entra
/// na Fase 7. Até lá <see cref="Unlock"/> fica nulo, as conquistas completas se
/// acumulam na fila (<see cref="AchievementEntry.playSynced"/> falso no
/// salvamento), e nada se perde: no dia em que o plugin ligar o gancho e
/// chamar <see cref="Achievements.SyncPlayGames"/> ao logar, a fila inteira sobe.
///
/// **Dispara ao completar, não ao resgatar** *(decidido pelo Raffael)*: a
/// conquista do Play é o registro de que a pessoa fez aquilo. O resgate é só a
/// moeda — quem nunca abre o painel não pode ficar sem a do Play.
/// </summary>
public static class PlayGamesMirror
{
    /// <summary>
    /// Desbloqueia uma conquista no Play pelo id dela, e responde se deu certo.
    /// **Quem preenche é a integração da Fase 7** — algo como
    /// <c>PlayGamesMirror.Unlock = (id, done) => Social.ReportProgress(id, 100, done);</c>
    /// — e depois chama <see cref="Achievements.SyncPlayGames"/> ao logar.
    /// </summary>
    public static Action<string, Action<bool>> Unlock;

    /// <summary>Tem para onde mandar? Falso até a Fase 7, e falso sem login.</summary>
    public static bool Available => Unlock != null;
}
