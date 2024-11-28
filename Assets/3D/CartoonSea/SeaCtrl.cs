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
        
        public float waveSpeed = 0.01f;
        public float waveHeight = 0.4f;
        public float waveDensity = 0.5f;
    }
    
    public class SeaCtrl : MonoBehaviour {
        private static readonly int CustomTime = Shader.PropertyToID("_CustomTime");
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
        private void UpdateTime() {
            if (isPause) {
                return;
            }
            _timer += Time.deltaTime;
            float timeX = _timer;
            float timeY = _timer * 2;
            float timeZ = _timer * 3;
            float timeW = 1.0f / _timer;
            var t = new Vector4(timeX, timeY, timeZ, timeW);
            _mpb.SetVector(CustomTime, t);
            _renderer.SetPropertyBlock(_mpb);
        }

        #region Shader相关
        private MeshRenderer _renderer;
        private Material _material;
        private MaterialPropertyBlock _mpb;

        private void InitShader() {
            _renderer = GetComponent<MeshRenderer>();
            _material = _renderer.material;
            _mpb = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_mpb);
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
            // // 根据物体的位置计算其在海面上的UV坐标（假设海面为XZ平面）
            // Vector3 objPos = t.position;
            // float normalizedX = Mathf.InverseLerp(-_seaSize.x / 2f, _seaSize.x / 2f, objPos.x);
            // float normalizedZ = Mathf.InverseLerp(-_seaSize.y / 2f, _seaSize.y / 2f, objPos.z);
            //
            // // 将归一化的X和Z坐标转换为ComputeBuffer中的索引
            // int texelX = Mathf.FloorToInt(normalizedX * _seaSize.x);
            // int texelZ = Mathf.FloorToInt(normalizedZ * _seaSize.y);
            // int index = texelX + texelZ * (int)_seaSize.x;
            //
            // if (index < 0 || index >= _waveData.Length) {
            //     Debug.LogWarning("Object out of wave data bounds.");
            //     return;
            // }
            //
            // // 从波浪数据中获取高度和法线信息
            // float waveHeight = _waveData[index].w; // 高度存储在w分量
            // Vector3 waveNormal =
            //     new Vector3(_waveData[index].x, _waveData[index].y, _waveData[index].z); // 法线存储在x, y, z分量
            //
            // // 更新物体的位置：将y坐标设置为波浪的高度
            // t.position = new Vector3(objPos.x, waveHeight, objPos.z);
            //
            // // 更新物体的旋转：使其朝向法线
            // t.rotation = Quaternion.FromToRotation(Vector3.up, waveNormal);
        }

        #endregion
    }
}