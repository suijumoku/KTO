using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(MM_PlayerPhaseState))]
public class MM_Test_Player : MonoBehaviour
{
    [SerializeField]
    [Header("�f�o�b�O���[�h")]
    bool IS_DEBUGMODE = false;
    [SerializeField]
    TextMeshProUGUI Debug_Phasetext;

    [Header("�^���X�e�[�^�X")]
    [SerializeField]
    private float _defaultGravity;
    [SerializeField]
    private float nowGravity;
    [SerializeField]
    private float _JumpPower;
    [SerializeField]
    private float _MovePower;
    [SerializeField]
    private float _LimitSpeed;
    [SerializeField, Header("������,-1~10")]
    private float _InertiaPower;

    [SerializeField]
    private float _NowXSpeed;
    [SerializeField]
    private float _NowYSpeed;
    [SerializeField]
    private int _pRotation = 1;
    [SerializeField]
    private Material[] _playerMaterials = new Material[2];
    [SerializeField]
    private MM_GroundCheck _groundCheck;

    bool isOnGround = false;
    bool isOnWater = false;

    Rigidbody _rb;
    PlayerInput _playerInput;
    MeshRenderer _meshRenderer;
    MM_PlayerPhaseState _pState;

    private Vector3 _velocity;

    private KK_PlayerModelSwitcher _modelSwitcher;
    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _playerInput = GetComponent<PlayerInput>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _pState = GetComponent<MM_PlayerPhaseState>();
        _modelSwitcher = GetComponent<KK_PlayerModelSwitcher>(); // PlayerModelSwitcher �R���|�[�l���g���擾

        if (_groundCheck == null)
            Debug.LogWarning($"{nameof(_groundCheck)}�ɃA�^�b�`����Ă��܂���");


        if (_playerInput.user.index == 0)
            _meshRenderer.material = _playerMaterials[0];
        else
            _meshRenderer.material = _playerMaterials[1];

        _pState.ChangeState(MM_PlayerPhaseState.State.Liquid);

        nowGravity = _defaultGravity;

