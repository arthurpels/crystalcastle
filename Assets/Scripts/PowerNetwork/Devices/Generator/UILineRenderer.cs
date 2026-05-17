using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UILineRenderer : Graphic {
  [SerializeField]
  private float thickness = 2f;
  private Vector2[] _points;

  public void SetPoints(Vector2[] points) {
    _points = points;
    SetVerticesDirty();
  }

  protected override void OnPopulateMesh(VertexHelper vh) {
    vh.Clear();
    if (_points == null || _points.Length < 2)
      return;

    for (int i = 0; i < _points.Length - 1; i++)
      DrawSegment(vh, _points[i], _points[i + 1]);
  }

  void DrawSegment(VertexHelper vh, Vector2 start, Vector2 end) {
    Vector2 dir = (end - start).normalized;
    Vector2 normal = new Vector2(-dir.y, dir.x) * thickness * 0.5f;

    int idx = vh.currentVertCount;
    UIVertex vertex = UIVertex.simpleVert;
    vertex.color = color;

    vertex.position = start - normal;
    vh.AddVert(vertex);
    vertex.position = start + normal;
    vh.AddVert(vertex);
    vertex.position = end + normal;
    vh.AddVert(vertex);
    vertex.position = end - normal;
    vh.AddVert(vertex);

    vh.AddTriangle(idx, idx + 1, idx + 2);
    vh.AddTriangle(idx, idx + 2, idx + 3);
  }
}
