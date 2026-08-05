using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Escreve a tabela de recordes num único TextMesh Pro. Um texto só, e não uma
/// linha por objeto: a lista é curta, e assim não há prefab de linha para
/// manter nem lixo de instanciação ao abrir o painel.
///
/// Serve tanto ao painel do menu (10 linhas) quanto à transição antes da
/// partida (3 linhas) — muda só o <c>maxRows</c>.
/// </summary>
public class RecordsBoard : MonoBehaviour
{
    [Tooltip("Onde escrever. Vazio: procura um TextMesh Pro neste objeto ou nos filhos.")]
    [SerializeField] TMP_Text target;

    [Tooltip("Quantas posições mostrar.")]
    [SerializeField, Min(1)] int maxRows = 10;

    [SerializeField] string emptyMessage = "Ainda sem recordes.\nEntre em dobra para inaugurar a tabela.";

    void Awake()
    {
        if (target == null)
            target = GetComponentInChildren<TMP_Text>();
    }

    // Ao ligar, e não no Awake: o painel fica desligado até alguém abrir, e a
    // tabela pode ter mudado desde a última vez.
    void OnEnable() => Refresh();

    public void Refresh()
    {
        if (target == null)
            return;

        var records = ScoreBoard.All();
        if (records.Count == 0)
        {
            target.text = emptyMessage;
            return;
        }

        var text = new StringBuilder();
        int rows = Mathf.Min(maxRows, records.Count);

        for (int i = 0; i < rows; i++)
        {
            if (i > 0)
                text.Append('\n');

            // mspace alinha a coluna do tempo sem precisar de fonte monoespaçada.
            text.Append(i + 1).Append(". ").Append(records[i].name)
                .Append("  <mspace=0.55em>").Append(ScoreBoard.FormatTime(records[i].seconds))
                .Append("</mspace>");
        }

        target.text = text.ToString();
    }
}
