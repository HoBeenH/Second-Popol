using Script.Dragon;
using Script.Player;

namespace Script
{
    // 싱글톤 관리와 가독성을 위한 파샤드 스크립트
    public static class Facade
    {
        public static PlayerController _PlayerController => PlayerController.Instance;
        public static Dragon_Controller _DragonController => Dragon_Controller.Instance;
        public static DragonPhaseManager _DragonPhaseManager => DragonPhaseManager.Instance;
        public static ObjPool _ObjPool => ObjPool.Instance;
        public static EffectManager _EffectManager => EffectManager.Instance;
        public static SkillManager _SkillManager => SkillManager.Instance;
        public static DragonPattern _DragonPattern => DragonPattern.Instance;
    }
}