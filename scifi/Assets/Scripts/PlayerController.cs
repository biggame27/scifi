using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 1f;
    public float collisionOffset = 0.02f;
    public float health;
    private float maxHealth;
    Vector2 coords;

    public HealthBar healthBar;
    private PlayerInput playerInput;
    [SerializeField]
    private FollowTarget followTarget;
    public GameObject[] buildings;
    public Sprite[] sprites; 
    [SerializeField]
    private Gold gold;
    [SerializeField]
    private GameObject turretInfo;
    [SerializeField]
    private PauseManager pauseManager;
    [SerializeField]
    private float inputDeadZone;

    Ray ray;
    RaycastHit hit;

    public float Health
    {
        set 
        {
            health = value;
            healthBar.SetHealth(value/maxHealth*100f);
            if(health <= 0)
            {
                Defeated();
            }
        }
        get
        {
            return health;
        }
    }

    public ContactFilter2D movementFilter;
    public Collider2D placingCheck;
    List<RaycastHit2D> castCollisions = new List<RaycastHit2D>();
    SpriteRenderer spriteRenderer;
    Animator animator;
    Transform turretStorage;

    public SwordAttack swordAttack;

    Vector2 movementInput;
    Rigidbody2D rb;

    bool canMove = true;
    bool building = false;
    int objInd = -1;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerInput = GetComponent<PlayerInput>();
        turretStorage = GameObject.Find("Turrets").transform;
        maxHealth = health;
    }

    void OnBuildLaser()
    {
        if(!building)
        {
            objInd = -1;
            followTarget.ChangeSprite(sprites[0]);
            placingCheck.enabled = true;
            building = true;
            LockMovement();
        }else
        {
            turretInfo.SetActive(false);
            placingCheck.enabled = false;
            UnlockMovement();
            building = false;
        }
    }
    void OnSwitchMap()
    {
        pauseManager.OnClick();
        if(pauseManager.paused)
        {
            playerInput.actions.FindActionMap("Player").Disable();
            playerInput.actions.FindActionMap("UI").Enable();
        }
        else
        {
            playerInput.actions.FindActionMap("Player").Enable();
            playerInput.actions.FindActionMap("UI").Disable();
        }
        
    }

    public void OnSwitchBuilding(string obj)
    {
        if(obj == "Turret")
        {
            objInd = 0;
            followTarget.ChangeSprite(sprites[1]);
        }
    }
    
    void FixedUpdate()
    {
        if(canMove && !float.IsNaN(movementInput.magnitude) && movementInput != Vector2.zero)
        {
            bool success = TryMove(movementInput);
            if(!success)
            {
                success = TryMove(new Vector2(movementInput.x, 0));
                if(!success)
                    success = TryMove(new Vector2(0, movementInput.y));
            }
            animator.SetBool("IsMoving", true);
        } else {
            animator.SetBool("IsMoving", false);
        }
        if(movementInput.x > 0)
            spriteRenderer.flipX = false;
        else if(movementInput.x < 0)
            spriteRenderer.flipX = true;
    }

    bool TryMove(Vector2 direction)
    {
        int count = rb.Cast(direction, movementFilter, castCollisions, moveSpeed * Time.fixedDeltaTime + collisionOffset);
        if(count <= 1)
        {
            rb.MovePosition(rb.position + direction*moveSpeed*Time.fixedDeltaTime);
            return true;
        }
        return false;
    }
    
    void OnMove(InputValue movementValue)
    {
        movementInput = movementValue.Get<Vector2>();
        if(movementInput.magnitude < inputDeadZone)
            movementInput = Vector2.zero;

    }

    void OnFire()
    {
        if(!building)
        {
            UnlockMovement();
            animator.SetTrigger("Attack" +  (int)Random.Range(1f, 5f));
        }
        else
        {
            if(!IsTouchingMouse() && gold.GetGold() >= 5)
            {
                if(objInd != -1)
                {
                    Instantiate(buildings[objInd], followTarget.newCoords, Quaternion.identity, turretStorage);
                    gold.SubtractGold(5);
                }
            }
        }
    }
    /*
    void OnLook(InputValue lookValue)
    {
        if(!building)
            return;
        coords = Camera.main.ScreenToWorldPoint(Touchscreen.current.primaryTouch.position.ReadValue());
    }
    */

    public bool IsTouchingMouse()
    {
        Vector2 point = followTarget.pos;
        //checks player
        bool check = placingCheck.OverlapPoint(point);
        if(check) return true;
        foreach(Transform child in turretStorage)
        {
            check = child.gameObject.GetComponent<Collider2D>().OverlapPoint(point);
            if(check)
            {
                SendTurretInfo(child);
                return true;
            }
        }
        turretInfo.SetActive(false);
        return false;
        
    }

    void SendTurretInfo(Transform currTurret)
    {
        turretInfo.SetActive(true);
        turretInfo.GetComponent<TurretInfo>().SetController(currTurret.gameObject.GetComponent<TurretLevelController>());
    }

    public Gold GetGold()
    {
        return gold;
    }

    void SwordAttack()
    {
        LockMovement();
        if(spriteRenderer.flipX)
            swordAttack.AttackLeft();
        else
            swordAttack.AttackRight();
    }

    void EndSwordAttack()
    {
        swordAttack.StopAttack();
        UnlockMovement();
    }

    void LockMovement()
    {
        canMove = false;
    }

    void UnlockMovement()
    {
        canMove = true;
    }

    public void Defeated()
    {
        animator.SetTrigger("Defeated");
        LockMovement();
    }

    public void Death()
    {
        pauseManager.Death();
    }

    public Vector2 GetCoords()
    {
        return coords;
    }

    public bool GetCanBuild()
    {
        return building;
    }
}
