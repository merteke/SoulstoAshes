using UnityEngine;

public class PlayerAnimatorr : MonoBehaviour
{
    public Animator am;
    public SpriteRenderer sr;
    PlayerMovement pm;
    public PlayerAttack playerAttack; // PlayerAttack scriptine referans

    void Start()
    {
        pm = GetComponent<PlayerMovement>();
        am = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        playerAttack = GetComponent<PlayerAttack>(); // PlayerAttack referansını al

    }

    void Update()
    {
        if (pm == null || am == null || sr == null) return;

        // Hareket kontrolü
        bool isMoving = pm.moveDir.x != 0 || pm.moveDir.y != 0;
        am.SetBool("Move", isMoving);

        // Yön kontrolü
        SpriteDirectionChecker();

        // Saldırı inputları PlayerAttack scriptinde yönetilecek, buradan kaldırıyoruz.
        // if (Input.GetMouseButtonDown(1))
        // {
        //     am.SetTrigger("Attack2");
        // }
        // if (Input.GetMouseButtonDown(0))
        // {
        //     am.SetTrigger("Attack1");
        // }

        /*if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            am.SetTrigger("Dash");
            if (pm != null)
            {
                pm.InitiateDash();
            }
        }*/
    }

    void SpriteDirectionChecker()
    {
        if (pm.lastHorizontalVector > 0) // Sağa gidiyor
        {
            sr.flipX = false;
        }
        else if (pm.lastHorizontalVector < 0) // Sola gidiyor
        {
            sr.flipX = true;
        }
    }

    public void SetAnimatorController(RuntimeAnimatorController c)
    {
        if(!am) am = GetComponent<Animator>();
        am.runtimeAnimatorController = c;
    }
}