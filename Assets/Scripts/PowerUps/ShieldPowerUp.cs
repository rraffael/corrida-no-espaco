using UnityEngine;

/// <summary>
/// **Proteção: segura o próximo golpe inteiro.** O primeiro poder do jogo, e ele
/// existe tanto para ser jogado quanto para ser o exemplo de como se escreve um:
/// a classe tem dez linhas de efeito, e todo o resto — nome, arte, quantos golpes
/// segura — vem da base, pelo arquivo.
///
/// **Segura o golpe inteiro, e não só o dano** *(definido pelo Raffael em
/// 21/08/2026)*: some a vida perdida e some também o custo de tempo — o tranco,
/// a parada e a retomada arrastada. **É como se a nave nunca tivesse batido.**
/// Por isso não há botão para escolher: um escudo que salvasse a vida e deixasse
/// a freada seria outro poder, não uma variação deste.
///
/// **Segura por contagem, e não por tempo**, e a diferença é de equilíbrio: um
/// escudo com duração pode comer três batidas seguidas se elas vierem juntas, e
/// some justamente o custo de ~3,5 s que dá tensão à corrida. Segurando um golpe,
/// o preço da segunda batida continua inteiro.
///
/// **Isto não fere a regra de que um golpe nunca é de graça.** Aquela regra é
/// sobre a *defesa em porcentagem*, que não pode virar imunidade por mais alta
/// que seja. Aqui o golpe é pago com o escudo, que se gasta e acaba.
///
/// **O obstáculo se desfaz no escudo**, e os dois somem juntos — o obstáculo
/// porque bateu, o escudo porque foi gasto. O que falta é mostrar isso em tela,
/// e isso é trabalho da arte.
///
/// **E o obstáculo morre como quem é destruído**, o que tem uma consequência
/// escolhida pelo Raffael em 21/08/2026: **Casulo desfeito no escudo solta
/// estilhaço**, e a nave leva. O escudo se gasta no mesmo golpe, então na hora em
/// que o caco chega ele já não existe — é atômico. A troca fica sendo *uma batida
/// grande vira vários raspões*, que vale muito a pena e ainda assim não é de
/// graça.
///
/// **Dois botões, e os dois são a régua de melhoria deste poder:** quantas cargas
/// ele tem (na base, porque todo poder por carga precisa disso) e se ele cala o
/// que o obstáculo solta ao morrer (aqui, porque só um poder defensivo se
/// pergunta isso). Uma proteção básica tem 1 carga e deixa o estilhaço sair; uma
/// melhorada sobe as cargas, cala o estilhaço, ou as duas coisas.
/// </summary>
[CreateAssetMenu(fileName = "Protecao", menuName = "Corrida no Espaço/Poderes/Proteção")]
public class ShieldPowerUp : PowerUpDefinition
{
    [Header("Proteção")]
    [Tooltip("Marcado, o obstáculo desfeito no escudo NÃO solta o que soltaria ao morrer — no " +
             "Casulo, o estilhaço some junto. A batida deixa de custar qualquer coisa.\n\n" +
             "Desmarcado (o padrão), o estilhaço sai e a nave leva, porque o escudo já foi gasto " +
             "no golpe que o criou.\n\n" +
             "Existe para ser MELHORIA do poder: a proteção comum troca uma batida grande por " +
             "vários raspões, e a melhorada apaga a batida inteira.")]
    [SerializeField] bool blocksDeathEffects;

    void Reset()
    {
        // Os valores com que este poder faz sentido, para uma ficha nova já
        // nascer certa em vez de nascer com o padrão da base.
        displayName = "Proteção";
        description = "Segura o próximo golpe inteiro — nem dano, nem freada.";
        lifetime = Lifetime.PorUso;
        charges = 1;
        color = new Color(0.55f, 0.85f, 1f, 1f);
    }

    public override void ModifyIncomingHit(ActivePowerUp active, ref ShipStats.Hit hit)
    {
        // Outro poder já segurou este golpe: não gastar carga por nada.
        if (hit.Damage <= 0f)
            return;

        // Os dois juntos, e é o que faz a batida sumir de verdade: zerar só o
        // dano deixaria a nave inteira e ainda assim parada, que é o custo que
        // mais se sente.
        hit.Damage = 0f;
        hit.BlockTimeCost = true;

        // A melhoria: cala também o que o obstáculo soltaria ao morrer. Só marca
        // quando é para calar — nunca desmarca, porque outro poder ativo pode
        // ter marcado antes e não cabe a este desfazer.
        if (blocksDeathEffects)
            hit.BlockDeathEffects = true;

        active.ConsumeCharge();
    }
}
