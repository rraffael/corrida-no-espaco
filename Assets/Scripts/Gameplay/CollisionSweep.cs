/// <summary>
/// A conta de "pulou por cima", dividida por tudo o que colide por distância.
///
/// **Por que existe:** o jogo só olha a distância em cada quadro. Na sem fim, a
/// velocidade cresce até uma coisa passar direto pela nave de um quadro para o
/// outro, sem nunca estar perto dela nas duas fotos, e a batida não acontecia.
///
/// **Só conta a janela pulada inteira:** antes acima dela, agora abaixo dela. Por
/// isso, abaixo da velocidade do salto nada muda. Testar o trecho inteiro seria
/// mais simples, mas daria batida em quem entrou na faixa logo atrás do obstáculo.
/// </summary>
public static class CollisionSweep
{
    /// <summary>Colisões decididas por esta conta desde o último zero. É o simulador que lê.</summary>
    public static int Hits { get; private set; }

    /// <param name="before">Altura em relação ao alvo no quadro anterior.</param>
    /// <param name="now">Altura em relação ao alvo agora.</param>
    /// <param name="reach">Alcance da colisão, para cima e para baixo.</param>
    public static bool Jumped(float before, float now, float reach) => before > reach && now < -reach;

    public static void Note() => Hits++;

    public static void ResetHits() => Hits = 0;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnLoad() => Hits = 0;
}
