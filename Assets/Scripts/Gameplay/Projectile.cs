using UnityEngine;

/// <summary>
/// Tiro da nave. Sobe em linha reta e some no primeiro obstáculo que encostar.
/// Quem cria e configura é a <see cref="ShipWeapon"/>.
/// </summary>
public class Projectile : MonoBehaviour
{
    float damage;
    float speed = 14f;
    float hitDistance = 0.35f;
    float despawnY = 8f;

    public void Configure(float shotDamage, float shotSpeed, float radius, float topY)
    {
        damage = shotDamage;
        speed = shotSpeed;
        hitDistance = radius;
        despawnY = topY;
    }

    void Update()
    {
        transform.position += Vector3.up * (speed * Time.deltaTime);

        if (transform.position.y > despawnY)
        {
            Destroy(gameObject);
            return;
        }

        CheckHit();
    }

    void CheckHit()
    {
        var targets = Obstacle.Active;

        // De trás para frente: acertar destrói o obstáculo, que sai da lista no
        // mesmo frame, e percorrer para a frente pularia um item.
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            var target = targets[i];
            if (target == null)
                continue;

            float reach = hitDistance + target.HitDistance;
            if (Vector2.Distance(transform.position, target.transform.position) > reach)
                continue;

            target.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }
    }
}
