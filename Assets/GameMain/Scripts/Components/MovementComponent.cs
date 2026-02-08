using UnityEngine;

namespace Components
{
    public class MovementComponent : MonoBehaviour
    {
        private bool _isMoving = false;
        private Vector3 _direction = Vector3.forward;
        private float _speed = 0;
        private Transform _cachedTransform;

        public void OnInit(float speed, Transform transform)
        {
            _speed = speed;
            _cachedTransform = transform;
        }

        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (_isMoving && _cachedTransform != null)
            {
                Move(elapseSeconds);
            }
        }

        public void OnReset()
        {
            _speed = 0;
            _cachedTransform = null;
            _isMoving = false;
        }

        private void Move(float deltaTime = 0)
        {
            _cachedTransform.Translate(_direction * _speed * deltaTime);
        }

        public void SetMove(bool isMoving) => _isMoving = isMoving;
        public void SetDirection(Vector3 direction) => _direction = direction;
    }
}