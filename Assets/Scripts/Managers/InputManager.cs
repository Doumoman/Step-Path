using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 업데이트문에 항상 키를 감지하게 하려면 너무 부하가 커지기 때문에,
/// 인풋을 체크해서 이벤트로 쫙 퍼트려주는 식으로(리스너 패턴) 매니저를 구현한다.
/// </summary>
public class InputManager
{
    // 사용할 키를 리스트로 만든다.
    // 
    // void 반환형의 Delegate다.
    public Action KeyAction = null;

    public Action[] KeyActionsDown = new Action[600];

    public Action[] KeyActionsUp = new Action[600];
    public bool[] KeyActionWhile = new bool[600];

    /// <summary>
    /// Monobehavior 받아서 사용하는 업데이트문과 다르다,
    /// 리스너 패턴으로 구현.
    /// </summary>
    public void OnUpdate()
    {
        // 키 입력이 아무것도 없었다면
        if (Input.anyKey == false)
        {
            return;
        }
        if (Input.anyKeyDown == false)
        {
            return;
        }
        if (Input.anyKey)
        {
        }
        // 키 액션이 있었다면 
        if (KeyAction != null)
            KeyAction.Invoke();
    }
    /*
     * 발견한 문제
     * 키를 뗄때 작동하는것도 필요한데
     * 키를 떼면 Input.anyKey == false 이기때문에 return되어 KeyAction이 작동하지 않는다.
     * */
}
