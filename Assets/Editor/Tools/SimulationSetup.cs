using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Cria as cinco personas do simulador de corridas em
/// <c>Assets/Simulacao/Personas/</c> — do Novato ao Perfeito.
///
/// **Rodar de novo não sobrescreve** o que já existe: os números são um
/// primeiro chute meu, e quem afina é o Raffael, na ficha. Uma persona apagada
/// volta com os números de fábrica.
///
/// <para>
/// **Os números, e de onde vieram.** O Perfeito é a Super IA inteira: reage no
/// mesmo quadro, nunca erra, cruza as faixas de uma vez e caça tudo o que dá
/// tempo de destruir. Daí para baixo, cada degrau reage mais tarde, enxerga
/// menos, erra mais e caça menos. O Novato fica perto de quem pegou o jogo
/// agora: quase meio segundo para reagir, um erro a cada quatro desvios, nem
/// repara no Debilitante e aperta o poder à toa.
/// </para>
/// </summary>
static class SimulationSetup
{
    public const string PersonaFolder = "Assets/Simulacao/Personas";

    struct Seed
    {
        public string File;
        public string Name;
        public string Description;
        public int Rank;
        public float Reaction, Think, Swipe;
        public float Danger, Clear, Look;
        public float Mistake, Hunt;
        public bool Rewards, Debuffs;
        public PilotPersona.PowerHabit Power;
    }

    static readonly Seed[] Seeds =
    {
        new Seed
        {
            File = "Persona-1-Novato", Name = "Novato", Rank = 1,
            Description = "Pegou o jogo agora. Reage tarde, vê o obstáculo em cima da hora, erra um desvio " +
                          "em cada quatro, nem repara no Debilitante e aperta o poder sem motivo. Não caça.",
            Reaction = 0.45f, Think = 0.25f, Swipe = 0.30f,
            Danger = 3f, Clear = 4f, Look = 8f,
            Mistake = 0.25f, Hunt = 0f,
            Rewards = false, Debuffs = false, Power = PilotPersona.PowerHabit.AToa,
        },
        new Seed
        {
            File = "Persona-2-Casual", Name = "Casual", Rank = 2,
            Description = "Joga de vez em quando. Desvia do que vê, mas vê tarde; erra de vez em quando, " +
                          "quase não caça e só lembra do poder quando se vê encurralado.",
            Reaction = 0.32f, Think = 0.15f, Swipe = 0.22f,
            Danger = 3.8f, Clear = 5f, Look = 10f,
            Mistake = 0.12f, Hunt = 0.15f,
            Rewards = false, Debuffs = true, Power = PilotPersona.PowerHabit.NoAperto,
        },
        new Seed
        {
            File = "Persona-3-Regular", Name = "Regular", Rank = 3,
            Description = "O jogador médio. Reação de gente atenta, erra pouco, vai atrás de prêmio e caça " +
                          "obstáculo quando a pista está calma. Poder no aperto.",
            Reaction = 0.22f, Think = 0.10f, Swipe = 0.15f,
            Danger = 4.5f, Clear = 6f, Look = 12f,
            Mistake = 0.06f, Hunt = 0.40f,
            Rewards = true, Debuffs = true, Power = PilotPersona.PowerHabit.NoAperto,
        },
        new Seed
        {
            File = "Persona-4-Veterano", Name = "Veterano", Rank = 4,
            Description = "Conhece o jogo. Reflexo rápido, quase não erra, caça quase sempre e guarda o " +
                          "poder para o aperto e para o fim da dobra.",
            Reaction = 0.15f, Think = 0.05f, Swipe = 0.10f,
            Danger = 5f, Clear = 7f, Look = 14f,
            Mistake = 0.02f, Hunt = 0.75f,
            Rewards = true, Debuffs = true, Power = PilotPersona.PowerHabit.Esperto,
        },
        new Seed
        {
            File = "Persona-5-Perfeito", Name = "Perfeito", Rank = 5,
            Description = "A Super IA jogando o tempo todo, e caçando: reage no mesmo quadro, nunca erra, " +
                          "cruza as faixas de uma vez. É o teto — se ele não vence, ninguém vence.",
            Reaction = 0f, Think = 0f, Swipe = 0f,
            Danger = 5f, Clear = 7f, Look = 14f,
            Mistake = 0f, Hunt = 1f,
            Rewards = true, Debuffs = true, Power = PilotPersona.PowerHabit.Esperto,
        },
    };

    [MenuItem(ProjectTools.PersonasItem, false, 120)]
    public static void Setup()
    {
        Directory.CreateDirectory(PersonaFolder);

        int created = 0;
        foreach (var seed in Seeds)
        {
            string path = $"{PersonaFolder}/{seed.File}.asset";
            if (AssetDatabase.LoadAssetAtPath<PilotPersona>(path) != null)
                continue;

            var persona = ScriptableObject.CreateInstance<PilotPersona>();
            persona.displayName = seed.Name;
            persona.description = seed.Description;
            persona.skillRank = seed.Rank;
            persona.reactionSeconds = seed.Reaction;
            persona.thinkInterval = seed.Think;
            persona.swipeInterval = seed.Swipe;
            persona.dangerDistance = seed.Danger;
            persona.clearDistance = seed.Clear;
            persona.lookAhead = seed.Look;
            persona.mistakeChance = seed.Mistake;
            persona.huntChance = seed.Hunt;
            persona.seeksRewards = seed.Rewards;
            persona.avoidsDebuffs = seed.Debuffs;
            persona.powerHabit = seed.Power;

            AssetDatabase.CreateAsset(persona, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        ProjectTools.MarkRun(ProjectTools.PersonasId);

        Debug.Log(created > 0
            ? $"[Simulador] {created} persona(s) criada(s) em {PersonaFolder}."
            : "[Simulador] As personas já existiam — nada foi sobrescrito.");
    }
}
