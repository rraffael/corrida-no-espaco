using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Desenha um placar: o topo, as reticências e o jogador com os vizinhos — a
/// regra de quais posições aparecem é do <see cref="LeaderboardLayout"/>.
///
/// **Uma <see cref="LeaderboardRow"/> por posição** *(pedido do Raffael,
/// 18/10/2026)*. Até ali era um texto só com colunas por marcação; agora cada
/// linha é a mesma peça, clonada da linha modelo, e o que ela mostra muda num
/// lugar só. As linhas são reaproveitadas de um desenho para o outro, e a linha
/// só reescreve o texto quando o conteúdo dela mudou — é o que deixa a subida
/// leve com cem linhas na tela.
///
/// **A subida** *(pedido do Raffael, 17/10/2026)*: ao concluir uma corrida, a
/// linha do jogador sai da posição antiga e sobe até a nova, com o número
/// contando e a rolagem acompanhando. Corre em tempo real porque o fim da
/// corrida pausa o jogo.
/// </summary>
public class LeaderboardView : MonoBehaviour
{
    [SerializeField] ScrollRect scroll;

    [Tooltip("O cabeçalho das colunas, fora da rolagem: fica à vista enquanto a tabela rola.")]
    [SerializeField] LeaderboardRow header;

    [Tooltip("Linha modelo, desligada, dentro do conteúdo da rolagem. É clonada por posição.")]
    [SerializeField] LeaderboardRow rowTemplate;

    [Tooltip("O aviso de placar vazio.")]
    [SerializeField] TMP_Text emptyLabel;

    [SerializeField] string emptyMessage = "Ninguém completou ainda.\nO primeiro tempo pode ser o seu.";

    [Tooltip("Quanto a subida dura, em segundos de tempo real.")]
    [SerializeField, Min(0.2f)] float climbSeconds = 1.6f;

    [Tooltip("Espaço entre uma linha e a outra.")]
    [SerializeField, Min(0f)] float rowSpacing = 4f;

    /// <summary>O aviso de placar vazio. O painel do troféu troca quando abre a sem fim.</summary>
    public string EmptyMessage
    {
        get => emptyMessage;
        set => emptyMessage = value;
    }

    readonly List<ScoreBoard.Entry> others = new List<ScoreBoard.Entry>();
    readonly List<LeaderboardRow> pool = new List<LeaderboardRow>();
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

        if (header != null)
            header.BindHeader(byDistance);
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
        int total = others.Count + (player != null ? 1 : 0);

        if (emptyLabel != null)
        {
            emptyLabel.text = emptyMessage;
            emptyLabel.gameObject.SetActive(total == 0);
        }

        if (header != null)
            header.gameObject.SetActive(total > 0);

        int used = 0;
        int playerLine = -1;

        if (total > 0)
        {
            foreach (int rank in LeaderboardLayout.Slots(total, player != null ? playerRank : 0))
            {
                if (rank == LeaderboardLayout.Gap)
                {
                    Row(used++).BindGap();
                    continue;
                }

                bool isPlayer = player != null && rank == playerRank;
                var entry = isPlayer ? player : OtherAt(rank, playerRank);
                if (entry == null)
                    continue;

                if (isPlayer)
                    playerLine = used;

                Row(used++).Bind(rank, entry, byDistance, isPlayer);
            }
        }

        for (int i = used; i < pool.Count; i++)
        {
            if (pool[i].gameObject.activeSelf)
                pool[i].gameObject.SetActive(false);
        }

        FitAndScroll(playerLine, used);
    }

    /// <summary>A linha de número <paramref name="index"/>, ligada; clona mais uma se faltar.</summary>
    LeaderboardRow Row(int index)
    {
        while (pool.Count <= index)
        {
            var created = Instantiate(rowTemplate, rowTemplate.transform.parent, false);
            created.name = $"Linha {pool.Count + 1}";
            pool.Add(created);
        }

        var row = pool[index];
        if (!row.gameObject.activeSelf)
            row.gameObject.SetActive(true);

        // Posição pela conta, e não por grupo de layout: a rolagem até o
        // jogador precisa saber onde cada linha está no mesmo quadro.
        var rect = row.Rect;
        rect.anchoredPosition = new Vector2(0f, -index * Step());
        return row;
    }

    float RowHeight() => rowTemplate != null ? rowTemplate.Rect.rect.height : 48f;

    float Step() => RowHeight() + rowSpacing;

    /// <summary>A linha de outro jogador na posição de tela, pulando a do jogador.</summary>
    ScoreBoard.Entry OtherAt(int rank, int playerRank)
    {
        int index = rank - 1;
        if (player != null && playerRank > 0 && rank > playerRank)
            index--;

        return index >= 0 && index < others.Count ? others[index] : null;
    }

    /// <summary>
    /// Ajusta a altura do conteúdo às linhas e rola até a do jogador, com ela no
    /// meio da janela.
    /// </summary>
    void FitAndScroll(int playerLine, int lines)
    {
        if (scroll == null || scroll.content == null)
            return;

        float height = lines > 0 ? lines * Step() - rowSpacing : 0f;
        var content = scroll.content;
        content.sizeDelta = new Vector2(content.sizeDelta.x, height);

        float viewport = scroll.viewport != null ? scroll.viewport.rect.height : 0f;
        float room = Mathf.Max(0f, height - viewport);

        float y = 0f;
        if (playerLine >= 0)
            y = Mathf.Clamp(playerLine * Step() - (viewport - RowHeight()) * 0.5f, 0f, room);

        scroll.StopMovement();
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, y);
    }

    void Awake()
    {
        if (rowTemplate != null)
            rowTemplate.gameObject.SetActive(false);
    }
}
