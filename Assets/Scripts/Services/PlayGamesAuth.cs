using System;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;

/// <summary>
/// Login no Google Play Games e ponte para as conquistas.
/// Não autentica às cegas: o login silencioso é opcional e, quando falha, o
/// jogo continua funcionando sem conta — quem decide insistir é a UI, chamando
/// <see cref="SignInManually"/> num botão.
/// Coloque numa raiz de cena: o objeto sobrevive às trocas de cena.
/// </summary>
public class PlayGamesAuth : MonoBehaviour
{
    public static PlayGamesAuth Instance { get; private set; }

    [Tooltip("Tenta o login silencioso ao carregar. Não abre tela nenhuma: se o " +
             "aparelho não tiver conta do Play Games, apenas falha em silêncio.")]
    [SerializeField] bool signInOnStart = true;

    /// <summary>Disparado ao fim de toda tentativa de login, com sucesso ou não.</summary>
    public event Action<bool> SignInFinished;

    public bool IsSignedIn => PlayGamesPlatform.Instance != null &&
                              PlayGamesPlatform.Instance.IsAuthenticated();

    /// <summary>Nome do jogador, ou string vazia quando não há login.</summary>
    public string PlayerName => IsSignedIn ? PlayGamesPlatform.Instance.GetUserDisplayName() : string.Empty;

    bool signInInProgress;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        PlayGamesPlatform.Activate();
    }

    void Start()
    {
        if (signInOnStart)
            SignIn();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Login silencioso: usa a conta que já autorizou o jogo antes e não
    /// interrompe ninguém. No Editor sempre falha — o plugin só funciona no
    /// aparelho —, e isso é esperado.
    /// </summary>
    public void SignIn()
    {
        if (signInInProgress || IsSignedIn)
            return;

        signInInProgress = true;
        PlayGamesPlatform.Instance.Authenticate(OnSignInResult);
    }

    /// <summary>
    /// Login com tela do Play Games. Chame de um botão, e só depois de o
    /// silencioso ter falhado: mostrar isso sem o jogador pedir é intrusivo.
    /// </summary>
    public void SignInManually()
    {
        if (signInInProgress || IsSignedIn)
            return;

        signInInProgress = true;
        PlayGamesPlatform.Instance.ManuallyAuthenticate(OnSignInResult);
    }

    /// <summary>
    /// Marca uma conquista como concluída. Sem login não faz nada e não quebra
    /// o jogo — conquista é enfeite, não regra de jogabilidade.
    /// Use as constantes de <c>GPGSIds</c>.
    /// </summary>
    public void UnlockAchievement(string achievementId, Action<bool> onDone = null)
    {
        ReportAchievement(achievementId, 100.0, onDone);
    }

    /// <summary>
    /// Progresso parcial de uma conquista incremental, de 0 a 100.
    /// </summary>
    public void ReportAchievement(string achievementId, double progress, Action<bool> onDone = null)
    {
        if (string.IsNullOrEmpty(achievementId))
        {
            Debug.LogWarning("[PlayGames] Conquista sem id: nada a reportar.");
            onDone?.Invoke(false);
            return;
        }

        if (!IsSignedIn)
        {
            onDone?.Invoke(false);
            return;
        }

        PlayGamesPlatform.Instance.ReportProgress(achievementId, progress, success =>
        {
            if (!success)
                Debug.LogWarning($"[PlayGames] Falha ao reportar a conquista {achievementId}.");

            onDone?.Invoke(success);
        });
    }

    /// <summary>
    /// Código de autorização para um servidor trocar por credenciais do jogador.
    /// O código vai direto para quem pediu e não fica guardado em lugar nenhum:
    /// é credencial, não estado de jogo. Só serve quando existir um backend.
    /// </summary>
    public void RequestServerSideAccess(Action<string> onCode, bool forceRefreshToken = false)
    {
        if (onCode == null)
            return;

        if (!IsSignedIn)
        {
            onCode(null);
            return;
        }

        PlayGamesPlatform.Instance.RequestServerSideAccess(forceRefreshToken, code =>
        {
            if (string.IsNullOrEmpty(code))
                Debug.LogWarning("[PlayGames] Não veio código de autorização.");

            onCode(code);
        });
    }

    void OnSignInResult(SignInStatus status)
    {
        signInInProgress = false;

        bool success = status == SignInStatus.Success;
        if (!success)
        {
            // Warning, não Error: ficar sem login é um estado normal do jogo.
            Debug.LogWarning($"[PlayGames] Login não concluído ({status}). O jogo segue sem conta.");
        }

        SignInFinished?.Invoke(success);
    }
}
