using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Monta o combate na Game.unity: a ficha da nave com vida e tiro automático, os
/// obstáculos que descem pelas faixas, o HUD de vida e os **três** painéis de fim
/// de corrida.
///
/// São três porque os desfechos são diferentes: fase de progressão vencida (sem
/// placar, só o que destravou), nave destruída numa fase de progressão, e o fim
/// da fase sem fim — o único com campo de nome e tabela, porque é a única fase
/// que pontua.
/// </summary>
static class BattleSetup
{
    const string ScenePath = "Assets/Scenes/Game.unity";

    const string ShipObject = "Ship";
    const string RaceObject = "Race";
    const string ObstaclesObject = "Obstacles";
    const string CanvasObject = "UI";
    const string PauseButtonObject = "BotaoMenu";
    const string SpeedHudObject = "HudVelocidade";
    const string HealthHudObject = "HudVida";
    const string WarpHudObject = "HudDobra";
    const string AbilityHudObject = "HudPoder";
    const string ModifierHudObject = "HudModificadores";
    const string TimeSlowOverlayObject = "VeuTempoLento";
    const string WarpVisualsObject = "MolduraDobra";
    const string ModifiersObject = "Modifiers";
    const string VictoryPanelObject = "PainelVitoria";
    const string DefeatPanelObject = "PainelDerrota";
    const string EndlessPanelObject = "PainelSemFim";

