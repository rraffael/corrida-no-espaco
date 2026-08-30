/// <summary>
/// Uma coisa que a nave **é ou não é** durante a corrida, e que não cabe num
/// número. O irmão do <see cref="ShipStat"/>: lá ficam os atributos que um poder
/// aumenta ou reduz; aqui, os que ele simplesmente liga.
///
/// **Por que existe, em vez de cada poder mexer direto em quem interessa.** A
/// regra da casa é que todo mundo lê da nave: a arma pergunta a cadência ao
/// <see cref="ShipStats"/>, não à ficha nem a um poder. "Os tiros perseguem"
/// tem a mesma natureza — é um estado da nave que a arma consulta —, e sem um
/// lugar assim o poder da nave teria de alcançar a <c>ShipWeapon</c> pelo
/// <c>GetComponent</c>, e o de fase teria de fazer o mesmo por outro caminho.
/// Os dois sistemas de poder voltariam a conhecer o resto do jogo, que é
/// justamente o que a separação deles evita.
///
/// **É contado, não ligado/desligado** — ver <see cref="ShipStats.AddTrait"/>.
/// Dois poderes ligando o mesmo traço e um deles acabando não pode apagar o do
/// outro.
/// </summary>
public enum ShipTrait
{
    /// <summary>
    /// Os tiros perseguem o obstáculo mais próximo, em vez de subir reto. Vale
    /// para tiro já em voo e para o que sair depois.
    /// </summary>
    HomingShots,

    /// <summary>
    /// O jogo pilota. O arraste do jogador é ignorado e a nave escolhe sozinha
    /// para onde ir — ver <see cref="Autopilot"/>.
    /// </summary>
    Autopilot,
}
