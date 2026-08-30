using UnityEngine;

/// <summary>
/// Solta os modificadores da fase na pista. O repertório e o ritmo estão na ficha
/// da fase, do mesmo jeito que os obstáculos — fase que não lista nenhum
/// simplesmente não tem modificador, e este componente fica quieto.
///
/// **É um sorteador separado do <see cref="ObstacleSpawner"/>, mas os dois
/// respeitam a mesma regra de faixa livre** — ver <see cref="LaneOccupancy"/>.
/// Separado porque o ritmo é outro: obstáculo é o pulso da fase e vem a cada
/// segundo e pouco, modificador é evento e vem a cada dez. Amarrar um no outro
/// faria o modificador herdar a rampa de dificuldade do obstáculo, e a fase
/// apertada acabaria chovendo poder.
/// </summary>
public class LevelModifierSpawner : MonoBehaviour
{
    [Tooltip("De onde vêm as posições das faixas. Vazio: procura o LaneTrack da cena.")]
    [SerializeField] LaneTrack track;

    [Tooltip("Arte do item. Vazio: o item não aparece, mas continua sendo pego.")]
    [SerializeField] Sprite sprite;

    [Tooltip("Altura em que o item nasce, acima do topo da tela. Igual à do obstáculo, " +
             "senão a regra da faixa livre compara levas diferentes.")]
    [SerializeField] float spawnY = 7f;

    [Tooltip("Distância vertical dentro da qual duas coisas contam como a mesma leva.")]
    [SerializeField, Min(0.5f)] float waveWindow = 3.5f;

    [Tooltip("Tamanho do item na pista, em unidades de mundo.")]
    [SerializeField] float size = 0.55f;

    LevelDefinition level;
    float timer;

    void Awake()
    {
        if (track == null)
            track = LaneTrack.Instance != null ? LaneTrack.Instance : FindAnyObjectByType<LaneTrack>();
    }

    void Start()
    {
        level = LevelSelection.Level;

        if (level == null || level.modifiers == null || level.modifiers.Length == 0)
        {
            // Fase sem repertório não é erro: é uma fase que não usa modificador.
            enabled = false;
            return;
        }

        timer = NextInterval();
    }

    void Update()
    {
        if (track == null)
            return;

        if (RaceDirector.Instance != null && !RaceDirector.Instance.IsRunning)
            return;

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        timer = NextInterval();
        TrySpawn();
    }

    float NextInterval() =>
        Mathf.Max(0.5f, level.modifierInterval +
                        Random.Range(-level.modifierIntervalJitter, level.modifierIntervalJitter));

    void TrySpawn()
    {
        var definition = Pick();
        if (definition == null)
            return;

        // Ocupa uma faixa, sempre: modificador largo não existe, e se um dia
        // existir é aqui que o número deixa de ser 1.
        if (!LaneOccupancy.TryPickLane(track.LaneCount, 1, spawnY, waveWindow, out int lane))
            return; // A leva atual já não deixa saída: melhor pular esta vez.

        Spawn(definition, lane);
    }

    /// <summary>
    /// Sorteio por peso entre os modificadores da fase.
    ///
    /// **O que já está valendo fica de fora do sorteio.** Um item na pista que
    /// só renova o que a nave já tem é uma decisão sem consequência — o jogador
    /// desvia para pegar e não sente nada mudar. Fora do sorteio, todo item que
    /// aparece traz alguma coisa nova. *(Renovar continua acontecendo se ele
    /// aparecer antes de o anterior acabar e o jogador pegar os dois — ver
    /// <see cref="LevelModifiers.Grant"/>.)*
    /// </summary>
    LevelModifier Pick()
    {
        var powers = LevelModifiers.Instance;

        float total = 0f;
        for (int i = 0; i < level.modifiers.Length; i++)
        {
            var candidate = level.modifiers[i];
            if (IsEligible(candidate, powers))
                total += candidate.spawnWeight;
        }

        if (total <= 0f)
            return null;

        float roll = Random.Range(0f, total);
        for (int i = 0; i < level.modifiers.Length; i++)
        {
            var candidate = level.modifiers[i];
            if (!IsEligible(candidate, powers))
                continue;

            roll -= candidate.spawnWeight;
            if (roll <= 0f)
                return candidate;
        }

        return null;
    }

    static bool IsEligible(LevelModifier candidate, LevelModifiers active) =>
        candidate != null &&
        candidate.spawnWeight > 0f &&
        (active == null || !active.IsActive(candidate));

    void Spawn(LevelModifier definition, int lane)
    {
        var item = new GameObject($"Modificador {definition.displayName} (faixa {lane})");
        item.transform.SetParent(transform, false);
        item.transform.position = new Vector3(track.LaneCenterX(lane), spawnY, 0f);

        var art = definition.sprite != null ? definition.sprite : sprite;

        var renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = art;
        renderer.color = definition.Color;
        // Acima do obstáculo: com os dois na mesma altura o item precisa ser o
        // que se vê, senão o jogador desvia de um prêmio.
        renderer.sortingOrder = 10;

        if (art != null)
        {
            var spriteSize = art.bounds.size;
            if (spriteSize.x > 0f && spriteSize.y > 0f)
                item.transform.localScale = new Vector3(size / spriteSize.x, size / spriteSize.y, 1f);
        }

        item.AddComponent<LevelModifierPickup>().Configure(definition, lane);
    }
}
