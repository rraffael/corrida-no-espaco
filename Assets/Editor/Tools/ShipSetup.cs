using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Cria a ficha da nave inicial em <c>Assets/Ships/</c>. Depois disto, os números
/// da nave são um arquivo de dados: mexer neles é editar no Inspector, e uma
/// segunda nave é um segundo arquivo.
///
/// Roda sem apagar o que já existe: se você tiver ajustado os números à mão,
/// rodar de novo não encosta neles.
/// </summary>
static class ShipSetup
{
    public const string ShipFolder = "Assets/Ships";
    public const string StarterShipPath = ShipFolder + "/NaveInicial.asset";

    [MenuItem(ProjectTools.ShipsItem, false, 105)]
    internal static void Setup()
    {
        var ship = GetOrCreateStarter();

        AssetDatabase.SaveAssets();
        ProjectTools.MarkRun(ProjectTools.ShipsId);

        Debug.Log($"[Nave] Ficha pronta em {StarterShipPath} — cruzeiro {ship.CruiseSpeed}, " +
                  $"aceleração {ship.Acceleration}, vida {ship.MaxHealth}, defesa {ship.DefensePercent}%. " +
                  "Rode o 'Montar combate' na sequência para ligá-la na nave da cena.");
    }

    /// <summary>
    /// A ficha inicial. **Não preenche campo nenhum de propósito:** os valores de
    /// fábrica do <see cref="ShipDefinition"/> já são os números com que o jogo
    /// foi equilibrado e aprovado no aparelho, então duplicá-los aqui criaria
    /// dois lugares para manter iguais — e um deles ia ficar para trás.
    /// </summary>
    internal static ShipDefinition GetOrCreateStarter()
    {
        var existing = AssetDatabase.LoadAssetAtPath<ShipDefinition>(StarterShipPath);
        if (existing != null)
            return existing;

        // A pasta é criada aqui, e não só no Setup: o BattleSetup chama este
        // método direto, e nesse caminho ninguém garantiu que ela existe.
        Directory.CreateDirectory(ShipFolder);
        AssetDatabase.Refresh();

        var created = ScriptableObject.CreateInstance<ShipDefinition>();
        created.displayName = "Nave inicial";

        AssetDatabase.CreateAsset(created, StarterShipPath);
        return created;
    }
}
