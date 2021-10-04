using System;
using System.Collections;
using System.Linq;
using Script.Player;
using UnityEngine;
using Random = UnityEngine.Random;
using static Script.Facade;

namespace Script.Dragon
{
    [Flags]
    public enum EDragonStatUpFlag
    {
        Default = 1 << 0,
        AntiMagic = 1 << 1,
        AntiSword = 1 << 2,
        SpeedUp = 1 << 3,
        DamageUp = 1 << 4,
        HealthUp = 1 << 5,
        End = 1 << 6
    }

    public class DragonPhaseManager : MonoSingleton<DragonPhaseManager>
    {
        public EDragonStatUpFlag m_StatUpFlag = EDragonStatUpFlag.Default;
        private readonly int[] m_Phase2StatUp = new int[3];
        private const int HEALTH = 0;
        private const int SPEED = 1;
        private const int DAMAGE = 2;
        private float m_WaitForSecondPhase = 60f;
        private int m_MagicCount;
        private int m_SwordCount;


        private void Start()
        {
            StartCoroutine(nameof(SecondPhaseStart));
        }

        public void HitCheck(ECurrentWeaponFlag currentWeaponFlag)
        {
            m_WaitForSecondPhase -= 1f;
            switch (currentWeaponFlag)
            {
                case ECurrentWeaponFlag.Sword:
                    m_SwordCount += 1;
                    break;
                case ECurrentWeaponFlag.Magic:
                    m_MagicCount += 1;
                    break;
                default:
                    throw new Exception($"Unknown Type : {currentWeaponFlag.ToString()}");
            }

            var _randomStat = Random.Range(0, 3);
            switch (_randomStat)
            {
                case 0:
                    m_Phase2StatUp[HEALTH] += 1;
                    break;
                case 1:
                    m_Phase2StatUp[SPEED] += 1;
                    break;
                case 2:
                    m_Phase2StatUp[DAMAGE] += 1;
                    break;
            }
        }

        private IEnumerator SecondPhaseStart()
        {
            while (true)
            {
                var _time = Time.time;
                if (_time >= m_WaitForSecondPhase)
                {
                    break;
                }

                yield return null;
            }

            m_StatUpFlag = m_MagicCount >= m_SwordCount
                ? m_StatUpFlag |= EDragonStatUpFlag.AntiMagic
                : m_StatUpFlag |= EDragonStatUpFlag.AntiSword;

            var _max = m_Phase2StatUp.Max();

            for (var i = 0; i < m_Phase2StatUp.Length; i++)
            {
                if (m_Phase2StatUp[i] != _max) 
                    continue;
                m_StatUpFlag = i switch
                {
                    0 => m_StatUpFlag |= EDragonStatUpFlag.HealthUp,
                    1 => m_StatUpFlag |= EDragonStatUpFlag.SpeedUp,
                    2 => m_StatUpFlag |= EDragonStatUpFlag.DamageUp,
                    _ => throw new Exception($"Can't Find : {_max}")
                };
                break;
            }

            Debug.Log($"Second\n{m_StatUpFlag.ToString()}");
            PhaseChange();
        }

        private void PhaseChange()
        {
            if (_DragonController.currentPhaseFlag.HasFlag(EDragonPhaseFlag.Phase1))
            {
                _DragonController.currentPhaseFlag |= EDragonPhaseFlag.Phase2;
                _DragonController.currentPhaseFlag &= ~EDragonPhaseFlag.Phase1;
                PhaseStatChange();
            }
            else if (_DragonController.currentPhaseFlag.HasFlag(EDragonPhaseFlag.Phase2))
            {
                Debug.Log("Current Phase Is Phase 2");
            }
        }


        private void PhaseStatChange()
        {
            if (m_StatUpFlag.Equals(EDragonStatUpFlag.Default))
            {
                Debug.Log($"{m_StatUpFlag.ToString()} Is Default");
                return;
            }

            if (m_StatUpFlag.HasFlag(EDragonStatUpFlag.AntiMagic))
            {
                _DragonController.DragonStat.magicDefence += 3;
            }

            if (m_StatUpFlag.HasFlag(EDragonStatUpFlag.AntiSword))
            {
                _DragonController.DragonStat.defence += 3;
            }

            if (m_StatUpFlag.HasFlag(EDragonStatUpFlag.SpeedUp))
            {
                _DragonController.nav.speed += 2f;
                _DragonController.DragonStat.animSpeed += 0.2f;
                _DragonController.currentPhaseFlag |= EDragonPhaseFlag.SpeedUp;
            }

            if (m_StatUpFlag.HasFlag(EDragonStatUpFlag.DamageUp))
            {
                _DragonController.DragonStat.damage += 5;
                _DragonController.currentPhaseFlag |= EDragonPhaseFlag.DamageUp;
            }

            if (m_StatUpFlag.HasFlag(EDragonStatUpFlag.HealthUp))
            {
                _DragonController.DragonStat.health += 300;
                _DragonController.currentPhaseFlag |= EDragonPhaseFlag.HealthUp;
            }

            m_StatUpFlag = EDragonStatUpFlag.End;

            var end = GetComponent<DragonPhaseManager>();
            end.enabled = false;
        }
    }
}