    [MenuItem(ProjectTools.BattleItem, false, 104)]
    internal static void Setup()
    {
        var scene = OpenGameScene();
        if (!scene.IsValid())
            return;

        var ship = FindInScene(scene, ShipObject);
        if (ship == null)
        {
            Debug.LogError("[Combate] Não achei o objeto 'Ship' na cena. Monte a corrida antes.");
            return;
        }

        var race = FindInScene(scene, RaceObject);
        if (race == null)
        {
            Debug.LogError("[Combate] Não achei o objeto 'Race' na cena. " +
                           "Rode antes o 'Montar corrida (fundo + HUD)'.");
            return;
        }

        var pixel = PlaceholderArt.Pixel();

        // ── Nave: vida, ficha e arma ─────────────────────────────────────
        GetOrAdd<Health>(ship);
        var stats = GetOrAdd<ShipStats>(ship);

        // A ficha é um asset, e sem ela a nave não tem número nenhum. Criar aqui
        // se faltar, em vez de só reclamar: quem roda o Montar não deveria ter de
        // saber que existe uma ordem entre os passos.
        var definition = ShipSetup.GetOrCreateStarter();
        UiBuilder.SetReference(stats, "definition", definition);

        // Os dois sistemas de poder, que são separados de ponta a ponta: um para
        // o que se pega na pista, outro para o que o jogador ativa. Os dois
        // entram vazios — quem dá modificador é o item da pista, e o poder da
        // nave vem da ficha da nave escolhida.
        //
        // Antes deles, a faxina: o componente único que existia até 22/08/2026
        // foi apagado, e quem já tinha rodado o Montar ficou com um script
        // faltando pendurado na nave. Sem isto, ele fica lá para sempre.
        //
        // **O resultado vai para o Console de propósito.** Na primeira rodada
        // isto não removeu nada e ninguém percebeu: a montagem correu no mesmo
        // minuto em que os arquivos foram renomeados, e naquele instante o
        // componente antigo ainda resolvia. O órfão só apareceu depois, e a
        // montagem já tinha dito "concluído". Contar em voz alta é o que separa
        // "não havia o que limpar" de "não limpou".
        int orphans = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(ship);
        Debug.Log(orphans > 0
                      ? $"[Combate] {orphans} script(s) faltando removido(s) da nave."
                      : "[Combate] Nenhum script faltando na nave.");

        GetOrAdd<LevelModifiers>(ship);
        GetOrAdd<ShipAbilities>(ship);

        // O piloto automático mora na nave e liga sozinho pelo traço. Quem o
        // liga é o poder da Raffa, mas ele entra em toda nave: o traço é da nave,
        // e um modificador de fase que tire o controle do jogador usaria o mesmo.
        GetOrAdd<Autopilot>(ship);

        var weapon = GetOrAdd<ShipWeapon>(ship);
        UiBuilder.SetReference(weapon, "projectileSprite", pixel);

        // ── Obstáculos ───────────────────────────────────────────────────
        var obstacles = GetOrCreate(scene, ObstaclesObject);
        obstacles.transform.position = Vector3.zero;
        var spawner = GetOrAdd<ObstacleSpawner>(obstacles);
        UiBuilder.SetReference(spawner, "sprite", pixel);

        // ── Modificadores de fase ────────────────────────────────────────
        // Sorteador próprio, e não um pedaço do de obstáculos: o ritmo é outro —
        // obstáculo é o pulso da fase, modificador é acontecimento. Os dois
        // respeitam a mesma regra de faixa livre, que mora no LaneOccupancy.
        var modifierRoot = GetOrCreate(scene, ModifiersObject);
        modifierRoot.transform.position = Vector3.zero;
        var modifierSpawner = GetOrAdd<LevelModifierSpawner>(modifierRoot);
        UiBuilder.SetReference(modifierSpawner, "sprite", PlaceholderArt.Circle());

        // ── Diretor da corrida ───────────────────────────────────────────
        var director = GetOrAdd<RaceDirector>(race);

        // ── UI ───────────────────────────────────────────────────────────
        var canvas = FindInScene(scene, CanvasObject);
        if (canvas == null)
        {
            Debug.LogError("[Combate] Não achei o Canvas 'UI'. O HUD e os painéis não foram montados.");
            return;
        }

        var healthHud = BuildHealthHud(canvas.transform);
        var warpHud = BuildWarpHud(canvas.transform, director);
        var abilityHud = BuildAbilityHud(canvas.transform, ship);
        var modifierHud = BuildModifierHud(canvas.transform, ship);
        BuildTimeSlowOverlay(canvas.transform);
        var victory = BuildVictoryPanel(canvas.transform, director);
        var defeat = BuildDefeatPanel(canvas.transform, director);
        var endless = BuildEndlessPanel(canvas.transform, director);

        // Depois dos painéis de fim, e não antes: o clarão da dobra tem de ficar
        // por cima deles, e quem manda na ordem de desenho é a ordem na
        // hierarquia. Montado antes, o painel de vitória o cobriria — e a dobra
        // completa e o painel acontecem no mesmo quadro.
        BuildWarpVisuals(canvas.transform, director);

        var speedHud = canvas.transform.Find(SpeedHudObject);
        var speedHudComponent = speedHud != null ? speedHud.GetComponent<SpeedHud>() : null;
        if (speedHudComponent != null)
            UiBuilder.SetReference(speedHudComponent, "director", director);

        UiBuilder.SetReference(director, "victoryPanel", victory.panel);
        UiBuilder.SetReference(director, "victoryTimeLabel", victory.time);
        UiBuilder.SetReference(director, "victoryUnlockLabel", victory.unlock);
        UiBuilder.SetReference(director, "defeatPanel", defeat);

        UiBuilder.SetReference(director, "endlessPanel", endless.panel);
        UiBuilder.SetReference(director, "endlessDistanceLabel", endless.distance);
        UiBuilder.SetReference(director, "nameField", endless.nameField);
        UiBuilder.SetReference(director, "placementLabel", endless.placement);
        UiBuilder.SetReference(director, "recordsBoard", endless.board);

        // O HUD e o botão de pausa somem quando a corrida acaba, para não
        // competirem com o painel de fim.
        var pauseButton = canvas.transform.Find(PauseButtonObject);
        UiBuilder.SetReferenceArray(director, "hideOnEnd",
                                    healthHud,
                                    warpHud,
                                    abilityHud,
                                    modifierHud,
                                    speedHud != null ? speedHud.gameObject : null,
                                    pauseButton != null ? pauseButton.gameObject : null);

        victory.panel.SetActive(false);
        defeat.SetActive(false);
        endless.panel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        ProjectTools.MarkRun(ProjectTools.BattleId);

        Selection.activeGameObject = ship;
        EditorGUIUtility.PingObject(ship);

        Debug.Log(
            "[Combate] Nave com ficha (vida 100, dano 25, 3 tiros/s), obstáculos, HUD de vida e " +
            "de dobra, e os três painéis de fim montados em " + ScenePath + ". " +
            "Fase de progressão: vencer é segurar a dobra, e não marca placar. " +
            "Fase sem fim: a corrida acaba quando a nave cai, e vale a distância percorrida.",
            ship);
    }

