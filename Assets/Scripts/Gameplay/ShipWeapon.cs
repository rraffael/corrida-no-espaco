using UnityEngine;

/// <summary>
/// Arma da nave. Atira sozinha, no ritmo da velocidade de ataque da ficha —
/// sem munição e sem botão. O dia em que o tiro virar comando do jogador, é aqui
/// que entra a condição, e o resto do jogo não muda.
///
/// **Ela para de atirar enquanto a nave troca de faixa** *(experiência de
/// 22/08/2026)*. A razão vem de um retorno de teste: alguém jogou e ficou em
/// dúvida se o jogo era destruir ou desviar. A dúvida era legítima, porque o
/// tiro é automático, infinito e sempre ligado — ou seja, **não é decisão de
/// ninguém**, e ainda por cima premia ficar parado, que é o contrário do que a
/// troca de faixa pede.
///
/// Com esta regra, o único comando do jogo passa a valer duas coisas ao mesmo
/// tempo: ficar na faixa é atirar e ganhar velocidade; sair é desviar e abrir
/// mão do abate. A dúvida do testador **vira a mecânica** em vez de ser um
/// mal-entendido, e a resposta passa a ser dele, momento a momento.
///
/// **É experiência, e desliga num campo** — <c>holdFireWhileChangingLane</c>.
/// Desmarcado, volta exatamente o comportamento antigo, para os dois serem
/// comparados no mesmo aparelho.
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

    [Header("Trocar de faixa")]
    [Tooltip("A nave PARA de atirar enquanto está deslizando de uma faixa para a outra.\n\n" +
             "É o que transforma o único botão do jogo numa escolha: ficar na faixa é atirar e " +
             "ganhar velocidade, sair é desviar e abrir mão do abate. Desmarcado, volta o " +
             "comportamento antigo — atira sempre, e a troca de faixa sai de graça.")]
    [SerializeField] bool holdFireWhileChangingLane = true;

    [Tooltip("Segundos parados a mais DEPOIS de chegar na faixa nova. Zero: volta a atirar assim " +
             "que encosta.\n\n" +
             "Existe porque a troca em si dura pouco — com velocidade 12 e faixa de 1,6, dá uns " +
             "0,13 s, que pode ser sutil demais para o jogador sentir a troca. Se o efeito não " +
             "aparecer no aparelho, é aqui que se aumenta antes de descartar a ideia.")]
    [SerializeField, Min(0f)] float holdFireAfterLaneChange = 0f;

    float cooldown;
    float resumeFireAt;
    ShipLaneController lanes;

    void Awake()
    {
        if (stats == null)
            stats = GetComponent<ShipStats>();

        lanes = GetComponent<ShipLaneController>();
    }

    void Update()
    {
        if (stats == null || !stats.Health.IsAlive)
            return;

        // A corrida acabou (vitória ou derrota): a nave para de atirar, senão o
        // tiro continua saindo por trás do painel de fim.
        if (RaceDirector.Instance != null && !RaceDirector.Instance.IsRunning)
            return;

        if (IsHoldingFire())
            return;

        cooldown -= Time.deltaTime;
        if (cooldown > 0f)
            return;

        cooldown = stats.ShotInterval;
        Fire();
    }

    /// <summary>
    /// A arma está calada porque a nave está trocando de faixa.
    ///
    /// **Congela o relógio do tiro junto**, e é de propósito: se ele continuasse
    /// correndo, a nave chegaria na faixa nova com o tiro já vencido e dispararia
    /// no mesmo instante — a troca sairia quase de graça, que é justamente o que
    /// esta regra existe para cobrar. Do jeito que está, tempo trocando de faixa
    /// é tempo sem atirar, ponto.
    /// </summary>
    bool IsHoldingFire()
    {
        if (!holdFireWhileChangingLane || lanes == null)
            return false;

        if (lanes.IsChangingLane)
        {
            resumeFireAt = Time.time + holdFireAfterLaneChange;
            return true;
        }

        return Time.time < resumeFireAt;
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

        // Depois de o tiro sair, e não antes: o gancho serve para contar tiros e
        // reagir, não para mexer no dano — esse já veio da ficha com os
        // modificadores aplicados.
        if (LevelPowerUps.Instance != null)
            LevelPowerUps.Instance.NotifyShotFired();
        if (ShipAbilities.Instance != null)
            ShipAbilities.Instance.NotifyShotFired();
    }
}
