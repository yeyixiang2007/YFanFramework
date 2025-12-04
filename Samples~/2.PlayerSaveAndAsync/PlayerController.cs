using Cysharp.Threading.Tasks;
using QFramework;
using TMPro;
using UnityEngine;
using YFan.Attributes;
using YFan.Modules;
using YFan.Runtime.Base.Abstract;
using YFan.Utils;

public class ChangePlayerPositionCommand : AbstractCommand
{
    public Vector2 moveInput;

    protected override void OnExecute()
    {
        var playerModel = this.GetModel<IPlayerModel>();

        var currentPos = playerModel.Position.Value;
        var speed = 5f * Time.deltaTime;
        playerModel.Position.Value = currentPos + new Vector3(moveInput.x * speed, 0, moveInput.y * speed);
    }
}

/// <summary>
/// 玩家控制器
/// </summary>
public class PlayerController : UIAbstractController
{
    [UIBind] public TMP_Text Txt_Status;

    public Transform PlayerTransform;
    public MeshRenderer PlayerRenderer;

    private IInputSystem _inputSystem;
    private IPlayerSystem _playerSystem;
    private IPlayerModel _playerModel;

    protected override void OnAwake()
    {
        // 获取系统和模型引用
        _inputSystem = this.GetSystem<IInputSystem>();
        _playerSystem = this.GetSystem<IPlayerSystem>();
        _playerModel = this.GetModel<IPlayerModel>();

        _playerModel.Position.RegisterWithInitValue(pos =>
        {
            if (PlayerTransform) PlayerTransform.position = pos;
        }).UnRegisterWhenGameObjectDestroyed(gameObject);

        _playerModel.SkinColor.RegisterWithInitValue(col =>
        {
            if (PlayerRenderer) PlayerRenderer.material.color = col;
        }).UnRegisterWhenGameObjectDestroyed(gameObject);

        Txt_Status.text = "Press [WASD] to move, [Z] to save, [L] to load";
    }

    private void Update()
    {
        if (!_inputSystem.IsReady) return;

        Vector2 moveInput = _inputSystem.GetVector2("Move");
        if (moveInput != Vector2.zero)
        {
            this.SendCommand<ChangePlayerPositionCommand>(new ChangePlayerPositionCommand
            {
                moveInput = moveInput
            });
        }

        bool isSave = _inputSystem.GetButtonDown("Save");
        bool isLoad = _inputSystem.GetButtonDown("Load");

        if (isSave)
        {
            _playerSystem.SaveGame();
            ShowSaveStatusAsync().Forget();
        }

        if (isLoad)
        {
            _playerSystem.LoadGame();
            ShowLoadStatusAsync().Forget();
        }
    }

    /// <summary>
    /// 显示保存状态异步
    /// </summary>
    /// <returns></returns>
    private async UniTaskVoid ShowSaveStatusAsync()
    {
        Txt_Status.text = "<color=green>Saving...</color>";
        Txt_Status.transform.localScale = Vector3.one * 1.2f;

        await TaskUtil.Delay(0.5f);

        Txt_Status.text = "Save Successful!";
        Txt_Status.transform.localScale = Vector3.one;

        await TaskUtil.Delay(1.5f);

        Txt_Status.text = "Press [WASD] to move, [Z] to save, [L] to load";
    }

    /// <summary>
    /// 显示读取状态异步
    /// </summary>
    /// <returns></returns>
    private async UniTaskVoid ShowLoadStatusAsync()
    {
        Txt_Status.text = "<color=yellow>Loading...</color>";

        await TaskUtil.Delay(0.5f);

        Txt_Status.text = "Load Successful!";
        await TaskUtil.Delay(1.0f);
        Txt_Status.text = "Press [WASD] to move, [Z] to save, [L] to load";
    }
}
