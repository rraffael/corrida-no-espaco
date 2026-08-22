using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Os **poderes de fase** que a nave pegou e ainda valem. Fica na nave, ao lado
/// do <see cref="ShipStats"/> e do <see cref="ShipAbilities"/>.
///
/// **É um sistema à parte do <see cref="ShipAbilities"/>, e não conversa com
/// ele.** Os dois vivem na mesma nave e ambos podem se meter num golpe, mas
/// nenhum sabe da existência do outro — quem os chama é a nave.
///
/// **Como um poder entra:** alguém chama <see cref="Grant"/>. Quem vai chamar é
/// o item que a nave pegar na pista — que ainda não existe, porque *como* o
/// poder chega até a nave é decisão de projeto em aberto. Até lá, esta é a
/// porta, e ela já funciona.
/// </summary>
[DefaultExecutionOrder(-54)]
public class LevelPowerUps : MonoBehaviour
{
    public static LevelPowerUps Instance { get; private set; }

    readonly List<ActiveLevelPowerUp> active = new List<ActiveLevelPowerUp>();

    /// <summary>Os poderes valendo agora. Para o HUD ler, quando houver HUD.</summary>
    public IReadOnlyList<ActiveLevelPowerUp> Active => active;

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

    /// <summary>
    /// Dá um poder de fase à nave. Devolve a cópia viva dele, ou <c>null</c> se
    /// o poder for instantâneo — nesse caso não sobrou nada para acompanhar.
    /// </summary>
    public ActiveLevelPowerUp Grant(LevelPowerUp definition)
    {
        if (definition == null)
            return null;

        var runtime = definition.CreateRuntime();
        runtime.Bind(definition, this);

        definition.OnGained(runtime);

        // Instantâneo já fez o que tinha de fazer no OnGained. Guardá-lo na lista
        // só criaria um item que nunca sai dela.
        if (definition.lifetime == LevelPowerUp.Lifetime.Instantaneo || runtime.Finished)
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

                if (definition.lifetime == LevelPowerUp.Lifetime.PorTempo)
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

    // ── Ganchos ──────────────────────────────────────────────────────────
    //
    // Laços escritos à mão, e não um método genérico com delegate: o tiro
    // dispara umas três vezes por segundo, e fechar uma closure a cada vez é
    // lixo para o coletor sem necessidade nenhuma.
    //
    // Percorrer para a frente é seguro: um gancho pode encerrar o poder, mas
    // encerrar só marca — quem tira da lista é o Update.

    /// <summary>
    /// Passa um golpe pelos poderes de fase ativos. Chamado pelo
    /// <see cref="ShipStats.TakeHit"/>, que é o único caminho por onde a nave
    /// toma dano.
    ///
    /// **Gastar a última carga encerra o poder na hora**, e não no fim do
    /// quadro: quem acabou é pulado já no golpe seguinte, mesmo que ele venha no
    /// mesmo instante. É o que faz um escudo de uma carga segurar a batida no
    /// Casulo e **não** segurar o estilhaço que sai dela.
    /// </summary>
    internal void ModifyIncomingHit(ref ShipStats.Hit hit)
    {
        // Na ordem em que foram pegos: se dois poderes seguram golpe, gasta
        // primeiro o mais antigo — é o que está mais perto de acabar sozinho.
        for (int i = 0; i < active.Count; i++)
        {
            var power = active[i];
            if (!power.Finished)
                power.Definition.ModifyIncomingHit(power, ref hit);
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

    /// <summary>Chamado pelo obstáculo quando ele passa pela nave e sai de cena.</summary>
    public void NotifyObstaclePassed(ObstacleStats obstacle)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var power = active[i];
            if (!power.Finished)
                power.Definition.OnObstaclePassed(power, obstacle);
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

    void OnLaneChanged(int lane)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var power = active[i];
            if (!power.Finished)
                power.Definition.OnLaneChanged(power, lane);
        }
    }

    /// <summary>Existe algum poder desta ficha valendo agora?</summary>
    public bool Has(LevelPowerUp definition)
    {
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i].Definition == definition && !active[i].Finished)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Derruba tudo o que está valendo. Serve para o fim da corrida: poder de
    /// fase é da partida, não do jogador.
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
