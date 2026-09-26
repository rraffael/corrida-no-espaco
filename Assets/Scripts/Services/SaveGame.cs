using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Tudo o que o jogador conquistou, num objeto só.
///
/// **Um campo por coisa, e só dados** — quem sabe o que cada um significa são
/// os serviços que o usam (<see cref="LevelProgress"/>, <see cref="ShipProgress"/>,
/// <see cref="Wallet"/>, <see cref="ShipSelection"/>, <see cref="ScoreBoard"/>).
/// Campo novo entra no fim, com valor padrão, e arquivo antigo continua abrindo.
/// </summary>
[Serializable]
public class SaveData
{
    /// <summary>Versão do formato. Sobe quando um campo mudar de sentido — não quando entrar um novo.</summary>
    public int version = SaveGame.CurrentVersion;

    public int stagesCleared;
    public string ship = string.Empty;
    public int wallet;
    public List<ShipLevelEntry> shipLevels = new List<ShipLevelEntry>();
    public string lastName = string.Empty;
    public List<ScoreBoard.Entry> records = new List<ScoreBoard.Entry>();
}

[Serializable]
public class ShipLevelEntry
{
    /// <summary>Nome do asset da nave, e não o de tela — o mesmo motivo do <see cref="ShipSelection"/>.</summary>
    public string ship;
    public int level;
}

/// <summary>
/// O **salvamento num arquivo só** *(26/09/2026)*: <c>save.json</c> na pasta de
/// dados do app. Antes, cada serviço guardava o seu pedaço solto em PlayerPrefs.
///
/// **Por que um arquivo.** É o que o salvamento em nuvem da Fase 7 vai subir e
/// baixar inteiro, e é o que dá para versionar, copiar e inspecionar. Pedaços
/// soltos em PlayerPrefs não sobem juntos — e um progresso que chega pela
/// metade é pior do que nenhum.
///
/// **Gravação à prova de queda:** escreve num arquivo temporário, guarda o
/// anterior como <c>.bak</c> e só então troca. Se o app morrer no meio, sobra
/// o arquivo velho inteiro, e a leitura cai no <c>.bak</c> se o principal
/// estiver ilegível.
///
/// **Migração:** sem arquivo nenhum, o que houver nas chaves antigas de
/// PlayerPrefs é importado uma vez. As chaves antigas ficam onde estão — não
/// custam nada e servem de rede se algo der errado.
/// </summary>
public static class SaveGame
{
    public const int CurrentVersion = 1;
    const string FileName = "save.json";

    static SaveData data;

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
    static string BackupPath => FilePath + ".bak";
    static string TempPath => FilePath + ".tmp";

    /// <summary>O salvamento em memória. Lido do disco no primeiro acesso.</summary>
    public static SaveData Data
    {
        get
        {
            if (data == null)
                Load();

            return data;
        }
    }

    /// <summary>Grava o que está em memória. Os serviços chamam depois de mudar algo.</summary>
    public static void Save()
    {
        if (data == null)
            return;

        data.version = CurrentVersion;

        try
        {
            File.WriteAllText(TempPath, JsonUtility.ToJson(data, true));

            if (File.Exists(FilePath))
            {
                File.Copy(FilePath, BackupPath, true);
                File.Delete(FilePath);
            }

            File.Move(TempPath, FilePath);
        }
        catch (Exception e)
        {
            // Não conseguir gravar não pode derrubar a partida. O progresso fica
            // em memória e a próxima gravação tenta de novo.
            Debug.LogError($"[Salvamento] Não deu para gravar em {FilePath}: {e.Message}");
        }
    }

    /// <summary>
    /// Apaga o salvamento — arquivo, cópia e memória. Existe para teste: nada
    /// do jogo chama. As chaves antigas de PlayerPrefs também vão, senão a
    /// próxima abertura as importaria de volta.
    /// </summary>
    public static void Delete()
    {
        foreach (var path in new[] { FilePath, BackupPath, TempPath })
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        data = null;
    }

    static void Load()
    {
        data = Read(FilePath) ?? Read(BackupPath);
        if (data != null)
        {
            Upgrade(data);
            return;
        }

        data = ImportLegacy();
        Save();
    }

    static SaveData Read(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            var parsed = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            return parsed;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Salvamento] {Path.GetFileName(path)} ilegível ({e.Message}).");
            return null;
        }
    }

    /// <summary>
    /// Deixa um salvamento antigo no formato de agora. Lista nula vira vazia —
    /// é o que um arquivo sem o campo traz. Quando o formato mudar de sentido,
    /// a conversão de uma versão para a outra entra aqui.
    /// </summary>
    static void Upgrade(SaveData loaded)
    {
        loaded.ship ??= string.Empty;
        loaded.lastName ??= string.Empty;
        loaded.shipLevels ??= new List<ShipLevelEntry>();
        loaded.records ??= new List<ScoreBoard.Entry>();
    }

    // ── Migração das chaves antigas ──────────────────────────────────────

    [Serializable]
    class LegacyRecords
    {
        public List<ScoreBoard.Entry> entries = new List<ScoreBoard.Entry>();
    }

    /// <summary>
    /// Traz o que estava em PlayerPrefs até 26/09/2026. As chaves são as que
    /// cada serviço usava; o nível das naves é procurado nave a nave pelo
    /// catálogo, porque a chave levava o nome dela.
    /// </summary>
    static SaveData ImportLegacy()
    {
        var imported = new SaveData
        {
            stagesCleared = PlayerPrefs.GetInt("corrida.degraus-vencidos", 0),
            ship = PlayerPrefs.GetString("cne.ship", string.Empty),
            wallet = PlayerPrefs.GetInt("cne.wallet", 0),
            lastName = PlayerPrefs.GetString("corrida.ultimo-nome", string.Empty),
        };

        string records = PlayerPrefs.GetString("corrida.recordes-distancia", string.Empty);
        if (!string.IsNullOrEmpty(records))
        {
            try
            {
                var parsed = JsonUtility.FromJson<LegacyRecords>(records);
                if (parsed?.entries != null)
                    imported.records = parsed.entries;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Salvamento] Recordes antigos ilegíveis ({e.Message}). Ficam de fora.");
            }
        }

        var catalog = ShipCatalog.Load();
        if (catalog != null)
        {
            foreach (var ship in catalog.ships)
            {
                if (ship == null)
                    continue;

                int level = PlayerPrefs.GetInt("cne.ship-level." + ship.name, 1);
                if (level > 1)
                    imported.shipLevels.Add(new ShipLevelEntry { ship = ship.name, level = level });
            }
        }

        return imported;
    }

    /// <summary>
    /// Esquece o que está em memória. Com o recarregamento de domínio desligado
    /// no Editor, o salvamento de uma rodada de teste sobreviveria à seguinte
    /// mesmo depois de apagado no disco.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnLoad() => data = null;
}
