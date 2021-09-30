using Script.Player;
using UnityEngine;

namespace Script.Dragon
{
    public class DragonEvent : MonoBehaviour
    {
        public Collider m_Col;

        public void AttackCol(int trueOrFalse)
        {
            switch (trueOrFalse)
            {
                case 0:
                    m_Col.enabled = false;
                    break;
                case 1:
                    m_Col.enabled = true;
                    break;
            }
        }
    }
}