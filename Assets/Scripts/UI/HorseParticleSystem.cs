using System;
using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TuanjieAI.UI
{
    /// <summary>
    /// 马形状粒子系统控制器：
    /// 粒子从屏幕四周飞入，沿弧线汇聚成马的形状，保持后向外消散。
    /// 通过 GetParticles/SetParticles 直接操控粒子位置，使用贝塞尔曲线+螺旋扰动实现自然飞行轨迹。
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class HorseParticleSystem : MonoBehaviour
    {
        [Header("阶段时长")]
        public float materializeDuration = 4f;
        public float holdDuration = 2f;
        public float dissolveDuration = 4f;
        public float hideDuration = 2f;

        [Header("粒子参数")]
        [Tooltip("粒子大小（世界空间单位）")]
        public float particleSize = 0.03f;
        [Tooltip("粒子总数")]
        public int particleCount = 8000;
        public Color particleColor = new Color(1f, 0.84f, 0.2f, 1f);

        [Header("汇聚设置")]
        [Tooltip("粒子起始散布半径（屏幕边缘距离）")]
        public float spawnRadius = 8f;
        [Tooltip("汇聚曲线缓动")]
        public AnimationCurve convergeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("每个粒子的随机延迟范围（0~1，占 materializeDuration 的比例）")]
        public float staggerRange = 0.4f;
        [Tooltip("贝塞尔曲线弧度强度")]
        public float arcStrength = 3f;
        [Tooltip("螺旋扰动强度")]
        public float swirlStrength = 0.3f;
        [Tooltip("螺旋扰动频率")]
        public float swirlFrequency = 3f;

        [Header("自动播放")]
        public bool playOnStart = true;

        private ParticleSystem _ps;
        private ParticleSystem.MainModule _main;
        private bool _isPlaying;

        // 每个粒子的目标位置
        private Vector3[] _targetPositions;
        // 每个粒子的起始位置
        private Vector3[] _startPositions;
        // 贝塞尔曲线控制点
        private Vector3[] _controlPoints;
        // 每个粒子的延迟（0~1）
        private float[] _delays;
        // 粒子数组缓存
        private ParticleSystem.Particle[] _particles;

        [Header("展示信息")]
        public Queue<SignInfo> SIQueue = new Queue<SignInfo>();
        public SignItemInfo SignItemInfoTemp;
        public GameObject AnimationGo;
        public SignInfo CurentSignInfoTemp;
        public Sequence HideAnimation;
        public Sequence ShowAnimation;
        public Sequence ShowSignInfo;
        public Sequence HideSignInfo;
        private void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
            _main = _ps.main;

            _main.startSize = particleSize;
            _main.startColor = particleColor;
            _main.startLifetime = Mathf.Max(materializeDuration + holdDuration + dissolveDuration + 2f, 10f);
            _main.startSpeed = 0f;
            _main.simulationSpace = ParticleSystemSimulationSpace.World;
            _main.maxParticles = particleCount;
            _main.loop = false;
            _main.playOnAwake = false;

            var emission = _ps.emission;
            emission.rateOverTime = 0f;

            var trails = _ps.trails;
            trails.enabled = false;

            _particles = new ParticleSystem.Particle[particleCount];
        }

        private void Start()
        {
            CollectTargetPositions();

            if (playOnStart)
                PlayEffect();
        }
        /// <summary>
        /// 外部调用
        /// </summary>
        /// <param name="signInfo"></param>
        public void ShowParticle(SignInfo signInfo)
        {
            if (_isPlaying)
            {
                SIQueue.Enqueue(signInfo);
                return;
            }
            if (SIQueue.Count > 0)
            {
                CurentSignInfoTemp = SIQueue.Dequeue();
                SIQueue.Enqueue(signInfo);
            }
            else
            {
                CurentSignInfoTemp = signInfo;
            }
            PlayEffect();
        }

        private void ShowParticle()
        {
            if (SIQueue.Count == 0)
                return;
            CurentSignInfoTemp = SIQueue.Dequeue();
            PlayEffect();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                PlayEffect();
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                StopEffect();
            }
        }

        /// <summary>加载 HorseShape.mesh 的顶点作为粒子目标位置。</summary>
        private void CollectTargetPositions()
        {
            Mesh mesh = null;
#if UNITY_EDITOR
            mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Textures/Horse/HorseShape.mesh");
#endif
            if (mesh == null) mesh = _ps.shape.mesh;
            if (mesh == null)
            {
                var mf = GetComponent<MeshFilter>();
                if (mf != null) mesh = mf.sharedMesh;
            }
            if (mesh == null)
            {
                var horse = GameObject.Find("Horse");
                if (horse != null)
                {
                    var horseMf = horse.GetComponent<MeshFilter>();
                    if (horseMf != null) mesh = horseMf.sharedMesh;
                }
            }
            if (mesh == null)
            {
                Debug.LogError("[HorseParticleSystem] No mesh found for target positions!");
                return;
            }

            var meshVerts = mesh.vertices;
            int meshVertCount = meshVerts.Length;
            _targetPositions = new Vector3[particleCount];
            _startPositions = new Vector3[particleCount];
            _controlPoints = new Vector3[particleCount];
            _delays = new float[particleCount];

            Camera cam = Camera.main;
            Transform tr = transform;

            for (int i = 0; i < particleCount; i++)
            {
                int vertIdx = Random.Range(0, meshVertCount);
                Vector3 localVert = meshVerts[vertIdx];
                _targetPositions[i] = tr.TransformPoint(localVert);
                _startPositions[i] = GetRandomEdgePosition(cam, tr.position);

                // 随机延迟，让粒子错峰出发
                _delays[i] = Random.Range(0f, staggerRange);

                // 贝塞尔控制点：起点和终点中间偏移一个随机方向
                Vector3 mid = (_startPositions[i] + _targetPositions[i]) * 0.5f;
                Vector3 perp = Vector3.Cross(
                    _targetPositions[i] - _startPositions[i],
                    Random.onUnitSphere
                ).normalized * Random.Range(0.3f, 1f) * arcStrength;
                _controlPoints[i] = mid + perp;
            }

            Debug.Log($"[HorseParticleSystem] Collected {particleCount} targets, arcs and delays from mesh '{mesh.name}'");
        }

        /// <summary>二次贝塞尔曲线。</summary>
        private static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        /// <summary>在相机视锥边缘生成随机世界坐标。</summary>
        private Vector3 GetRandomEdgePosition(Camera cam, Vector3 center)
        {
            if (cam == null) return center + Random.insideUnitSphere * spawnRadius;

            float dist = Vector3.Distance(cam.transform.position, center);
            float halfH = dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfW = halfH * cam.aspect;

            int edge = Random.Range(0, 4);
            float margin = 1.5f;
            Vector3 right = cam.transform.right;
            Vector3 up = cam.transform.up;
            Vector3 forward = cam.transform.forward;
            Vector3 basePos = cam.transform.position + forward * dist;

            float t;
            switch (edge)
            {
                case 0:
                    t = Random.Range(-1f, 1f);
                    return basePos - right * (halfW + margin) + up * (t * halfH);
                case 1:
                    t = Random.Range(-1f, 1f);
                    return basePos + right * (halfW + margin) + up * (t * halfH);
                case 2:
                    t = Random.Range(-1f, 1f);
                    return basePos + up * (halfH + margin) + right * (t * halfW);
                default:
                    t = Random.Range(-1f, 1f);
                    return basePos - up * (halfH + margin) + right * (t * halfW);
            }
        }

        /// <summary>播放完整效果：汇聚 → 保持 → 消散。</summary>
        public void PlayEffect()
        {
            StartCoroutine(EffectCoroutine());
        }

        private IEnumerator EffectCoroutine()
        {
            _isPlaying = true;
            _ps.Clear();
            _ps.Play();

            float totalDuration = materializeDuration + holdDuration + dissolveDuration + 2f;
            _main.startLifetime = totalDuration;

            // 发射所有粒子
            var emitParams = new ParticleSystem.EmitParams();
            for (int i = 0; i < particleCount; i++)
            {
                emitParams.position = _startPositions[i];
                emitParams.startSize = 0f;
                emitParams.startColor = new Color(particleColor.r, particleColor.g, particleColor.b, 0f);
                emitParams.startLifetime = totalDuration;
                emitParams.velocity = Vector3.zero;
                _ps.Emit(emitParams, 1);
            }

            //隐藏主界面留言
            HideAnimation.Stop();
            HideAnimation = Sequence.Create()
                .Group(Tween.Scale(AnimationGo.transform, Vector3.zero, materializeDuration))
                .OnComplete(() =>
                {
                    AnimationGo.SetActive(false);
                });
            
            // ── 阶段 1：Materialize — 粒子沿弧线汇聚到马形状 ──
            float elapsed = 0f;
            while (elapsed < materializeDuration)
            {
                elapsed += Time.deltaTime;

                int count = _ps.GetParticles(_particles);
                for (int i = 0; i < count; i++)
                {
                    // 每个粒子有自己的延迟
                    float delay = _delays[i];
                    float adjustedDuration = materializeDuration * (1f - staggerRange);
                    float localT = Mathf.Clamp01((elapsed - delay * materializeDuration) / adjustedDuration);
                    float curve = convergeCurve.Evaluate(localT);

                    // 贝塞尔曲线位置
                    Vector3 pos = Bezier(_startPositions[i], _controlPoints[i], _targetPositions[i], curve);

                    // 叠加螺旋扰动（随汇聚进度衰减）
                    float swirlDecay = 1f - curve;
                    float angle = Time.time * swirlFrequency + i * 0.7f;
                    Vector3 swirl = new Vector3(
                        Mathf.Cos(angle) * swirlStrength * swirlDecay,
                        Mathf.Sin(angle * 1.3f) * swirlStrength * swirlDecay,
                        Mathf.Sin(angle * 0.7f) * swirlStrength * 0.5f * swirlDecay
                    );
                    _particles[i].position = pos + swirl;

                    // 粒子大小：从 0 渐变到目标大小
                    _particles[i].startSize = particleSize * Mathf.SmoothStep(0f, 1f, curve);
                    // 透明度：跟随进度
                    _particles[i].startColor = new Color(particleColor.r, particleColor.g, particleColor.b, curve);
                }
                _ps.SetParticles(_particles, count);

                yield return null;
            }

            // ── 阶段 2：Hold — 保持马形状，带有机抖动 ──
            if (holdDuration > 0f)
            {
                float holdEnd = Time.time + holdDuration;
                while (Time.time < holdEnd)
                {
                    int count = _ps.GetParticles(_particles);
                    for (int i = 0; i < count; i++)
                    {
                        Vector3 jitter = new Vector3(
                            Mathf.Sin(Time.time * 5f + i * 0.3f) * 0.008f,
                            Mathf.Cos(Time.time * 4f + i * 0.5f) * 0.008f,
                            Mathf.Sin(Time.time * 6f + i * 0.7f) * 0.005f
                        );
                        _particles[i].position = _targetPositions[i] + jitter;
                        _particles[i].startSize = particleSize * (0.9f + 0.1f * Mathf.Sin(Time.time * 3f + i));
                    }
                    _ps.SetParticles(_particles, count);
                    yield return null;
                }
            }

            //显示当前签名
            ShowSignInfo.Stop();
            SignItemInfoTemp.gameObject.SetActive(true);
            SignItemInfoTemp.transform.localScale = Vector3.zero;
            SignItemInfoTemp.SetImage(CurentSignInfoTemp.InfoImage);
            ShowSignInfo = Sequence.Create()
                .Group(Tween.Scale(SignItemInfoTemp.transform, Vector3.one, dissolveDuration-1))
                .OnComplete(() =>
                {
                    
                });
            // ── 阶段 3：Dissolve — 粒子沿弧线向外消散 ──
            elapsed = 0f;
            Vector3 center = transform.position;
            while (elapsed < dissolveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dissolveDuration);
                float curve = 1f - convergeCurve.Evaluate(t);

                int count = _ps.GetParticles(_particles);
                for (int i = 0; i < count; i++)
                {
                    // 向外飞散，带弧线
                    Vector3 outward = (_targetPositions[i] - center).normalized;
                    Vector3 endPos = _targetPositions[i] + outward * spawnRadius * 0.5f;
                    Vector3 ctrlPos = (_targetPositions[i] + endPos) * 0.5f + Random.onUnitSphere * 0.5f;

                    _particles[i].position = Bezier(_targetPositions[i], ctrlPos, endPos, t);
                    _particles[i].startSize = particleSize * curve;
                    _particles[i].startColor = new Color(particleColor.r, particleColor.g, particleColor.b, curve);
                }
                _ps.SetParticles(_particles, count);

                yield return null;
            }
            
            //隐藏主界面留言
            ShowAnimation.Stop();
            AnimationGo.SetActive(true);
            AnimationGo.transform.localScale = Vector3.zero;
            ShowAnimation = Sequence.Create()
                .Group(Tween.Scale(AnimationGo.transform, Vector3.one, hideDuration))
                .OnComplete(() =>
                {
                    
                });
            HideSignInfo.Stop();
            HideSignInfo = Sequence.Create()
                .Group(Tween.Scale(SignItemInfoTemp.transform, Vector3.zero, hideDuration))
                .OnComplete(() =>
                {
                    SignItemInfoTemp.gameObject.SetActive(false);
                });
            // 等待显示隐藏
            elapsed = 0f;
            while (elapsed < hideDuration)
            {
                elapsed += Time.deltaTime;
                
                yield return null;
            }
            _ps.Stop();
            _isPlaying = false;
            ShowParticle();
        }

        /// <summary>停止粒子效果。</summary>
        public void StopEffect()
        {
            _ps.Stop();
            _isPlaying = false;
        }

        public bool IsPlaying => _isPlaying;
    }
}
