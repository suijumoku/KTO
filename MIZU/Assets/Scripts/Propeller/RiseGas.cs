using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// �K�v�ȃR���|�[�l���g�������I�ɃA�^�b�`
[RequireComponent(typeof(Collider), typeof(Rigidbody))]

public class RiseGas : MonoBehaviour
{
    [Header("�v���y���̕��͈͓̔���Gas��Ԃ̃v���C���[�̑��x�̒���")]
    [Tooltip("�㉺�̑��x")]
    [SerializeField] private float riseSpeed = 100f;   //  �㏸���x
    [Tooltip("���E�̑��x")]
    [SerializeField] private float moveRightSpeed = 100f;  //  �E�����ւ̈ړ����x

    [SerializeField] private MM_PlayerPhaseState _pState;

    //  Gas�ɉe����^����A�N�e�B�u��Direction�̃��X�g
    private readonly List<GasDirection> activeDirections = new();

    private new Rigidbody rigidbody;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        //  �K�v�ȃR���|�[�l���g�̎擾
        
        //  �G���[�̃`�F�b�N
        if (!TryGetComponent<MM_PlayerPhaseState>(out _pState))
        {
            Debug.LogError("MM_PlayerPhaseState �R���|�[�l���g��������܂���");
        }
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        if (_pState.GetState() != MM_PlayerPhaseState.State.Gas) return;

        Vector3 finalMove = Vector3.zero;

        foreach (var direction in activeDirections)
        {
            // �e�����ɉ��������x���悶�Ĉړ��x�N�g����ݐ�
            float speed = Mathf.Abs(direction.Dir.x) > 0 ? moveRightSpeed : riseSpeed;
            finalMove += direction.Dir.normalized * speed;
        }

        if (finalMove == Vector3.zero)
        {
            //rigidbody.velocity = Vector3.zero;
            return;
        }

        rigidbody.AddForce(finalMove, ForceMode.Acceleration);

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out GasDirection dir))
        {
            //  �K�X�G���A�ɓ�����Direction�����X�g�ɒǉ�
            if (!activeDirections.Contains(dir))
            {
                activeDirections.Add(dir);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out GasDirection directionSwap))
        {
            //  �K�X�G���A����o��Direction�����X�g����폜
            if (activeDirections.Contains(directionSwap))
            {
                activeDirections.Remove(directionSwap);
            }
        }
    }
}