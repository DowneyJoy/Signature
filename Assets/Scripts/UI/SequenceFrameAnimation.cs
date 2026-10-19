using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TuanjieAI.UI
{
    /// <summary>
    /// 序列帧动画播放器：从 Resources 或指定路径加载帧图，逐帧播放。
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SequenceFrameAnimation : MonoBehaviour
    {
        [Tooltip("帧图所在文件夹路径（如 'Assets/Textures/Horse'）")]
        public string frameFolderPath = "Assets/Textures/Horse";

        [Tooltip("帧前缀（如 'frame_'）")]
        public string framePrefix = "frame_";

        [Tooltip("帧编号位数（如 4 表示 frame_0001）")]
        public int frameNumberDigits = 4;

        [Tooltip("总帧数")]
        public int totalFrames = 360;

        [Tooltip("播放帧率（帧/秒）")]
        public float frameRate = 30f;

        [Tooltip("是否循环")]
        public bool loop = false;

        [Tooltip("是否在 Start 时自动播放")]
        public bool playOnStart = false;

        private Image _image;
        private Sprite[] _frames;
        private float _timer;
        private int _currentIndex;
        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;
        public int CurrentFrame => _currentIndex;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void Start()
        {
            if (playOnStart)
                Play();
        }

        /// <summary>加载帧图并开始播放。</summary>
        public void Play()
        {
            if (_frames == null || _frames.Length == 0)
                LoadFrames();

            if (_frames == null || _frames.Length == 0)
            {
                Debug.LogError("[SequenceFrameAnimation] No frames loaded!");
                return;
            }

            _currentIndex = 0;
            _timer = 0f;
            _isPlaying = true;
            UpdateFrame();
        }

        /// <summary>暂停播放。</summary>
        public void Pause()
        {
            _isPlaying = false;
        }

        /// <summary>停止播放并显示第一帧。</summary>
        public void Stop()
        {
            _isPlaying = false;
            _currentIndex = 0;
            UpdateFrame();
        }

        /// <summary>从指定帧开始播放。</summary>
        public void PlayFromFrame(int frameIndex)
        {
            if (_frames == null || _frames.Length == 0)
                LoadFrames();

            if (_frames == null || _frames.Length == 0) return;

            _currentIndex = Mathf.Clamp(frameIndex, 0, _frames.Length - 1);
            _timer = 0f;
            _isPlaying = true;
            UpdateFrame();
        }

        private void Update()
        {
            if (!_isPlaying || _frames == null || _frames.Length == 0) return;

            _timer += Time.deltaTime;
            float frameInterval = 1f / frameRate;

            while (_timer >= frameInterval)
            {
                _timer -= frameInterval;
                _currentIndex++;

                if (_currentIndex >= _frames.Length)
                {
                    if (loop)
                        _currentIndex = 0;
                    else
                    {
                        _currentIndex = _frames.Length - 1;
                        _isPlaying = false;
                        break;
                    }
                }
            }

            UpdateFrame();
        }

        private void UpdateFrame()
        {
            if (_frames == null || _frames.Length == 0) return;
            int idx = Mathf.Clamp(_currentIndex, 0, _frames.Length - 1);
            if (_frames[idx] != null)
                _image.sprite = _frames[idx];
        }

        private void LoadFrames()
        {
#if UNITY_EDITOR
            _frames = new Sprite[totalFrames];
            int loaded = 0;

            for (int i = 0; i < totalFrames; i++)
            {
                string num = i.ToString().PadLeft(frameNumberDigits, '0');
                string path = $"{frameFolderPath}/{framePrefix}{num}.png";
                var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null)
                {
                    _frames[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
                    loaded++;
                }
            }

            Debug.Log($"[SequenceFrameAnimation] Loaded {loaded}/{totalFrames} frames from {frameFolderPath}");
#else
            // Runtime: 从 Resources 加载
            var sprites = Resources.LoadAll<Sprite>(frameFolderPath);
            _frames = sprites;
#endif
        }
    }
}
