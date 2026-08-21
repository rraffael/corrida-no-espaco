using UnityEngine;

/// <summary>
/// Estilhaço solto por um obstáculo destruído. Desce pela faixa mais rápido que
/// o resto do cenário e machuca quem estiver embaixo — é o que torna caro
/// destruir um obstáculo desses de frente.
///
/// Ser atingido custa vida e um **raspão** na velocidade (<see cref="RaceSpeed.Graze"/>):
/// um tropeço curto, e não a parada de quatro segundos que um obstáculo cobra.
/// </summary>
public class Shrapnel : MonoBehaviour
{
    float damage;
    float extraSpeed = 6f;
    float despawnY = -7f;
    float hitDistance = 0.45f;

    RaceSpeed race;

    public void Configure(float shrapnelDamage, float speedOverRace, float bottomY)
    {
        damage = shrapnelDamage;
        extraSpeed = speedOverRace;
        despawnY = bottomY;
    }

    void Awake()
    {
        race = RaceSpeed.Instance;
    }

    void Update()
    {
        // Soma à velocidade da corrida: se andasse só com ela, nunca alcançaria
        // a nave e o perigo não existiria.
        float step = ((race != null ? race.Current : 0f) + extraSpeed) * Time.deltaTime;
        transform.position += Vector3.down * step;

        if (transform.position.y < despawnY)
        {
            Destroy(gameObject);
            return;
        }

        CheckHit();
    }

    void CheckHit()
    {
        var ship = ShipStats.Instance;
        if (ship == null || !ship.Health.IsAlive)
            return;

        if (Vector2.Distance(transform.position, ship.transform.position) > hitDistance)
            return;

        var hit = ship.TakeHit(damage);

        // Raspão, e não batida: um tropeço de menos de um segundo. Um Casulo
        // solta vários estilhaços, então dar aqui o preço da batida cobraria
        // quatro segundos por caco e faria da Fase 3 um castigo.
        if (!hit.BlockTimeCost)
            race?.Graze();

        Destroy(gameObject);
    }
}
