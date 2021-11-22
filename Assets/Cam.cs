using System;
using Cinemachine;
using DG.Tweening;
using Script;
using UnityEngine;
using UnityEngine.UI;
using Sequence = DG.Tweening.Sequence;
using static Script.Facade;

public class Cam : MonoSingleton<Cam>
{
    [SerializeField] private Image image;
    private Sequence sequence;
    public Action end;
    public CinemachineFreeLook cam;
    public Transform tr;

    private void Start()
    {
        tr = _PlayerController.transform;
        Cursor.visible = false;
        image.DOFade(0f, 2f);
        sequence = DOTween.Sequence();
        sequence
            .Append(image.DOFade(1f, 2f).OnComplete(() =>
            {
                cam.Follow = tr;
                cam.LookAt = tr;
            }))
            .AppendInterval(1f)
            .Append(image.DOFade(0f, 1f))
            .OnComplete(GameStart)
            .Pause();
    }

    public void EndEvent() => sequence.Restart();

    private void GameStart()
    {
        end.Invoke();
    }
}