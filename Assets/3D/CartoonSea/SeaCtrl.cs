using System;
using System.Collections.Generic;
using UnityEngine;

namespace CartoonSea3D {
    [Serializable]
    public class BaseWaveConfig {
        public Texture2D noiseTexture;
        public Color shallowWaterColor = new Color(0f, 0.8f, 1.0f, 0.5f);
        public Color deepWaterColor = new Color(0f, 0.1f, 0.5f, 1f);
        public Color foamColor = Color.white;
        
        public float waveSpeed = 0.01f; // 波浪速度
        public float waveHeight = 0.4f; // 波浪高度
        public float waveDensity = 0.5f; // 波浪密度
        public Vector2 waveMoveVelocity = new Vector2(0.4f, 0.4f); // 波浪运动的方向速度（X、Y）
    }
    
    public class SeaCtrl : MonoBehaviour {
        public BaseWaveConfig baseWaveConfig;
        public bool isPause;
        public Vector2 initSeaSize = new Vector2(10, 10);
        
        private MeshFilter _meshFilter;
        private Vector2 _meshSize;

        private void Awake() {
            _meshFilter = GetComponent<MeshFilter>();
            InitSeaSize();
            InitShader();
        }

        Vector2 SeaSize {
            get {
                Vector3 scale = transform.lossyScale;
                Vector2 seaSize = new Vector2(_meshSize.x * scale.x, _meshSize.y * scale.z);
                return seaSize;
            }
            set {
                var t = transform; 
                var targetScale = new Vector3(value.x / _meshSize.x, transform.lossyScale.y, value.y / _meshSize.y);
                t.localScale = new Vector3(
                    targetScale.x / t.lossyScale.x * t.localScale.x,
                    targetScale.y / t.lossyScale.y * t.localScale.y,
                    targetScale.z / t.lossyScale.z * t.localScale.z
                );
            }
        }

        private void InitSeaSize() {
            Mesh mesh = _meshFilter.mesh;
            Vector3[] vertices = mesh.vertices;
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var vertex in vertices) {
                minX = Mathf.Min(minX, vertex.x);
                maxX = Mathf.Max(maxX, vertex.x);
                minZ = Mathf.Min(minZ, vertex.z);
                maxZ = Mathf.Max(maxZ, vertex.z);
            }
            _meshSize = new Vector2(maxX - minX, maxZ - minZ);
            
            SeaSize = initSeaSize;
        }

        void Update() {
            UpdateTime();
            UpdateFloatingObjs();
        }
        
        private float _timer;
        private Vector4 _customTime;
        private void UpdateTime() {
            if (isPause) {
                return;
            }
            _timer += Time.deltaTime;
            float timeX = _timer;
            float timeY = _timer * 2;
            float timeZ = _timer * 3;
            float timeW = 1.0f / _timer;
            _customTime = new Vector4(timeX, timeY, timeZ, timeW);
            _mpb.SetVector(CustomTime, _customTime);
            _renderer.SetPropertyBlock(_mpb);
        }

        #region Shader相关
        private MeshRenderer _renderer;
        private Material _material;
        private MaterialPropertyBlock _mpb;
        
        private static readonly int CustomTime = Shader.PropertyToID("_CustomTime");
        private static readonly int WaveMoveVelocity = Shader.PropertyToID("_WaveMoveVelocity");
        private static readonly int WaveHeight = Shader.PropertyToID("_WaveHeight");
        private static readonly int WaveDensity = Shader.PropertyToID("_WaveDensity");
        private static readonly int ModelPosition = Shader.PropertyToID("_ModelPosition");
        private static readonly int FoamColor = Shader.PropertyToID("_FoamColor");
        private static readonly int ShallowWaterColor = Shader.PropertyToID("_ShallowWaterColor");
        private static readonly int DeepWaterColor = Shader.PropertyToID("_DeepWaterColor");
        private static readonly int SurfaceNoise = Shader.PropertyToID("_SurfaceNoise");

        private void InitShader() {
            _renderer = GetComponent<MeshRenderer>();
            _material = _renderer.material;
            _mpb = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_mpb);
            SetUpShaderProperty();
        }

        public void SetUpShaderProperty() {
            // 计算世界坐标下的模型位置
            Vector3 worldPos = transform.position;
            Vector3 modelPos = _renderer.localToWorldMatrix.inverse.MultiplyPoint(worldPos);

            // 设置Shader属性
            _mpb.SetVector(ModelPosition, modelPos);
            _mpb.SetFloat(WaveHeight, baseWaveConfig.waveHeight);
            _mpb.SetFloat(WaveDensity, baseWaveConfig.waveDensity);
            _mpb.SetVector(WaveMoveVelocity, baseWaveConfig.waveMoveVelocity);
            _mpb.SetColor(FoamColor, baseWaveConfig.foamColor);
            _mpb.SetColor(ShallowWaterColor, baseWaveConfig.shallowWaterColor);
            _mpb.SetColor(DeepWaterColor, baseWaveConfig.deepWaterColor);

            // 设置纹理
            _mpb.SetTexture(SurfaceNoise, baseWaveConfig.noiseTexture);

            // 将MaterialPropertyBlock应用到Renderer
            _renderer.SetPropertyBlock(_mpb);
        }

        #endregion

        #region 漂浮物品管理
        public List<Transform> floatingObjs = new List<Transform>();
        private Dictionary<SeaFloatingObjCtrl, Transform> _floatingObjsDict = new Dictionary<SeaFloatingObjCtrl, Transform>();

        private void UpdateFloatingObjs() {
            foreach (var obj in floatingObjs) {
                UpdateFloatingObjTransform(obj);
            }
        }

        public void UpdateFloatingObjTransform(Transform t) {
            // 计算漂浮物体的 Y 坐标，使其随波浪浮动
            Vector3 position = t.position;
            float waveHeight = Mathf.Sin(position.x * baseWaveConfig.waveDensity + _customTime.y * baseWaveConfig.waveMoveVelocity.x) *
                               Mathf.Sin(position.z * baseWaveConfig.waveDensity + _customTime.y * baseWaveConfig.waveMoveVelocity.y) *
                               baseWaveConfig.waveHeight;

            position.y = waveHeight; // 更新漂浮物体的高度
            t.position = position;
        }

        #endregion
    }
}