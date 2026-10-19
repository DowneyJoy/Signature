using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TuanjieAI.UI
{
    /// <summary>
    /// 签名板：在 RawImage 区域内用鼠标/触摸写字，支持保存为带透明通道的 PNG。
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class SignPad : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {public enum DrawMode
        {
            Pen,
            Eraser
        }
        [Header("笔刷设置")]
        public Color brushColor = Color.black;
        public int brushSize = 6;
        public int textureWidth = 1570;
        public int textureHeight = 653;

        [Header("保存设置")]
        public string saveFolder = "Signatures";
        public string fileNamePrefix = "Signature";
        private static readonly Color Transparent = new Color(0, 0, 0, 0);
        private Texture2D _drawTexture;
        private RawImage _rawImage;
        private bool _isDrawing;
        private Vector2 _lastPoint;
        private bool _hasContent;
        public GameRoot gameRoot;
        public OCRSample ocrSample;
        private DrawMode _mode = DrawMode.Pen;
        
        /// <summary>当前是否处于橡皮擦模式。</summary>
        public bool IsEraser => _mode == DrawMode.Eraser;
        [Header("橡皮擦设置")]
        public int eraserSize = 20;
        private int CurrentRadius => IsEraser ? eraserSize : brushSize;
        private Color CurrentColor => IsEraser ? Transparent : brushColor;
        private void Start()
        {
            _rawImage = GetComponent<RawImage>();

            // 创建透明画布纹理
            _drawTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            ClearTexture();

            _rawImage.texture = _drawTexture;
            
            // if (PlayerPrefs.HasKey("brushSize"))
            // {
            //     brushSize = PlayerPrefs.GetInt("brushSize");
            // }
            // else
            // {
            //     brushSize = 6;
            //     PlayerPrefs.SetInt("brushSize", brushSize);
            //     PlayerPrefs.Save();
            // }
        }
        /// <summary>在画笔/橡皮擦模式间切换，返回切换后是否为橡皮擦模式。</summary>
        public bool ToggleEraseMode()
        {
            _mode = IsEraser ? DrawMode.Pen : DrawMode.Eraser;
            return IsEraser;
        }

        /// <summary>设置绘制模式。</summary>
        public void SetMode(DrawMode mode)
        {
            _mode = mode;
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            //检查是否有人签名
            if(gameRoot!=null)
                gameRoot.isSignOver = true;
            
            _isDrawing = true;
            _lastPoint = ScreenToTexture(eventData.position);
            DrawCircle(_lastPoint, CurrentRadius, CurrentColor);
            if (!IsEraser)
                _hasContent = true;
            _drawTexture.Apply();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDrawing) return;

            Vector2 currentPoint = ScreenToTexture(eventData.position);
            DrawLine(_lastPoint, currentPoint, CurrentRadius, CurrentColor);
            _lastPoint = currentPoint;
            _drawTexture.Apply();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if(ocrSample!=null)
                ocrSample.RunOCR();
            _isDrawing = false;
        }

        /// <summary>清空画布。</summary>
        public void Clear()
        {
            //检查是否有人签名
            if(gameRoot!=null)
                gameRoot.isSignOver = false;
            ClearTexture();
            _hasContent = false;
        }

        /// <summary>保存为带透明通道的 PNG。返回保存路径，失败返回 null。</summary>
        public string SaveAsPNG()
        {
            if (!_hasContent)
            {
                Debug.LogWarning("[SignPad] 画布为空，无内容可保存");
                return null;
            }

            // 确保未压缩的像素已读取
            _drawTexture.Apply();

            byte[] pngData = _drawTexture.EncodeToPNG();
            if (pngData == null || pngData.Length == 0)
            {
                Debug.LogError("[SignPad] PNG 编码失败");
                return null;
            }

            string dir = Path.Combine(Application.persistentDataPath, saveFolder);
            Directory.CreateDirectory(dir);

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filePath = Path.Combine(dir, $"{fileNamePrefix}_{timestamp}.png");
            File.WriteAllBytes(filePath, pngData);

            Debug.Log($"[SignPad] 签名已保存: {filePath}");
            return filePath;
        }

        /// <summary>获取当前签名 Texture2D（只读，勿直接修改）。</summary>
        public Texture2D GetSignatureTexture()
        {
            return _drawTexture;
        }

        // ────────────── 内部方法 ──────────────

        private Vector2 ScreenToTexture(Vector2 screenPos)
        {
            RectTransform rt = _rawImage.rectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, null, out Vector2 localPos);

            // 将 localPos 映射到纹理像素坐标
            Rect rect = rt.rect;
            float u = (localPos.x - rect.x) / rect.width;
            float v = (localPos.y - rect.y) / rect.height;

            int px = Mathf.RoundToInt(u * textureWidth);
            int py = Mathf.RoundToInt(v * textureHeight);

            px = Mathf.Clamp(px, 0, textureWidth - 1);
            py = Mathf.Clamp(py, 0, textureHeight - 1);

            return new Vector2(px, py);
        }

        private void DrawLine(Vector2 from, Vector2 to, int radius, Color color)
        {
            float dist = Vector2.Distance(from, to);
            int steps = Mathf.CeilToInt(dist / (radius * 0.3f));
            steps = Mathf.Max(steps, 1);

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 point = Vector2.Lerp(from, to, t);
                DrawCircle(point, radius, color);
            }
        }

        private void DrawCircle(Vector2 center, int radius, Color color)
        {
            int cx = (int)center.x;
            int cy = (int)center.y;
            int r2 = radius * radius;

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x * x + y * y <= r2)
                    {
                        int px = cx + x;
                        int py = cy + y;
                        if (px >= 0 && px < textureWidth && py >= 0 && py < textureHeight)
                        {
                            _drawTexture.SetPixel(px, py, color);
                        }
                    }
                }
            }
        }

        private void ClearTexture()
        {
            if(_drawTexture == null)
                return;
            Color transparent = new Color(0, 0, 0, 0);
            Color[] pixels = new Color[textureWidth * textureHeight];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = transparent;
            _drawTexture.SetPixels(pixels);
            _drawTexture.Apply();
        }
        
        private void Update()
        {
            // if (Input.GetKeyDown(KeyCode.Space))
            // {
            //     brushSize++;
            //     if (brushSize >= 12)
            //         brushSize = 5;
            //     PlayerPrefs.SetInt("brushSize", brushSize);
            //     PlayerPrefs.Save();
            // }
        
        }
    }
}
