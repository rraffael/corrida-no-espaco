using UnityEngine;

/// <summary>
/// Arma da nave. Atira sozinha, no ritmo da velocidade de ataque da ficha —
/// por enquanto sem munição e sem botão. O dia em que o tiro virar comando do
/// jogador, é aqui que entra a condição, e o resto do jogo não muda.
/// </summary>
public class ShipWeapon : MonoBehaviour
{
    [Tooltip("De onde vêm dano e cadência. Vazio: procura o ShipStats deste objeto.")]
    [SerializeField] ShipStats stats;

    [Header("Tiro")]
    [Tooltip("Arte do tiro. Vazio: o tiro não aparece, mas continua acertando.")]
    [SerializeField] Sprite projectileSprite;

    [SerializeField] Color projectileColor = new Color(0.6f, 1f, 0.9f, 1f);
    [SerializeField] Vector2 projectileSize = new Vector2(0.12f, 0.5f);

    [Tooltip("Velocidade do tiro, em unidades por segundo. Precisa ser bem maior que a da corrida.")]
    [SerializeField, Min(1f)] float projectileSpeed = 16f;

    [Tooltip("Onde o tiro nasce, acima do centro da nave.")]
    [SerializeField] float muzzleOffset = 0.6f;

    [SerializeField, Min(0.05f)] float projectileHitDistance = 0.35f;

    [Tooltip("Acima deste Y o tiro some, por ter saído da tela.")]
    [SerializeField] float despawnY = 8f;

    float cooldown;

    void Awake()
    {
        if (stats == null)
            stats = GetComponent<ShipStats>();
    }

    void Update()
    {
        if (stats == null || !stats.Health.IsAlive)
            return;

        // A corrida acabou (vitória ou derrota): a nave para de atirar, senão o
        // tiro continua saindo por trás do painel de fim.
        if (RaceDirector.Instance != null && !RaceDirector.Instance.IsRunning)
            return;

        cooldown -= Time.deltaTime;
        if (cooldown > 0f)
            return;

        cooldown = stats.ShotInterval;
        Fire();
    }

    void Fire()
    {
        var shot = new GameObject("Tiro");
        shot.transform.position = transform.position + Vector3.up * muzzleOffset;

        var renderer = shot.AddComponent<SpriteRenderer>();
        renderer.sprite = projectileSprite;
        renderer.color = projectileColor;
        renderer.sortingOrder = 5;

        // A arte é um retângulo branco esticado, do mesmo jeito que as divisas
        // das faixas: o tamanho vem do transform, não de um sprite por calibre.
        if (projectileSprite != null)
        {
            var size = projectileSprite.bounds.size;
            if (size.x > 0f && size.y > 0f)
                shot.transform.localScale = new Vector3(projectileSize.x / size.x, projectileSize.y / size.y, 1f);
        }

        shot.AddComponent<Projectile>()
            .Configure(stats.Damage, projectileSpeed, projectileHitDistance, despawnY);
    }
}
