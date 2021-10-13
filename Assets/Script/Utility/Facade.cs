using Script.Dragon;
using Script.Dragon.FSM;
using Script.Player.FSM;

namespace Script
{
    // 싱글톤 관리와 가독성을 위한 파샤드 스크립트
    public static class Facade
    {
        public static Player_Controller _PlayerController => Player_Controller.Instance;
        public static Dragon_Controller _DragonController => Dragon_Controller.Instance;
        public static ObjPool _ObjPool => ObjPool.Instance;
        public static EffectManager _EffectManager => EffectManager.Instance;
        public static Dragon_Pattern _DragonPattern => Dragon_Pattern.Instance;
    }
}