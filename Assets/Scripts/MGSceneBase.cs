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

    // ★ クリックで進める用（PreviewPanel上の透明ボタンに割り当て）
    private bool previewProceedRequested = false;
    public void OnPreviewClick() => previewProceedRequested = true;

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
    //[SerializeField] private TextMeshProUGUI resultHeader; //Unity上で変更管理するので一旦消す
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

    [Header("Optional Result Cue (lag mask)")]
    [SerializeField] private GameObject resultCuePanel;  // 任意の全画面パネル（最初は非表示）
    [SerializeField] private float resultCueDuration = 0.6f; // 稼ぐ時間（将来SE長さに合わせる）

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
        // previewDesc は Inspector の静的文言をそのまま使用（シーンで直書きOK）

        // 開始フロー
        StartCoroutine(MainFlow());
    }

    IEnumerator MainFlow()
    {
        // プレビュー → カウントダウン
        yield return WaitPreviewProceed();
        yield return StartCountdown();
        SafeSetActive(playPanel, true);

        // プレイ実行
        var results = new List<(string name, int rawScore)>();
        yield return PlayRound(r => results = r ?? new List<(string, int)>());

        // ★ここで先にポイント計算（GameManagerの状態を更新）
        var gm = GameManager.Instance;
        gm.CalculatePoints(results, LowerScoreIsBetter);

        // ★演出で“間”を作って裏でUIのレイアウトを落ち着かせる
        if (resultCuePanel)
        {
            resultCuePanel.SetActive(true);
            yield return new WaitForSeconds(resultCueDuration); // 将来SEに差し替え可
            resultCuePanel.SetActive(false);
        }

        // リザルトUI表示
        yield return ShowResult(results);

        // ★進行だけ実行（計算は済んでいるため）
        gm.AdvanceAfterRound(false);   // ← 既存メソッドそのまま使用
    }

    // ====== UI 切替 ======
    IEnumerator WaitPreviewProceed()
    {
        previewProceedRequested = false;
        SafeSetActive(previewPanel, true);

        while (!previewProceedRequested)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                previewProceedRequested = true;
            yield return null;
        }

        SafeSetActive(previewPanel, false);
    }

    IEnumerator StartCountdown()
    {
        SafeSetActive(countdownPanel, true);
        if (countdownLabel) countdownLabel.text = "3";
        yield return new WaitForSeconds(1f);
        if (countdownLabel) countdownLabel.text = "2";
        yield return new WaitForSeconds(1f);
        if (countdownLabel) countdownLabel.text = "1";
        yield return new WaitForSeconds(1f);
        if (countdownLabel) countdownLabel.text = "<color=#22c55e>GO!</color>";
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

        // フェード用
        var cg = resultPanel.GetComponent<CanvasGroup>();
        if (!cg) cg = resultPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // 参加者＆辞書化（軽量化）
        var joined = GameManager.Instance.GetJoinedPlayers();
        var rawByName = new Dictionary<string, int>(results.Count);
        for (int i = 0; i < results.Count; i++) rawByName[results[i].name] = results[i].rawScore;

        // まず全行を非表示
        for (int i = 0; i < lineSlots.Length; i++)
            if (lineSlots[i]?.root) lineSlots[i].root.SetActive(false);

        // 一括で中身を流し込む
        for (int i = 0; i < joined.Count && i < lineSlots.Length; i++)
        {
            var p = joined[i];
            var w = lineSlots[i];
            if (w == null || w.root == null) continue;

            w.root.SetActive(true);

            if (w.nameLabel) w.nameLabel.text = p.playerName;
            if (w.scoreLabel) w.scoreLabel.text = rawByName.TryGetValue(p.playerName, out var raw)
                                                  ? FormatRawScore(raw) : "--";
            if (w.pointLabel) w.pointLabel.text = $"{p.lastGamePoints} pt";

            if (w.icon)
            {
                var c = p.playerColor; c.a = 1f; // 参加者は100%
                w.icon.color = c;
            }
        }

        // レイアウト確定 → フェードイン
        Canvas.ForceUpdateCanvases();
        float t = 0f, dur = 0.12f;
        while (t < dur) { t += Time.deltaTime; cg.alpha = t / dur; yield return null; }
        cg.alpha = 1f;

        // 確定入力待ち
        bool proceed = false;
        while (!proceed)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                proceed = true;
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
}
