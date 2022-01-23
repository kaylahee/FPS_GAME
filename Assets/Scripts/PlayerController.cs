using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private CloseWeapon currentHand;
    // 스피드 조정 변수
    [SerializeField]
    // inspector 창에서 수정가능토록 함
    private float walkSpeed;
    [SerializeField]
    private float runSpeed;
    // 앉기 스피드
    [SerializeField]
    private float crouchSpeed;
    // 대입만 해주면 되기 때문에 applySpeed 만듬
    private float applySpeed;

    [SerializeField]
    private float jumpForce;

    // 상태 변수
    private bool isWalk = false;
    private bool isRun = false;
    private bool isCrouch = false;
    private bool isGround = true;

    // 움직임 체크 변수
    private Vector3 lastPos;

    // 앉았을 때 얼마나 앉을지 결정하는 변수
    [SerializeField]
    private float crouchPosY;
    // 원래 값을 저장할 변수
    private float originPosY;
    private float applyCrouchPosY;

    // 땅 착지 여부
    private CapsuleCollider capsuleCollider;

    // 카메라 민감도
    [SerializeField]
    private float lookSensitivity;

    // 카메라 한계
    [SerializeField]
    private float cameraRotationLimit;
    private float currentCameraRotationX = 0;     // 정면을 바라보는 중 -> 0

    // 필요한 컴포넌트
    [SerializeField]
    private Camera theCamera;
    private Rigidbody myRigid;  // 플레이어의 실제적(물리적) 몸
    private GunController theGunController;
    private Crosshair theCrosshair;
    private StatusController theStatusController;

    // Start is called before the first frame update
    void Start()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
        // Rigidbody 컴포넌트를 myRigid라는 변수에 넣겠다는 의미
        myRigid = GetComponent<Rigidbody>();
        theGunController = FindObjectOfType<GunController>();
        theCrosshair = FindObjectOfType<Crosshair>();
        theStatusController = FindObjectOfType<StatusController>();

        // 초기화
        applySpeed = walkSpeed;
        // 상대적인 것을 기준으로 해야하기 때문에 local 변수를 씀
        originPosY = theCamera.transform.localPosition.y;
        applyCrouchPosY = originPosY;
    }

    // Update is called once per frame
    void Update()
    {
        IsGround();
        TryJump();
        TryRun();
        TryCrouch();
        Move();
        MoveCheck();
        CameraRotation();
        CharacterRotation();
    }

    // 앉기 시도
    private void TryCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            Crouch();
        }
    }

    // 앉기 동작
    private void Crouch()
    {
        // 앉기 혹은 서기에 관한 내용을 넣음
        // 스위치 역할
        isCrouch = !isCrouch;
        theCrosshair.CrouchingAnimation(isCrouch);

        /*if (isCrouch)
            isCrouch = false;
        else
            isCrouch = true;*/

        if (isCrouch)
        {
            applySpeed = crouchSpeed;
            applyCrouchPosY = crouchPosY;
        }
        else
        {
            applySpeed = walkSpeed;
            applyCrouchPosY = originPosY;
        }

        // theCamera.transform.localPosition = new Vector3(theCamera.transform.localPosition.x, applyCrouchPosY, theCamera.transform.localPosition.z);

        StartCoroutine(CrouchCoroutine());
    }

    // 부드러운 동작 실행
    IEnumerator CrouchCoroutine()
    {
        float _posY = theCamera.transform.localPosition.y;
        int count = 0;

        while (_posY != applyCrouchPosY) // 원하는 값이 되면 벗어난다
        {
            // 보관 함수
            _posY = Mathf.Lerp(_posY, applyCrouchPosY, 0.3f);
            theCamera.transform.localPosition = new Vector3(0, _posY, 0);
            if (count < 15)
                break;
            yield return null;
        }
        theCamera.transform.localPosition = new Vector3(0, applyCrouchPosY, 0f);
        // 1초 동안 대기한다는 의미
        // 병렬 처리 지원
        /*yield return new WaitForSeconds(1f);*/
    }

    // 지면 체크
    private void IsGround()
    {
        // 아래로 광선을 쏘는 것
        isGround = Physics.Raycast(transform.position, Vector3.down /*고정된 좌표*/
            , capsuleCollider.bounds.extents.y /*이만큼의 거리만큼 광선을 쏴주는 것*/
            + 0.1f /*대각선에 있어도 그 오차를 상쇄시킬 수 있도록*/);
        theCrosshair.JumpAnimation(!isGround);
    }

    // 점프 시도
    private void TryJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGround /*isGround == true*/ && theStatusController.GetCurrentSP() > 0)
        {
            Jump();
        }
    }

    // 점프 동작
    private void Jump()
    {
        // 앉은 상태에서 점프시 앉은 상태 해제
        if (isCrouch)
        {
            Crouch();
        }
        theStatusController.DecreaseStamina(100);
        myRigid.velocity = transform.up /*(0, 1, 0)*/ * jumpForce;
    }

    // 달리기 시도
    private void TryRun()
    {
        if (Input.GetKey(KeyCode.LeftShift) && theStatusController.GetCurrentSP() > 0)
        {
            Running();
        }
        if (Input.GetKeyUp(KeyCode.LeftShift) || theStatusController.GetCurrentSP() <= 0)
        {
            RunningCancel();
        }
    }

    // 달리기 동작
    private void Running()
    {
        if (isCrouch)
        {
            Crouch();
        }

        theGunController.CancelFineSight();

        isRun = true;
        theCrosshair.RunningAnimation(isRun);
        theStatusController.DecreaseStamina(10);
        applySpeed = runSpeed;
    }

    // 달리기 동작 취소
    private void RunningCancel()
    {
        isRun = false;
        theCrosshair.RunningAnimation(isRun);
        applySpeed = walkSpeed;
    }

    // 움직임 실행
    private void Move()
    {
        // x가 좌우 // Horizantal : 키보드 화살표 좌우나 A,D
        float _moveDirX = Input.GetAxisRaw("Horizontal");
        // z가 상하 // y는 점프
        float _moveDirZ = Input.GetAxisRaw("Vertical");

        // Vector3 = (1, 0, 0) ->  _moveDirX 를 곱하여 좌우 방향
        Vector3 _moveHorizontal = transform.right * _moveDirX;
        // _moveDirZ 를 곱하여 상하 방향
        Vector3 _moveVertical = transform.forward * _moveDirZ;
        // (1, 0, 0) + (0, 0, 1) = (1, 0, 1) = 2
        // normalized를 해주면 이렇게 됨 (0.5, 0, 0.5) = 1
        // 정규화를 해주면 1초에 얼마나 이동시킬건지에 대한 계산이 편해짐
        Vector3 _velocity = (_moveHorizontal + _moveVertical).normalized * applySpeed;

        // Time.deltaTime : 1초동안 이만큼 움직이게 하게끔 하는 것
        // 이것이 없으면 플레이어는 순간이동하게 될 것
        myRigid.MovePosition(transform.position + _velocity * Time.deltaTime);
    }

    // 움직임 체크
    private void MoveCheck()
    {
        if(!isRun && !isCrouch && isGround)
        {
            if (Vector3.Distance(lastPos, transform.position) >= 0.01f)
            {
                isWalk = true;
            }
            else
            {
                isWalk = false;
            }

            theCrosshair.WalkingAnimation(isWalk);
            lastPos = transform.position;
        }
    }

    // 좌우 캐릭터 회전
    private void CharacterRotation()
    {
        float _yRotation = Input.GetAxisRaw("Mouse X");
        Vector3 _characterRotationY = new Vector3(0f, _yRotation, 0f) * lookSensitivity;
        myRigid.MoveRotation(myRigid.rotation * Quaternion.Euler(_characterRotationY));
        // Debug.Log(myRigid.rotation);
        // Debug.Log(myRigid.rotation.eulerAngles);
    }

    // 상하 카메라 회전
    private void CameraRotation()
    {
        float _xRotation = Input.GetAxisRaw("Mouse Y");
        // 어느정도 천천히 움직이도록
        float _cameraRotationX = _xRotation * lookSensitivity;
        currentCameraRotationX -= _cameraRotationX;
        // currentCameraRotationX 가 최대 최소 값 사이에서 움직이도록
        currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -cameraRotationLimit, cameraRotationLimit);

        theCamera.transform.localEulerAngles = new Vector3(currentCameraRotationX, 0f, 0f);
    }
}
