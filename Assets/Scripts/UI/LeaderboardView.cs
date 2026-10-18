using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Desenha um placar: o topo, as reticências e o jogador com os vizinhos — a
/// regra de quais posições aparecem é do <see cref="LeaderboardLayout"/>.
///
/// **Um texto só dentro de uma rolagem**, como o <see cref="RecordsBoard"/> já
/// fazia: cem linhas de texto não pesam, e cem objetos de linha pesariam ao
/// abrir e ao animar.
///
/// **A subida** *(pedido do Raffael, 17/10/2026)*: ao concluir uma corrida, a
/// linha do jogador sai da posição antiga e sobe até a nova, com o número
/// contando e a rolagem acompanhando. Corre em tempo real porque o fim da
/// corrida pausa o jogo.
/// </summary>
public class LeaderboardView : MonoBehaviour
{
    [SerializeField] ScrollRect scroll;
    [SerializeField] TMP_Text target;

    [SerializeField] string emptyMessage = "Ninguém completou ainda.\nO primeiro tempo pode ser o seu.";

    [Tooltip("Quanto a subida dura, em segundos de tempo real.")]
    [SerializeField, Min(0.2f)] float climbSeconds = 1.6f;

    [SerializeField] Color highlightColor = new Color(1f, 0.84f, 0.3f, 1f);
    [SerializeField] Color headerColor = new Color(0.68f, 0.76f, 0.9f, 1f);

    // Colunas em porcentagem da largura: a mesma tela serve ao painel do menu e
    // ao de fim de corrida, que não têm a mesma largura.
    const string ShipColumn = "<pos=43%>";
    const string ScoreColumn = "<pos=72%>";
    const string KillsColumn = "<pos=90%>";
    const string NameColumn = "<pos=11%>";
    const int NameLength = 12;

    readonly List<ScoreBoard.Entry> others = new List<ScoreBoard.Entry>();
    ScoreBoard.Entry player;
    bool byDistance;
    Coroutine climb;

    /// <summary>
    /// Mostra o placar parado. Sem <paramref name="highlight"/>, o destaque vai
    /// para a melhor corrida do piloto, se ele tiver uma ali.
    /// </summary>
    public void Show(string boardId, ScoreBoard.Entry highlight = null)
    {
        Load(boardId, highlight ?? ScoreBoard.BestOf(boardId, ScoreBoard.PilotName));
        Render(RankOfPlayer());
    }

    /// <summary>
    /// Mostra a corrida que acabou de entrar subindo de <paramref name="fromRank"/>
    /// até onde ela caiu. Precisa do objeto ligado: a subida é uma corrotina.
    /// </summary>
    public void PlayClimb(string boardId, ScoreBoard.Entry entry, int fromRank)
    {
        Load(boardId, entry);
        int to = RankOfPlayer();

        // Placar cheio: a última linha saiu para a nova entrar, então "o fim da
        // tabela" é uma posição a menos do que a conta de antes dizia.
        fromRank = Mathf.Min(fromRank, others.Count + 1);

        if (climb != null)
            StopCoroutine(climb);

        if (to <= 0 || fromRank <= to || !isActiveAndEnabled)
        {
            Render(to);
            return;
        }

        climb = StartCoroutine(Climb(fromRank, to));
    }

    IEnumerator Climb(int from, int to)
    {
        // Começa parado na posição antiga por um instante: sem isso o olho não
        // registra de onde a linha saiu.
        Render(from);
        yield return new WaitForSecondsRealtime(0.35f);

        for (float t = 0f; t < climbSeconds; t += Time.unscaledDeltaTime)
        {
            // Rápido no começo e freando no fim, para a posição nova assentar.
            float eased = 1f - Mathf.Pow(1f - t / climbSeconds, 3f);
            Render(Mathf.RoundToInt(Mathf.Lerp(from, to, eased)));
            yield return null;
        }

        Render(to);
        climb = null;
    }

    void Load(string boardId, ScoreBoard.Entry highlight)
    {
        byDistance = ScoreBoard.IsDistanceBoard(boardId);
        others.Clear();
        player = null;

        foreach (var entry in ScoreBoard.Board(boardId))
        {
            if (entry == null)
                continue;

            if (entry == highlight)
                player = entry;
            else
                others.Add(entry);
        }
    }

