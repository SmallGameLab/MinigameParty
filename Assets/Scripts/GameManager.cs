using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// ゲームの大域ステート
public enum GameMode { Diagnosis, FreePlay }

[Serializable]
public class PlayerData
{
    public int id;
    public KeyCode key;
    public string playerName;
    public Color playerColor;
    public bool isJoined;
    public bool isReady;

    public Dictionary<string, int> genrePoints = new Dictionary<string, int>();
    public int lastGameRawScore;
    public int lastGamePoints;
    public string determinedAnimalType = "";
    public string complimentMessage = "";

    public PlayerData(int id, KeyCode key, string name, Color color)
    {
        this.id = id; this.key = key; this.playerName = name; this.playerColor = color;
        ResetForNewGame();
    }

    public void ResetForNewGame()
    {
        isJoined = false; isReady = false;
        genrePoints = new Dictionary<string, int> { { "reflex", 0 }, { "mash", 0 }, { "hold", 0 } };
        lastGameRawScore = 0; lastGamePoints = 0;
        determinedAnimalType = ""; complimentMessage = "";
    }
}

/// ミニゲームの“辞書”。Prefabは使わず、sceneName で遷移する
[Serializable]
public struct MiniGameInfo
{
    public string sceneName;  // Build Settings 登録名そのまま
    public string gameName;   // 画面表示用（任意）
    public string genre;      // "reflex" | "mash" | "hold"
}