    [MenuItem(ProjectTools.BattleUndoItem, false, 105)]
    static void Teardown()
    {
        var scene = OpenGameScene();
        if (!scene.IsValid())
            return;

        int removed = 0;

        foreach (var name in new[] { ObstaclesObject, ModifiersObject })
        {
            var root = FindInScene(scene, name);
            if (root == null)
                continue;

            Undo.DestroyObjectImmediate(root);
            removed++;
        }

        var canvas = FindInScene(scene, CanvasObject);
        if (canvas != null)
        {
            foreach (var name in new[] { HealthHudObject, WarpHudObject, AbilityHudObject,
                                         ModifierHudObject, TimeSlowOverlayObject,
                                         WarpVisualsObject, VictoryPanelObject,
                                         DefeatPanelObject, EndlessPanelObject })
            {
                var target = canvas.transform.Find(name);
                if (target == null)
                    continue;

                Undo.DestroyObjectImmediate(target.gameObject);
                removed++;
            }
        }

        RemoveComponent<ShipWeapon>(FindInScene(scene, ShipObject), ref removed);
        RemoveComponent<Autopilot>(FindInScene(scene, ShipObject), ref removed);
        RemoveComponent<ShipAbilities>(FindInScene(scene, ShipObject), ref removed);
        RemoveComponent<LevelModifiers>(FindInScene(scene, ShipObject), ref removed);
        RemoveComponent<ShipStats>(FindInScene(scene, ShipObject), ref removed);
        RemoveComponent<Health>(FindInScene(scene, ShipObject), ref removed);
        RemoveComponent<RaceDirector>(FindInScene(scene, RaceObject), ref removed);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        ProjectTools.Forget(ProjectTools.BattleId);

        Debug.Log($"[Combate] {removed} item(ns) removido(s) de {ScenePath}.");
    }

    static GameObject BuildHealthHud(Transform canvas)
    {
        var existing = canvas.Find(HealthHudObject);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var hud = UiBuilder.NewUI(HealthHudObject, canvas);
        Undo.RegisterCreatedObjectUndo(hud, "Montar combate");
        hud.transform.SetAsFirstSibling();

        // Rodapé à DIREITA desde 30/08/2026: a vida trocou de canto com o poder
        // da nave. Ver BuildAbilityHud para o motivo da troca.
        var rect = (RectTransform)hud.transform;
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(320f, 110f);
        rect.anchoredPosition = new Vector2(-48f, 48f);

        var label = UiBuilder.Label(hud, "100", 64f, new Color(0.7f, 1f, 0.8f, 0.9f));
        label.alignment = TextAlignmentOptions.Right;

        hud.AddComponent<HealthHud>();
        return hud;
    }

    /// <summary>Contagem da dobra, logo acima do velocímetro do rodapé.</summary>
    static GameObject BuildWarpHud(Transform canvas, RaceDirector director)
    {
        var existing = canvas.Find(WarpHudObject);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var hud = UiBuilder.NewUI(WarpHudObject, canvas);
        Undo.RegisterCreatedObjectUndo(hud, "Montar combate");
        hud.transform.SetAsFirstSibling();

        var rect = (RectTransform)hud.transform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(700f, 80f);
        // Acima do HudVelocidade, que ocupa de 48 a 168.
        rect.anchoredPosition = new Vector2(0f, 180f);

        UiBuilder.Label(hud, string.Empty, 44f, new Color(0.6f, 1f, 1f, 1f));

        var component = hud.AddComponent<WarpChargeHud>();
        UiBuilder.SetReference(component, "director", director);

        return hud;
    }

