using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickableSprite : MonoBehaviour
{
    public event Action<GameObject> OnClick;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void UIMove_Clicked()
    {
        OnClick?.Invoke(gameObject);
    }
}
