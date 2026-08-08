/// <summary>
/// A identidade do app na loja, num lugar só.
///
/// Existe porque o `applicationId` foi perdido uma vez sem ninguém perceber:
/// mudar o `productName` fez a Unity regerar o pacote a partir de
/// `com.&lt;companyName&gt;.&lt;productName&gt;`, e o `.aab` saiu com o nome errado
/// (08/08/2026). Com o valor numa constante, o `PreflightCheck` consegue exigir
/// que ele seja exatamente este — e o erro passa a gritar antes do build, em vez
/// de aparecer como recusa da Play.
///
/// **Este valor é permanente.** Depois de o app ser publicado em qualquer trilha,
/// o pacote não muda mais: trocar significa app novo, sem histórico e sem os
/// testadores.
/// </summary>
static class ProjectIdentity
{
    public const string ApplicationId = "br.com.raffael.corridanoespaco";
}
