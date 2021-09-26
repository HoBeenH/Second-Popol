using System;
using System.Collections;
using Script.Player;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Script.Dragon
{
    [System.Flags]
    public enum EDragonPhaseFlag
    {
        Phase1 = 1 << 0,
        Phase2 = 1 << 1,
        Angry = 1 << 2,
        Exhausted = 1 << 3,
        Dead = 1 << 4
    }

    [System.Flags]
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
        private EDragonStatUpFlag m_StatUpFlag = EDragonStatUpFlag.Default;
        public EDragonPhaseFlag currentPhaseFlag = EDragonPhaseFlag.Phase1;
        private readonly WaitForSeconds m_ReadyForSecondPhase = new WaitForSeconds(30f);
        private readonly WaitForSeconds m_ExhaustedTime = new WaitForSeconds(5f);
        private readonly WaitForSeconds m_AngryTime = new WaitForSeconds(10f);
        private readonly int[] m_Phase2StatUp = new int[3];
        private const int HEALTH = 0;
        private const int SPEED = 1;
        private const int DAMAGE = 2;
        private int m_MagicCount;
        private int m_SwordCount;
        private int m_DragonAngry;
        private int m_DragonAngryMax = 5;

        private void Start()
        {
            StartCoroutine(nameof(SecondPhaseStart));
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                ++m_DragonAngry;
                var _currentWeapon = PlayerController.Instance.currentWeaponFlag;

                if (m_StatUpFlag != EDragonStatUpFlag.End)
                {
                    HitCheck(_currentWeapon);
                }

                if (m_DragonAngry >= m_DragonAngryMax)
                {
                    m_DragonAngry = 0;
                    ++m_DragonAngryMax;
                    currentPhaseFlag |= EDragonPhaseFlag.Angry;
                }

                var _damage = _currentWeapon switch
                {
                    ECurrentWeaponFlag.Magic => PlayerController.Instance.PlayerStat.skillDamage -
                                            DragonController.Instance.dragonStat.magicDefence,
                    ECurrentWeaponFlag.Sword => PlayerController.Instance.PlayerStat.damage -
                                            DragonController.Instance.dragonStat.defence,
                    // ECurrentWeaponFlag.Parry => Dragon Stun,
                    _ => throw new Exception($"Unknown Type : {_currentWeapon.ToString()}")
                };
                if (_damage <= 0)
                    return;
                DragonController.Instance.TakeDamage(_damage);
            }
        }

        private void HitCheck(ECurrentWeaponFlag currentWeaponFlag)
        {
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
            yield return m_ReadyForSecondPhase;
            m_StatUpFlag = m_MagicCount >= m_SwordCount
                ? m_StatUpFlag |= EDragonStatUpFlag.AntiMagic
                : m_StatUpFlag |= EDragonStatUpFlag.AntiSword;

            var _index = 0;
            for (var i = 0; i < m_Phase2StatUp.Length - 1; i++)
            {
                if (m_Phase2StatUp[i] <= m_Phase2StatUp[i + 1])
                {
                    _index = i + 1;
                }
            }

            m_StatUpFlag = _index switch
            {
                0 => m_StatUpFlag |= EDragonStatUpFlag.HealthUp,
                1 => m_StatUpFlag |= EDragonStatUpFlag.SpeedUp,
                2 => m_StatUpFlag |= EDragonStatUpFlag.DamageUp,
                _ => throw new Exception($"Can't Find : {_index}")
            };
            PhaseChange();
        }

        private void PhaseChange()
        {
            if (currentPhaseFlag.HasFlag(EDragonPhaseFlag.Phase1))
            {
                currentPhaseFlag |= EDragonPhaseFlag.Phase2;
                currentPhaseFlag &= ~EDragonPhaseFlag.Phase1;
                PhaseStatChange();
            }
            else if (currentPhaseFlag.HasFlag(EDragonPhaseFlag.Phase2))
            {
                Debug.Log("Current Phase Is Phase 2");
            }
        }
        

        private void PhaseStatChange()
        {
            if (m_StatUpFlag == EDragonStatUpFlag.Default)
            {
                Debug.Log($"{m_StatUpFlag.ToString()} Is Default");
                return;
            }

            if (m_StatUpFlag.HasFlag(EDragonStatUpFlag.AntiMagic))
            {
                DragonController.Instance.dragonStat.magicDefence += 3;
            }

            if (m_StatUpFlag.HasFlag(EDragonStatUpFlag.AntiSword))
            {
                DragonController.Instance.dragonStat.defence += 3;
            }

            if (m_StatUpFlag.HasFlag(EDragonStatUpFlag.SpeedUp))
            {
                DragonController.Instance.dragonStat.animSpeed += 0.2f;
                DragonController.Instance.dragonStat.moveSpeed += 3f;
            }

            if (m_StatUpFlag.HasFlag(EDragonStatUpFlag.DamageUp))
            {
                DragonController.Instance.dragonStat.damage += 5;
            }

            if (m_StatUpFlag.HasFlag(EDragonStatUpFlag.HealthUp))
            {
                DragonController.Instance.dragonStat.recovery += 1;
                DragonController.Instance.dragonStat.maxHealth = 300;
                DragonController.Instance.dragonStat.currentHealth = 250;
            }

            m_StatUpFlag = EDragonStatUpFlag.End;
        }

        public IEnumerator DragonAngry()
        {
            while (currentPhaseFlag != EDragonPhaseFlag.Dead)
            {
                if (currentPhaseFlag.HasFlag(EDragonPhaseFlag.Angry))
                {
                    this.currentPhaseFlag &= ~EDragonPhaseFlag.Angry;
                    DragonHasBuff(true);
                    yield return m_AngryTime;

                    this.currentPhaseFlag |= EDragonPhaseFlag.Exhausted;
                    DragonHasBuff(false);
                    yield return m_ExhaustedTime;

                    this.currentPhaseFlag &= ~EDragonPhaseFlag.Exhausted;
                    DragonHasBuff(true);
                }

                yield return null;
            }
        }

        private void DragonHasBuff(bool isBuff)
        {
            if (isBuff)
            {
                DragonController.Instance.dragonStat.animSpeed += 0.1f;
                DragonController.Instance.dragonStat.moveSpeed += 1.5f;
                DragonController.Instance.dragonStat.damage += 2;
            }
            else
            {
                DragonController.Instance.dragonStat.animSpeed -= 0.2f;
                DragonController.Instance.dragonStat.moveSpeed -= 3f;
                DragonController.Instance.dragonStat.damage -= 4;
            }
        }

    }
}
