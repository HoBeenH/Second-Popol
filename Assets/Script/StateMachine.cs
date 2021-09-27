using System;
using System.Collections.Generic;
using UnityEngine;

namespace Script
{
    public class StateMachine<T>
    {
        private readonly Dictionary<Type, State<T>> m_States = new Dictionary<Type, State<T>>();
        private State<T> CurrentState { get; set; }
        public readonly Animator animator;
        private readonly T m_Owner;

        public StateMachine(Animator anim, T currentOwner, State<T> state)
        {
            this.animator = anim;
            this.CurrentState = state;
            this.m_Owner = currentOwner;
            SetState(state);
            CurrentState?.OnStateEnter();
        }


        public bool IsEnd()
        {
            var _currentAnim = animator.GetCurrentAnimatorStateInfo(0);
            return _currentAnim.normalizedTime >= 1f && _currentAnim.fullPathHash == CurrentState.animToHash;
        }

        public void SetState(State<T> state)
        {
            state.AddState(this, m_Owner);
            m_States[state.GetType()] = state;
        }

        public void Update()
        {
            var _currentAnim = animator.GetCurrentAnimatorStateInfo(0);

            if (CurrentState.animToHash == _currentAnim.fullPathHash || CurrentState.animToHash == 0)
            {
                var _state = CurrentState;
                CurrentState?.OnStateChangePoint();
                if (_state == CurrentState)
                {
                    CurrentState?.OnStateUpdate();
                }
            }
        }

        public void FixedUpdate()
        {
            var _currentAnim = animator.GetCurrentAnimatorStateInfo(0);

            if (CurrentState.animToHash == _currentAnim.fullPathHash || CurrentState.animToHash == 0)
            {
                var _state = CurrentState;
                CurrentState?.OnStateChangePoint();
                if (_state == CurrentState)
                {
                    CurrentState?.OnStateFixedUpdate();
                }
            }
        }

        public TR ChangeState<TR>() where TR : State<T>
        {
            var _newType = typeof(TR);
            if (CurrentState.GetType() == _newType)
            {
                return CurrentState as TR;
            }

            CurrentState?.OnStateExit();
            CurrentState = m_States[_newType];
            CurrentState?.OnStateEnter();
            return CurrentState as TR;
        }
    }
}