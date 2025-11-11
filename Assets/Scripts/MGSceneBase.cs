using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 各ミニゲーム“シーン”の共通土台（アタッチ不可）
/// - プレビュー: タイトル/説明/サムネを Inspector から設定して表示
/// - カウントダウン: 3,2,1,GO!
/// - プレイ: 継承側で PlayRound 実装
/// - 結果表示: ResultPanel/LinesRoot の事前配置オブジェクトに中身だけ流し込み
/// </summary>
public abstract class MGSceneBase : MonoBehaviour
{
    // ====== 継承側で最低限指定するメタ ======
    protected abstract bool LowerScoreIsBetter { get; }   // 反応時間など「小さい方が良い」なら true
    [SerializeField] private string displayGameName;      // 画面に出すタイトル（未入力ならfallback）

    // ====== プレビューUI（Inspectorで差込） ======
    [Header("Preview Panel")]
    [SerializeField] private GameObject previewPanel;
    [SerializeField] private TextMeshProUGUI previewTitle;
    [SerializeField] private TextMeshProUGUI previewDesc;
    [SerializeField] private Image previewThumb;          // サムネ表示先
    [SerializeField] private Sprite previewThumbSprite;   // 差し込み用スプライト（Inspectorで設定）

    // ====== カウントダウンUI ======
    [Header("Countdown Panel")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TextMeshProUGUI countdownLabel;

    // ====== プレイUI（各ゲームで自由に） ======
    [Header("Play Panel")]
    [SerializeField] private GameObject playPanel;

    // ====== 結果UI（LinesRootの既存子に流し込む） ======
    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultHeader;
    [SerializeField] private Transform linesRoot; // 子に Line_1..4 を置いておく

    [Serializable]
    private class LineWidgets
    {
        public GameObject root;            // Line_x 自体
        public Image icon;                 // カラー丸/枠など
        public TextMeshProUGUI nameLabel;  // プレイヤー名
        public TextMeshProUGUI scoreLabel; // raw score文字列 e.g. "123 ms"
        public TextMeshProUGUI pointLabel; // 付与ポイント e.g. "8 pt"
    }
    [SerializeField] private LineWidgets[] lineSlots = new LineWidgets[4]; // LinesRootの子に対応

    // ====== オプション: SE（演出用フック） ======
    [Header("Audio (optional)")]
    [SerializeField] private AudioSource seSource;
    [SerializeField] private AudioClip sePreviewNext;   // プレビューから進む時
    [SerializeField] private AudioClip seCountdown;     // カウントダウンtick
    [SerializeField] private AudioClip seGo;            // GO!
    [SerializeField] private AudioClip seResultPop;     // ラインが「ぴぴぴ」と広がる時
    [SerializeField] private AudioClip seResultType;    // 動物タイプ「ドンッ」など（後でResultSceneで使用）

    protected virtual void Start()
    {
        // 画面状態初期化
        SafeSetActive(previewPanel, true);
        SafeSetActive(countdownPanel, false);
        SafeSetActive(playPanel, false);
        SafeSetActive(resultPanel, false);

        // プレビューUIへInspectorの値を反映
        if (previewTitle) previewTitle.text = string.IsNullOrWhiteSpace(displayGameName) ? "MiniGame" : displayGameName;
        if (previewThumb && previewThumbSprite) previewThumb.sprite = previewThumbSprite;
        // previewDesc は各シーンで自由にテキスト編集できる（Inspectorの静的文言をそのまま使う）

        // 開始フロー
        StartCoroutine(MainFlow());
    }

    IEnumerator MainFlow()
    {
        // プレビュー待ち（Enterまたはクリックは、UIのButtonでこの OnPreviewNext を結線）
        yield return WaitPreviewProceed();

        // カウントダウン
        yield return StartCountdown();

        // プレイ
        var results = new List<(string name, int rawScore)>();
        yield return PlayRound(r => results = r ?? new List<(string, int)>());

        // 結果
        yield return ShowResult(results);

        // 診断 or フリープレイの戻り
        GameManager.Instance.ReportRoundResult(results, LowerScoreIsBetter);
    }

    // ====== UI 切替 ======
    IEnumerator WaitPreviewProceed()
    {
        SafeSetActive(previewPanel, true);
        bool proceed = false;
        // マウスやEnterで進めたい場合は、PreviewPanelのClickCatcher(Button)にこの関数を結線
        void local() => proceed = true;
        // ここでは Enter キーのバックアップもつけておく
        while (!proceed)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                proceed = true;
            yield return null;
        }
        PlaySE(sePreviewNext);
        SafeSetActive(previewPanel, false);
    }

