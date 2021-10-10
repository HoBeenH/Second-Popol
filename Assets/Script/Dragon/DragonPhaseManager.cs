using System;
using System.Collections;
using System.Linq;
using Script.Player;
using UnityEngine;
using Random = UnityEngine.Random;
using static Script.Facade;

namespace Script.Dragon
{
    public class DragonPhaseManager : MonoSingleton<DragonPhaseManager>
    {
        [Flags]
        private enum EDragonStatUpFlag
        {
            Default = 1 << 0,
            AntiMagic = 1 << 1,
            AntiSword = 1 << 2,
            SpeedUp = 1 << 3,
            DamageUp = 1 << 4,
        }

        private EDragonStatUpFlag m_StatUpFlag;
        private float m_WaitForSecondPhase = 20f;
        private int m_MagicCount;
        private int m_SwordCount;

        private void Start()
        {
            StartCoroutine(nameof(SecondPhaseStart));
        }

        public void HitCheck(EPlayerFlag playerFlag)
        {
            m_WaitForSecondPhase -= 1f;
            switch (playerFlag)
            {
                case EPlayerFlag.Sword:
                    m_SwordCount += 1;
                    break;
                case EPlayerFlag.Magic:
                    m_MagicCount += 1;
                    break;
                default:
                    throw new Exception($"Unknown Type : {playerFlag.ToString()}");
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


            m_StatUpFlag |= m_MagicCount >= m_SwordCount
                ? EDragonStatUpFlag.AntiMagic | EDragonStatUpFlag.SpeedUp
                : EDragonStatUpFlag.AntiSword | EDragonStatUpFlag.DamageUp;

            Debug.Log($"Second\n{m_StatUpFlag.ToString()}");
            PhaseStatChange();
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
                _EffectManager.DragonMesh = EPrefabName.SpeedUp;
            }

            if (m_StatUpFlag.HasFlag(EDragonStatUpFlag.DamageUp))
            {
                _DragonController.DragonStat.damage += 5;
                _EffectManager.DragonMesh = EPrefabName.DamageUp;
            }

            PhaseChange();
        }

        private void PhaseChange()
        {
            if (_DragonController.currentStateFlag.HasFlag(EDragonPhaseFlag.Phase1))
            {
                _DragonController.currentStateFlag |= EDragonPhaseFlag.Phase2SetUp;
                _DragonController.currentStateFlag &= ~EDragonPhaseFlag.Phase1;
            }
            else if (_DragonController.currentStateFlag.HasFlag(EDragonPhaseFlag.Phase2) ||
                     _DragonController.currentStateFlag.HasFlag(EDragonPhaseFlag.Phase2SetUp))
            {
                Debug.Log("Current Phase Is Phase 2");
            }

            var _end = GetComponent<DragonPhaseManager>();
            Destroy(_end);
        }
    }
}