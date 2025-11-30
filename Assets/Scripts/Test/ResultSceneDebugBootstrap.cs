using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ResultScene 単体で再生したとき用のテストデータブートストラップ。
/// - GameManager.Instance がいなければ「デバッグ用 GameManager」を生成して
///   ダミーのプレイヤーデータを流し込む。
/// - ロビーから普通に来たとき（GameManager がすでに存在＆参加者あり）は何もしない。
/// </summary>
[DefaultExecutionOrder(-200)]  // 他の Start より先に動きやすくする
public class ResultSceneDebugBootstrap : MonoBehaviour
{
    void Start()
    {
        // すでに本番用 GameManager がいて、参加者もいるなら何もしない
        if (GameManager.Instance != null && GameManager.Instance.GetJoinedPlayers().Count > 0)
        {
            Debug.Log("[ResultSceneDebugBootstrap] 本番 GameManager を検出 → 何もしません。");
            return;
        }

        // GameManager がいなければデバッグ用を生成
        if (GameManager.Instance == null)
        {
            Debug.Log("[ResultSceneDebugBootstrap] GameManager が見つからないのでデバッグ用を生成します。");
            var go = new GameObject("GameManager (Debug)");
            go.AddComponent<GameManager>();  // Awake が走るが、ここでは中身はまだ気にしない
        }
        else
        {
            Debug.Log("[ResultSceneDebugBootstrap] 空の GameManager はいるが参加者がいないのでテストデータを流し込みます。");
        }

        // ここまで来たら必ず Instance はいる
        var gm = GameManager.Instance;

        // ==== ダミープレイヤーを 3 人分つくる ====
        gm.players = new List<PlayerData>();

        // 1人目：スピード高め
        var p1 = new PlayerData(0, KeyCode.Q, "みどり", Color.green);
        p1.isJoined = true;
        p1.genrePoints["reflex"] = 18;  // スピード
        p1.genrePoints["mash"] = 10;  // たいりょく
        p1.genrePoints["hold"] = 8;   // しゅうちゅう
        gm.players.Add(p1);

        // 2人目：たいりょく高め
        var p2 = new PlayerData(1, KeyCode.R, "あお", Color.blue);
        p2.isJoined = true;
        p2.genrePoints["reflex"] = 9;
        p2.genrePoints["mash"] = 17;
        p2.genrePoints["hold"] = 11;
        gm.players.Add(p2);

        // 3人目：しゅうちゅう高め
        var p3 = new PlayerData(2, KeyCode.U, "あか", Color.red);
        p3.isJoined = true;
        p3.genrePoints["reflex"] = 7;
        p3.genrePoints["mash"] = 8;
        p3.genrePoints["hold"] = 19;
        gm.players.Add(p3);

        // 診断モード想定にしておく（念のため）
        gm.currentGameMode = GameMode.Diagnosis;

        // 動物タイプ＆ほめ言葉を自動計算
        gm.CalculateFinalResults();

        Debug.Log("[ResultSceneDebugBootstrap] テスト用プレイヤーデータをセットしました。参加者数: "
                  + gm.GetJoinedPlayers().Count);
    }
}
