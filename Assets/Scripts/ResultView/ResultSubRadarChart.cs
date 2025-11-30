using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 個々のプレイヤーのスコアを受け取り、
/// 3軸レーダーチャートの【塗りつぶしポリゴン】を描画するコンポーネント。
/// ResultRadarChart の子オブジェクトとして使う想定。
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class ResultSubRadarChart : Graphic
{
    [Header("各ジャンルの値（0〜1 に正規化した値）")]
    [Range(0f, 1f)] public float reflex; // スピード
    [Range(0f, 1f)] public float mash;   // たいりょく
    [Range(0f, 1f)] public float hold;   // しゅうちゅう力

    [Header("レーダーチャートの半径(px)")]
    public float radius = 200f;  // 最大でも Rect の半径までにクランプする

    /// <summary>
    /// メッシュ描画本体
    /// </summary>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        // RectTransform の矩形を取得
        Rect r = GetPixelAdjustedRect();

        // 幅や高さが ほぼ0 のときは描かない（レイアウト前対策）
        if (r.width <= 1f || r.height <= 1f)
        {
            // デバッグ用ログ
            Debug.Log($"[ResultSubRadarChart] Skip draw: rect too small w={r.width}, h={r.height}", this);
            return;
        }

        Color32 c = (Color32)color;
        Vector2 center = r.center;

        // Rect のサイズに応じて最大半径を制限
        float maxR = Mathf.Min(r.width, r.height) * 0.5f;
        float useRadius = Mathf.Min(radius, maxR);

        // 正三角形の3方向（ResultRadarChart と同じ向き）
        Vector2[] dirs = new Vector2[3];
        dirs[0] = new Vector2(0, 1);               // reflex
        dirs[1] = new Vector2(-0.8660254f, -0.5f); // mash
        dirs[2] = new Vector2(0.8660254f, -0.5f);  // hold

        float[] vals = new float[3] { reflex, mash, hold };

        // すべて0なら、ほんの少しだけ広げて「存在は見える」ようにする保険
        if (Mathf.Approximately(vals[0], 0f) &&
            Mathf.Approximately(vals[1], 0f) &&
            Mathf.Approximately(vals[2], 0f))
        {
            Debug.Log("[ResultSubRadarChart] all values are 0 → tiny triangle as fallback", this);
            vals[0] = vals[1] = vals[2] = 0.05f;
        }

        Vector2[] pts = new Vector2[3];
        for (int i = 0; i < 3; i++)
        {
            float v = Mathf.Clamp01(vals[i]);
            pts[i] = center + dirs[i] * (useRadius * v);
        }

        // 頂点（中心＋3点）
        vh.AddVert(center, c, Vector2.zero); // 0
        vh.AddVert(pts[0], c, Vector2.zero); // 1
        vh.AddVert(pts[1], c, Vector2.zero); // 2
        vh.AddVert(pts[2], c, Vector2.zero); // 3

        // 三角形3つ
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(0, 2, 3);
        vh.AddTriangle(0, 3, 1);

        // デバッグログ
        Debug.Log($"[ResultSubRadarChart] Draw: rect=({r.width}x{r.height}) radius={useRadius} vals=({reflex}, {mash}, {hold})", this);
    }

    /// <summary>
    /// スコアをセットして再描画するためのヘルパ
    /// </summary>
    public void SetValues(float reflex01, float mash01, float hold01, Color fillColor)
    {
        reflex = Mathf.Clamp01(reflex01);
        mash = Mathf.Clamp01(mash01);
        hold = Mathf.Clamp01(hold01);

        // 塗りつぶしの色（プレイヤーカラーなど）: アルファは必ず1にする
        var c = fillColor;
        c.a = 1f;
        color = c;

        Debug.Log($"[ResultSubRadarChart] SetValues r={reflex} m={mash} h={hold} color={color}", this);

        // メッシュ描画の再要求
        SetVerticesDirty();
    }

    /// <summary>
    /// RectTransform のサイズが変わったときにも再描画する。
    /// （レイアウト確定後にもう一度 OnPopulateMesh が呼ばれるようにする）
    /// </summary>
    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        SetVerticesDirty();
    }
}