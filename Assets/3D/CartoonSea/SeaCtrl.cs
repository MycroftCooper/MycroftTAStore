using System;
using UnityEngine;

namespace CartoonSea3D {
    [Serializable]
    public struct BaseWaveConfig {
        public Color shallowWaterColor;
        public Color deepWaterColor;
        public Color foamColor;
        
        public float waveSpeed;
        public float waveHeight;
        public float waveDensity;
    }
    
    public class SeaCtrl : MonoBehaviour {
        public BaseWaveConfig baseWaveConfig;
        public bool isPause;
        
        private MeshRenderer _renderer;
        private Material _material;
        private MaterialPropertyBlock _mpb;

        private void Awake() {
            _renderer = GetComponent<MeshRenderer>();
            _material = _renderer.material;
            _mpb = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_mpb);
        }

        void Update() {
            UpdateTime();
        }

        private static readonly int CustomTime = Shader.PropertyToID("_CustomTime");
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
            _mpb.SetVector(CustomTime, new Vector4(timeX, timeY, timeZ, timeW));
            _renderer.SetPropertyBlock(_mpb);
        }

        private Vector3 GetSurfacePos(Vector3 pos) {
            return new Vector3(pos.x, pos.y, pos.z);
        }
    }
}