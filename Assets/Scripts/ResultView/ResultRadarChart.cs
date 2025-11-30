using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 3軸（正三角形）レーダーチャートの【背景】を描画するコンポーネント。
/// - UGUI の Graphic を継承して Mesh を生成する方式。
/// - Canvas 上の RectTransform の中心を原点として描画する。
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class ResultRadarChart : Graphic
{
    [Header("レーダーチャートの半径(px)")]
    public float radius = 200f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var color32 = (Color32)color;

        // 中心点（RectTransform の中心）
        Vector2 center = Vector2.zero;

        // 正三角形の3方向（角度：90°, 210°, 330°）
        Vector2[] dirs = new Vector2[3];
        dirs[0] = new Vector2(0, 1);             // 上（reflex）
        dirs[1] = new Vector2(-0.8660254f, -0.5f); // 左下（mash）
        dirs[2] = new Vector2(0.8660254f, -0.5f); // 右下（hold）

        // 3頂点の位置
        Vector2[] pts = new Vector2[3];
        for (int i = 0; i < 3; i++)
            pts[i] = center + dirs[i] * radius;

        // 頂点追加（中心＋3点）
        vh.AddVert(center, color32, Vector2.zero); // 0
        vh.AddVert(pts[0], color32, Vector2.zero); // 1
        vh.AddVert(pts[1], color32, Vector2.zero); // 2
        vh.AddVert(pts[2], color32, Vector2.zero); // 3

        // 三角形3つで塗りつぶす
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(0, 2, 3);
        vh.AddTriangle(0, 3, 1);
    }
}
