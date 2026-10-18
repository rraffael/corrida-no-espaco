using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Uma linha do placar: posição, piloto, nave e o que ele fez na fase *(pedido
/// do Raffael, 18/10/2026: uma peça só, replicada por linha, em vez de um texto
/// com colunas por marcação)*. O <see cref="LeaderboardView"/> clona a linha
/// modelo uma vez por posição que aparece, e a mesma peça serve de cabeçalho e
/// de reticências.
///
/// **As colunas são âncoras em fração da largura**, montadas pelo
/// <c>UiBuilder</c>: a linha cabe no painel do menu e no de fim de corrida, que
/// não têm a mesma largura. Cada texto encolhe a letra antes de vazar a coluna.
///
/// **O que a última coluna mostra depende do placar:** abates nas fases, e a
/// velocidade máxima na sem fim, onde ela varia de corrida para corrida — nas
/// fases todo mundo termina no teto da dobra.
/// </summary>
public class LeaderboardRow : MonoBehaviour
{
    [SerializeField] Image background;
    [SerializeField] TMP_Text rankLabel;
    [SerializeField] TMP_Text nameLabel;
    [SerializeField] TMP_Text shipLabel;
    [SerializeField] TMP_Text scoreLabel;
    [SerializeField] TMP_Text extraLabel;

    [Header("Cores")]
    [SerializeField] Color textColor = new Color(0.92f, 0.96f, 1f, 1f);
    [SerializeField] Color headerColor = new Color(0.68f, 0.76f, 0.9f, 1f);
    [SerializeField] Color highlightColor = new Color(1f, 0.84f, 0.3f, 1f);
    [SerializeField] Color highlightBackground = new Color(1f, 0.84f, 0.3f, 0.16f);

    const int NameLength = 12;

    // O que está desenhado agora. Durante a subida a vista refaz a tabela a cada
    // quadro, e só duas linhas mudam de fato: as outras saem daqui sem tocar no
    // texto, que é o que custa no TextMesh Pro.
    enum Kind { None, Header, Gap, Entry }

    Kind shownKind;
    int shownRank;
    ScoreBoard.Entry shownEntry;
    bool shownHighlight;
    bool shownByDistance;

    public RectTransform Rect => (RectTransform)transform;

    /// <summary>O cabeçalho das colunas.</summary>
    public void BindHeader(bool byDistance)
    {
        if (shownKind == Kind.Header && shownByDistance == byDistance)
            return;

        Remember(Kind.Header, 0, null, false, byDistance);

        Set(rankLabel, "#");
        Set(nameLabel, "Piloto");
        Set(shipLabel, "Nave");
        Set(scoreLabel, byDistance ? "Distância" : "Tempo");
        Set(extraLabel, byDistance ? "Vel. máx." : "Abates");
        Paint(headerColor, FontStyles.Normal, Color.clear);
    }

    /// <summary>As reticências: "tem muito mais gente aqui no meio".</summary>
    public void BindGap()
    {
        if (shownKind == Kind.Gap)
            return;

        Remember(Kind.Gap, 0, null, false, false);

        Set(rankLabel, string.Empty);
        Set(nameLabel, "· · ·");
        Set(shipLabel, string.Empty);
        Set(scoreLabel, string.Empty);
        Set(extraLabel, string.Empty);
        Paint(headerColor, FontStyles.Normal, Color.clear);
    }

    public void Bind(int rank, ScoreBoard.Entry entry, bool byDistance, bool highlighted)
    {
        if (shownKind == Kind.Entry && shownRank == rank && shownEntry == entry &&
            shownHighlight == highlighted && shownByDistance == byDistance)
            return;

        Remember(Kind.Entry, rank, entry, highlighted, byDistance);

        string name = string.IsNullOrEmpty(entry.name) ? "Piloto" : entry.name;
        if (name.Length > NameLength)
            name = name.Substring(0, NameLength);

        Set(rankLabel, rank.ToString("n0"));
        // O nome é digitado pelo jogador: com o rich text desligado nesta
        // coluna, um "<" nele aparece como "<" e não vira marcação.
        Set(nameLabel, name);
        Set(shipLabel, ScoreBoard.DescribeShip(entry));

        if (byDistance)
        {
            Set(scoreLabel, ScoreBoard.FormatDistance(entry.distance));
            Set(extraLabel, ScoreBoard.FormatTopSpeed(entry.topSpeed));
        }
        else
        {
            Set(scoreLabel, ScoreBoard.FormatTime(entry.time));
            Set(extraLabel, entry.kills.ToString());
        }

        if (highlighted)
            Paint(highlightColor, FontStyles.Bold, highlightBackground);
        else
            Paint(textColor, FontStyles.Normal, Color.clear);
    }

    void Remember(Kind kind, int rank, ScoreBoard.Entry entry, bool highlighted, bool byDistance)
    {
        shownKind = kind;
        shownRank = rank;
        shownEntry = entry;
        shownHighlight = highlighted;
        shownByDistance = byDistance;
    }

    void Paint(Color color, FontStyles style, Color back)
    {
        foreach (var label in new[] { rankLabel, nameLabel, shipLabel, scoreLabel, extraLabel })
        {
            if (label == null)
                continue;

            label.color = color;
            label.fontStyle = style;
        }

        if (background != null)
            background.color = back;
    }

    static void Set(TMP_Text label, string text)
    {
        if (label != null)
            label.text = text;
    }
}
