using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{

    CharacterController mController;
    Vector3 velocity;
    public float Gravity;
    public float moveSpeed;
    public float jumpHeight = 10;
    public float lookSensitivity;
    Vector3 rot;

    float defaultHeight;
    public float crouchHeight;
    public float crouchSpeed;
    bool crouch;
    public enum myEnum
    {
        hold,
        toggle
    };

    public myEnum crouchType;
    // Use this for initialization
    void Start()
    {
        mController = GetComponent<CharacterController>();

        defaultHeight = mController.height;
    }

    // Update is called once per frame
    void Update()
    {


        Move();
        GravityControl();
        Jump();
        Crouch();


    }

    private void Move()
    {
        if (Input.GetKey(KeyCode.W))
            mController.Move(transform.forward * moveSpeed * Time.deltaTime);
        if (Input.GetKey(KeyCode.S))
            mController.Move(-transform.forward * moveSpeed * Time.deltaTime);
        if (Input.GetKey(KeyCode.A))
            mController.Move(-transform.right * moveSpeed * Time.deltaTime);
        if (Input.GetKey(KeyCode.D))
            mController.Move(transform.right * moveSpeed * Time.deltaTime);

    }

    void GravityControl()
    {



        if (!mController.isGrounded)
        {
            velocity.y += Gravity * Time.deltaTime;
        }
        mController.Move(velocity * Time.deltaTime);
    }

    void Jump()
    {
        if (Input.GetButtonDown("Jump") && mController.isGrounded)
        {
            velocity.y += Mathf.Sqrt(jumpHeight * -2f * Gravity);
        }

    }

    void Crouch()
    {
        if (crouchType.ToString() == "hold")
        {
            if (Input.GetKey(KeyCode.C))
            {
                mController.height = Mathf.Lerp(mController.height, crouchHeight, crouchSpeed * Time.deltaTime);
            }
            else
            {
                mController.height = Mathf.Lerp(mController.height, defaultHeight, crouchSpeed * Time.deltaTime);
            }
        }
        if (crouchType.ToString() == "toggle")
        {

            if (Input.GetKeyDown(KeyCode.C))
            {
                crouch = !crouch;
            }

            if (crouch)
            {
                mController.height = Mathf.Lerp(mController.height, crouchHeight, crouchSpeed * Time.deltaTime);
            }
            else
            {
                mController.height = Mathf.Lerp(mController.height, defaultHeight, crouchSpeed * Time.deltaTime);
            }
        }

    }


}
