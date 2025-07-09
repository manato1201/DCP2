using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpeedControl
{
    /// <summary>
    /// スピード値を保持し、変更時にイベントを発火するクラス
    /// </summary>
    public class Speed
    {
        private float _speed = 1f;
        public float CurrentSpeed => _speed;
        public event Action<float> OnValueChange;

        /// <summary>
        /// スピードを設定し、イベント通知
        /// </summary>
        public void SetSpeed(float speed)
        {
            _speed = speed;
            OnValueChange?.Invoke(_speed);
        }
    }

    /// <summary>
    /// バトル全体のスピード制御を行うシングルトンコントローラ
    /// </summary>
    public class BattleSpeedController : MonoBehaviour
    {
        public static BattleSpeedController Instance { get; private set; }

        private readonly List<Speed> _subscribers = new List<Speed>();

        private float _battleSpeed = 1f;
        private float _skillControlledSpeed = 1f;
        private bool _isPaused = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>Speedの購読を開始</summary>
        public void Subscribe(Speed speed)
        {
            if (!_subscribers.Contains(speed))
                _subscribers.Add(speed);
        }

        /// <summary>Speedの購読を終了</summary>
        public void Unsubscribe(Speed speed)
        {
            if (_subscribers.Contains(speed))
                _subscribers.Remove(speed);
        }

        /// <summary>倍速設定</summary>
        public void SetDoubleSpeed(bool isDouble)
        {
            _battleSpeed = isDouble ? 2f : 1f;
            NotifyAll();
        }

        /// <summary>一時停止設定</summary>
        public void SetPause(bool isPause)
        {
            _isPaused = isPause;
            NotifyAll();
        }

        /// <summary>スキルによるスピード制御設定</summary>
        public void SetSkillControlledSpeed(float speed)
        {
            _skillControlledSpeed = speed;
            NotifyAll();
        }

        /// <summary>全登録Speedインスタンスに通知</summary>
        private void NotifyAll()
        {
            foreach (var speed in _subscribers)
            {
                float calculated = CalculateSpeed(speed);
                speed.SetSpeed(calculated);
            }
        }

        /// <summary>Speedインスタンスごとの最終スピードを計算</summary>
        private float CalculateSpeed(Speed instance)
        {
            if (_isPaused)
                return 0f;

            float result = _battleSpeed;
            // フィルターが有効ならスキル制御値を掛け合わせ
            if (instance is IFilterableSpeed filterable && !filterable.UseSkillControlledSpeed)
                return result;

            result *= _skillControlledSpeed;
            return result;
        }
    }

    /// <summary>
    /// スキル制御フィルターを持つSpeed拡張
    /// </summary>
    public interface IFilterableSpeed
    {
        bool UseSkillControlledSpeed { get; set; }
    }

    /// <summary>
    /// キャラクターの移動やアニメーションにSpeedを適用する例
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterSpeedController : MonoBehaviour, IFilterableSpeed
    {
        private Speed _speed = new Speed();
        private Animator _animator;

        public bool UseSkillControlledSpeed { get; set; } = true;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _speed.OnValueChange += UpdateAnimatorSpeed;
            BattleSpeedController.Instance.Subscribe(_speed);
        }

        private void OnDestroy()
        {
            BattleSpeedController.Instance.Unsubscribe(_speed);
            _speed.OnValueChange -= UpdateAnimatorSpeed;
        }

        private void UpdateAnimatorSpeed(float newSpeed)
        {
            _animator.speed = newSpeed;
        }
    }
}
