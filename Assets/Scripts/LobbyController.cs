using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ロビー専用の統合コントローラ
/// - Q/R/U/P で 未参加→参加→準備OK→未参加 をトグル
/// - 2人以上かつ全員準備OKで「Enterでモード選択へ」を表示
/// - Enterで JoinPanel→ModePanel に切替
/// - モード選択の2ボタンから GameManager を呼ぶ
/// - 画面左下に簡易デバッグ表示（キー入力/参加状況）
/// </summary>
public class LobbyController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject joinPanel;
    public GameObject modePanel;

    [Header("JoinPanel refs")]
    public Image cardQ; public TextMeshProUGUI nameQ; public TextMeshProUGUI keyQ;
    public Image cardR; public TextMeshProUGUI nameR; public TextMeshProUGUI keyR;
    public Image cardU; public TextMeshProUGUI nameU; public TextMeshProUGUI keyU;
    public Image cardP; public TextMeshProUGUI nameP; public TextMeshProUGUI keyP;
    public TextMeshProUGUI readyHint; // "Enterでモード選択へ"
    public TextMeshProUGUI debugText; // 任意（未設定でもOK）

    [Header("ModePanel refs")]
    public Button btnDiagnosis;
    public Button btnFreePlay;

    // 見た目の濃さ（参加/未参加）
    private const float JoinedAlpha = 0.75f;
    private const float ReadyAlpha = 1.00f;
    private const float IdleAlpha = 0.50f;

    public static bool ForceOpenModePanelOnStart = false;

    void Start()
    {
        // 最初はJoinPanel表示、ModePanel非表示
        if (joinPanel) joinPanel.SetActive(true);
        if (modePanel) modePanel.SetActive(false);
        if (readyHint) readyHint.gameObject.SetActive(false);

        // モード選択ボタンの結線
        if (btnDiagnosis) btnDiagnosis.onClick.AddListener(() =>
        {
            GameManager.Instance.StartGameSequence(GameMode.Diagnosis);
        });
        // LobbyController.cs で btnFreePlay の onClick をこうする
        if (btnFreePlay) btnFreePlay.onClick.AddListener(() =>
        {
            GameManager.Instance.StartFreePlaySelection();
        });


        // 最初に表示を同期
        RefreshCardsVisual();

        // デバッグ表示
        ShowDebug("Lobby Start");
        ShowPlayersOnce();

        // 既存処理の前にこれを追加
        if (ForceOpenModePanelOnStart)
        {
            ForceOpenModePanelOnStart = false;
            // joinPanel 非表示、modePanel 表示に切り替え
            if (joinPanel) joinPanel.SetActive(false);
            if (modePanel) modePanel.SetActive(true);
            if (readyHint) readyHint.gameObject.SetActive(false);
            return; // 以降の初期化は不要（既にモード選択画面）
        }

        // …以下はあなたの既存初期化（joinPanelを表示して準備待ち）…
    }

    void Update()
    {
        // Q/R/U/P でトグル
        HandleToggleKey(KeyCode.Q);
        HandleToggleKey(KeyCode.R);
        HandleToggleKey(KeyCode.U);
        HandleToggleKey(KeyCode.P);

        // 2人以上&全員準備OK→Enter受付
        var joined = GameManager.Instance.players.Where(p => p.isJoined).ToList();
        bool canProceed = joined.Count >= 2 && joined.All(p => p.isReady);

        if (readyHint) readyHint.gameObject.SetActive(canProceed);

        if (canProceed && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            // モード選択へ
            if (joinPanel) joinPanel.SetActive(false);
            if (modePanel) modePanel.SetActive(true);
            ShowDebug("Enter → ModePanel");
        }
    }

    void HandleToggleKey(KeyCode kc)
    {
        if (!Input.GetKeyDown(kc)) return;

        var p = GameManager.Instance.players.Find(x => x.key == kc);
        if (p == null)
        {
            ShowDebug($"Key:{kc} pressed, but player not found (playerConfigs未設定?)");
            return;
        }

        // トグル順：未参加 → 参加 → 準備OK → 未参加
        if (!p.isJoined) { p.isJoined = true; p.isReady = false; }
        else if (!p.isReady) { p.isReady = true; }
        else { p.isReady = false; p.isJoined = false; }

        // 視覚更新＋デバッグ
        RefreshCardsVisual();
        ShowDebug($"Key:{kc}  Joined:{p.isJoined} Ready:{p.isReady}");
    }

    void RefreshCardsVisual()
    {
        // GameManagerのplayersとカードを同期
        ApplyToCard(KeyCode.Q, cardQ, nameQ, keyQ);
        ApplyToCard(KeyCode.R, cardR, nameR, keyR);
        ApplyToCard(KeyCode.U, cardU, nameU, keyU);
        ApplyToCard(KeyCode.P, cardP, nameP, keyP);
    }

    void ApplyToCard(KeyCode kc, Image card, TextMeshProUGUI nameLabel, TextMeshProUGUI keyLabel)
    {
        var p = GameManager.Instance.players.Find(x => x.key == kc);
        if (p == null || card == null || nameLabel == null || keyLabel == null) return;

        nameLabel.text = p.playerName;
        keyLabel.text = kc.ToString();

        // 基本色は card.color に設定済み（緑/青/赤/黄）
        var c = card.color;
        if (!p.isJoined) c.a = IdleAlpha;    // 未参加：薄め
        else if (!p.isReady) c.a = JoinedAlpha;  // 参加：中
        else c.a = ReadyAlpha;   // 準備OK：濃い
        card.color = c;

        // Outline/Shadow でさらに差をつける（付いていれば制御、無ければ何もしない）
        var outline = card.GetComponent<UnityEngine.UI.Outline>();
        var shadow = card.GetComponent<UnityEngine.UI.Shadow>();
        if (outline) outline.effectColor = p.isReady ? Color.white : new Color(1, 1, 1, 0.4f);
        if (shadow) shadow.effectDistance = p.isReady ? new Vector2(6, -6) : new Vector2(3, -3);

        // 文字コントラスト補強（背景が濃い時は白文字）
        var textColor = (c.a >= 0.75f) ? new Color(1, 1, 1, 0.95f) : new Color(0, 0, 0, 0.9f);
        nameLabel.color = textColor;
        keyLabel.color = textColor;
    }

    /// <summary>
    /// 左下のデバッグテキスト＆Consoleに状態を出す（debugText未設定でもOK）
    /// </summary>
    void ShowDebug(string msg)
    {
        // Console
        Debug.Log($"[Lobby] {msg}");

        if (!debugText) return;

        var list = GameManager.Instance.players;
        int joined = list.Count(p => p.isJoined);
        int ready = list.Count(p => p.isReady);

        debugText.text =
            $"[DEBUG] {msg}\nJoined:{joined} Ready:{ready}\n" +
            $"Q:{Flag(list, KeyCode.Q)}  R:{Flag(list, KeyCode.R)}  U:{Flag(list, KeyCode.U)}  P:{Flag(list, KeyCode.P)}";
    }

    void ShowPlayersOnce()
    {
        var list = GameManager.Instance.players;
        Debug.Log($"players.Count = {list.Count}");
        foreach (var p in list)
            Debug.Log($"player: {p.playerName} key={p.key}");
    }

    string Flag(System.Collections.Generic.List<PlayerData> list, KeyCode kc)
    {
        var p = list.Find(x => x.key == kc);
        if (p == null) return "--";
        return (p.isJoined ? "J" : "-") + (p.isReady ? "R" : "-");
    }
}