    /// <summary>
    /// O canto do poder da nave, **no rodapé à esquerda** desde 30/08/2026.
    ///
    /// Ele nasceu no alto à direita com o argumento de que embaixo é onde o
    /// polegar mora. O argumento estava certo e o lugar, errado: **o alto à
    /// direita já era do botão de pausa**, e o canto caiu em cima dele. O
    /// Raffael viu no aparelho e mandou trocar.
    ///
    /// Agora os quatro cantos têm dono, e nenhum divide com ninguém: pausa no
    /// alto à direita, modificadores de fase no alto à esquerda, poder da nave no
    /// rodapé à esquerda e vida no rodapé à direita — a vida veio da esquerda
    /// para abrir a vaga. As duas coisas do rodapé continuam em cantos opostos
    /// pelo motivo de sempre: são as duas que se conferem de relance, e juntas
    /// competiriam pelo mesmo olhar.
    /// </summary>
    static GameObject BuildAbilityHud(Transform canvas, GameObject ship)
    {
        var existing = canvas.Find(AbilityHudObject);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var hud = UiBuilder.NewUI(AbilityHudObject, canvas);
        Undo.RegisterCreatedObjectUndo(hud, "Montar combate");
        hud.transform.SetAsFirstSibling();

        var rect = (RectTransform)hud.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.sizeDelta = new Vector2(150f, 150f);
        rect.anchoredPosition = new Vector2(48f, 48f);

        // O quadrado de trás. Cor de marcador de lugar: quem manda de verdade é
        // a cor da ficha do poder, que o AbilityHud escreve aqui ao iniciar.
        var frame = hud.AddComponent<Image>();
        frame.sprite = UiBuilder.BuiltinSprite();
        frame.type = Image.Type.Sliced;
        frame.color = new Color(1f, 1f, 1f, 0.22f);
        frame.raycastTarget = false;

        var iconObject = UiBuilder.NewUI("Icone", hud.transform);
        var icon = iconObject.AddComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        UiBuilder.Stretch(iconObject, 22f);

        // Usos no canto de baixo, recarga no meio: o número grande do meio é o
        // que muda o tempo todo, e o de baixo só desce de um em um.
        var cooldownObject = UiBuilder.NewUI("Recarga", hud.transform);
        UiBuilder.Stretch(cooldownObject);
        var cooldown = UiBuilder.Label(cooldownObject, string.Empty, 52f,
                                       new Color(1f, 1f, 1f, 0.95f));
        cooldown.alignment = TextAlignmentOptions.Center;
        cooldown.raycastTarget = false;

        var usesObject = UiBuilder.NewUI("Usos", hud.transform);
        var uses = UiBuilder.Label(usesObject, "0", 34f, UiBuilder.DimLabelColor);
        var usesRect = (RectTransform)uses.transform;
        usesRect.anchorMin = Vector2.zero;
        usesRect.anchorMax = new Vector2(1f, 0f);
        usesRect.pivot = new Vector2(0.5f, 0f);
        usesRect.sizeDelta = new Vector2(0f, 40f);
        usesRect.anchoredPosition = new Vector2(0f, 8f);
        uses.alignment = TextAlignmentOptions.Center;
        uses.raycastTarget = false;

        // O CanvasGroup existe já na montagem, e não só no Awake do componente,
        // para o canto nascer sem bloquear toque mesmo antes de rodar o jogo.
        var group = hud.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        var component = hud.AddComponent<AbilityHud>();
        UiBuilder.SetReference(component, "abilities", ship.GetComponent<ShipAbilities>());
        UiBuilder.SetReference(component, "frame", frame);
        UiBuilder.SetReference(component, "icon", icon);
        UiBuilder.SetReference(component, "usesLabel", uses);
        UiBuilder.SetReference(component, "cooldownLabel", cooldown);

        return hud;
    }

