using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Os **modificadores de fase** que a nave pegou e ainda valem. Fica na nave, ao
/// lado do <see cref="ShipStats"/> e do <see cref="ShipAbilities"/>.
///
/// **É um sistema à parte do <see cref="ShipAbilities"/>, e não conversa com
/// ele.** Os dois vivem na mesma nave e ambos podem se meter num golpe, mas
/// nenhum sabe da existência do outro — quem os chama é a nave.
///
/// **Como um modificador entra:** o item na pista
/// (<see cref="LevelModifierPickup"/>) chama <see cref="Grant"/> ao encostar na
/// nave.
/// </summary>
[DefaultExecutionOrder(-54)]
public class LevelModifiers : MonoBehaviour
{
    public static LevelModifiers Instance { get; private set; }

    readonly List<ActiveLevelModifier> active = new List<ActiveLevelModifier>();

    /// <summary>Os que valem agora, na ordem em que foram pegos. O HUD lê daqui.</summary>
    public IReadOnlyList<ActiveLevelModifier> Active => active;

    /// <summary>Os atributos da nave, para os modificadores que mexem neles.</summary>
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
        // nele em vez de o controlador de faixa ter de saber que modificador existe.
        lanes = GetComponent<ShipLaneController>();
        if (lanes != null)
            lanes.LaneChanged += OnLaneChanged;
    }

    void OnDisable()
    {
        if (lanes != null)
            lanes.LaneChanged -= OnLaneChanged;
    }

    /// <summary>Este modificador está valendo agora? O sorteio da pista pergunta.</summary>
    public bool IsActive(LevelModifier definition) => Find(definition) != null;

    ActiveLevelModifier Find(LevelModifier definition)
    {
        if (definition == null)
            return null;

        for (int i = 0; i < active.Count; i++)
        {
            if (!active[i].Finished && active[i].Definition == definition)
                return active[i];
        }

        return null;
    }

    /// <summary>
    /// Dá um modificador de fase à nave. Devolve a cópia viva dele, ou
    /// <c>null</c> se for instantâneo — nesse caso não sobrou nada para
    /// acompanhar.
    ///
    /// **Pegar o mesmo de novo renova, não empilha** *(regra do Raffael,
    /// 30/08/2026)*. Dois modificadores diferentes correm com relógios
    /// independentes, cada um acabando na sua hora; o mesmo pego duas vezes volta
    /// a valer inteiro e continua sendo um só.
    ///
    /// A razão de renovar e não empilhar é que empilhar dobraria o efeito: dois
    /// "+100% de cadência" virariam +200% por o jogador ter passado em cima duas
    /// vezes, e a segunda pegada valeria mais que a primeira. Renovando, o que se
    /// ganha é **tempo**, que é o que o jogador entende ao pegar de novo.
    /// </summary>
    public ActiveLevelModifier Grant(LevelModifier definition)
    {
        if (definition == null)
            return null;

        var existing = Find(definition);
        if (existing != null)
        {
            existing.Renew();
            definition.OnRenewed(existing);
            return existing;
        }

        var runtime = definition.CreateRuntime();
        runtime.Bind(definition, this);

        definition.OnGained(runtime);

        // Instantâneo já fez o que tinha de fazer no OnGained. Guardá-lo na lista
        // só criaria um item que nunca sai dela.
        if (definition.lifetime == LevelModifier.Lifetime.Instantaneo || runtime.Finished)
        {
            definition.OnLost(runtime);
            runtime.RemoveApplied();
            return null;
        }

        active.Add(runtime);
        return runtime;
    }

    void Update()
    {
        if (active.Count == 0)
            return;

        // De trás para a frente: um modificador pode acabar no próprio OnTick, e
        // remover de uma lista que se está percorrendo para a frente pula o
        // vizinho.
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var modifier = active[i];
            var definition = modifier.Definition;

            if (!modifier.Finished)
            {
                definition.OnTick(modifier, Time.deltaTime);

                if (definition.lifetime == LevelModifier.Lifetime.PorTempo)
                {
                    modifier.SecondsLeft -= Time.deltaTime;
                    if (modifier.SecondsLeft <= 0f)
                        modifier.Finish();
                }
            }

            if (modifier.Finished)
            {
                active.RemoveAt(i);
                definition.OnLost(modifier);
                modifier.RemoveApplied();
            }
        }
    }

    // ── Ganchos ──────────────────────────────────────────────────────────

    /// <summary>
    /// Passa um golpe pelos modificadores valendo. Chamado pelo
    /// <see cref="ShipStats.TakeHit"/>.
    /// </summary>
    internal void ModifyIncomingHit(ref ShipStats.Hit hit)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var modifier = active[i];
            if (!modifier.Finished)
                modifier.Definition.ModifyIncomingHit(modifier, ref hit);
        }
    }

    /// <summary>Chamado pelo obstáculo quando ele morre de tiro — não na batida.</summary>
    public void NotifyObstacleDestroyed(ObstacleStats obstacle)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var modifier = active[i];
            if (!modifier.Finished)
                modifier.Definition.OnObstacleDestroyed(modifier, obstacle);
        }
    }

    /// <summary>Chamado pelo obstáculo quando ele passa pela nave e sai de cena.</summary>
    public void NotifyObstaclePassed(ObstacleStats obstacle)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var modifier = active[i];
            if (!modifier.Finished)
                modifier.Definition.OnObstaclePassed(modifier, obstacle);
        }
    }

    /// <summary>Chamado pela arma a cada tiro que sai.</summary>
    public void NotifyShotFired()
    {
        for (int i = 0; i < active.Count; i++)
        {
            var modifier = active[i];
            if (!modifier.Finished)
                modifier.Definition.OnShotFired(modifier);
        }
    }

    void OnLaneChanged(int lane)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var modifier = active[i];
            if (!modifier.Finished)
                modifier.Definition.OnLaneChanged(modifier, lane);
        }
    }

    /// <summary>
    /// Derruba tudo que estava valendo. Serve para o fim da corrida: modificador
    /// é da partida, não do jogador.
    /// </summary>
    public void ClearAll()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var modifier = active[i];
            active.RemoveAt(i);
            modifier.Finish();
            modifier.Definition.OnLost(modifier);
            modifier.RemoveApplied();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
