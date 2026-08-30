using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O modificador **enquanto ele é um item descendo pela pista**, antes de alguém
/// pegar. Desce na velocidade da corrida, como o obstáculo, e vale ao encostar na
/// nave.
///
/// Sem Physics2D, pelo mesmo motivo do <see cref="Obstacle"/>: o jogo inteiro é
/// matemática de posição, e distância entre dois pontos é mais previsível que
/// colisor mal configurado.
///
/// **Ele não se parece com o quadradinho do HUD, e isso é regra** *(22/08/2026)*.
/// Aqui é um item redondo na pista, com a cor da categoria; lá é um quadrado no
/// canto que esvazia. Se os dois se parecessem, o jogador não saberia o que tem
/// e o que ainda pode pegar.
/// </summary>
public class LevelModifierPickup : MonoBehaviour
{
    /// <summary>Todos os itens em cena. A regra da faixa livre percorre isto.</summary>
    public static readonly List<LevelModifierPickup> Active = new List<LevelModifierPickup>();

    [SerializeField] LevelModifier definition;

    [Tooltip("Abaixo deste Y o item já passou da nave e some.")]
    [SerializeField] float despawnY = -7f;

    [Tooltip("Distância da nave que conta como pegar.")]
    [SerializeField, Min(0.1f)] float pickupDistance = 0.55f;

    /// <summary>Faixa que este item ocupa.</summary>
    public int Lane { get; private set; }

    public LevelModifier Definition => definition;

    RaceSpeed race;
    SpriteRenderer art;
    float spin;

    void Awake()
    {
        race = RaceSpeed.Instance;
        art = GetComponent<SpriteRenderer>();
    }

    void OnEnable() => Active.Add(this);

    void OnDisable() => Active.Remove(this);

    public void Configure(LevelModifier modifier, int lane)
    {
        definition = modifier;
        Lane = lane;
    }

    void Update()
    {
        float step = (race != null ? race.Current : 0f) * Time.deltaTime;
        transform.position += Vector3.down * step;

        // Um giro lento é o que separa o item do obstáculo num relance: obstáculo
        // desce parado, isto aqui está vivo. Sai de graça e não precisa de arte.
        if (art != null)
        {
            spin += 90f * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0f, 0f, spin);
        }

        if (transform.position.y < despawnY)
        {
            Destroy(gameObject);
            return;
        }

        CheckPickup();
    }

    void CheckPickup()
    {
        var ship = ShipStats.Instance;
        if (ship == null || !ship.Health.IsAlive || definition == null)
            return;

        // Ao alcance na horizontal e na vertical, e não faixa contra faixa:
        // pegar no meio de uma troca conta, que é o que o jogador vê acontecer —
        // a mesma regra da batida.
        var delta = ship.transform.position - transform.position;
        if (Mathf.Abs(delta.x) > pickupDistance || Mathf.Abs(delta.y) > pickupDistance)
            return;

        if (LevelModifiers.Instance != null)
            LevelModifiers.Instance.Grant(definition);

        Destroy(gameObject);
    }
}
