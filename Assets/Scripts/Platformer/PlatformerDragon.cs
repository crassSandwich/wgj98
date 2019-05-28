﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlatformerDragon : MonoBehaviour
{
    public float MoveSpeed, GroundedThreshold, TurnSpeed, DropSpeed, CriticalVerticality, JumpDelay;
    public AnimationCurve JumpCurve;

    float jumpTime => JumpCurve.keys[JumpCurve.keys.Length - 1].time;
    bool jumping => jumpTimer <= jumpTime;

    Rigidbody2D rb;
    float jumpTimer, jumpDelayTimer;
    bool grounded;

    void Start ()
    {
        rb = GetComponent<Rigidbody2D>();
        jumpTimer = jumpTime;
    }

    void Update ()
    {
        if (jumpDelayTimer <= 0 && Input.GetButton ("Jump"))
        {
            jumpDelayTimer = JumpDelay;
            jumpTimer = 0;
        }
        jumpDelayTimer -= Time.deltaTime;
        jumpTimer += Time.deltaTime;
    }

    void FixedUpdate ()
    {
        if (!grounded && !jumping)
        {
            var rotateDelta = TurnSpeed * -Input.GetAxisRaw("Horizontal") * Time.deltaTime;
            rb.MoveRotation(rb.rotation + rotateDelta);
        }

        if (grounded)
        {
            rb.velocity = Vector2.zero;
            rb.MovePosition(rb.position + Vector2.right * MoveSpeed * Input.GetAxisRaw("Horizontal") * Time.deltaTime);
        }

        // rb.rotation doesn't constrain itself to [0, 360), while transform.rotation.eulerAngles.z lags behind by a frame
        var effectiveRotation = Mathf.Repeat(rb.rotation, 360);

        Vector2 rotDir;
        switch ((int) (effectiveRotation / 90))
        {
            case 4:
            case 0:
                rotDir = new Vector2(1, 1);
                break;
            case 1:
                rotDir = new Vector2(-1, 1);
                break;
            case 2:
                rotDir = new Vector2(-1, -1);
                break;
            case 3:
                rotDir = new Vector2(1, -1);
                break;
            default:
                throw new System.Exception($"unexpected rotation value {effectiveRotation}");
        }

        var vert = verticality(effectiveRotation);

        if (vert >= CriticalVerticality)
        {
            rotDir.y = Mathf.Sign(rb.velocity.y);
        }

        var newVel = new Vector2
        (
            rotDir.x * (1 - vert),
            Input.GetButton("Drop") ? DropSpeed : rotDir.y * vert
        ).normalized * rb.velocity.magnitude;

        rb.velocity = newVel;

        if (jumping)
        {
            rb.MovePosition(rb.position + Vector2.up * JumpCurve.Evaluate(jumpTimer));
        }
    }

    void OnCollisionEnter2D (Collision2D col)
    {
        foreach (var contact in col.contacts)
        {
            if (contact.normal.y >= GroundedThreshold)
            {
                grounded = true;
            }
        }
    }

    void OnCollisionExit2D (Collision2D col)
    {
        grounded = false;
    }

    // returns 0 if perfectly horizontal, 1 if perfectly vertical, or something in between
    float verticality (float angleDegrees)
    {
        // triangle wave with period of 90, min of 0, and max of 1

        float pos = Mathf.Repeat(angleDegrees, 180) / 180;

        if (pos < .5f)
        {
            return Mathf.Lerp(0, 1, pos * 2);
        }
        else
        {
            return Mathf.Lerp(1, 0, (pos - .5f) * 2);
        }
    }
}
