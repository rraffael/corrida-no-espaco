using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Os poderes que a nave está carregando agora. Fica na nave, ao lado do
/// <see cref="ShipStats"/>: um guarda o que a nave é, o outro o que está
/// acontecendo com ela.
///
/// É um componente separado, e não mais campos no <c>ShipStats</c>, porque são
/// duas perguntas diferentes — e porque assim o dia de tirar poderes de uma nave
/// específica é tirar um componente, não mexer em código.
///
/// **Como um poder entra:** alguém chama <see cref="Grant"/>. Quem vai chamar é
/// o item que a nave pegar na pista — que ainda não existe, porque *como* o poder
/// chega até a nave é decisão de projeto em aberto. Até lá, esta é a porta, e ela
/// já funciona.
/// </summary>
[DefaultExecutionOrder(-54)]
public class ShipPowerUps : MonoBehaviour
{
    public static ShipPowerUps Instance { get; private set; }

    readonly List<ActivePowerUp> active = new List<ActivePowerUp>();

    /// <summary>Os poderes valendo agora. Para o HUD ler, quando houver HUD.</summary>
    public IReadOnlyList<ActivePowerUp> Active => active;

    /// <summary>Os atributos da nave, para os poderes que mexem neles.</summary>
    public ShipStats Stats { get; private set; }

    ShipLaneController lanes;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        Stats = GetComponent<ShipStats>();
    }

    void OnEnable()
    {
        // A troca de faixa já era anunciada por evento, então o gancho se pendura
        // nele em vez de o controlador de faixa ter de saber que poder existe.
        lanes = GetComponent<ShipLaneController>();
        if (lanes != null)
            lanes.LaneChanged += OnLaneChanged;
    }

    void OnDisable()
    {
        if (lanes != null)
            lanes.LaneChanged -= OnLaneChanged;
    }

    // ── Ganchos ──────────────────────────────────────────────────────────
    //
    // Os três são laços escritos à mão, e não um método genérico com delegate:
    // o tiro dispara umas três vezes por segundo, e fechar uma closure a cada
    // vez é lixo para o coletor sem necessidade nenhuma.
    //
    // Percorrer para a frente é seguro: um gancho pode encerrar o poder, mas
    // encerrar só marca — quem tira da lista é o Update.

    void OnLaneChanged(int lane)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var power = active[i];
            if (!power.Finished)
                power.Definition.OnLaneChanged(power, lane);
        }
    }

    /// <summary>Chamado pelo obstáculo quando ele morre de tiro — não na batida.</summary>
    public void NotifyObstacleDestroyed(ObstacleStats obstacle)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var power = active[i];
            if (!power.Finished)
                power.Definition.OnObstacleDestroyed(power, obstacle);
        }
    }

    /// <summary>Chamado pela arma a cada tiro que sai.</summary>
    public void NotifyShotFired()
    {
        for (int i = 0; i < active.Count; i++)
        {
            var power = active[i];
            if (!power.Finished)
                power.Definition.OnShotFired(power);
        }
    }

    /// <summary>
    /// Dá um poder à nave. Devolve a cópia viva dele, ou <c>null</c> se o poder
    /// for instantâneo — porque nesse caso não sobrou nada para acompanhar.
    /// </summary>
    public ActivePowerUp Grant(PowerUpDefinition definition)
    {
        if (definition == null)
            return null;

        var runtime = definition.CreateRuntime();
        runtime.Bind(definition, this);

        definition.OnGained(runtime);

        // Instantâneo já fez o que tinha de fazer no OnGained. Guardá-lo na lista
        // só criaria um item que nunca sai dela.
        if (definition.lifetime == PowerUpDefinition.Lifetime.Instantaneo || runtime.Finished)
        {
            definition.OnLost(runtime);
            runtime.RemoveAppliedModifiers();
            return null;
        }

        active.Add(runtime);
        return runtime;
    }

    void Update()
    {
        if (active.Count == 0)
            return;

        // De trás para a frente: um poder pode acabar no próprio OnTick, e
        // remover de uma lista que se está percorrendo para a frente pula o
        // vizinho.
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var power = active[i];
            var definition = power.Definition;

            if (!power.Finished)
            {
                definition.OnTick(power, Time.deltaTime);

                if (definition.lifetime == PowerUpDefinition.Lifetime.PorTempo)
                {
                    power.SecondsLeft -= Time.deltaTime;
                    if (power.SecondsLeft <= 0f)
                        power.Finish();
                }
            }

            if (power.Finished)
            {
                active.RemoveAt(i);
                definition.OnLost(power);
                power.RemoveAppliedModifiers();
            }
        }
    }

    /// <summary>
    /// Passa um golpe pelos poderes ativos antes de ele chegar na vida. Chamado
    /// pelo <see cref="ShipStats.TakeHit"/>, que é o único caminho por onde a
    /// nave toma dano.
    ///
    /// **Gastar a última carga encerra o poder na hora**, e não no fim do quadro:
    /// quem acabou é pulado já no golpe seguinte, mesmo que ele venha no mesmo
    /// instante. É o que faz um escudo de uma carga segurar a batida no Casulo e
    /// **não** segurar o estilhaço que sai dela — na hora em que o caco chega, o
    /// escudo já não existe. A limpeza da lista vem depois; o que manda é a
    /// marca de encerrado.
    /// </summary>
    internal void ModifyIncomingHit(ref ShipStats.Hit hit)
    {
        if (active.Count == 0)
            return;

        // Na ordem em que foram pegos, e não de trás para a frente: se dois
        // poderes seguram golpe, gasta primeiro o mais antigo — é o que está mais
        // perto de acabar sozinho.
        for (int i = 0; i < active.Count; i++)
        {
            var power = active[i];
            if (!power.Finished)
                power.Definition.ModifyIncomingHit(power, ref hit);
        }
    }

    /// <summary>Existe algum poder desta ficha valendo agora?</summary>
    public bool Has(PowerUpDefinition definition)
    {
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i].Definition == definition && !active[i].Finished)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Derruba tudo o que está valendo. Serve para o fim da corrida: o poder é
    /// da partida, não do jogador, e cada filha ainda ganha o <c>OnLost</c> para
    /// desfazer o que tiver mexido.
    /// </summary>
    public void ClearAll()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var power = active[i];
            active.RemoveAt(i);
            power.Finish();
            power.Definition.OnLost(power);
            power.RemoveAppliedModifiers();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
