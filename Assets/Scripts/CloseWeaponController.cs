using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 미완성 클래스 = 추상 클래스
public abstract class CloseWeaponController : MonoBehaviour {


    // 현재 장착된 Hand형 타입 무기
    [SerializeField]
    protected CloseWeapon currentCloseWeapon;

    // 공격중??
    protected bool isAttack = false;
    protected bool isSwing = false;

    protected RaycastHit hitInfo;

    protected void TryAttack()
    {
        // 왼쪽 버튼을 누르면
        if (Input.GetButton("Fire1"))
        {
            if (!isAttack)
            {
                // 코루틴 실행
                StartCoroutine(AttackCoroutine());
            }
        }
    }

    protected IEnumerator AttackCoroutine()
    {
        // 중복 실행을 막음
        isAttack = true;
        // 공격 애니메이션 실행
        currentCloseWeapon.anim.SetTrigger("Attack");

        // 약간의 딜레이 후
        yield return new WaitForSeconds(currentCloseWeapon.attackDelayA);
        isSwing = true;

        // 적중 여부를 판단할 수 있는 코루틴을 실행시킴
        StartCoroutine(HitCoroutine());

        // 일정 시간이 지나면 isSwing이 false가 되면서 코루틴이 꺼지게 됨
        yield return new WaitForSeconds(currentCloseWeapon.attackDelayB);
        isSwing = false;

        // 잠시후 isAttack이 false가 되면서 마우스의 좌클릭이 이루어지면 위의 과정이 이루어지게 함
        yield return new WaitForSeconds(currentCloseWeapon.attackDelay - currentCloseWeapon.attackDelayA - currentCloseWeapon.attackDelayB);
        isAttack = false;
    }

    // 미완성 = 추상 코루틴
    protected abstract IEnumerator HitCoroutine();

    protected bool CheckObject()
    {
        // 전방에 무엇이 있는지 Raycast를 통해 확인 후 있다면 true를 반환시킴
        /* transform. forward == transform.TransformDirection(Vector3.forward -> 캐릭터 기준으로 바꾼 것*/
        if (Physics.Raycast(transform.position, transform.forward, out hitInfo, currentCloseWeapon.range))
        {
            return true;
        }
        return false;

    }

    // 완성 함수이지만, 추가 편집이 가능한 함수
    public virtual void CloseWeaponChange(CloseWeapon _closeWeapon)
    {
        if (WeaponManager.currentWeapon != null)
        {
            WeaponManager.currentWeapon.gameObject.SetActive(false);
        }

        // 바꿔야할 총을 현재 총으로
        currentCloseWeapon = _closeWeapon;
        WeaponManager.currentWeapon = currentCloseWeapon.GetComponent<Transform>();
        WeaponManager.currentWeaponAnim = currentCloseWeapon.anim;

        // 초기화
        currentCloseWeapon.transform.localPosition = Vector3.zero;
        currentCloseWeapon.gameObject.SetActive(true);
    }
}