    IEnumerator StartCountdown()
    {
        SafeSetActive(countdownPanel, true);
        if (countdownLabel) countdownLabel.text = "3";
        PlaySE(seCountdown);
        yield return new WaitForSeconds(1f);
        if (countdownLabel) countdownLabel.text = "2";
        PlaySE(seCountdown);
        yield return new WaitForSeconds(1f);
        if (countdownLabel) countdownLabel.text = "1";
        PlaySE(seCountdown);
        yield return new WaitForSeconds(1f);
        if (countdownLabel) countdownLabel.text = "<color=#22c55e>GO!</color>";
        PlaySE(seGo);
        yield return new WaitForSeconds(0.4f);
        SafeSetActive(countdownPanel, false);
    }

    // ====== 各ミニゲーム固有ロジック（継承側で実装） ======
    protected abstract IEnumerator PlayRound(Action<List<(string name, int rawScore)>> onFinish);

    // ====== 結果：LinesRootの子UIに流し込む ======
    IEnumerator ShowResult(List<(string name, int rawScore)> results)
    {
        SafeSetActive(playPanel, false);
        SafeSetActive(resultPanel, true);

        if (resultHeader) resultHeader.text = "けっか";

        // 参加プレイヤー＆ポイント参照
        var joined = GameManager.Instance.GetJoinedPlayers();
        // raw の並び順は昇順/降順どちらでもOK（順位ポイントはGameManager側で計算するためここは表示用）
        var byName = results.ToDictionary(x => x.name, x => x.rawScore);
        var pointsMap = joined.ToDictionary(p => p.playerName, p => p.lastGamePoints);

        // Lines の初期化
        for (int i = 0; i < lineSlots.Length; i++)
        {
            if (lineSlots[i]?.root) lineSlots[i].root.SetActive(false);
        }

        // 表示行を上から joined 順に埋める
        for (int i = 0; i < joined.Count && i < lineSlots.Length; i++)
        {
            var p = joined[i];
            var w = lineSlots[i];
            if (w == null || w.root == null) continue;

            w.root.SetActive(true);

            if (w.nameLabel) w.nameLabel.text = p.playerName;
            if (w.scoreLabel) w.scoreLabel.text = byName.TryGetValue(p.playerName, out var raw)
                                                  ? FormatRawScore(raw) : "--";
            if (w.pointLabel) w.pointLabel.text = pointsMap.TryGetValue(p.playerName, out var pt)
                                                  ? $"{pt} pt" : "- pt";
            if (w.icon) w.icon.color = p.playerColor;

            // ちょっとした「ぴぴぴ」演出
            PlaySE(seResultPop);
            yield return new WaitForSeconds(0.06f);
        }

        // クリック/Enterで確定（GameManager.ReportRoundResult へ）
        bool proceed = false;
        while (!proceed)
        {
            if (Input.GetMouseButtonDown(0) ||
                Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                proceed = true;
            }
            yield return null;
        }
    }

    // ====== 表示用のスコア整形（必要に応じて拡張） ======
    protected virtual string FormatRawScore(int raw)
    {
        // デフォルト: ms 表示
        return $"{raw} ms";
    }

    // ====== ヘルパ ======
    protected void SafeSetActive(GameObject go, bool v) { if (go) go.SetActive(v); }
    protected void PlaySE(AudioClip clip) { if (seSource && clip) seSource.PlayOneShot(clip); }
}
