using UnityEngine;

/// <summary>
/// Solta obstáculos no alto do corredor, um por vez, em faixa sorteada. O
/// intervalo é fixo, mas como os obstáculos descem na velocidade da corrida,
/// quanto mais rápido o jogador vai, mais apertado fica.
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Tooltip("De onde vêm as posições das faixas. Vazio: procura o LaneTrack da cena.")]
    [SerializeField] LaneTrack track;

    [Header("Ritmo")]
    [Tooltip("Segundos entre um obstáculo e o próximo.")]
    [SerializeField, Min(0.2f)] float spawnInterval = 1.4f;

    [Tooltip("Variação sorteada para mais ou para menos, para o ritmo não ficar de metrônomo.")]
    [SerializeField, Min(0f)] float intervalJitter = 0.4f;

    [Tooltip("Altura em que o obstáculo nasce, acima do topo da tela.")]
    [SerializeField] float spawnY = 7f;

    [Header("Obstáculo")]
    [Tooltip("Arte do obstáculo. Vazio: ele fica invisível, mas continua batendo.")]
    [SerializeField] Sprite sprite;

    [SerializeField] Color color = new Color(1f, 0.55f, 0.4f, 1f);
    [SerializeField] Vector2 size = new Vector2(0.9f, 0.9f);

    [Tooltip("Vida de cada obstáculo. Com 50 e tiro de 25, são dois tiros.")]
    [SerializeField, Min(1f)] float obstacleHealth = 50f;

    [Tooltip("Dano que ele causa na nave ao bater.")]
    [SerializeField, Min(0f)] float contactDamage = 20f;

    float timer;

    void Awake()
    {
        if (track == null)
            track = LaneTrack.Instance != null ? LaneTrack.Instance : FindAnyObjectByType<LaneTrack>();

        timer = spawnInterval;
    }

    void Update()
    {
        if (track == null)
            return;

        // A corrida acabou: para de soltar coisa nova.
        if (RaceDirector.Instance != null && !RaceDirector.Instance.IsRunning)
            return;

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        timer = Mathf.Max(0.2f, spawnInterval + Random.Range(-intervalJitter, intervalJitter));
        Spawn(Random.Range(0, track.LaneCount));
    }

    void Spawn(int lane)
    {
        var obstacle = new GameObject($"Obstaculo (faixa {lane})");
        obstacle.transform.SetParent(transform, false);
        obstacle.transform.position = new Vector3(track.LaneCenterX(lane), spawnY, 0f);

        var renderer = obstacle.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 8;

        if (sprite != null)
        {
            var spriteSize = sprite.bounds.size;
            if (spriteSize.x > 0f && spriteSize.y > 0f)
                obstacle.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
        }

        // Health antes de Obstacle: o [RequireComponent] resolveria sozinho, mas
        // na ordem explícita dá para configurar a vida antes de o Update rodar.
        obstacle.AddComponent<Health>();
        obstacle.AddComponent<Obstacle>().Configure(obstacleHealth, contactDamage);
    }
}
