using System;
using System.Collections;
using System.Collections.Generic;
using Script.Dragon.FSM;
using Script.Player.FSM;
using UnityEngine;

namespace Script
{
    public class StateMachine<T>
    {
        private readonly Dictionary<Type, State<T>> m_States = new Dictionary<Type, State<T>>();
        private State<T> CurrentState { get; set; }
        private readonly T m_Owner;
        private readonly WaitUntil m_WaitIdle;
        private readonly Type m_Idle;

        public readonly Animator anim;

        // 공격이 끊길때 사용할 코루틴 리스트
        public readonly List<Coroutine> cancel = new List<Coroutine>();

        public StateMachine(Animator anim, T currentOwner, State<T> state)
        {
            this.anim = anim;
            this.CurrentState = state;
            this.m_Owner = currentOwner;
            SetState(state);
            CurrentState?.OnStateEnter();
            m_WaitIdle = new WaitUntil(() =>
                this.anim.GetCurrentAnimatorStateInfo(0).fullPathHash == Animator.StringToHash("Base Layer.Move"));

            if (currentOwner.GetType() == typeof(Dragon_Controller))
            {
                m_Idle = typeof(Dragon_Movement);
            }

            if (currentOwner.GetType() == typeof(Player_Controller))
            {
                m_Idle = typeof(Player_Movement);
            }
        }

        // 변수의 상태가 종료되면 자동으로 Idle로 전환된다
        public IEnumerator WaitForState(params int[] hash)
        {
            foreach (var currentAnim in hash)
            {
                yield return new WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).fullPathHash == currentAnim);
            }

            yield return m_WaitIdle;

            CurrentState?.OnStateExit();
            CurrentState = m_States[m_Idle];
            CurrentState?.OnStateEnter();
        }

        public void SetState(State<T> state)
        {
            state.AddState(this, m_Owner);
            m_States[state.GetType()] = state;
        }

        public void OnUpdate()
        {
            var _currentAnim = anim.GetCurrentAnimatorStateInfo(0);

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

        public void ChangeState(Type newType)
        {
            if (CurrentState.GetType() == newType)
            {
                return;
            }

            CurrentState?.OnStateExit();
            CurrentState = m_States[newType];
            cancel?.Clear();
            CurrentState?.OnStateEnter();
        }
    }
}