using System;
using UnityEngine;

/// <summary>
/// O **molde de evolução das naves**: até onde cada atributo chega no nível
/// máximo, em que níveis ficam os patamares, em que ordem a escadinha sobe e
/// quanto cada nível custa. Mora em <c>Assets/Resources/</c> e **vale para toda
/// nave que não aponte outro** — nave nova já nasce com a progressão inteira,
/// e só falta dizer o que ela ganha nos patamares.
///
/// **A escadinha** *(desenho do Raffael, 26/09/2026)*. Os níveis que não são
/// patamar são níveis de atributo, e cada um sobe **um** atributo, na ordem de
/// <see cref="ladderOrder"/>, em rodízio: vida, cruzeiro, dano, defesa,
/// aceleração, cadência, abate, e de novo. Com 60 níveis e patamares em 15, 40
/// e 60, são 56 níveis de atributo — **8 degraus iguais para cada um dos 7**. É o
/// jeito mais uniforme de distribuir: todo nível dá alguma coisa, e nenhum
/// atributo fica muitos níveis sem subir.
///
/// **Os patamares não dão atributo:** o nível de patamar é o do upgrade do poder
/// ou da passiva, e só isso — é o nível que se sente diferente.
///
/// Ver <see cref="ShipEvolution"/> para a conta.
/// </summary>
[CreateAssetMenu(fileName = "ShipEvolutionModel", menuName = "Corrida no Espaço/Modelo de evolução de nave")]
public class ShipEvolutionModel : ScriptableObject
{
    public const string ResourcePath = "ShipEvolutionModel";

    [Header("Níveis")]
    [Min(2)] public int maxLevel = 60;

    [Tooltip("Nível do 1º upgrade do poder (patamar 2).")]
    [Min(2)] public int upgrade1Level = 15;

    [Tooltip("Nível do 2º upgrade do poder (patamar 3).")]
    [Min(2)] public int upgrade2Level = 40;

    [Tooltip("Nível em que a passiva destrava (patamar 4, o final).")]
    [Min(2)] public int passiveLevel = 60;

    [Header("Onde cada atributo chega no nível máximo")]
    [Tooltip("Velocidade de cruzeiro no nível máximo, em vezes a inicial.")]
    [Min(1f)] public float cruiseSpeedMultiplier = 1.25f;

    [Min(1f)] public float accelerationMultiplier = 1.25f;

    [Tooltip("Ganho de velocidade por abate no nível máximo, em vezes o inicial — o TOTAL.\n\n" +
             "O ganho sai da aceleração, então parte dele já vem de ela subir. Só o que falta " +
             "é somado por fora: com aceleração 1,25x e total 1,5x, o fator próprio sobe 1,2x.")]
    [Min(1f)] public float killSpeedGainMultiplier = 1.5f;

    [Min(1f)] public float maxHealthMultiplier = 1.5f;

    [Tooltip("Pontos de defesa somados no nível máximo. Em pontos, e não em vezes, porque toda " +
             "nave nasce com 0% — e qualquer multiplicador de zero é zero.")]
    [Min(0f)] public float defenseBonusPoints = 25f;

    [Min(1f)] public float damageMultiplier = 1.5f;

    [Min(1f)] public float attackSpeedMultiplier = 1.15f;

    [Header("Escadinha")]
    [Tooltip("A ordem em que os atributos sobem, em rodízio. Cada atributo que aparece aqui sobe " +
             "a mesma quantidade de vezes (ou uma a mais, quando a conta não fecha redonda).")]
    public ShipStat[] ladderOrder =
    {
        ShipStat.MaxHealth,
        ShipStat.CruiseSpeed,
        ShipStat.Damage,
        ShipStat.DefensePercent,
        ShipStat.Acceleration,
        ShipStat.AttackSpeed,
        ShipStat.KillSpeedGain,
    };

    [Header("Custo — provisório")]
    [Tooltip("Custo do nível 2, em moeda do jogo. Zero enquanto a economia não existe: dá para " +
             "evoluir tudo de graça e testar. A conta de verdade é da Parte 8.")]
    [Min(0)] public int baseCost = 0;

    [Tooltip("Quanto o custo cresce a cada nível. Zero: todo nível custa o base.")]
    [Min(0)] public int costPerLevel = 0;

    /// <summary>Quanto custa passar de <paramref name="level"/> - 1 para <paramref name="level"/>.</summary>
    public int CostToReach(int level) => Mathf.Max(0, baseCost + costPerLevel * (level - 2));

    /// <summary>Qual patamar o nível abre. 0: nenhum — é nível de atributo.</summary>
    public int TierOpenedAt(int level)
    {
        if (level == upgrade1Level) return 2;
        if (level == upgrade2Level) return 3;
        if (level == passiveLevel) return 4;
        return 0;
    }

    /// <summary>Em que patamar a nave está, de 1 (inicial) a 4 (final).</summary>
    public int TierAt(int level)
    {
        if (level >= passiveLevel) return 4;
        if (level >= upgrade2Level) return 3;
        if (level >= upgrade1Level) return 2;
        return 1;
    }

    static ShipEvolutionModel cached;

    /// <summary>
    /// O molde padrão, de Resources. Sem o asset, um com os valores de fábrica
    /// desta classe — que são os mesmos números — e um aviso: a evolução não
    /// pode parar o jogo por falta de um arquivo.
    /// </summary>
    public static ShipEvolutionModel Default
    {
        get
        {
            if (cached != null)
                return cached;

            cached = Resources.Load<ShipEvolutionModel>(ResourcePath);
            if (cached != null)
                return cached;

            Debug.LogWarning($"[Evolução] Não achei o molde em Resources/{ResourcePath}. Rode o " +
                             "Montar. Usando os valores de fábrica por enquanto.");
            cached = CreateInstance<ShipEvolutionModel>();
            return cached;
        }
    }

    /// <summary>
    /// Os níveis de atributo, em ordem, e qual atributo cada um sobe. Calculado
    /// uma vez: o molde não muda com o jogo rodando.
    /// </summary>
    [NonSerialized] ShipStat[] ladder;
    [NonSerialized] int[] ladderLevels;

    internal void Ladder(out int[] levels, out ShipStat[] stats)
    {
        if (ladder == null)
            BuildLadder();

        levels = ladderLevels;
        stats = ladder;
    }

    void BuildLadder()
    {
        int count = 0;
        for (int level = 2; level <= maxLevel; level++)
        {
            if (TierOpenedAt(level) == 0)
                count++;
        }

        ladderLevels = new int[count];
        ladder = new ShipStat[count];

        int order = ladderOrder != null ? ladderOrder.Length : 0;
        int index = 0;
        for (int level = 2; level <= maxLevel; level++)
        {
            if (TierOpenedAt(level) != 0)
                continue;

            ladderLevels[index] = level;
            ladder[index] = order > 0 ? ladderOrder[index % order] : ShipStat.MaxHealth;
            index++;
        }
    }

    // Mexer num número do Inspector tem de refazer a escadinha, senão o Editor
    // mostraria a antiga até recompilar.
    void OnValidate()
    {
        ladder = null;
        ladderLevels = null;
    }
}
