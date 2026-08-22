using System;
using UnityEngine;

/// <summary>
/// Move a nave entre as faixas do <see cref="LaneTrack"/>. O comando é arrastar
/// o dedo para a esquerda ou para a direita, e cada arraste vale **uma única
/// faixa**: pouco importa se o dedo andou o mínimo ou atravessou a tela inteira,
/// a nave anda uma faixa e só aceita o próximo comando depois de soltar o dedo.
/// </summary>
public class ShipLaneController : MonoBehaviour
{
    [Header("Faixas")]
    [Tooltip("De onde vêm as posições das faixas. Vazio: procura o LaneTrack da cena.")]
    [SerializeField] LaneTrack track;

    [Tooltip("Faixa em que a nave começa. Negativo: começa na faixa do meio.")]
    [SerializeField] int startingLane = -1;

    [Header("Comando")]
    [Tooltip("Quanto o dedo precisa andar na horizontal, em polegadas, para o arraste valer uma troca de faixa. " +
             "Em polegadas e não em pixels para o gesto ter o mesmo tamanho em qualquer densidade de tela.")]
    [SerializeField, Min(0.05f)] float swipeInchesPerLane = 0.18f;

    [Header("Movimento")]
    [Tooltip("Velocidade da troca de faixa, em unidades de mundo por segundo.")]
    [SerializeField, Min(1f)] float laneChangeSpeed = 12f;

    [Tooltip("Inclinação da nave enquanto ela se desloca de lado, em graus. Zero desliga.")]
    [SerializeField] float bankAngle = 18f;

    /// <summary>Faixa de destino atual. A nave pode ainda estar deslizando até ela.</summary>
    public int CurrentLane { get; private set; }

    /// <summary>
    /// A nave ainda está deslizando até a faixa de destino — quer dizer, está
    /// **no meio de uma troca**, e não parada numa faixa.
    ///
    /// Existe para a arma poder segurar o tiro durante a troca, que é o que
    /// transforma "ficar na faixa" e "sair dela" numa escolha em vez de duas
    /// coisas que acontecem sozinhas. Ver <see cref="ShipWeapon"/>.
    /// </summary>
    public bool IsChangingLane { get; private set; }

    /// <summary>Disparado quando a faixa de destino muda, com o índice novo.</summary>
    public event Action<int> LaneChanged;

    float dragInches;
    float targetX;
    bool wasPressing;

    /// <summary>Este arraste já valeu a faixa dele. Só zera quando o dedo sai da tela.</summary>
    bool gestureSpent;

    /// <summary>O dedo desceu em cima da UI (botão de menu): o gesto não é comando de nave.</summary>
    bool gestureIgnored;

    void Awake()
    {
        if (track == null)
            track = LaneTrack.Instance != null ? LaneTrack.Instance : FindAnyObjectByType<LaneTrack>();
    }

    void Start()
    {
        if (track == null)
        {
            Debug.LogError("[Nave] Nenhum LaneTrack na cena. A nave não tem faixas para onde ir.", this);
            enabled = false;
            return;
        }

        CurrentLane = track.ClampLane(startingLane < 0 ? track.CenterLane : startingLane);
        targetX = track.LaneCenterX(CurrentLane);
        SnapToTarget();
    }

    void OnDisable()
    {
        EndGesture();
    }

    void Update()
    {
        ReadInput();
        MoveTowardsLane();
    }

    void ReadInput()
    {
        var input = TouchInput.Instance;

        // Time.timeScale em zero é o jogo pausado (menu aberto): o arraste de
        // quem está mexendo no menu não pode virar comando de nave.
        if (input == null || !input.IsPressing || Time.timeScale <= 0f)
        {
            EndGesture();
            return;
        }

        if (!wasPressing)
        {
            // Cada toque novo começa a contar o arraste do zero: sobra do gesto
            // anterior não pode virar troca de faixa fantasma ao encostar o dedo.
            wasPressing = true;
            dragInches = 0f;
            gestureSpent = false;
            gestureIgnored = input.IsOverUI();
        }

        if (gestureIgnored || gestureSpent)
            return;

        dragInches += input.InchDelta.x;
        if (Mathf.Abs(dragInches) < swipeInchesPerLane)
            return;

        // Uma faixa por arraste. O gesto se esgota aqui mesmo quando a nave já
        // está na ponta e não tem para onde ir — assim continuar arrastando
        // "contra a parede" não deixa crédito acumulado para o outro lado.
        gestureSpent = true;
        MoveLane(dragInches > 0f ? 1 : -1);
    }

    void EndGesture()
    {
        dragInches = 0f;
        wasPressing = false;
        gestureSpent = false;
        gestureIgnored = false;
    }

    void MoveLane(int step)
    {
        int next = track.ClampLane(CurrentLane + step);
        if (next == CurrentLane)
            return;

        CurrentLane = next;
        targetX = track.LaneCenterX(CurrentLane);
        LaneChanged?.Invoke(CurrentLane);
    }

    void MoveTowardsLane()
    {
        var position = transform.position;
        float x = Mathf.MoveTowards(position.x, targetX, laneChangeSpeed * Time.deltaTime);
        transform.position = new Vector3(x, position.y, position.z);

        // Chegou quando o MoveTowards grudou no destino — ele encaixa exato, então
        // não é preciso tolerância generosa aqui, só proteção contra ruído de float.
        IsChangingLane = Mathf.Abs(targetX - x) > 0.0001f;

        if (bankAngle <= 0f)
            return;

        // Inclina proporcional ao que falta andar, então a nave endireita sozinha
        // ao chegar na faixa, sem precisar de estado extra.
        float remaining = Mathf.Clamp((targetX - x) / track.LaneWidth, -1f, 1f);
        transform.rotation = Quaternion.Euler(0f, 0f, -remaining * bankAngle);
    }

    void SnapToTarget()
    {
        var position = transform.position;
        transform.position = new Vector3(targetX, position.y, position.z);
        transform.rotation = Quaternion.identity;
        IsChangingLane = false;
    }

    /// <summary>Manda a nave para uma faixa direto, sem gesto. Para cutscene, respawn e testes.</summary>
    public void SetLane(int index, bool instant = false)
    {
        if (track == null)
            return;

        CurrentLane = track.ClampLane(index);
        targetX = track.LaneCenterX(CurrentLane);
        LaneChanged?.Invoke(CurrentLane);

        if (instant)
            SnapToTarget();
    }
}
