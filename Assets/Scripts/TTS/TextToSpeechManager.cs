using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TuanjieAI.TTS
{
    /// <summary>
    /// 文字转语音管理器：通过 PowerShell 调用 Windows 内置 System.Speech 引擎，
    /// 将文本合成为 WAV 文件，再加载为 AudioClip 通过 AudioSource 播放。
    /// 仅支持 Windows 平台。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class TextToSpeechManager : MonoBehaviour
    {
        public static TextToSpeechManager Instance { get; private set; }

        [Header("语音参数")]
        [Range(-10, 10), Tooltip("语速，-10 最慢，10 最快")]
        public int rate = 0;

        [Range(0, 100), Tooltip("音量，0 静音，100 最大")]
        public int volume = 100;

        /// <summary>状态变化回调，参数为状态描述文本。</summary>
        public event System.Action<string> OnStatusChanged;

        /// <summary>语音播放完毕回调。</summary>
        public event System.Action OnPlaybackFinished;

        private AudioSource _audioSource;
        private string _tempDir;
        private bool _isBusy;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _audioSource = GetComponent<AudioSource>();
            _tempDir = Path.Combine(Application.temporaryCachePath, "TTS");
            Directory.CreateDirectory(_tempDir);
        }

        private void Update()
        {
            // 播放结束检测
            if (_audioSource.isPlaying == false && _audioSource.clip != null && _isBusy == false)
            {
                // clip 播放完毕会自动停止，这里不做额外处理
            }
        }

        /// <summary>是否正在生成或播放语音。</summary>
        public bool IsBusy => _isBusy;

        /// <summary>开始将文本转换为语音并播放。</summary>
        public void Speak(string text)
        {
            if (_isBusy)
            {
                StopAllCoroutines();
                _audioSource.Stop();
                _isBusy = false;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                SetStatus("文本为空，请输入要朗读的内容");
                return;
            }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
            StartCoroutine(SpeakCoroutine(text));
#else
            SetStatus("文字转语音仅支持 Windows 平台");
#endif
        }

        /// <summary>停止当前播放和生成。</summary>
        public void Stop()
        {
            StopAllCoroutines();
            _audioSource.Stop();
            _isBusy = false;
            SetStatus("已停止");
        }

        private IEnumerator SpeakCoroutine(string text)
        {
            _isBusy = true;
            SetStatus("正在生成语音…");

            string textFile = Path.Combine(_tempDir, "tts_input.txt");
            string wavFile = Path.Combine(_tempDir, "tts_output.wav");
            string scriptFile = Path.Combine(_tempDir, "tts_script.ps1");

            // 写入文本文件（UTF-8 with BOM 以便 PowerShell 正确识别）
            File.WriteAllText(textFile, text, new UTF8Encoding(true));

            // 生成 PowerShell 脚本
            string script = BuildPowerShellScript(textFile, wavFile, rate, volume);
            File.WriteAllText(scriptFile, script, new UTF8Encoding(true));

            // 执行 PowerShell
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptFile}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            Process process;
            try
            {
                process = Process.Start(psi);
            }
            catch (System.Exception e)
            {
                SetStatus($"启动 PowerShell 失败: {e.Message}");
                _isBusy = false;
                yield break;
            }

            if (process == null)
            {
                SetStatus("无法启动 PowerShell 进程");
                _isBusy = false;
                yield break;
            }

            // 等待 PowerShell 执行完成（非阻塞）
            float timeout = 30f;
            float elapsed = 0f;
            while (!process.HasExited)
            {
                elapsed += Time.deltaTime;
                if (elapsed > timeout)
                {
                    process.Kill();
                    SetStatus("语音生成超时");
                    _isBusy = false;
                    yield break;
                }
                yield return null;
            }

            if (process.ExitCode != 0)
            {
                string stderr = process.StandardError.ReadToEnd();
                SetStatus($"语音生成失败: {stderr}");
                _isBusy = false;
                yield break;
            }

            // 检查 WAV 文件是否生成
            if (!File.Exists(wavFile))
            {
                SetStatus("未找到生成的音频文件");
                _isBusy = false;
                yield break;
            }

            SetStatus("正在加载音频…");

            // 加载 WAV 为 AudioClip
            yield return LoadAndPlay(wavFile);
        }

        private IEnumerator LoadAndPlay(string wavPath)
        {
            string url = "file://" + wavPath.Replace("\\", "/");
            using (var request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    SetStatus($"音频加载失败: {request.error}");
                    _isBusy = false;
                    yield break;
                }

                var clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip == null)
                {
                    SetStatus("音频加载失败: AudioClip 为空");
                    _isBusy = false;
                    yield break;
                }

                clip.name = "TTS_Audio";

                // 释放旧的 AudioClip
                if (_audioSource.clip != null)
                    Destroy(_audioSource.clip);

                _audioSource.clip = clip;
                _audioSource.Play();

                SetStatus("正在播放…");
                _isBusy = false;

                // 等待播放结束
                float waitTime = clip.length + 0.5f;
                while (waitTime > 0f)
                {
                    if (!_audioSource.isPlaying)
                        break;
                    waitTime -= Time.deltaTime;
                    yield return null;
                }

                SetStatus("播放完毕");
                OnPlaybackFinished?.Invoke();
            }
        }

        private static string BuildPowerShellScript(string textFile, string wavFile, int rate, int volume)
        {
            // 使用单引号包裹路径，路径中的单引号用两个单引号转义
            string safeTextFile = textFile.Replace("'", "''");
            string safeWavFile = wavFile.Replace("'", "''");

            return $@"
Add-Type -AssemblyName System.Speech
$text = Get-Content -Path '{safeTextFile}' -Raw -Encoding UTF8
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$synth.Rate = {rate}
$synth.Volume = {volume}
$synth.SetOutputToWaveFile('{safeWavFile}')
$synth.Speak($text)
$synth.Dispose()
";
        }

        private void SetStatus(string status)
        {
            OnStatusChanged?.Invoke(status);
        }

        private void OnDestroy()
        {
            // 清理临时文件
            if (Directory.Exists(_tempDir))
            {
                try { Directory.Delete(_tempDir, true); } catch { }
            }
        }
    }
}
