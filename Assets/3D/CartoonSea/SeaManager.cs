using System;
using System.Collections.Generic;
using UnityEngine;

namespace CartoonSea3D {
    public class SeaManager : MonoBehaviour {
        public bool isPause;
        
        public GameObject seaPrefab;
        public int unitSeaLength = 10;

        public int seaEnableUnitD4Radius = 2;
        public int NeedEnableSeaCount => (int)Mathf.Pow(seaEnableUnitD4Radius * 2, 2);

        private Dictionary<Vector2Int, SeaCtrl> _seaMap;
        
        public Transform playerTf;

        public Vector2Int WorldPosToUnitSeaPos(Vector3 pos) {
            int x = Mathf.CeilToInt(pos.x) / unitSeaLength + 1;
            int y = Mathf.CeilToInt(pos.y) / unitSeaLength + 1;
            return new Vector2Int(x, y);
        }

        private void Awake() {
            InitSeaMap();
        }

        private void Update() {
            if (isPause) {
                return;
            }
            
        }

        private void InitSeaMap() {
            _seaMap = new Dictionary<Vector2Int, SeaCtrl>();
            var seaList = transform.GetComponentsInChildren<SeaCtrl>();
            if (seaList == null || seaList.Length == 0) {
                return;
            } 
            foreach (var sea in seaList) {
                var unitSeaPos = WorldPosToUnitSeaPos(sea.transform.position);
                _seaMap.Add(unitSeaPos, sea);
            }
        }

        private void UpdateSea() {
            
        }
    }
}