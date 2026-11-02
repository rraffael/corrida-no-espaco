using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// O carimbo de uma rodada do simulador: uma impressão do código, das fichas, da
/// cena e do plano. Dois relatórios com o mesmo carimbo mediram o mesmo jogo.
///
/// **Tirado na largada e conferido no fim**, para acusar um arquivo salvo no meio
/// da noite. O código do Editor fica de fora: mudar o relatório não muda a corrida.
/// </summary>
static class SimulationStamp
{
    public static string Take(string planJson) =>
        $"código {Files("Assets/Scripts", "*.cs")} · " +
        $"fichas {Files("Assets", "*.asset")} · " +
        $"cena {OfBytes(File.ReadAllBytes("Assets/Scenes/Game.unity"))} · " +
        $"plano {Of(planJson)}";

    /// <summary>Impressão curta de um texto.</summary>
    public static string Of(string text) => OfBytes(Encoding.UTF8.GetBytes(text));

    static string Files(string folder, string pattern)
    {
        using (var buffer = new MemoryStream())
        {
            // Em ordem fixa, e com o caminho junto: renomear também muda o carimbo.
            var paths = Directory.GetFiles(folder, pattern, SearchOption.AllDirectories)
                .Select(path => path.Replace('\\', '/'))
                .OrderBy(path => path, System.StringComparer.Ordinal);

            foreach (string path in paths)
            {
                byte[] name = Encoding.UTF8.GetBytes(path);
                buffer.Write(name, 0, name.Length);

                byte[] content = File.ReadAllBytes(path);
                buffer.Write(content, 0, content.Length);
            }

            return OfBytes(buffer.ToArray());
        }
    }

    static string OfBytes(byte[] bytes)
    {
        using (var sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(bytes);
            var text = new StringBuilder();
            for (int i = 0; i < 4; i++)
                text.Append(hash[i].ToString("x2"));

            return text.ToString();
        }
    }
}
