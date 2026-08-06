using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// O botão único de montagem. O Raffael roda **só este**, sempre; quem decide o
/// que ele faz é o Claude, mexendo na lista <see cref="Steps"/> a cada mudança
/// de código.
///
/// Existe porque a alternativa era o Raffael ter de lembrar, a cada pedido, qual
/// das montagens aquela mudança exigia — e em que ordem. Errar a ordem custa
/// caro: montar o combate antes da corrida acusa "não achei o objeto Race", e
/// montar a seleção de fase antes do menu deixa o painel sem onde se pendurar.
///
/// **Por que esta ordem:** primeiro o que só cria asset e não abre cena, depois
/// tudo que mexe na Game.unity, depois tudo que mexe na Menu.unity. Assim a cena
/// aberta troca uma vez só, em vez de a cada passo.
/// </summary>
static class Montar
{
    readonly struct Step
    {
        public readonly string Title;
        public readonly Action Run;

        public Step(string title, Action run)
        {
            Title = title;
            Run = run;
        }
    }

    /// <summary>
    /// O que a montagem faz hoje. **Esta lista é reescrita a cada pedido**: entra
    /// o que a mudança daquele dia exige remontar, sai o que já não precisa.
    /// Lista vazia é resposta válida — quer dizer que a mudança foi só de código
    /// e não encosta em cena nem em asset.
    ///
    /// Estado em 06/08/2026: o Bloco B inteiro está por rodar, e ele mexeu nas
    /// fichas de obstáculo, nos painéis de fim, nos textos do menu e criou a tela
    /// de seleção de fase. Por isso, hoje, tudo entra.
    /// </summary>
    static readonly Step[] Steps =
    {
        new Step("Fases e fichas de obstáculo (assets)", LevelSetup.Setup),
        new Step("Corrida — fundo e HUD (Game.unity)", RaceSetup.Setup),
        new Step("Combate — nave, obstáculos e os três painéis de fim (Game.unity)", BattleSetup.Setup),
        new Step("Menu — recordes e transição (Menu.unity)", MenuSetup.Setup),
        new Step("Seleção de fase (Menu.unity)", LevelSelectSetup.Setup),
    };

    [MenuItem(ProjectTools.SetupAllItem, false, 90)]
    static void Run()
    {
        if (Steps.Length == 0)
        {
            Debug.Log("[Montar] Nada a montar desta vez — a última mudança foi só de código, " +
                      "e não encosta em cena nem em asset.");
            return;
        }

        for (int i = 0; i < Steps.Length; i++)
        {
            var step = Steps[i];
            Debug.Log($"[Montar] {i + 1}/{Steps.Length} — {step.Title}");

            try
            {
                step.Run();
            }
            catch (Exception e)
            {
                // Parar na primeira falha em vez de seguir: cena montada pela
                // metade é muito pior de diagnosticar do que uma que nem começou.
                Debug.LogError($"[Montar] Parou no passo \"{step.Title}\". " +
                               "Os passos anteriores foram salvos; os seguintes não rodaram.\n" + e);
                return;
            }
        }

        Debug.Log($"[Montar] {Steps.Length} passo(s) concluído(s), cenas salvas. " +
                  "Se algum passo tiver reclamado acima, é ele que manda — este aviso só diz " +
                  "que a sequência chegou ao fim.");
    }
}
