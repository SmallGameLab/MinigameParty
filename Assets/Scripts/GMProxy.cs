using UnityEngine;

/// <summary>
/// シーン内ボタンから GameManager.Singleton を呼ぶための橋渡し。
/// - ボタンはこの GMProxy を参照できる（同じシーンにあるから）
/// - 実処理は GameManager.Instance に委譲
/// </summary>
public class GMProxy : MonoBehaviour
{
    /// <summary>FreePlay：指定シーン名のミニゲームを1本だけ開始</summary>
    public void StartFreePlayByScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) { Debug.LogError("[GMProxy] sceneName が空"); return; }
        if (GameManager.Instance == null) { Debug.LogError("[GMProxy] GameManager.Instance が見つかりません（LobbyScene経由で起動してください）"); return; }
        GameManager.Instance.StartFreePlay(sceneName);
    }

    /// <summary>ロビー（初期状態）に戻る</summary>
    public void BackToLobby()
    {
        if (GameManager.Instance == null) { Debug.LogError("[GMProxy] GameManager.Instance 不在"); return; }
        GameManager.Instance.LoadLobbyScene();
    }

    /// <summary>ロビーのモード選択を開いた状態で戻る（必要なら）</summary>
    public void BackToModeSelect()
    {
        if (GameManager.Instance == null) { Debug.LogError("[GMProxy] GameManager.Instance 不在"); return; }
        // もし LoadLobbyToModeSelection() が GameManager にあるならこちらを使う
        // GameManager.Instance.LoadLobbyToModeSelection();
        GameManager.Instance.LoadLobbyScene(); // 無ければ通常戻りでもOK
    }

    /// <summary>診断を開始（必要時だけ）</summary>
    public void StartDiagnosis()
    {
        if (GameManager.Instance == null) { Debug.LogError("[GMProxy] GameManager.Instance 不在"); return; }
        GameManager.Instance.StartDiagnosis();
    }
}