    int RankOfPlayer()
    {
        if (player == null)
            return 0;

        // As outras linhas já estão em ordem; o jogador entra na frente da
        // primeira que ele vence, como no ScoreBoard.
        return ScoreBoard.InsertionIndex(others, player, byDistance) + 1;
    }

    /// <summary>Desenha com o jogador na posição <paramref name="playerRank"/> e rola até ele.</summary>
    void Render(int playerRank)
    {
        if (target == null)
            return;

        int total = others.Count + (player != null ? 1 : 0);
        if (total == 0)
        {
            target.text = emptyMessage;
            FitAndScroll(-1, 1);
            return;
        }

        var text = new StringBuilder();
        AppendHeader(text);
        int lines = 1;
        int playerLine = -1;

        foreach (int rank in LeaderboardLayout.Slots(total, player != null ? playerRank : 0))
        {
            if (rank == LeaderboardLayout.Gap)
            {
                // Três pontos em pé: o "tem muito mais gente aqui no meio".
                text.Append("\n<align=center>.\n.\n.</align>");
                lines += 3;
                continue;
            }

            bool isPlayer = player != null && rank == playerRank;
            var entry = isPlayer ? player : OtherAt(rank, playerRank);
            if (entry == null)
                continue;

            text.Append('\n');
            if (isPlayer)
            {
                playerLine = lines;
                text.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(highlightColor)).Append("><b>");
            }

            AppendRow(text, rank, entry);

            if (isPlayer)
                text.Append("</b></color>");

            lines++;
        }

        target.text = text.ToString();
        FitAndScroll(playerLine, lines);
    }

    /// <summary>A linha de outro jogador na posição de tela, pulando a do jogador.</summary>
    ScoreBoard.Entry OtherAt(int rank, int playerRank)
    {
        int index = rank - 1;
        if (player != null && playerRank > 0 && rank > playerRank)
            index--;

        return index >= 0 && index < others.Count ? others[index] : null;
    }

    void AppendHeader(StringBuilder text)
    {
        text.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(headerColor)).Append('>')
            .Append('#').Append(NameColumn).Append("Piloto")
            .Append(ShipColumn).Append("Nave");

        if (byDistance)
            text.Append(ScoreColumn).Append("Distância");
        else
            text.Append(ScoreColumn).Append("Tempo").Append(KillsColumn).Append("Abates");

        text.Append("</color>");
    }

    void AppendRow(StringBuilder text, int rank, ScoreBoard.Entry entry)
    {
        string name = string.IsNullOrEmpty(entry.name) ? "Piloto" : entry.name;
        if (name.Length > NameLength)
            name = name.Substring(0, NameLength);

        // O nome é digitado pelo jogador: sem o noparse, um "<" nele viraria
        // marcação e desmontaria as colunas. A nave vai um pouco menor, que é a
        // coluna mais apertada.
        text.Append(rank.ToString("n0")).Append(NameColumn)
            .Append("<noparse>").Append(name).Append("</noparse>")
            .Append(ShipColumn).Append("<size=85%>").Append(ScoreBoard.DescribeShip(entry)).Append("</size>");

        if (byDistance)
            text.Append(ScoreColumn).Append(ScoreBoard.FormatDistance(entry.distance));
        else
            text.Append(ScoreColumn).Append(ScoreBoard.FormatTime(entry.time))
                .Append(KillsColumn).Append(entry.kills);
    }

    /// <summary>
    /// Ajusta a altura do conteúdo ao texto e rola até a linha do jogador, com
    /// ela no meio da janela. Todas as linhas têm a mesma altura — o texto não
    /// quebra —, então a posição sai de uma conta, sem medir linha a linha.
    /// </summary>
    void FitAndScroll(int playerLine, int lines)
    {
        if (scroll == null || scroll.content == null)
            return;

        float height = target.preferredHeight;
        var content = scroll.content;
        content.sizeDelta = new Vector2(content.sizeDelta.x, height);

        float viewport = scroll.viewport != null ? scroll.viewport.rect.height : 0f;
        float room = Mathf.Max(0f, height - viewport);

        float y = 0f;
        if (playerLine >= 0 && lines > 0)
        {
            float lineHeight = height / lines;
            y = Mathf.Clamp(playerLine * lineHeight - (viewport - lineHeight) * 0.5f, 0f, room);
        }

        scroll.StopMovement();
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, y);
    }
}
