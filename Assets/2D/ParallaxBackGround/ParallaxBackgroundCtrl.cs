using System.Collections.Generic;
using UnityEngine;

public class ParallaxBackgroundCtrl : MonoBehaviour {
    public float horizontalFactor = 1f;
    public float verticalFactor = 1f;     
    
    public bool offsetUvX = true;
    public bool offsetUvY;
    public bool movePosX = true;
    public bool movePosY;
    
    public SpriteRenderer[] needOffsetUVObjects;
    public Transform[] needMoveObjects;
    public SpriteRenderer[] needScaleObjects;

    private Transform _trans;
    private Camera _mainCamera;
    private Dictionary<SpriteRenderer, float> _needOffsetUVObjects;
    private Dictionary<Transform, float> _needMoveXObjects;
    private Dictionary<Transform, float> _needMoveYObjects;
    private Vector3 _lastCameraPos;
    private Vector2 _lastScreenSize;

    void Start() {
        _trans = transform;
        _mainCamera = Camera.main;
        if (_mainCamera == null) {
            return;
        }
        _lastScreenSize = GetScreenSize();
        _lastCameraPos = _mainCamera.transform.position;
        _lastScreenSize = GetScreenSize();
        
        _needOffsetUVObjects = new Dictionary<SpriteRenderer, float>();
        _needMoveXObjects = new Dictionary<Transform, float>();
        _needMoveYObjects = new Dictionary<Transform, float>();
        foreach (var obj in needOffsetUVObjects) {
            var t = obj.transform;
            float deep = t.localPosition.z;
            _needOffsetUVObjects.Add(obj, deep);
            if (!offsetUvX) {
                _needMoveXObjects.Add(t, deep);
            }
            if (!offsetUvY) {
                _needMoveYObjects.Add(t, deep);
            }
        }
        foreach (var obj in needMoveObjects) {
            var t = obj.transform;
            float deep = t.localPosition.z;
            _needMoveXObjects.Add(t, deep);
            _needMoveYObjects.Add(t, deep);
        }
        ScaleBackground(_lastScreenSize);
    }
    
    void Update() {
        var currentSize = GetScreenSize();
        if (Mathf.Abs(_lastScreenSize.x - currentSize.x) > 0.01f ||
            Mathf.Abs(_lastScreenSize.y - currentSize.y) > 0.01f) {
            _lastScreenSize = currentSize;
            ScaleBackground(currentSize);
        }

        var currentCameraPos = _mainCamera.transform.position;
        Vector2 dir = currentCameraPos - _lastCameraPos;
        if (Mathf.Abs(dir.x) > 0.01f ||
            Mathf.Abs(dir.y) > 0.01f) {
            _trans.position = currentCameraPos;
            _lastCameraPos = currentCameraPos;
            MoveBackground(dir);
        }
    }

    private void MoveBackground(Vector2 dir) {
        foreach (var (spriteRenderer, deep) in _needOffsetUVObjects) {
            var mat = spriteRenderer.material;
            float uvMoveX = 0, uvMoveY = 0;
            if (offsetUvX && dir.x != 0) {
                uvMoveX = 1/deep * dir.x * horizontalFactor;
            }

            if (offsetUvY && dir.y != 0) {
                uvMoveY = 1/deep * dir.y * verticalFactor;
            }
            var uvOffset = mat.mainTextureOffset;
            uvMoveX = (uvOffset.x + uvMoveX) % 1;
            uvMoveY = (uvOffset.y + uvMoveY) % 1;
            mat.mainTextureOffset = new Vector2(uvMoveX, uvMoveY);
        }

        if (movePosY && dir.y != 0) {
            foreach (var (trans, deep) in _needMoveYObjects) {
                if (trans.parent != transform) {
                    continue;
                }
                float moveY = 1 / deep * -dir.y * verticalFactor;
                var oldPos = trans.localPosition;
                float targetYPos = oldPos.y + moveY;
                var targetPos = new Vector3(oldPos.x, targetYPos, oldPos.z);
                trans.localPosition = targetPos;
            }
        }

        if (movePosX && dir.x != 0) {
            foreach (var (trans, deep) in _needMoveXObjects) {
                if (trans.parent != transform) {
                    continue;
                }
                float moveX = 1/deep * dir.x * horizontalFactor;
                var oldPos = trans.localPosition;
                var targetMovePos = new Vector3(oldPos.x + moveX, oldPos.y, oldPos.z);
                trans.localPosition = targetMovePos;
            }
        }
    }

    private void ScaleBackground(Vector2 targetSize) {
        foreach (var spriteRenderer in needScaleObjects) {
            Transform t = spriteRenderer.transform;
            float pixelsPerUnit = spriteRenderer.sprite.pixelsPerUnit; // 获取每单位对应的像素数
            Texture2D texture = spriteRenderer.sprite.texture;
            Vector2 spriteTargetSize = new Vector2(targetSize.x * pixelsPerUnit, targetSize.y * pixelsPerUnit);

            // 设置Sprite的Scale
            Vector3 targetScale = new Vector3(spriteTargetSize.x / texture.width, spriteTargetSize.y / texture.height, 1);
            t.localScale =  new Vector3(
                targetScale.x / t.lossyScale.x * t.localScale.x,
                targetScale.y / t.lossyScale.y * t.localScale.y,
                targetScale.z / t.lossyScale.z * t.localScale.z
            );

            // 设置Tiling
            spriteRenderer.material.mainTextureScale = new Vector2(t.localScale.x, t.localScale.y);
        }
    }

    private Vector2 GetScreenSize() {
        // 获取屏幕的宽度和高度
        float worldScreenHeight = _mainCamera.orthographicSize * 2.0f;
        float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;
        return new Vector2(worldScreenWidth, worldScreenHeight);
    }
}