        _InertiaPower = Mathf.Clamp(_InertiaPower, -1, 10);

    }

    private void Update()
    {
        if (Debug_Phasetext != null)
            Debug_Phasetext.text = "Player:" + _pState.GetState();
        PlayerStateUpdateFunc();
    }

    private void FixedUpdate()
    {
        Gravity();
        GroundCheck();
        Move();
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Death();
        }
    }

    void Gravity()
    {
        _rb.AddForce(new Vector3(0, -nowGravity, 0), ForceMode.Acceleration);
    }

    void Move()
    {
        var nowXSpeed = Mathf.Sqrt(Mathf.Pow(_rb.velocity.x, 2));
        var nowYSpeed = Mathf.Sqrt(Mathf.Pow(_rb.velocity.y, 2));
        _NowXSpeed = nowXSpeed;
        _NowYSpeed = nowYSpeed;

        // ����ȊO�̎��̉��ړ�
        if (_pState.GetState() != MM_PlayerPhaseState.State.Gas)
        {
            if (_velocity.x != 0)
                _rb.AddForce(_velocity, ForceMode.Acceleration);
            else
                _rb.AddForce(new Vector3(-_rb.velocity.x * _InertiaPower, _rb.velocity.y, _rb.velocity.z), ForceMode.Acceleration);
        }
        // �K�X�̎��̏c�ړ�
        else
        {
            if (_velocity.y != 0)
                _rb.AddForce(_velocity, ForceMode.Acceleration);
            else
                _rb.AddForce(new Vector3(_rb.velocity.x, -_rb.velocity.y * _InertiaPower, _rb.velocity.z), ForceMode.Acceleration);
        }

        if (nowXSpeed > _LimitSpeed)
        {
            _rb.velocity = new Vector3(_rb.velocity.x / (nowXSpeed / _LimitSpeed), _rb.velocity.y, _rb.velocity.z);
            //print("LimitedXSpeed");
        }
        if (nowYSpeed > _LimitSpeed)
        {
            _rb.velocity = new Vector3(_rb.velocity.x, _rb.velocity.y / (nowYSpeed / _LimitSpeed), _rb.velocity.z);
            //print("LimitedYSpeed");
        }

        // �v�Z�ł��؂�
        if (nowXSpeed < 1)
            _rb.velocity = new Vector3(0, _rb.velocity.y, _rb.velocity.z);

    }

    private void GroundCheck()
    {
        isOnGround = _groundCheck.IsGround();
        isOnWater = _groundCheck.IsPuddle();
    }

    private void PlayerStateUpdateFunc()
    {
        switch (_pState.GetState())
        {
            case MM_PlayerPhaseState.State.Gas: PlayerGasStateUpdateFunc(); break;
            case MM_PlayerPhaseState.State.Solid: PlayerSolidStateUpdateFunc(); break;
            case MM_PlayerPhaseState.State.Liquid: PlayerLiquidStateUpdateFunc(); break;
            case MM_PlayerPhaseState.State.Slime: PlayerSlimeStateUpdateFunc(); break;
            default: Debug.LogError($"�G���[�A�v���C���[�̃X�e�[�g��{_pState.GetState()}�ɂȂ��Ă��܂�"); break;
        }
    }

    private void PlayerGasStateUpdateFunc() { }
    private void PlayerSolidStateUpdateFunc() { }
    private void PlayerLiquidStateUpdateFunc()
    {
        StartCoroutine(IsPuddleCollisionDeadCount());
    }
    private void PlayerSlimeStateUpdateFunc() { }
    private void Death()
    {
        _groundCheck.ResetFlag();
        this.gameObject.SetActive(false);
    }
    // ���\�b�h���͉��ł�OK
    // public�ɂ���K�v������
    public void OnMove(InputAction.CallbackContext context)
    {
        // �C�̂Ȃ牡�ړ��͂ł��Ȃ�
        if (_pState.GetState() == MM_PlayerPhaseState.State.Gas) return;
        // �ő̂̎����ɐG��ĂȂ������瓮���Ȃ�
        if (_pState.GetState() == MM_PlayerPhaseState.State.Solid)
            if (!isOnWater) return;
        // MoveAction�̓��͒l���擾
        var axis = context.ReadValue<Vector2>();

        print($"{nameof(axis.x)}:{axis.x}");
        // �v���C���[���E�����Ȃ�1�A���Ȃ�|1
        if (axis.x != 0)
            _pRotation = axis.x > 0f ? 1 : -1;

        // 2D�Ȃ̂ŉ��ړ�����
        _velocity = new Vector3(axis.x * _MovePower, 0, 0);

    }
    public void OnGasMove(InputAction.CallbackContext context)
    {
        // �C�̂łȂ���Ώc�ړ��͂ł��Ȃ�
        if (_pState.GetState() != MM_PlayerPhaseState.State.Gas)
            return;

        // MoveAction�̓��͒l���擾
        var axis = context.ReadValue<Vector2>();

        // �C�̂̎��͏c�ړ�����
        _velocity = new Vector3(0, axis.y * _MovePower, 0);
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        // �������u�Ԃ�����������
        if (!context.performed) return;
        // �n�ʂɂ��Ȃ��Ȃ璵�ׂȂ�
        if (!isOnGround) return;
        // ���ɐG��Ă����璵�ׂȂ�
        if (isOnWater) return;
        // �C�̂Ȃ璵�ׂȂ�
        if (_pState.GetState() == MM_PlayerPhaseState.State.Gas) return;

        _velocity = new Vector3(_velocity.x, 0, 0);

        _rb.AddForce(new Vector3(0, _JumpPower, 0), ForceMode.VelocityChange);

        print("Jump��������܂���");
    }


    // ���ɐG�ꂽ�玀�S�܂ł̃J�E���g���J�n
    private IEnumerator IsPuddleCollisionDeadCount()
    {
        float contactTime = 0f;
        float destroyTime = 2f;

        while (isOnWater)
        {
            contactTime += Time.deltaTime;
            yield return null;
            if (contactTime >= destroyTime)
            {
                Death();
            }
            // print($"{nameof(contactTime)}:{contactTime}");
        }
    }

    /// <summary>
    /// �C�̂֕ω�
    /// </summary>
    public void OnStateChangeGas(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // ������Ȃ�������󂯕t���Ȃ�
        if (_pState.GetState() != MM_PlayerPhaseState.State.Liquid) return;

        _pState.ChangeState(MM_PlayerPhaseState.State.Gas);

        // �d�͂�0�ɂ���
        nowGravity = 0;
        // ��C��R�𔭐�������
        _rb.drag = 10;

        print("GAS(�C��)�ɂȂ�܂���");

        if (IS_DEBUGMODE)
            return;
        // ���f�����C�̂̂�ɕς��鏈��
        _modelSwitcher.SwitchToModel(_modelSwitcher.gasModel);
        //

        print("GAS(�C��)�ɂȂ�܂���");
    }

    /// <summary>
    /// �ő̂֕ω�
    /// </summary>
    public void OnStateChangeSolid(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // ������Ȃ�������󂯕t���Ȃ�
        if (_pState.GetState() != MM_PlayerPhaseState.State.Liquid) return;

        _pState.ChangeState(MM_PlayerPhaseState.State.Solid);


        _velocity = Vector3.zero;
        //_rb.angularVelocity = Vector3.zero;

        print("SOLID(�ő�)�ɂȂ�܂���");

        if (IS_DEBUGMODE)
            return;
        // ���f�����ő̂̂�ɕς��鏈��
        _modelSwitcher.SwitchToModel(_modelSwitcher.solidModel);
        //

        print("SOLID(�ő�)�ɂȂ�܂���");
    }
    /// <summary>
    /// �t�̂֕ω�
    /// </summary>
    public void OnStateChangeLiquid(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // �ő́E�C�́E�X���C������Ȃ�������󂯕t���Ȃ�
        if (_pState.GetState() == MM_PlayerPhaseState.State.Liquid) return;

        _pState.ChangeState(MM_PlayerPhaseState.State.Liquid);

        // �d�͂�ʏ�ɖ߂�
        nowGravity = _defaultGravity;
        // ��C��R���Ȃ���
        _rb.drag = 0;

        print("LIQUID(��)�ɂȂ�܂���");

        if (IS_DEBUGMODE)
            return;
        // ���f���𐅂̂�ɕς��鏈��
        _modelSwitcher.SwitchToModel(_modelSwitcher.liquidModel);
        //

        print("LIQUID(��)�ɂȂ�܂���");
    }

    /// <summary>
    /// �X���C���֕ω�
    /// </summary>
    public void OnStateChangeSlime(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // ������Ȃ�������󂯕t���Ȃ�
        if (_pState.GetState() != MM_PlayerPhaseState.State.Liquid) return;

        _pState.ChangeState(MM_PlayerPhaseState.State.Slime);

        print("SLIME(�X���C��)�ɂȂ�܂���");

        if (IS_DEBUGMODE)
            return;
        // ���f�����X���C���̂�ɕς��鏈��
        _modelSwitcher.SwitchToModel(_modelSwitcher.slimeModel);
        //

        print("SLIME(�X���C��)�ɂȂ�܂���");

    }

    public int GetPlayerOrientation()
    {
        return _pRotation;
    }

    public Vector2 GetSpeed()
    {
        return _rb.velocity;
    }
}