[Serializable] public struct PointEntry { public int rank; public int points; }
[Serializable] public struct PointTable { public int playerCount; public List<PointEntry> entries; }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Players")]
    public List<PlayerData> players = new List<PlayerData>();
    [Serializable] public struct PlayerInitialConfig { public KeyCode key; public string defaultName; public Color color; }
    public List<PlayerInitialConfig> playerConfigs = new List<PlayerInitialConfig>();

    [Header("MiniGames (Scene-Driven)")]
    public List<MiniGameInfo> allMiniGames = new List<MiniGameInfo>();   // ここに sceneName / gameName / genre を登録（Inspector）

    // 診断モード用キュー（sceneName ベース）
    private List<MiniGameInfo> diagnosisQueue = new List<MiniGameInfo>();
    private int diagnosisIndex = 0;

    // FreePlay 単発起動用
    private MiniGameInfo? currentFreePlayGame = null;

    [Header("Points")]
    public List<PointTable> pointTables = new List<PointTable>();

    [Header("Global")]
    public GameMode currentGameMode;

    // 任意：Zキーでロビーへ戻す（仕様の最後の条項）
    [SerializeField] private bool enableGlobalZBack = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePlayers();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (enableGlobalZBack && Input.GetKeyDown(KeyCode.Z))
        {
            LoadLobbyScene();
        }
    }

    void InitializePlayers()
    {
        players.Clear();
        for (int i = 0; i < playerConfigs.Count; i++)
        {
            var cfg = playerConfigs[i];
            players.Add(new PlayerData(i, cfg.key, cfg.defaultName, cfg.color));
        }
    }

    // ===== 公開API（ロビーや各シーンから呼ぶ） =====

    /// ロビーから：診断モードを開始（6本のシーンを組んで最初へ遷移）
    public void StartDiagnosis()
    {
        currentGameMode = GameMode.Diagnosis;
        BuildDiagnosisQueue();      // 6本選定
        diagnosisIndex = 0;
        if (diagnosisQueue.Count == 0) { Debug.LogError("[GM] 診断用のミニゲームがありません"); return; }
        LoadSceneSafe(diagnosisQueue[0].sceneName);
    }

    /// FreePlay 選択から：1本だけシーンを指定して開始
    public void StartFreePlay(string sceneName)
    {
        var info = allMiniGames.FirstOrDefault(m => m.sceneName == sceneName);
        if (string.IsNullOrEmpty(info.sceneName))
        {
            Debug.LogError($"[GM] FreePlay: scene '{sceneName}' が allMiniGames に見つかりません");
            return;
        }
        currentGameMode = GameMode.FreePlay;
        currentFreePlayGame = info;
        LoadSceneSafe(info.sceneName);
    }

    // ===== 内部：診断6本の組み立て =====
    void BuildDiagnosisQueue()
    {
        diagnosisQueue.Clear();

        // ジャンル別シャッフル → 各2本 → 合体 → 全体シャッフル
        var reflex = allMiniGames.Where(g => g.genre == "reflex").OrderBy(_ => UnityEngine.Random.value).Take(2);
        var mash   = allMiniGames.Where(g => g.genre == "mash").OrderBy(_ => UnityEngine.Random.value).Take(2);
        var hold   = allMiniGames.Where(g => g.genre == "hold").OrderBy(_ => UnityEngine.Random.value).Take(2);

        diagnosisQueue.AddRange(reflex);
        diagnosisQueue.AddRange(mash);
        diagnosisQueue.AddRange(hold);

        diagnosisQueue = diagnosisQueue.OrderBy(_ => UnityEngine.Random.value).ToList();
    }

    // ===== ポイント計算（仕様どおり：Standard Competition Ranking） =====
    public void CalculatePoints(List<(string name, int rawScore)> results, bool lowerScoreIsBetter)
    {
        var joined = GetJoinedPlayers();
        if (joined.Count == 0 || results == null || results.Count == 0) return;

        var ordered = lowerScoreIsBetter
            ? results.OrderBy(r => r.rawScore).ToList()
            : results.OrderByDescending(r => r.rawScore).ToList();

        var table = pointTables.FirstOrDefault(t => t.playerCount == joined.Count);
        if (table.entries == null || table.entries.Count == 0)
        {
            Debug.LogError("[GM] ポイントテーブル未設定");
            return;
        }

        int i = 0;
        while (i < ordered.Count)
        {
            int score = ordered[i].rawScore;
            int start = i;
            while (i < ordered.Count && ordered[i].rawScore == score) i++;
            int end = i - 1; // 同点グループの最後
            int rankForPoints = end + 1; // 1-based
            var entry = table.entries.FirstOrDefault(e => e.rank == rankForPoints);
            int pts = (entry.points > 0) ? entry.points : table.entries.Last().points;

            for (int j = start; j <= end; j++)
            {
                var p = players.FirstOrDefault(x => x.playerName == ordered[j].name);
                if (p != null) p.lastGamePoints = pts;
            }
        }

        // どのジャンルのゲームだったかを特定（Diagnosis中はキュー、FreePlay中は currentFreePlayGame ）
        string genre = "";
        if (currentGameMode == GameMode.Diagnosis && diagnosisIndex < diagnosisQueue.Count)
        {
            // diagnosisIndex は「このラウンドの終了時点」なので現在ラウンドは diagnosisIndex
            genre = diagnosisQueue[diagnosisIndex].genre;
        }
        else if (currentGameMode == GameMode.FreePlay && currentFreePlayGame.HasValue)
        {
            genre = currentFreePlayGame.Value.genre;
        }

        if (!string.IsNullOrEmpty(genre))
        {
            foreach (var p in joined)
                p.genrePoints[genre] += p.lastGamePoints;
        }
    }

    // ===== 診断の最終判定 =====
    public void CalculateFinalResults()
    {
        foreach (var p in GetJoinedPlayers())
        {
            p.determinedAnimalType = DetermineAnimalType(p.genrePoints);
            p.complimentMessage = GenerateCompliment(p.genrePoints);
        }
    }

    private string DetermineAnimalType(Dictionary<string, int> points)
    {
        int r = points["reflex"], m = points["mash"], h = points["hold"];
        bool top(int x) => x >= 16; bool mid(int x) => x >= 10 && x <= 15;

        // 上位
        if (top(r) && top(m) && top(h)) return "ライオン";
        if (top(r) && top(m) && !top(h)) return "オオカミ";
        if (top(r) && top(h) && !top(m)) return "チーター";
        if (top(m) && top(h) && !top(r)) return "ゴリラ";
        if (top(r) && !top(m) && !top(h)) return "うさぎ";
        if (top(m) && !top(r) && !top(h)) return "くま";
        if (top(h) && !top(r) && !top(m)) return "ぞう";

        // 中間
        if (mid(r) && mid(m) && mid(h)) return "パンダ";
        if (mid(r) && mid(m) && !mid(h)) return "イヌ";
        if (mid(r) && mid(h) && !mid(m)) return "ネコ";
        if (mid(m) && mid(h) && !mid(r)) return "サル";
        if (mid(r) && !mid(m) && !mid(h)) return "リス";
        if (mid(m) && !mid(r) && !mid(h)) return "ペンギン";
        if (mid(h) && !mid(r) && !mid(m)) return "カメ";

        return "ひよこ";
    }

    private string GenerateCompliment(Dictionary<string, int> points)
    {
        int r = points["reflex"], m = points["mash"], h = points["hold"];
        int max = Mathf.Max(r, m, h);
        if (max <= 0) return "つぎは がんばろう！";

        bool topR = (r == max), topM = (m == max), topH = (h == max);
        int topCount = (topR ? 1 : 0) + (topM ? 1 : 0) + (topH ? 1 : 0);

        if (topCount == 3) return "なんでもできるね！いろんなことにチャレンジしてみよう！";
        if (topCount == 2)
        {
            if (topR && topM) return "動くのがじょうずだね！スポーツでかつやくできそう！";
            if (topR && topH) return "すばやくてしっかりできるね！ピアノやダンスが得意そう！";
            if (topM && topH) return "がんばりやさんだね！書道や工作が得意そう！";
        }
        if (topCount == 1)
        {
            if (topR) return "はやく動けるね！ドッジボールやかけっこが得意そう！";
            if (topM) return "たくさん動けるね！サッカーや水泳が得意そう！";
            if (topH) return "よくがんばれたね！パズルや読書が得意そう！";
        }
        return "いろんなことができてすごいね！";
    }

    // ===== 便利関数 =====
    public List<PlayerData> GetJoinedPlayers() => players.Where(p => p.isJoined).ToList();

    public void LoadLobbyScene()
    {
        // 新規開始に向けてクリーンアップ
        foreach (var p in players) p.ResetForNewGame();
        diagnosisQueue.Clear(); diagnosisIndex = 0; currentFreePlayGame = null;
        SceneManager.LoadScene("LobbyScene");
    }

    void LoadResultScene() => SceneManager.LoadScene("ResultScene");

    void LoadFreePlaySelectScene()
    {
        // 後フェーズであなたが作る FreePlaySelectScene のシーン名に合わせてください
        SceneManager.LoadScene("FreePlaySelectScene");
        currentFreePlayGame = null;
    }

    void LoadSceneSafe(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[GM] sceneName が空です");
            return;
        }
        // Build Settings 未登録だとエラーになる点に注意
        SceneManager.LoadScene(sceneName);
    }

    // ===== 追加：ポイントだけ先に試算（状態は書き換えない）=====
    public Dictionary<string, int> PreviewPoints(List<(string name, int rawScore)> results, bool lowerScoreIsBetter)
    {
        var joined = GetJoinedPlayers();
        var map = new Dictionary<string, int>();
        if (joined.Count == 0 || results == null || results.Count == 0) return map;

        var ordered = lowerScoreIsBetter
            ? results.OrderBy(r => r.rawScore).ToList()
            : results.OrderByDescending(r => r.rawScore).ToList();

        var table = pointTables.FirstOrDefault(t => t.playerCount == joined.Count);
        if (table.entries == null || table.entries.Count == 0) return map;

        int i = 0;
        while (i < ordered.Count)
        {
            int score = ordered[i].rawScore;
            int start = i;
            while (i < ordered.Count && ordered[i].rawScore == score) i++;
            int end = i - 1; // 同点グループ最後
            int rankForPoints = end + 1;
            var entry = table.entries.FirstOrDefault(e => e.rank == rankForPoints);
            int pts = (entry.points > 0) ? entry.points : table.entries.Last().points;

            for (int j = start; j <= end; j++)
            {
                string nm = ordered[j].name;
                map[nm] = pts;
            }
        }
        return map;
    }

    // ===== 追加：結果を確定して次へ（従来の ReportRoundResult の中身）=====
    public void CommitRoundResult(List<(string name, int rawScore)> results, bool lowerScoreIsBetter)
    {
        CalculatePoints(results, lowerScoreIsBetter);

        if (currentGameMode == GameMode.Diagnosis)
        {
            diagnosisIndex++;
            if (diagnosisIndex >= diagnosisQueue.Count)
            {
                CalculateFinalResults();
                LoadResultScene();
            }
            else
            {
                LoadSceneSafe(diagnosisQueue[diagnosisIndex].sceneName);
            }
        }
        else
        {
            LoadFreePlaySelectScene();
        }
    }

    public void ReportRoundResult(List<(string name, int rawScore)> results, bool lowerScoreIsBetter)
    {
        // 後方互換の窓口：実体は CommitRoundResult へ
        CommitRoundResult(results, lowerScoreIsBetter);
    }
}
