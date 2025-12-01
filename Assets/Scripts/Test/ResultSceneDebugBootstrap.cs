using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ResultScene 単体で再生したとき用のテストデータブートストラップ（ジャンルポイントのみランダム）
/// - GameManager.Instance がいなければデバッグ用 GameManager を生成
/// - プレイヤー3〜4人を固定で作り、ジャンルポイントのみをランダムに設定
/// - ロビーから来た場合（GameManager が既に実データ持ち）は何もしない
/// </summary>
[DefaultExecutionOrder(-200)]
public class ResultSceneDebugBootstrap : MonoBehaviour
{
    // ランダムにする範囲（0〜20）
    [SerializeField] private int minPoint = 0;
    [SerializeField] private int maxPoint = 20;

    void Start()
    {
        // 本番 GameManager かつ参加者あり → 何もしない
        if (GameManager.Instance != null &&
            GameManager.Instance.GetJoinedPlayers().Count > 0)
        {
            Debug.Log("[Bootstrap] 本番データ検出 → テストモード無効化");
            return;
        }

        // GameManager が Scene にいなければデバッグ用を生成
        if (GameManager.Instance == null)
        {
            Debug.Log("[Bootstrap] GameManager が無いのでデバッグ用を生成します");
            var go = new GameObject("GameManager (Debug)");
            go.AddComponent<GameManager>();
        }

        var gm = GameManager.Instance;

        // ===== プレイヤーを固定で 3～4 人作る =====
        gm.players = new List<PlayerData>();

        // 固定プレイヤー設定
        var fixedPlayers = new (string name, KeyCode key, Color color)[]
        {
            ("みどり", KeyCode.Q, Color.green),
            ("あお",    KeyCode.R, Color.blue),
            ("あか",    KeyCode.U, Color.red),
            ("きいろ", KeyCode.P, new Color(1f,0.9f,0.2f))
        };

        // 今回は 4 人固定にして問題なし（必要なら 3 に減らしてもOK）
        int playerCount = fixedPlayers.Length;

        for (int i = 0; i < playerCount; i++)
        {
            var fp = fixedPlayers[i];
            var p = new PlayerData(i, fp.key, fp.name, fp.color);
            p.isJoined = true;

            // ===== ★ ジャンルポイントのみランダム生成 =====
            p.genrePoints["reflex"] = Random.Range(minPoint, maxPoint + 1);
            p.genrePoints["mash"] = Random.Range(minPoint, maxPoint + 1);
            p.genrePoints["hold"] = Random.Range(minPoint, maxPoint + 1);

            gm.players.Add(p);
        }

        gm.currentGameMode = GameMode.Diagnosis;

        // 動物タイプ & 褒め言葉を自動計算
        gm.CalculateFinalResults();

        Debug.Log("[Bootstrap] ランダムポイントを割り当てました → プレイヤー: "
                  + gm.GetJoinedPlayers().Count);
    }
}
