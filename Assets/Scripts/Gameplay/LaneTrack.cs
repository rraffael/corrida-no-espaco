using UnityEngine;

/// <summary>
/// Geometria das faixas do corredor. É a fonte única de "onde fica a faixa N":
/// nave, obstáculos e cenário perguntam aqui em vez de cada um carregar a sua
/// própria constante e sair do lugar quando a largura mudar.
/// </summary>
[DefaultExecutionOrder(-50)]
public class LaneTrack : MonoBehaviour
{
    public static LaneTrack Instance { get; private set; }

    [Tooltip("Quantas faixas o corredor tem. O jogo foi desenhado para 3.")]
    [SerializeField, Min(2)] int laneCount = 3;

    [Tooltip("Distância, em unidades de mundo, entre os centros de duas faixas vizinhas.")]
    [SerializeField, Min(0.1f)] float laneWidth = 1.6f;

    public int LaneCount => laneCount;
    public float LaneWidth => laneWidth;

    /// <summary>Faixa do meio. Com número par de faixas, cai na de cima das duas centrais.</summary>
    public int CenterLane => laneCount / 2;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int ClampLane(int index) => Mathf.Clamp(index, 0, laneCount - 1);

    /// <summary>
    /// X do centro da faixa, em mundo. As faixas ficam distribuídas em torno da
    /// posição deste objeto, então mover o Track leva o corredor inteiro junto.
    /// </summary>
    public float LaneCenterX(int index)
    {
        float offset = (index - (laneCount - 1) * 0.5f) * laneWidth;
        return transform.position.x + offset;
    }

    /// <summary>Largura total do corredor, da borda externa de uma ponta à da outra.</summary>
    public float TrackWidth => laneCount * laneWidth;

    /// <summary>Faixa cujo centro está mais perto de um X qualquer. Útil para spawn e colisão.</summary>
    public int NearestLane(float worldX)
    {
        float relative = (worldX - transform.position.x) / laneWidth + (laneCount - 1) * 0.5f;
        return ClampLane(Mathf.RoundToInt(relative));
    }

    // Desenha o corredor na Scene view mesmo com o jogo parado, para dar para
    // conferir se as faixas cabem no enquadramento da câmera sem entrar em Play.
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.5f);
        for (int i = 0; i < laneCount; i++)
        {
            float x = LaneCenterX(i);
            Gizmos.DrawLine(new Vector3(x, -20f, 0f), new Vector3(x, 20f, 0f));
        }
    }
}
