using UnityEngine;

/// <summary>
/// Validação de toque no aparelho. Desenha um marcador embaixo do dedo e
/// escreve na tela o que o <see cref="TouchInput"/> está lendo.
/// É ferramenta de teste: remover da cena antes de publicar.
/// </summary>
[RequireComponent(typeof(TouchInput))]
public class TouchTester : MonoBehaviour
{
    [Tooltip("Opcional. Se ficar vazio, o marcador é criado em tempo de execução.")]
    [SerializeField] Transform marker;

    [SerializeField] float markerSize = 0.6f;
    [SerializeField] Color markerColor = new Color(0.3f, 1f, 0.5f, 0.8f);

    TouchInput input;
    int tapCount;
    string lastEvent = "nenhum";
    GUIStyle style;

    void Awake()
    {
        input = GetComponent<TouchInput>();

        if (marker == null)
            marker = CreateMarker();
    }

    void OnEnable()
    {
        input.Pressed += OnPressed;
        input.Released += OnReleased;
        input.Tapped += OnTapped;
    }

    void OnDisable()
    {
        input.Pressed -= OnPressed;
        input.Released -= OnReleased;
        input.Tapped -= OnTapped;
    }

    void Update()
    {
        marker.gameObject.SetActive(input.IsPressing);

        if (input.IsPressing)
            marker.position = input.WorldPosition();
    }

    void OnPressed(Vector2 position) => lastEvent = $"Pressed  {position}";

    void OnReleased(Vector2 position) => lastEvent = $"Released {position}";

    void OnTapped(Vector2 position)
    {
        tapCount++;
        lastEvent = $"Tapped   {position}";
    }

    void OnGUI()
    {
        // IMGUI só para depuração: não exige montar Canvas nem prefab, o que
        // deixa o teste utilizável numa cena vazia.
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(Screen.height * 0.025f),
                normal = { textColor = Color.white }
            };
        }

        float margin = Screen.height * 0.03f;
        var area = new Rect(margin, margin, Screen.width - margin * 2f, Screen.height * 0.4f);

        GUILayout.BeginArea(area);
        GUILayout.Label($"dedos ativos : {input.ActiveTouchCount}", style);
        GUILayout.Label($"pressionando : {input.IsPressing}", style);
        GUILayout.Label($"tela         : {input.ScreenPosition}", style);
        GUILayout.Label($"delta px     : {input.ScreenDelta}", style);
        GUILayout.Label($"delta pol    : {input.InchDelta:F3}", style);
        GUILayout.Label($"mundo        : {input.WorldPosition()}", style);
        GUILayout.Label($"sobre UI     : {input.IsOverUI()}", style);
        GUILayout.Label($"taps         : {tapCount}", style);
        GUILayout.Label($"último       : {lastEvent}", style);
        GUILayout.Label($"tela/dpi     : {Screen.width}x{Screen.height} @ {Screen.dpi}", style);
        GUILayout.EndArea();
    }

    Transform CreateMarker()
    {
        var created = GameObject.CreatePrimitive(PrimitiveType.Quad);
        created.name = "TouchMarker";
        created.transform.SetParent(transform, false);
        created.transform.localScale = Vector3.one * markerSize;

        Destroy(created.GetComponent<Collider>());

        var renderer = created.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default")) { color = markerColor };

        return created.transform;
    }
}
