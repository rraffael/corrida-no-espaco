using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Um cartão do Compêndio: imagem e nome em cima, e embaixo descrição,
/// atributos, poder e extra — o modelo que o Raffael desenhou, igual para nave,
/// modificador e obstáculo. Seção sem conteúdo some, e o grupo de layout fecha
/// o buraco.
///
/// A altura do cartão é a dos textos: um <see cref="VerticalLayoutGroup"/> no
/// cartão soma as seções, e o da lista empilha os cartões.
/// </summary>
public class CompendiumCard : MonoBehaviour
{
    [SerializeField] Image image;
    [SerializeField] TMP_Text titleLabel;
    [SerializeField] TMP_Text subtitleLabel;
    [SerializeField] TMP_Text descriptionLabel;
    [SerializeField] TMP_Text attributesLabel;
    [SerializeField] TMP_Text powerLabel;
    [SerializeField] TMP_Text extraLabel;

    public void Bind(Compendium.Entry entry)
    {
        if (image != null)
        {
            image.sprite = entry.Image;
            image.color = entry.Tint;
            image.enabled = entry.Image != null;
        }

        if (titleLabel != null)
            titleLabel.text = entry.Title;

        if (subtitleLabel != null)
            subtitleLabel.text = entry.Subtitle;

        Section(descriptionLabel, null, entry.Description);
        Section(attributesLabel, "Atributos", entry.Attributes);
        Section(powerLabel, "Poder", entry.Power);
        Section(extraLabel, "Extra", entry.Extra);
    }

    static void Section(TMP_Text label, string heading, string body)
    {
        if (label == null)
            return;

        bool shown = !string.IsNullOrWhiteSpace(body);
        label.gameObject.SetActive(shown);
        if (!shown)
            return;

        label.text = heading == null ? body : $"<b><color=#FFD65A>{heading}</color></b>\n{body}";
    }
}