    /// <summary>
    /// A fileira dos modificadores de fase, no alto à esquerda — o canto oposto
    /// ao do poder da nave, que fica no alto à direita. Os dois são "o que está
    /// valendo agora", e separá-los pelos cantos é o que impede o jogador de
    /// confundir o que ele pegou com o que ele pode ativar.
    ///
    /// A cena ganha **um quadradinho modelo, desligado**, que o
    /// <see cref="ModifierHud"/> clona conforme precisa. Modificador novo no jogo
    /// não pede montagem nenhuma.
    /// </summary>
    static GameObject BuildModifierHud(Transform canvas, GameObject ship)
    {
        var existing = canvas.Find(ModifierHudObject);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var hud = UiBuilder.NewUI(ModifierHudObject, canvas);
        Undo.RegisterCreatedObjectUndo(hud, "Montar combate");
        hud.transform.SetAsFirstSibling();

        var rect = (RectTransform)hud.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(600f, 110f);
        rect.anchoredPosition = new Vector2(48f, -48f);

        // Layout horizontal: a fileira cresce para a direita conforme entram
        // modificadores, e some sozinha quando não há nenhum. Sem ele, cada
        // quadradinho precisaria de posição calculada em runtime.
        var layout = hud.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        var template = BuildModifierSlot(hud.transform);

        var component = hud.AddComponent<ModifierHud>();
        UiBuilder.SetReference(component, "modifiers", ship.GetComponent<LevelModifiers>());
        UiBuilder.SetReference(component, "slotTemplate", template);

        return hud;
    }

    /// <summary>
    /// O véu azulado do tempo lento, cobrindo a tela inteira.
    ///
    /// **Atrás de todo o resto do HUD**, como primeiro filho do Canvas: ele tinge
    /// a pista, não os mostradores. Um véu por cima dos números deixaria a vida
    /// mais difícil de ler bem no momento em que o poder existe para dar folga.
    ///
    /// **Fora do `hideOnEnd`**, ao contrário dos outros: o `RaceDirector` derruba
    /// os poderes ao fim da corrida, então o véu se apaga sozinho pelo caminho
    /// certo — e escondê-lo à força faria o painel de fim aparecer com um corte
    /// de cor no meio da transição.
    /// </summary>
    static void BuildTimeSlowOverlay(Transform canvas)
    {
        var existing = canvas.Find(TimeSlowOverlayObject);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var overlay = UiBuilder.NewUI(TimeSlowOverlayObject, canvas);
        Undo.RegisterCreatedObjectUndo(overlay, "Montar combate");
        overlay.transform.SetAsFirstSibling();
        UiBuilder.Stretch(overlay);

        var veil = overlay.AddComponent<Image>();
        veil.sprite = PlaceholderArt.Pixel();
        veil.raycastTarget = false;
        veil.color = new Color(1f, 1f, 1f, 0f);
        veil.enabled = false;

        var component = overlay.AddComponent<TimeSlowOverlay>();
        UiBuilder.SetReference(component, "veil", veil);
    }

