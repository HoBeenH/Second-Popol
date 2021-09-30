My email is "kripto289@gmail.com"
You can contact me for any questions.

My English is not very good, and if there are any translation errors, you can let me know :)

This simple tutorial only for HDRP
Pack includes prefabs of main effects + prefabs of collision effects (Assets\KriptoFX\Realistic Effects Pack v1\Prefabs\).

Supported platforms: PC/Consoles/VR/Mobiles
All effects tested on Oculus Rift CV1 with single and dual mode rendering and work correctly.

------------------------------------------------------------------------------------------------------------------------------------------
NOTE: For correct effects working you must:

	Camera -> Add Component -> Volume -> "IsGlobal = true" -> set the profile "\Assets\KriptoFX\Realistic Effects Pack v1\PostProcess Profile.asset"

	In unity 2019.3+ added new "Threshold" parameter for bloom and you need change bloom settings (because default behaviour of bloom intencity was changed):

	Threshold 1.5
	Intencity 0.5
	Scatter 0.9

------------------------------------------------------------------------------------------------------------------------------------------
Using effects:

Just drag and drop prefab of effect on scene and use that :)
If you want use effects in runtime, use follow code:

"Instantiate(prefabEffect, position, rotation);"

Using projectile collision event:
void Start ()
{
	var tm = GetComponentInChildren<RFX1_TransformMotion>(true);
	if (tm!=null) tm.CollisionEnter += Tm_CollisionEnter;
}

private void Tm_CollisionEnter(object sender, RFX1_TransformMotion.RFX1_CollisionInfo e)
{
        Debug.Log(e.Hit.transform.name); //will print collided object name to the console.
}

Using shield interaction:
You need add script "RFX1_ShieldInteraction" on projectiles which should react on shields.

------------------------------------------------------------------------------------------------------------------------------------------
Effect modification:

For scaling just change "transform" scale of effect.
All effects includes helpers scripts (collision behaviour, light/shader animation etc) for work out of box.
Also you can add additional scripts for easy change of base effects settings. Just add follow scripts to prefab of effect.

RFX1_EffectSettingColor - for change color of effect (uses HUE color). Can be added on any effect.
RFX1_EffectSettingProjectile - for change projectile fly distance, speed and collided layers.
RFX1_EffectSettingVisible - for change visible status of effect using smooth fading by time.
RFX1_Target - for homing move to target.

