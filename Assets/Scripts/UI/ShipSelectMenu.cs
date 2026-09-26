using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A aba de naves do menu: todas as naves do catálogo, com os atributos e os
/// poderes de cada uma, e a escolha de com qual jogar.
///
/// A lista sai do <see cref="ShipCatalog"/> em runtime, e não da cena: nave nova
/// no catálogo aparece aqui sozinha. Esta classe só desenha — quem guarda a
/// escolha é o <see cref="ShipSelection"/>.
///
/// **Todas destravadas, e é assim de propósito por enquanto** *(30/08/2026)*:
/// não existe economia, então não há de que destravar. O cadeado desta tela é o
/// dia da loja.
///
/// **É também a oficina** *(26/09/2026)*: cada linha mostra o nível da nave e
/// tem o botão de evoluir, e o alto da aba mostra o saldo da moeda provisória.
/// </summary>
public class ShipSelectMenu : MonoBehaviour
{
    [Tooltip("Onde as linhas nascem. Tem o VerticalLayoutGroup que as empilha.")]
    [SerializeField] RectTransform rowsRoot;

    [Tooltip("Linha modelo, desligada na cena. É clonada uma vez por nave.")]
    [SerializeField] ShipSelectRow rowTemplate;

    [Tooltip("O saldo da moeda do jogo, no alto da aba.")]
    [SerializeField] TMPro.TMP_Text walletLabel;

    [Tooltip("Botão de teste que dá moeda. Só aparece no Editor e em build de desenvolvimento: " +
             "serve para testar custo de evolução enquanto não há jeito de ganhar.")]
    [SerializeField] UnityEngine.UI.Button devGiveButton;

    const int DevGiveAmount = 1000;

    readonly List<ShipSelectRow> rows = new List<ShipSelectRow>();
    readonly List<ShipDefinition> ships = new List<ShipDefinition>();

    bool built;

    void OnEnable()
    {
        Build();
        Refresh();
    }

    /// <summary>Uma vez por sessão: a lista de naves não muda enquanto o jogo roda.</summary>
    void Build()
    {
        if (built)
            return;

        built = true;

        if (rowTemplate == null || rowsRoot == null)
        {
            Debug.LogError("[Naves] Falta a linha modelo ou o contêiner das linhas.", this);
            return;
        }

        rowTemplate.gameObject.SetActive(false);

        if (devGiveButton != null)
        {
            bool dev = Application.isEditor || Debug.isDebugBuild;
            devGiveButton.gameObject.SetActive(dev);
            devGiveButton.onClick.RemoveAllListeners();
            devGiveButton.onClick.AddListener(() =>
            {
                Wallet.Add(DevGiveAmount);
                Refresh();
            });
        }

        var catalog = ShipCatalog.Load();
        if (catalog == null)
            return;

        foreach (var ship in catalog.ships)
        {
            if (ship == null)
                continue;

            ships.Add(ship);

            var row = Instantiate(rowTemplate, rowsRoot);
            row.name = $"Nave {ship.displayName}";
            row.gameObject.SetActive(true);
            rows.Add(row);
        }

        if (ships.Count == 0)
            Debug.LogWarning("[Naves] O catálogo está vazio. A aba abre sem nenhuma nave.", this);
    }

    void Refresh()
    {
        var selected = ShipSelection.Ship;

        // Evoluir uma nave muda o saldo, e o saldo muda o que as OUTRAS linhas
        // podem pagar — por isso a linha avisa e a aba redesenha tudo.
        for (int i = 0; i < rows.Count; i++)
            rows[i].Bind(ships[i], ships[i] == selected, Choose, Refresh);

        if (walletLabel != null)
            walletLabel.text = $"{Wallet.CurrencyName}: {Wallet.Balance}";
    }

    void Choose(ShipDefinition ship)
    {
        ShipSelection.Choose(ship);

        // Redesenha tudo, e não só as duas linhas que mudaram: são poucas linhas,
        // e "redesenhar o que mudou" é onde nasce a linha que fica destacada por
        // engano depois da terceira nave.
        Refresh();
    }
}
