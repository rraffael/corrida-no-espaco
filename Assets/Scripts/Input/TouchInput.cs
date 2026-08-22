using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Camada única de leitura de toque, baseada no Input System novo.
/// No aparelho lê o Touchscreen; no Editor cai para o mouse, então dá para
/// testar sem gerar build. O resto do jogo consome só esta classe e nunca
/// conversa direto com Touchscreen/Mouse.
/// </summary>
[DefaultExecutionOrder(-100)]
public class TouchInput : MonoBehaviour
{
    public static TouchInput Instance { get; private set; }

    [Tooltip("Quanto o dedo pode andar, em pixels, para o toque ainda valer como tap.")]
    [SerializeField] float tapMoveTolerance = 25f;

    [Tooltip("Duração máxima, em segundos, de um toque para valer como tap.")]
    [SerializeField] float tapMaxDuration = 0.3f;

    [Tooltip("Janela máxima, em segundos, entre dois toques para valerem como toque duplo. " +
             "Curto demais e o jogador não consegue acertar; longo demais e dois toques " +
             "separados viram um duplo sem querer. Afinar no aparelho.")]
    [SerializeField] float doubleTapMaxInterval = 0.28f;

    [Tooltip("Distância máxima, em pixels, entre os dois toques de um toque duplo. Existe para " +
             "dois toques em cantos opostos da tela não contarem como um só gesto.")]
    [SerializeField] float doubleTapMaxDistance = 140f;

    /// <summary>Há pelo menos um dedo (ou o botão do mouse) pressionado agora.</summary>
    public bool IsPressing { get; private set; }

    /// <summary>Posição do toque primário em pixels de tela.</summary>
    public Vector2 ScreenPosition { get; private set; }

    /// <summary>Quanto o toque primário andou desde o frame anterior, em pixels.</summary>
    public Vector2 ScreenDelta { get; private set; }

    /// <summary>Dedos na tela. Sempre 0 ou 1 quando o input vem do mouse.</summary>
    public int ActiveTouchCount { get; private set; }

    /// <summary>
    /// Arraste convertido para polegadas. É isto que a jogabilidade deve usar:
    /// pixels variam com a densidade do aparelho, polegadas não, então o
    /// controle fica com o mesmo "peso" num celular 720p e num 1440p.
    /// </summary>
    public Vector2 InchDelta => ScreenDelta / (Screen.dpi > 0f ? Screen.dpi : 160f);

    public event Action<Vector2> Pressed;
    public event Action<Vector2> Released;
    public event Action<Vector2> Tapped;

    /// <summary>
    /// Dois toques seguidos, perto um do outro. É o gesto que ativa o poder da
    /// nave — decidido assim em 22/08/2026 para o jogador **não precisar tirar o
    /// polegar da faixa** para apertar um botão de canto, que é o pior momento
    /// possível para pedir isso.
    ///
    /// **Arraste nunca vira toque duplo**, e isso é de graça: um toque só conta
    /// como tap se o dedo tiver ficado quase parado, então quem estava trocando
    /// de faixa jamais dispara isto. Na dúvida, arraste ganha — ativar poder sem
    /// querer custa muito mais que um toque duplo ignorado.
    /// </summary>
    public event Action<Vector2> DoubleTapped;

    Vector2 pressStartPosition;
    float pressStartTime;
    bool hasPreviousPosition;

    float lastTapTime = float.NegativeInfinity;
    Vector2 lastTapPosition;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnEnable()
    {
        // Enable/Disable são contados por referência, então não atrapalha
        // outros sistemas que também usem EnhancedTouch.
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        IsPressing = false;
        ScreenDelta = Vector2.zero;
        ActiveTouchCount = 0;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        Vector2 position;
        bool pressing;

        if (!TryReadTouchscreen(out position, out pressing) &&
            !TryReadMouse(out position, out pressing))
        {
            // Nenhum dispositivo de apontamento disponível neste frame.
            position = ScreenPosition;
            pressing = false;
        }

        UpdateState(position, pressing);
    }

