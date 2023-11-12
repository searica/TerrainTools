using UnityEngine;

namespace TerrainTools.Visualization
{
    public class HoverInfo
    {
        private GameObject _gameObject;
        private Transform _transform;
        private TextMesh _textMesh;

        public string Text
        {
            get { return _textMesh.text; }
            set { _textMesh.text = value; }
        }

        public bool Enabled
        {
            get { return _gameObject.activeSelf; }
            set { _gameObject.SetActive(value); }
        }

        public Color Color
        {
            get { return _textMesh.color; }
            set { _textMesh.color = value; }
        }

        public HoverInfo(Transform parentTransform)
        {
            _gameObject = new GameObject();
            _gameObject.transform.parent = parentTransform;
            _transform = _gameObject.transform;

            _textMesh = _gameObject.AddComponent<TextMesh>();
            _textMesh.transform.localPosition = Vector3.zero;

            //Fix: normalize the secondary VFX scale away from the hoverInfo scale
            _textMesh.transform.localScale = new Vector3(
                0.1f / parentTransform.localScale.x,
                0.1f / parentTransform.localScale.y,
                0.1f / parentTransform.localScale.z
            );
            _textMesh.anchor = TextAnchor.MiddleCenter;
            _textMesh.alignment = TextAlignment.Center;
            _textMesh.fontSize = 16;
        }

        public void RotateToPlayer()
        {
            var playerXAxisDirection = new Vector3(
                GameCamera.m_instance.transform.position.x,
                _transform.position.y,
                GameCamera.m_instance.transform.position.z
            );
            _transform.LookAt(playerXAxisDirection, Vector3.up);
            _transform.Rotate(90f, 180f, 0f);
        }
    }
}