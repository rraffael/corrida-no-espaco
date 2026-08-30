using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A fileira de quadradinhos que mostra os **modificadores de fase** valendo
/// agora: um por modificador, com o ícone no meio e um relógio circular
/// esvaziando em volta.
///
/// **Sem número nenhum, de propósito** *(pedido do Raffael, 30/08/2026)*. O
/// jogador está desviando de obstáculo a 15 unidades por segundo — ele não vai
/// ler "2,4 s". O que ele consegue é um relance, e um anel que esvazia responde
/// "quanto falta" mais depressa do que qualquer algarismo.
///
/// **Não se parece com o canto do poder da nave, e isso é regra** *(22/08/2026)*.
/// O canto do poder é fixo, mora sozinho no alto à direita e pisca ao ficar
/// pronto; isto aqui é uma fileira que cresce e encolhe no alto à esquerda. Se os
/// dois se parecessem, o jogador não saberia o que tem e o que pode ativar.
///
/// A cor de cada quadradinho é a da categoria — verde para Reforço, vermelho para
/// Debilitante, roxo para Especial —, a mesma com que o item apareceu na pista.
/// É o que liga uma coisa à outra sem texto: pegou aquele verde, apareceu aquele
/// verde.
/// </summary>
[DefaultExecutionOrder(50)]
public class ModifierHud : MonoBehaviour
{
    [Tooltip("De quem esta fileira fala. Vazio: procura os modificadores da nave da cena.")]
    [SerializeField] LevelModifiers modifiers;

    [Tooltip("O quadradinho modelo, desligado. É clonado uma vez por modificador que aparecer.")]
    [SerializeField] ModifierHudSlot slotTemplate;

    /// <summary>
    /// Os quadradinhos já criados. **Nunca são destruídos, só desligados**: um
    /// modificador acaba e volta a ser pego várias vezes numa corrida, e criar e
    /// destruir objeto de UI a cada vez é lixo para o coletor bem no momento em
    /// que o jogo está mais cheio.
    /// </summary>
    readonly List<ModifierHudSlot> slots = new List<ModifierHudSlot>();

    void Start()
    {
        if (modifiers == null)
            modifiers = LevelModifiers.Instance;

        if (slotTemplate != null)
            slotTemplate.gameObject.SetActive(false);

        if (modifiers != null)
            return;

        Debug.LogWarning("[HUD] Nenhum LevelModifiers na cena. A fileira de modificadores não aparece.",
                         this);
        enabled = false;
    }

    void Update()
    {
        var active = modifiers.Active;

        for (int i = 0; i < active.Count; i++)
            SlotAt(i).Show(active[i]);

        // Os que sobraram da lista de antes saem de cena, mas continuam existindo
        // para o próximo modificador reaproveitar.
        for (int i = active.Count; i < slots.Count; i++)
            slots[i].Hide();
    }

    ModifierHudSlot SlotAt(int index)
    {
        while (slots.Count <= index)
        {
            var clone = Instantiate(slotTemplate, slotTemplate.transform.parent);
            clone.name = $"Modificador {slots.Count + 1}";
            slots.Add(clone);
        }

        return slots[index];
    }
}
