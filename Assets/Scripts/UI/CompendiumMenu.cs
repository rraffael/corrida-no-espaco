using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// O painel do Compêndio no menu *(pedido do Raffael, 18/10/2026)*: três abas —
/// Naves, Modificadores de Fase, Obstáculos e Inimigos — e um cartão por coisa.
/// Serve ao jogador curioso e a quem está testando: tudo o que existe no jogo
/// se acha aqui, com os números de verdade.
///
/// Os cartões são clonados de um modelo desligado e reaproveitados entre as
/// abas, como nas outras listas do menu. O conteúdo vem do
/// <see cref="Compendium"/>, que lê as fichas — coisa nova no jogo aparece aqui
/// sozinha depois do Montar.
/// </summary>
public class CompendiumMenu : MonoBehaviour
{
    [SerializeField] ScrollRect scroll;
    [SerializeField] RectTransform cardsRoot;
    [SerializeField] CompendiumCard cardTemplate;
    [SerializeField] TMP_Text emptyLabel;

    [Tooltip("Os botões das abas, na ordem de Compendium.Section.")]
    [SerializeField] Button[] tabs;

    [SerializeField] Color selectedTabColor = new Color(0.16f, 0.42f, 0.75f, 0.95f);
    [SerializeField] Color idleTabColor = new Color(0.20f, 0.24f, 0.36f, 0.95f);

    readonly List<CompendiumCard> cards = new List<CompendiumCard>();
    Compendium.Section current = Compendium.Section.Ships;

    void OnEnable() => Show(current);

    /// <summary>Aba "Naves".</summary>
    public void OnShipsTab() => Show(Compendium.Section.Ships);

    /// <summary>Aba "Modificadores de Fase".</summary>
    public void OnModifiersTab() => Show(Compendium.Section.Modifiers);

    /// <summary>Aba "Obstáculos e Inimigos".</summary>
    public void OnObstaclesTab() => Show(Compendium.Section.Obstacles);

    void Show(Compendium.Section section)
    {
        current = section;

        if (cardTemplate == null || cardsRoot == null)
        {
            Debug.LogError("[Compêndio] Falta o cartão modelo ou o contêiner dos cartões.", this);
            return;
        }

        cardTemplate.gameObject.SetActive(false);

        var entries = Compendium.Entries(section);
        for (int i = 0; i < entries.Count; i++)
        {
            if (i == cards.Count)
                cards.Add(Instantiate(cardTemplate, cardsRoot, false));

            var card = cards[i];
            card.name = "Cartao " + entries[i].Title;
            card.gameObject.SetActive(true);
            card.Bind(entries[i]);
        }

        for (int i = entries.Count; i < cards.Count; i++)
            cards[i].gameObject.SetActive(false);

        if (emptyLabel != null)
            emptyLabel.gameObject.SetActive(entries.Count == 0);

        PaintTabs();

        // O grupo de layout só mede no fim do quadro; forçar aqui é o que deixa
        // a rolagem voltar ao topo já sabendo a altura nova da lista.
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardsRoot);
        if (scroll != null)
        {
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
        }
    }

    void PaintTabs()
    {
        if (tabs == null)
            return;

        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] != null && tabs[i].image != null)
                tabs[i].image.color = i == (int)current ? selectedTabColor : idleTabColor;
        }
    }
}
