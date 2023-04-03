using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerController : MonoBehaviour
{
    private float speed = 4;

    void Update()
    {
        Move();
    }

    void Move()
    {
        float horizontalMove = Input.GetAxis("Horizontal");
        float verticalMove = Input.GetAxis("Vertical");
        var transform1 = transform;
        Vector3 move = transform1.forward * verticalMove + transform1.right * horizontalMove;
        transform.Translate(move * (speed * Time.deltaTime));
    }
    
}
