using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PaintingNew : MonoBehaviour
{
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int Color1 = Shader.PropertyToID("_Color");

    private RenderTexture canvasTexture;
    private readonly List<Vector2> splineBuffer = new List<Vector2>();

    [SerializeField] private float previousBrushSize = 0f;

    // private float previousVelocity = 0f;
    [SerializeField] private Vector2 lastPosition;
    [SerializeField] private float lastMoveTime;

    [SerializeField] private int segCount = 40;

    [SerializeField] private Vector2 paintBrushSizeRange = new Vector2(0f, 60f);
    [SerializeField] private float speedFactor = 5;

    /*-------------------------------------------------------------------*/

    private RenderTexture texRender; //画布
    [SerializeField] private Material mat; //给定的shader新建材质
    [SerializeField] private Texture brushTypeTexture; //画笔纹理，半透明
    [SerializeField] private Color brushColor = Color.black;
    [SerializeField] private RawImage renderPanel; //使用UGUI的RawImage显示，方便进行添加UI,将pivot设为(0.5,0.5)

    /*-------------------------------------------------------------------*/
    //绘画区域
    private Vector2 rawMousePosition; //raw图片的左下角对应鼠标位置

    private float rawWidth; //raw图片宽度

    private float rawHeight; //raw图片长度

    /*-------------------------------------------------------------------*/
    //鼠标平滑跟随速度
    [SerializeField] private float mouseSmoothVelocity = 0.3f;
    private Vector2 _prevMousePoint;
    private Vector2 _prevSmoothMousePoint;
    private Vector2 _prevMouseChangeVector;

    /*-------------------------------------------------------------------*/

    //毛刺效果
    [SerializeField]private float targetOffset = 5;

    /*-------------------------------------------------------------------*/
    //速度相关
    private List<float> _velocityBuffer = new List<float>();
    private float _smoothVelocity;

    private readonly int velocityBufferSize = 5;

    /*-------------------------------------------------------------------*/

    private void Awake() {
        Application.targetFrameRate = 60;
    }

    private void Start() {
        InitPaintCanvas();

        // 显示到屏幕上
        DrawImage();
    }


    void Update() {
// 监听鼠标输入
        if (Input.GetMouseButtonDown(0)) {
            Vector2 mousePos = Input.mousePosition;
            MouseDown(mousePos);
        }
        else if (Input.GetMouseButton(0)) {
            Vector2 mousePos = Input.mousePosition;
            MouseMove(mousePos);
        }
        else if (Input.GetMouseButtonUp(0)) {
            Vector2 mousePos = Input.mousePosition;
            MouseUp(mousePos);
        }

        if (Input.GetKeyDown(KeyCode.C)) {
            Clear(texRender);
        }
    }

    private void InitPaintCanvas() {
        //raw图片鼠标位置，宽度计算
        rawWidth = renderPanel.rectTransform.sizeDelta.x;
        rawHeight = renderPanel.rectTransform.sizeDelta.y;
        Vector2 rawanchorPositon =
            new Vector2(renderPanel.rectTransform.anchoredPosition.x - renderPanel.rectTransform.sizeDelta.x / 2.0f,
                renderPanel.rectTransform.anchoredPosition.y - renderPanel.rectTransform.sizeDelta.y / 2.0f);
        //计算Canvas位置偏差
        Canvas canvas = renderPanel.canvas;
        Vector2 canvasOffset = RectTransformUtility.WorldToScreenPoint(Camera.main, canvas.transform.position) -
                               canvas.GetComponent<RectTransform>().sizeDelta / 2;
        //最终鼠标相对画布的位置
        rawMousePosition = rawanchorPositon + new Vector2(Screen.width / 2.0f, Screen.height / 2.0f) + canvasOffset;

        texRender = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        Clear(texRender);
    }

    void DrawImage() {
        renderPanel.texture = texRender;
    }

    private void MouseDown(Vector2 position) {
        ResetMouseSmoothing(position.x, position.y);
        // 鼠标按下初始化绘制
        previousBrushSize = 0;
        splineBuffer.Clear();
        splineBuffer.Add(position);
        lastPosition = position;
        lastMoveTime = Time.time;
        _velocityBuffer.Clear();
    }

    private void MouseMove(Vector2 position) {
        if (lastPosition == position) {
            // lastMoveTime = Time.time;
            return;
        }

        var temp = GetSmoothMousePoint((int)position.x, (int)position.y);
        // 鼠标拖动绘制
        DrawStroke(lastPosition, temp, false);
        lastPosition = temp;
        lastMoveTime = Time.time;
    }

    private void MouseUp(Vector2 position) {
        // 鼠标抬起结束绘制
        DrawStroke(lastPosition, position, true);
        lastPosition = position;
        lastMoveTime = Time.time;
    }

    // 重置鼠标平滑数据
    private void ResetMouseSmoothing(float a, float b) {
        _prevMousePoint = new Vector2(a, b);
        _prevSmoothMousePoint = new Vector2(a, b);
        _prevMouseChangeVector = Vector2.zero;
    }

    // 获取平滑后的鼠标位置
    private Vector2 GetSmoothMousePoint(float a, float b) {
        Vector2 mouseChange = new Vector2(a - _prevMousePoint.x, b - _prevMousePoint.y);
        Vector2 smoothMousePoint = _prevSmoothMousePoint;

        // 检查鼠标的变化是否方向相反，若是则做平滑调整
        if (Vector2.Dot(mouseChange, _prevMouseChangeVector) < 0) {
            smoothMousePoint = _prevMousePoint;
            // _lastRotation += Mathf.PI;
        }

        // 更新鼠标变化向量
        _prevMouseChangeVector = mouseChange;
        _prevMousePoint = new Vector2(a, b);

        // 平滑处理鼠标位置
        smoothMousePoint.x += mouseSmoothVelocity * (a - smoothMousePoint.x);
        smoothMousePoint.y += mouseSmoothVelocity * (b - smoothMousePoint.y);

        // 更新平滑后的鼠标位置
        _prevSmoothMousePoint = smoothMousePoint;

        return smoothMousePoint;
    }


    private void DrawStroke(Vector2 start, Vector2 end, bool brushEnded) {
        if (end == Vector2.zero) return;

        splineBuffer.Add(end);

        if (splineBuffer.Count > 3) {
            // 平滑速度计算
            float deltaTime = Time.time - lastMoveTime;
            float distance = Vector2.Distance(start, end);
            float velocity = distance / Mathf.Max(deltaTime * 1000f, 1f);

            // 平滑速度 (缓冲区计算平均速度)
            _velocityBuffer.Add(velocity);
            if (_velocityBuffer.Count > velocityBufferSize) {
                _velocityBuffer.RemoveAt(0);
            }

            _smoothVelocity = _velocityBuffer.Average(); // 平滑后的速度

            // 速度归一化（限制在 0 到 speedFactor 之间）
            float normalizedVelocity = Mathf.Clamp01(_smoothVelocity / speedFactor);

            // 笔刷大小反向映射 (速度快 -> 细；速度慢 -> 粗)
            float brushSize = Mathf.Lerp(
                paintBrushSizeRange.y, // 慢 -> 粗
                paintBrushSizeRange.x, // 快 -> 细
                normalizedVelocity);

            // 取4个点
            var points = splineBuffer;

            for (int j = 0, m = points.Count - 3; j < m; j++) {
                var p0 = points[j];
                var p1 = points[j + 1];
                var p2 = points[j + 2];
                var p3 = points[j + 3];
                var v0 = new Vector2((p2.x - p0.x) / 2, (p2.y - p0.y) / 2);
                var v1 = new Vector2((p3.x - p1.x) / 2, (p3.y - p1.y) / 2);

                // Catmull-Rom 样条曲线插值
                var tmp1 = (2 * p1.x - 2 * p2.x + v0.x + v1.x);
                var tmp2 = (-3 * p1.x + 3 * p2.x - 2 * v0.x - v1.x);
                var tmp3 = (2 * p1.y - 2 * p2.y + v0.y + v1.y);
                var tmp4 = (-3 * p1.y + 3 * p2.y - 2 * v0.y - v1.y);

                for (int i = 1, n = segCount + 1; i <= n; i++) {
                    float seg = (float)i / segCount;

                    float tX = (tmp1 * Mathf.Pow(seg, 3)) + (tmp2 * Mathf.Pow(seg, 2)) + v0.x * seg + p1.x;
                    float tY = (tmp3 * Mathf.Pow(seg, 3)) + (tmp4 * Mathf.Pow(seg, 2)) + v0.y * seg + p1.y;

                    // 平滑笔刷大小过渡
                    float interpolatedBrushSize = Mathf.Lerp(previousBrushSize, brushSize, seg);

                    float random = Random.Range(-targetOffset, targetOffset);
                    float xSign = Random.Range(0, 100) < 50 ? -1 : 1;
                    float ySign = Random.Range(0, 100) < 50 ? -1 : 1;

                    DrawBrush(texRender, tX + xSign * random, tY + ySign * random, interpolatedBrushSize);
                }
            }

            previousBrushSize = brushSize; // 更新笔刷大小
            splineBuffer.RemoveAt(0); // 移除旧的点
        }
    }

    void Clear(RenderTexture destTexture) {
        Graphics.SetRenderTarget(destTexture);
        GL.PushMatrix();
        GL.Clear(true, true, Color.white);
        GL.PopMatrix();
    }

    void DrawBrush(RenderTexture destTexture, float x, float y, float scale) {
        DrawBrush(destTexture, new Rect(x, y, brushTypeTexture.width, brushTypeTexture.height), scale);
    }

    void DrawBrush(RenderTexture destTexture, Rect destRect, float scale) {
        //增加鼠标位置根据raw图片位置换算。
        float left = (destRect.xMin - rawMousePosition.x) * Screen.width / rawWidth - destRect.width * scale / 2.0f;
        float right = (destRect.xMin - rawMousePosition.x) * Screen.width / rawWidth + destRect.width * scale / 2.0f;
        float top = (destRect.yMin - rawMousePosition.y) * Screen.height / rawHeight - destRect.height * scale / 2.0f;
        float bottom = (destRect.yMin - rawMousePosition.y) * Screen.height / rawHeight +
                       destRect.height * scale / 2.0f;

        Graphics.SetRenderTarget(destTexture);

        GL.PushMatrix();
        GL.LoadOrtho();

        mat.SetTexture(MainTex, brushTypeTexture);
        mat.SetColor(Color1, brushColor);
        mat.SetPass(0);

        GL.Begin(GL.QUADS);

        GL.TexCoord2(0.0f, 0.0f);
        GL.Vertex3(left / Screen.width, top / Screen.height, 0);
        GL.TexCoord2(1.0f, 0.0f);
        GL.Vertex3(right / Screen.width, top / Screen.height, 0);
        GL.TexCoord2(1.0f, 1.0f);
        GL.Vertex3(right / Screen.width, bottom / Screen.height, 0);
        GL.TexCoord2(0.0f, 1.0f);
        GL.Vertex3(left / Screen.width, bottom / Screen.height, 0);

        GL.End();
        GL.PopMatrix();
    }
}