    /// <summary>
    /// A moldura da dobra e o clarão das ondas.
    ///
    /// **Oito braços, dois por canto** — um deitado e um em pé. Cada um cresce do
    /// canto dele em direção ao meio da borda, e no cheio os oito se encontram e
    /// fecham o quadro. Oito e não quatro porque a moldura tem de crescer **dos
    /// cantos para o meio**: uma barra por borda só cresceria de uma ponta, e a
    /// leitura de "o cerco está se fechando" iria embora.
    ///
    /// **O clarão é o último filho do Canvas**, por cima até dos painéis de fim.
    /// A dobra completa e o painel de vitória acontecem no mesmo quadro, e o
    /// clarão precisa passar por cima dele — é ele que faz a vitória parecer o
    /// espaço quebrando, em vez de uma caixa que apareceu. Não recebe toque, então
    /// os botões do painel continuam funcionando por baixo.
    /// </summary>
    static void BuildWarpVisuals(Transform canvas, RaceDirector director)
    {
        var existing = canvas.Find(WarpVisualsObject);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var root = UiBuilder.NewUI(WarpVisualsObject, canvas);
        Undo.RegisterCreatedObjectUndo(root, "Montar combate");
        root.transform.SetAsLastSibling();
        UiBuilder.Stretch(root);

        var pixel = PlaceholderArt.Pixel();

        const float thickness = 10f;
        var arms = new RectTransform[8];
        var images = new Image[8];
        int index = 0;

        // Os quatro cantos, e em cada um o braço deitado e o em pé. O PIVÔ é o que
        // diz de que ponta o braço cresce — o WarpVisuals lê o pivô para saber
        // disso, então mexer aqui na posição sem mexer no pivô o faz crescer para
        // o lado errado.
        for (int corner = 0; corner < 4; corner++)
        {
            float x = corner % 2 == 0 ? 0f : 1f;
            float y = corner < 2 ? 1f : 0f;

            arms[index] = Arm(root.transform, $"Deitado {corner + 1}", pixel,
                              new Vector2(x, y), new Vector2(0f, thickness), out images[index]);
            index++;

            arms[index] = Arm(root.transform, $"EmPe {corner + 1}", pixel,
                              new Vector2(x, y), new Vector2(thickness, 0f), out images[index]);
            index++;
        }

        var flashObject = UiBuilder.NewUI("Clarao", root.transform);
        UiBuilder.Stretch(flashObject);
        var flash = flashObject.AddComponent<Image>();
        flash.sprite = pixel;
        flash.raycastTarget = false;
        flash.enabled = false;

        var component = root.AddComponent<WarpVisuals>();
        UiBuilder.SetReference(component, "director", director);
        UiBuilder.SetReferenceArray(component, "arms", arms);
        UiBuilder.SetReferenceArray(component, "armImages", images);
        UiBuilder.SetReference(component, "flash", flash);
    }

    /// <summary>
    /// Um braço da moldura, ancorado num canto. <paramref name="size"/> com X
    /// zero é braço deitado (a largura vem das âncoras, que o WarpVisuals estica);
    /// com Y zero é braço em pé.
    /// </summary>
    static RectTransform Arm(Transform parent, string name, Sprite sprite, Vector2 corner,
                             Vector2 size, out Image image)
    {
        var go = UiBuilder.NewUI(name, parent);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = corner;
        rect.anchorMax = corner;
        rect.pivot = corner;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.enabled = false;

        return rect;
    }

    /// <summary>O quadradinho que vira todos os outros: disco, anel e ícone.</summary>
    static ModifierHudSlot BuildModifierSlot(Transform parent)
    {
        var circle = PlaceholderArt.Circle();

        var go = UiBuilder.NewUI("ModeloDeModificador", parent);
        ((RectTransform)go.transform).sizeDelta = new Vector2(96f, 96f);

        var backdrop = go.AddComponent<Image>();
        backdrop.sprite = circle;
        backdrop.raycastTarget = false;

        var ringObject = UiBuilder.NewUI("Anel", go.transform);
        UiBuilder.Stretch(ringObject);
        var ring = ringObject.AddComponent<Image>();
        ring.sprite = circle;
        ring.raycastTarget = false;

        // O relógio: um disco preenchido em radial, começando em cima e andando
        // no sentido horário, que é para onde o olho espera que um relógio ande.
        ring.type = Image.Type.Filled;
        ring.fillMethod = Image.FillMethod.Radial360;
        ring.fillOrigin = (int)Image.Origin360.Top;
        ring.fillClockwise = true;
        ring.fillAmount = 1f;

        var iconObject = UiBuilder.NewUI("Icone", go.transform);
        UiBuilder.Stretch(iconObject, 26f);
        var icon = iconObject.AddComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;

        var slot = go.AddComponent<ModifierHudSlot>();
        UiBuilder.SetReference(slot, "ring", ring);
        UiBuilder.SetReference(slot, "backdrop", backdrop);
        UiBuilder.SetReference(slot, "icon", icon);

        go.SetActive(false);

        return slot;
    }

