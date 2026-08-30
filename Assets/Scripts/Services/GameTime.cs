using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quem manda no <see cref="Time.timeScale"/>. **Ninguém mais escreve nele.**
///
/// Existe desde 30/08/2026, quando entrou o modificador de tempo lento. Até
/// então o timeScale tinha dois donos que nunca se encontravam — a pausa punha
/// 0, o fim de corrida punha 0, e os dois voltavam a 1 —, e isso bastava. Com um
/// terceiro interessado que quer 0,5, "voltar a 1" deixou de ser verdade: sair
/// da pausa no meio de um tempo lento cancelaria o efeito, e o jogador veria o
/// poder dele evaporar por ter aberto o menu.
///
/// **A pausa ganha de tudo.** Ela é 0 absoluto, e não um fator: enquanto estiver
/// pausado o jogo está parado, tenha quantos efeitos tiver.
///
/// **Entre lentidões, a mais lenta ganha** — não se multiplicam nem se somam.
/// Multiplicar faz duas de 50% virarem 25%, e uma terceira quase congelaria a
/// tela; somar como o <see cref="StatModifier"/> faz duas de -50% chegarem a
/// zero, que é o jogo travado. Nos dois casos o terceiro efeito quebra o jogo,
/// e é o tipo de coisa que só aparece muito depois, quando já houver dez
/// modificadores. Com o mínimo, empilhar nunca piora — o teto do estrago é o
/// efeito mais forte que existir.
/// </summary>
public static class GameTime
{
    /// <summary>
    /// Uma lentidão ligada. Quem pediu guarda isto e devolve para desligar —
    /// é classe e não índice de propósito: índice em lista que encolhe é a
    /// receita de um efeito desligar o de outro.
    /// </summary>
    public sealed class Slow
    {
        internal float Factor;
        internal Slow(float factor) => Factor = factor;
    }

    static readonly List<Slow> slows = new List<Slow>();

    /// <summary>O jogo está pausado (menu aberto, ou corrida encerrada).</summary>
    public static bool IsPaused { get; private set; }

    /// <summary>
    /// Quanto o tempo está andando, ignorando a pausa. 1 é normal, 0,5 é metade.
    /// Para o HUD dizer que o tempo está lento sem ter de saber quem o deixou
    /// assim.
    /// </summary>
    public static float SlowFactor { get; private set; } = 1f;

    /// <summary>Alguma coisa está segurando o tempo agora.</summary>
    public static bool IsSlowed => SlowFactor < 1f;

    /// <summary>
    /// Segura o tempo em <paramref name="factor"/> do normal até alguém devolver
    /// isto para <see cref="Release"/>. 0,5 é meia velocidade.
    /// </summary>
    public static Slow Hold(float factor)
    {
        var slow = new Slow(Mathf.Clamp(factor, 0.05f, 1f));
        slows.Add(slow);
        Apply();
        return slow;
    }

    /// <summary>Solta uma lentidão. Passar <c>null</c> ou o que já saiu não faz nada.</summary>
    public static void Release(Slow slow)
    {
        if (slow != null && slows.Remove(slow))
            Apply();
    }

    public static void SetPaused(bool paused)
    {
        IsPaused = paused;
        Apply();
    }

    /// <summary>
    /// Volta tudo ao normal. Chamado ao sair de uma cena: lentidão e pausa são
    /// da partida, e a cena seguinte não pode nascer congelada nem arrastada.
    /// </summary>
    public static void ResetAll()
    {
        slows.Clear();
        IsPaused = false;
        Apply();
    }

    static void Apply()
    {
        float slowest = 1f;
        for (int i = 0; i < slows.Count; i++)
            slowest = Mathf.Min(slowest, slows[i].Factor);

        SlowFactor = slowest;
        Time.timeScale = IsPaused ? 0f : slowest;
    }

    /// <summary>
    /// Zera o estado antes de a primeira cena carregar. Com o recarregamento de
    /// domínio desligado — que é o padrão para entrar em Play depressa — uma
    /// classe estática guarda o que sobrou da sessão anterior, e o jogo abriria
    /// pausado ou lento por causa da rodada de teste anterior.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnLoad()
    {
        slows.Clear();
        IsPaused = false;
        SlowFactor = 1f;
    }
}
