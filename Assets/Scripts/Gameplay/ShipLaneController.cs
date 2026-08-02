using System;
using UnityEngine;

/// <summary>
/// Move a nave entre as faixas do <see cref="LaneTrack"/>. O comando é arrastar
/// o dedo para a esquerda ou para a direita; cada tanto de arraste vale uma
/// faixa, então um arraste longo e contínuo atravessa várias sem soltar o dedo.
/// </summary>
public class ShipLaneController : MonoBehaviour
{
    [Header("Faixas")]
    [Tooltip("De onde vêm as posições das faixas. Vazio: procura o LaneTrack da cena.")]
    [SerializeField] LaneTrack track;

    [Tooltip("Faixa em que a nave começa. Negativo: começa na faixa do meio.")]
    [SerializeField] int startingLane = -1;

    [Header("Comando")]
    [Tooltip("Quanto o dedo precisa andar na horizontal, em polegadas, para trocar de faixa. " +
             "Em polegadas e não em pixels para o gesto ter o mesmo tamanho em qualquer densidade de tela.")]
    [SerializeField, Min(0.05f)] float swipeInchesPerLane = 0.18f;

    [Header("Movimento")]
    [Tooltip("Velocidade da troca de faixa, em unidades de mundo por segundo.")]
    [SerializeField, Min(1f)] float laneChangeSpeed = 12f;

    [Tooltip("Inclinação da nave enquanto ela se desloca de lado, em graus. Zero desliga.")]
    [SerializeField] float bankAngle = 18f;

    /// <summary>Faixa de destino atual. A nave pode ainda estar deslizando até ela.</summary>
    public int CurrentLane { get; private set; }

    /// <summary>Disparado quando a faixa de destino muda, com o índice novo.</summary>
    public event Action<int> LaneChanged;

    float dragInches;
    float targetX;
    bool wasPressing;

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
        dragInches = 0f;
        wasPressing = false;
    }

    void Update()
    {
        ReadInput();
        MoveTowardsLane();
    }

    void ReadInput()
    {
        var input = TouchInput.Instance;
        if (input == null || !input.IsPressing)
        {
            dragInches = 0f;
            wasPressing = false;
            return;
        }

        // Cada toque novo começa a contar o arraste do zero: sobra do gesto
        // anterior não pode virar troca de faixa fantasma ao encostar o dedo.
        if (!wasPressing)
            dragInches = 0f;

        wasPressing = true;
        dragInches += input.InchDelta.x;

        // While, e não if: num arraste rápido o dedo anda mais de uma faixa entre
        // dois frames, e o gesto tem de valer as duas.
        while (Mathf.Abs(dragInches) >= swipeInchesPerLane)
        {
            int step = dragInches > 0f ? 1 : -1;
            dragInches -= step * swipeInchesPerLane;

            if (!TryMoveLane(step))
            {
                // Já está na faixa da ponta. Zera para o jogador não precisar
                // "desfazer" um arraste acumulado contra a parede antes de voltar.
                dragInches = 0f;
                break;
            }
        }
    }

    bool TryMoveLane(int step)
    {
        int next = track.ClampLane(CurrentLane + step);
        if (next == CurrentLane)
            return false;

        CurrentLane = next;
        targetX = track.LaneCenterX(CurrentLane);
        LaneChanged?.Invoke(CurrentLane);
        return true;
    }

    void MoveTowardsLane()
    {
        var position = transform.position;
        float x = Mathf.MoveTowards(position.x, targetX, laneChangeSpeed * Time.deltaTime);
        transform.position = new Vector3(x, position.y, position.z);

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