    struct VictoryPanel
    {
        public GameObject panel;
        public TextMeshProUGUI time;
        public TextMeshProUGUI unlock;
    }

    /// <summary>
    /// Fase de progressão vencida. **Sem campo de nome e sem tabela**: estas
    /// fases não pontuam, elas ensinam o jogo e apresentam obstáculo novo. O que
    /// o jogador leva daqui é a fase seguinte, e é isso que o painel anuncia.
    /// </summary>
    static VictoryPanel BuildVictoryPanel(Transform canvas, RaceDirector director)
    {
        var existing = canvas.Find(VictoryPanelObject);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var panel = UiBuilder.Panel(VictoryPanelObject, canvas);
        Undo.RegisterCreatedObjectUndo(panel, "Montar combate");
        panel.transform.SetAsLastSibling();

        var box = UiBuilder.Box("Caixa", panel.transform, new Vector2(900f, 780f), Vector2.zero);

        UiBuilder.Label("Titulo", box.transform, "Fase concluída!", 64f,
                        new Vector2(0f, 280f), new Vector2(820f, 110f), UiBuilder.LabelColor);

        UiBuilder.Label("Rotulo", box.transform, "Seu tempo", 34f,
                        new Vector2(0f, 190f), new Vector2(820f, 60f), UiBuilder.DimLabelColor);

        var time = UiBuilder.Label("Tempo", box.transform, "0:00.00", 96f,
                                   new Vector2(0f, 100f), new Vector2(820f, 130f), UiBuilder.LabelColor);

        var unlock = UiBuilder.Label("Destravou", box.transform, string.Empty, 36f,
                                     new Vector2(0f, -30f), new Vector2(820f, 110f),
                                     new Color(0.7f, 1f, 0.8f, 1f));

        var back = UiBuilder.Button("BotaoVoltar", box.transform, "Voltar ao menu",
                                    new Vector2(0f, -250f), new Vector2(440f, 110f),
                                    UiBuilder.NeutralButton);
        UnityEventTools.AddPersistentListener(back.onClick, director.BackToMenu);

        return new VictoryPanel
        {
            panel = panel,
            time = time,
            unlock = unlock,
        };
    }

    struct EndlessPanel
    {
        public GameObject panel;
        public TextMeshProUGUI distance;
        public TextMeshProUGUI placement;
        public TMP_InputField nameField;
        public RecordsBoard board;
    }

