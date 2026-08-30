using UnityEngine;

/// <summary>
/// Tiro da nave. Sobe em linha reta e some no primeiro obstáculo que encostar.
/// Quem cria e configura é a <see cref="ShipWeapon"/>.
///
/// **Pode ser teleguiado**, e aí ele curva atrás do obstáculo mais próximo em vez
/// de subir reto — é o que o poder da Nay liga. O tiro não sabe de poder nenhum:
/// recebe uma taxa de curva no nascimento e obedece. Zero é o tiro de sempre.
/// </summary>
public class Projectile : MonoBehaviour
{
    float damage;
    float speed = 14f;
    float hitDistance = 0.35f;
    float despawnY = 8f;

    /// <summary>Graus por segundo que este tiro consegue virar. Zero: sobe reto.</summary>
    float turnRate;

    Vector2 direction = Vector2.up;
    Obstacle target;

    public void Configure(float shotDamage, float shotSpeed, float radius, float topY,
                          float homingTurnRate = 0f)
    {
        damage = shotDamage;
        speed = shotSpeed;
        hitDistance = radius;
        despawnY = topY;
        turnRate = homingTurnRate;
    }

    void Update()
    {
        if (turnRate > 0f)
            Steer();

        transform.position += (Vector3)(direction * (speed * Time.deltaTime));

        // Só a saída por cima conta: um tiro teleguiado pode descer atrás de um
        // alvo, e some pela mesma porta por onde os obstáculos somem.
        if (transform.position.y > despawnY || transform.position.y < -despawnY)
        {
            Destroy(gameObject);
            return;
        }

        CheckHit();
    }

    /// <summary>
    /// Vira na direção do alvo, no máximo <see cref="turnRate"/> graus por
    /// segundo. **Com limite de curva, e não apontando direto**: um tiro que
    /// aponta instantaneamente nunca erra, e aí o poder deixa de ser "os tiros
    /// perseguem" e vira "os tiros acertam", que é outra coisa e muito mais forte.
    /// </summary>
    void Steer()
    {
        if (target == null)
            target = FindTarget();

        if (target == null)
            return;

        var toTarget = (Vector2)(target.transform.position - transform.position);
        if (toTarget.sqrMagnitude < 0.0001f)
            return;

        float maxDegrees = turnRate * Time.deltaTime;
        direction = Vector3.RotateTowards(direction, toTarget.normalized,
                                          maxDegrees * Mathf.Deg2Rad, 0f).normalized;

        // A arte é um retângulo comprido: sem girar junto, o tiro curvado parece
        // andar de lado.
        transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
    }

    /// <summary>
    /// O obstáculo mais próximo que ainda está à frente. **Escolhe uma vez e
    /// mantém** enquanto ele viver: trocar de alvo a cada quadro faria o tiro
    /// zigue-zaguear entre dois obstáculos e não chegar em nenhum.
    /// </summary>
    Obstacle FindTarget()
    {
        Obstacle best = null;
        float bestDistance = float.MaxValue;

        var candidates = Obstacle.Active;
        for (int i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            if (candidate == null || candidate.transform.position.y < transform.position.y)
                continue;

            float distance = ((Vector2)(candidate.transform.position - transform.position)).sqrMagnitude;
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = candidate;
        }

        return best;
    }

    void CheckHit()
    {
        var targets = Obstacle.Active;

        // De trás para frente: acertar destrói o obstáculo, que sai da lista no
        // mesmo frame, e percorrer para a frente pularia um item.
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            var candidate = targets[i];
            if (candidate == null)
                continue;

            float reach = hitDistance + candidate.HitDistance;
            if (Vector2.Distance(transform.position, candidate.transform.position) > reach)
                continue;

            candidate.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }
    }
}
