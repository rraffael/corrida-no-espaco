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
    bool phasedThrough;

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

    /// <summary>Sai de cena na hora, para não acertar de novo no próximo passo do mesmo quadro.</summary>
    void Despawn()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    void FixedUpdate()
    {
        // Soma à velocidade da corrida: se andasse só com ela, nunca alcançaria
        // a nave e o perigo não existiria.
        float previousY = transform.position.y;
        float step = ((race != null ? race.Current : 0f) + extraSpeed) * Time.deltaTime;
        transform.position += Vector3.down * step;

        // Antes de sumir: num engasgo, o mesmo quadro pode pular a nave e o fim da tela.
        if (CheckHit(previousY))
            return;

        if (transform.position.y < despawnY)
            Despawn();
    }

    /// <summary>Devolve se o estilhaço acertou e já está saindo de cena.</summary>
    bool CheckHit(float previousY)
    {
        var ship = ShipStats.Instance;
        if (ship == null || !ship.Health.IsAlive)
            return false;

        float shipY = ship.transform.position.y;
        bool jumped = Mathf.Abs(transform.position.x - ship.transform.position.x) <= hitDistance &&
                      CollisionSweep.Jumped(previousY - shipY, transform.position.y - shipY, hitDistance);
        bool touching = jumped || Vector2.Distance(transform.position, ship.transform.position) <= hitDistance;

        // Estilhaço é pedaço de obstáculo: quem atravessa um atravessa o outro,
        // com a mesma carência do Obstacle quando o efeito acaba lá dentro.
        if (ship.Has(ShipTrait.Phasing))
        {
            phasedThrough = touching;
            return false;
        }

        if (!touching)
        {
            phasedThrough = false;
            return false;
        }

        if (phasedThrough)
            return false;

        if (jumped)
            CollisionSweep.Note();

        var hit = ship.TakeHit(damage);

        // Raspão, e não batida: um tropeço de menos de um segundo. Um Casulo
        // solta vários estilhaços, então dar aqui o preço da batida cobraria
        // quatro segundos por caco e faria da Fase 3 um castigo.
        if (!hit.BlockTimeCost)
            race?.Graze();

        Despawn();
        return true;
    }
}
