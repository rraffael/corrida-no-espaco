using UnityEngine;

/// <summary>
/// Rola o cenário para baixo na velocidade do <see cref="RaceSpeed"/>, dando a
/// impressão de que a nave avança. Os ladrilhos são filhos deste objeto: quando
/// um sai por baixo da tela, volta para cima do outro. Fundo infinito sem
/// instanciar nada durante a corrida, que é o que importa num celular.
/// </summary>
[DefaultExecutionOrder(-40)]
public class ScrollingBackground : MonoBehaviour
{
    [Tooltip("De onde vem a velocidade. Vazio: procura o RaceSpeed da cena.")]
    [SerializeField] RaceSpeed speed;

    [Tooltip("Fração da velocidade da corrida que esta camada usa. 1 = acompanha a nave. " +
             "Existe para o dia em que houver mais de uma camada de fundo (paralaxe): " +
             "a de trás anda mais devagar.")]
    [SerializeField, Min(0f)] float speedFactor = 1f;

    Transform[] tiles;
    float tileHeight;
    float span;

    void Awake()
    {
        if (speed == null)
            speed = RaceSpeed.Instance != null ? RaceSpeed.Instance : FindAnyObjectByType<RaceSpeed>();

        CollectTiles();
    }

    void CollectTiles()
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length < 2)
        {
            Debug.LogError(
                "[Fundo] Precisa de pelo menos dois ladrilhos filhos para rolar sem buraco. " +
                "Rode Tools > Corrida no Espaço > Montagem > Montar corrida (fundo + HUD).", this);
            enabled = false;
            return;
        }

        tiles = new Transform[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            tiles[i] = renderers[i].transform;

        // A altura vem do próprio ladrilho, e não de um número no Inspector:
        // trocar a arte por outra de tamanho diferente continua funcionando.
        tileHeight = renderers[0].bounds.size.y;
        span = tileHeight * tiles.Length;

        if (tileHeight > 0f)
            return;

        Debug.LogError("[Fundo] O ladrilho tem altura zero — sprite faltando?", this);
        enabled = false;
    }

    void Update()
    {
        if (speed == null)
            return;

        float step = speed.Current * speedFactor * Time.deltaTime;
        if (step <= 0f)
            return;

        foreach (var tile in tiles)
        {
            var position = tile.localPosition;
            position.y -= step;

            // While e não if: num frame longo (troca de cena, primeiro frame
            // depois da pausa) o ladrilho pode passar mais de uma altura.
            while (position.y <= -tileHeight)
                position.y += span;

            tile.localPosition = position;
        }
    }
}
