using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UI;
using Core;

public class GameManager : Singleton<GameManager>
{

    InputManager _input = new InputManager();
    ResourceManager _resource = new ResourceManager();
    SoundManager _sound = new SoundManager();


    public static InputManager Input { get { return Instance._input; } }
    public static ResourceManager Resource { get { return Instance._resource; } }
    public static SoundManager Sound { get { return Instance._sound; } }

    private void Start()
    {
        Application.targetFrameRate = 60;
    }
    private void Update()
    {
        _input.OnUpdate();
    }
}