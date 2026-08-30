using UnityEngine;

/// <summary>
/// Rola o cenário para baixo na velocidade do <see cref="RaceSpeed"/>, dando a
/// impressão de que a nave avança. Os ladrilhos são filhos deste objeto: quando
/// um sai por baixo da tela, volta para cima do outro. Fundo infinito sem
/// instanciar nada durante a corrida, que é o que importa num celular.
///
/// <para>
/// **E é aqui que a dobra se anuncia** *(30/08/2026)*. Enquanto a nave estiver
/// carregando a dobra, o campo de estrelas **corre mais e estica**: os pontos
/// viram riscos verticais, que é a imagem que qualquer pessoa já reconhece como
/// velocidade de dobra sem precisar que ninguém explique.
/// </para>
///
/// **Esticar sai de graça e a aceleração não bastava.** As estrelas já corriam
/// mais rápido quando a nave corria mais rápido — o fundo inteiro anda na
/// velocidade da corrida —, então acelerar mais só faria "um pouco mais do
/// mesmo", que é justamente o que não se lê de relance. Esticar muda a **forma**,
/// e forma nova o olho pega na periferia sem tirar a atenção da pista.
/// </summary>
[DefaultExecutionOrder(-40)]
public class ScrollingBackground : MonoBehaviour
{
    [Tooltip("De onde vem a velocidade. Vazio: procura o RaceSpeed da cena.")]
    [SerializeField] RaceSpeed speed;

    [Tooltip("Quem manda na dobra. Vazio: procura o RaceDirector da cena. Sem ele o fundo " +
             "funciona igual, só não reage à dobra.")]
    [SerializeField] RaceDirector director;

    [Tooltip("Fração da velocidade da corrida que esta camada usa. 1 = acompanha a nave. " +
             "Existe para o dia em que houver mais de uma camada de fundo (paralaxe): " +
             "a de trás anda mais devagar.")]
    [SerializeField, Min(0f)] float speedFactor = 1f;

    [Header("Dobra")]
    [Tooltip("Quanto o fundo corre A MAIS com a dobra carregada. 0,6 = +60% no auge.")]
    [SerializeField, Range(0f, 2f)] float warpExtraSpeed = 0.6f;

    [Tooltip("Quanto o ladrilho ESTICA na vertical com a dobra carregada. 1,8 = quase o dobro " +
             "de altura, e é o que transforma as estrelas em riscos.")]
    [SerializeField, Range(1f, 4f)] float warpStretch = 1.8f;

    [Tooltip("Segundos para o efeito entrar e sair. Curto, mas não instantâneo: esticar de " +
             "um quadro para o outro parece falha de renderização, e não velocidade.")]
    [SerializeField, Min(0.01f)] float warpResponse = 0.35f;

    Transform[] tiles;

    /// <summary>Altura de um ladrilho sem esticão nenhum, medida uma vez no Awake.</summary>
    float baseTileHeight;

    /// <summary>Escala vertical de fábrica dos ladrilhos, para o esticão medir contra ela.</summary>
    float baseScaleY = 1f;

    /// <summary>
    /// Quanto o fundo já andou, sempre dentro de uma altura de ladrilho.
    ///
    /// **É um número só, e não a posição de cada ladrilho** — essa foi a mudança
    /// de 30/08/2026. Antes cada ladrilho carregava a própria posição e embrulhava
    /// sozinho, o que funcionava enquanto a altura fosse constante. Com o esticão
    /// da dobra ela deixa de ser: as posições viriam de uma altura e o embrulho de
    /// outra, e os ladrilhos abririam fresta no meio da tela. Derivando tudo de um
    /// deslocamento só, a altura pode mudar a qualquer quadro que as peças
    /// continuam encostadas.
    /// </summary>
    float offset;

    /// <summary>De 0 a 1, suavizado. Quanto da dobra o fundo está mostrando agora.</summary>
    float warp;

    void Awake()
    {
        if (speed == null)
            speed = RaceSpeed.Instance != null ? RaceSpeed.Instance : FindAnyObjectByType<RaceSpeed>();

        CollectTiles();
    }

    void Start()
    {
        if (director == null)
            director = RaceDirector.Instance;
    }

    void CollectTiles()
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length < 2)
        {
            Debug.LogError(
                "[Fundo] Precisa de pelo menos dois ladrilhos filhos para rolar sem buraco. " +
                "Rode Tools > Corrida no Espaço > Montar.", this);
            enabled = false;
            return;
        }

        tiles = new Transform[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            tiles[i] = renderers[i].transform;

        // A altura vem do próprio ladrilho, e não de um número no Inspector:
        // trocar a arte por outra de tamanho diferente continua funcionando.
        baseTileHeight = renderers[0].bounds.size.y;
        baseScaleY = tiles[0].localScale.y;

        if (baseTileHeight > 0f)
            return;

        Debug.LogError("[Fundo] O ladrilho tem altura zero — sprite faltando?", this);
        enabled = false;
    }

    void Update()
    {
        if (speed == null)
            return;

        warp = Mathf.MoveTowards(warp, WantedWarp(), Time.deltaTime / warpResponse);

        float stretch = Mathf.Lerp(1f, warpStretch, warp);
        float tileHeight = baseTileHeight * stretch;

        offset += speed.Current * speedFactor * (1f + warpExtraSpeed * warp) * Time.deltaTime;
        offset = Mathf.Repeat(offset, tileHeight);

        for (int i = 0; i < tiles.Length; i++)
        {
            var tile = tiles[i];

            var scale = tile.localScale;
            scale.y = baseScaleY * stretch;
            tile.localScale = scale;

            var position = tile.localPosition;
            position.y = i * tileHeight - offset;
            tile.localPosition = position;
        }
    }

    /// <summary>
    /// Onde o efeito quer chegar: zero fora da dobra, e a fração da carga
    /// enquanto ela sobe.
    ///
    /// **Segue a carga e não é liga-desliga**, de propósito: assim o fundo
    /// cresce junto com a moldura da tela, e as duas coisas contam a mesma
    /// história em vez de duas.
    /// </summary>
    float WantedWarp()
    {
        if (director == null || director.IsEndless || !director.IsRunning)
            return 0f;

        return director.IsCharging ? Mathf.Max(0.25f, director.WarpFraction) : 0f;
    }
}