    bool TryReadTouchscreen(out Vector2 position, out bool pressing)
    {
        position = ScreenPosition;
        pressing = false;

        if (Touchscreen.current == null)
        {
            ActiveTouchCount = 0;
            return false;
        }

        var touches = ETouch.activeTouches;
        ActiveTouchCount = touches.Count;

        // Existe touchscreen mas nenhum dedo encostado: ainda assim é a fonte
        // válida de input, então não caímos para o mouse.
        if (touches.Count == 0)
            return true;

        var primary = touches[0];
        position = primary.screenPosition;
        pressing = primary.phase != UnityEngine.InputSystem.TouchPhase.Ended
                && primary.phase != UnityEngine.InputSystem.TouchPhase.Canceled;
        return true;
    }

    bool TryReadMouse(out Vector2 position, out bool pressing)
    {
        var mouse = Mouse.current;
        if (mouse == null)
        {
            position = ScreenPosition;
            pressing = false;
            return false;
        }

        position = mouse.position.ReadValue();
        pressing = mouse.leftButton.isPressed;
        ActiveTouchCount = pressing ? 1 : 0;
        return true;
    }

    void UpdateState(Vector2 position, bool pressing)
    {
        // Só há delta válido entre dois frames do mesmo arraste; ao encostar o
        // dedo o delta precisa ser zero, senão a nave dá um salto.
        ScreenDelta = pressing && hasPreviousPosition ? position - ScreenPosition : Vector2.zero;
        ScreenPosition = position;

        if (pressing && !IsPressing)
        {
            pressStartPosition = position;
            pressStartTime = Time.unscaledTime;
            Pressed?.Invoke(position);
        }
        else if (!pressing && IsPressing)
        {
            Released?.Invoke(position);

            bool quickEnough = Time.unscaledTime - pressStartTime <= tapMaxDuration;
            bool stillEnough = Vector2.Distance(position, pressStartPosition) <= tapMoveTolerance;
            if (quickEnough && stillEnough)
            {
                Tapped?.Invoke(position);
                CheckDoubleTap(position);
            }
        }

        IsPressing = pressing;
        hasPreviousPosition = pressing;
    }

    /// <summary>
    /// Um tap acabou de acontecer: fecha um toque duplo com o anterior, ou vira
    /// o primeiro de um novo par.
    ///
    /// **O tempo é o não escalado**, como o resto da detecção de toque: com o
    /// jogo pausado o relógio da partida para, e um gesto ficaria pela metade
    /// esperando um segundo toque que nunca "chega" no tempo certo.
    /// </summary>
    void CheckDoubleTap(Vector2 position)
    {
        bool inWindow = Time.unscaledTime - lastTapTime <= doubleTapMaxInterval;
        bool nearby = Vector2.Distance(position, lastTapPosition) <= doubleTapMaxDistance;

        if (inWindow && nearby)
        {
            DoubleTapped?.Invoke(position);

            // Zera em vez de guardar este toque: sem isso, três toques seguidos
            // disparariam dois gestos, e o terceiro sairia de graça.
            lastTapTime = float.NegativeInfinity;
            return;
        }

        lastTapTime = Time.unscaledTime;
        lastTapPosition = position;
    }

    /// <summary>
    /// Posição do toque no mundo. Assume jogo 2D com a câmera olhando para o
    /// plano z = 0, que é o caso das cenas atuais.
    /// </summary>
    public Vector3 WorldPosition()
    {
        var cam = Camera.main;
        if (cam == null)
            return Vector3.zero;

        float distance = cam.orthographic ? -cam.transform.position.z : 10f;
        var world = cam.ScreenToWorldPoint(new Vector3(ScreenPosition.x, ScreenPosition.y, distance));
        world.z = 0f;
        return world;
    }

    /// <summary>
    /// True quando o toque está em cima de um elemento de UI. Use para não
    /// atirar quando o jogador só quis apertar um botão de HUD.
    /// </summary>
    public bool IsOverUI()
    {
        var events = EventSystem.current;
        return events != null && events.IsPointerOverGameObject();
    }
}