    /// <summary>
    /// Fim da fase sem fim. Não é derrota: a nave cair é o fim previsto de lá, e
    /// o que interessa é até onde ela chegou. É o **único** painel com campo de
    /// nome e tabela, porque é a única fase que pontua.
    /// </summary>
    static EndlessPanel BuildEndlessPanel(Transform canvas, RaceDirector director)
    {
        var existing = canvas.Find(EndlessPanelObject);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var panel = UiBuilder.Panel(EndlessPanelObject, canvas);
        Undo.RegisterCreatedObjectUndo(panel, "Montar combate");
        panel.transform.SetAsLastSibling();

        var box = UiBuilder.Box("Caixa", panel.transform, new Vector2(900f, 1250f), Vector2.zero);

        UiBuilder.Label("Titulo", box.transform, "Corrida encerrada", 60f,
                        new Vector2(0f, 520f), new Vector2(820f, 110f), UiBuilder.LabelColor);

        UiBuilder.Label("Rotulo", box.transform, "Distância percorrida", 34f,
                        new Vector2(0f, 420f), new Vector2(820f, 60f), UiBuilder.DimLabelColor);

        var distance = UiBuilder.Label("Distancia", box.transform, "0 km", 88f,
                                       new Vector2(0f, 330f), new Vector2(820f, 130f),
                                       UiBuilder.LabelColor);

        var nameField = UiBuilder.InputField("CampoNome", box.transform, "Seu nome",
                                             new Vector2(0f, 190f), new Vector2(640f, 110f));

        var save = UiBuilder.Button("BotaoSalvar", box.transform, "Salvar distância",
                                    new Vector2(0f, 55f), new Vector2(440f, 110f),
                                    UiBuilder.PrimaryButton);
        UnityEventTools.AddPersistentListener(save.onClick, director.SaveScore);

        var placement = UiBuilder.Label("Colocacao", box.transform, "Salve sua distância na tabela.", 32f,
                                        new Vector2(0f, -35f), new Vector2(820f, 60f),
                                        UiBuilder.DimLabelColor);

        UiBuilder.Label("TituloTabela", box.transform, "Maiores distâncias", 36f,
                        new Vector2(0f, -110f), new Vector2(820f, 60f), UiBuilder.DimLabelColor);

        var listObject = UiBuilder.NewUI("Lista", box.transform);
        UiBuilder.PlaceCentered(listObject, new Vector2(0f, -320f), new Vector2(760f, 340f));
        var list = UiBuilder.Label(listObject, string.Empty, 36f, UiBuilder.LabelColor);
        list.alignment = TextAlignmentOptions.Top;

        var board = listObject.AddComponent<RecordsBoard>();
        UiBuilder.SetReference(board, "target", list);

        var boardSerialized = new SerializedObject(board);
        boardSerialized.FindProperty("maxRows").intValue = 5;
        boardSerialized.ApplyModifiedPropertiesWithoutUndo();

        var back = UiBuilder.Button("BotaoVoltar", box.transform, "Voltar ao menu",
                                    new Vector2(0f, -540f), new Vector2(440f, 110f),
                                    UiBuilder.NeutralButton);
        UnityEventTools.AddPersistentListener(back.onClick, director.BackToMenu);

        return new EndlessPanel
        {
            panel = panel,
            distance = distance,
            placement = placement,
            nameField = nameField,
            board = board,
        };
    }

    static GameObject BuildDefeatPanel(Transform canvas, RaceDirector director)
    {
        var existing = canvas.Find(DefeatPanelObject);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var panel = UiBuilder.Panel(DefeatPanelObject, canvas);
        Undo.RegisterCreatedObjectUndo(panel, "Montar combate");
        panel.transform.SetAsLastSibling();

        var box = UiBuilder.Box("Caixa", panel.transform, new Vector2(860f, 620f), Vector2.zero);

        UiBuilder.Label("Titulo", box.transform, "Nave destruída", 64f,
                        new Vector2(0f, 180f), new Vector2(780f, 110f), UiBuilder.LabelColor);

        UiBuilder.Label("Explicacao", box.transform,
                        "A corrida acabou antes da dobra.\nA fase só é concluída entrando em dobra.", 34f,
                        new Vector2(0f, 50f), new Vector2(780f, 120f), UiBuilder.DimLabelColor);

        var back = UiBuilder.Button("BotaoVoltar", box.transform, "Voltar ao menu",
                                    new Vector2(0f, -160f), new Vector2(440f, 110f),
                                    UiBuilder.DangerButton);
        UnityEventTools.AddPersistentListener(back.onClick, director.BackToMenu);

        return panel;
    }

    static void RemoveComponent<T>(GameObject target, ref int removed) where T : Component
    {
        if (target == null)
            return;

        var component = target.GetComponent<T>();
        if (component == null)
            return;

        Undo.DestroyObjectImmediate(component);
        removed++;
    }

    static GameObject GetOrCreate(Scene scene, string name)
    {
        var existing = FindInScene(scene, name);
        if (existing != null)
            return existing;

        var created = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(created, "Montar combate");
        return created;
    }

    static T GetOrAdd<T>(GameObject target) where T : Component
    {
        var component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    static Scene OpenGameScene()
    {
        var current = SceneManager.GetActiveScene();
        if (current.path == ScenePath)
            return current;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return default;

        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    static GameObject FindInScene(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == name)
                return root;
        }

        return null;
    }
}
