using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialPlayerController : MonoBehaviour {

    #region Variables
    [Header("Player Options")]
    public float playerHeight;

    [Header("Movement Options")]
    public float movementSpeed;
    public float smoothSpeed;
    public bool wallWalk = true;

    [Header("Jump Options")]
    public float jumpForce;
    public float jumpSpeed;

    [Header("Gravity")]
    public float gravity = 2.5f;

    [Header("Grounding")]
    public Vector3 groundCheckPoint = new Vector3(0, -0.87f, 0); //origin of ground check sphere
    public float groundCheckRadius = 0.40f;//0.55f is original but current removes sudden "snap" at the end
    //Gravity private variables
    private bool grounded; //if player is standing on surface
    [SerializeField]
    private float lerpSpeed = 15f; //rotation speed

    //Grounded Private Variables //also check them out
    private Vector3 liftPoint = new Vector3(0, 2.2f, 0);//investigate
    private RaycastHit groundHit;
    //add visualiser for feet

    [Header("Physics")]
    public LayerMask discludePlayer;

    [Header("References")]
    public SphereCollider[] sphereCol;//the "head" and "torso" of the player

    //private variables
    private Vector3 velocity; //temp velocity vector / intended velocity before alteration //rename to direction
    private Vector3 momentum;//added for midair
    private Vector3 move; //used for player input
    private Vector3 vel;

    private Vector3 myNormal; // character normal //added for cross products for surface
    private Vector3 surfaceNormal; // normal of surface below, gained through raycast

    //gizmo variables
    private Vector3 S1Pos;
    private float S1Radius;
    private Vector3 S2Pos;
    private float S2Radius;
    private Vector3 S3Pos;
    private float S3Radius;
    #endregion

    #region Main Methods
    void Start()
    {
        myNormal = transform.up; //added for cross
        Cursor.visible = false; //should be handled elsewhere
        Cursor.lockState = CursorLockMode.Locked;//should be handled elsewhere
    }

    void Update()
    {
        Gravity();
        SimpleMove();
        Jump();
        FinalMove();
        GroundChecking();
        CollisionCheck();
    }
    #endregion

    #region Movement Methods
    private void SimpleMove()
    {
        if (grounded)
        {
            velocity = move * movementSpeed;
            momentum = Vector3.zero;
        }
    }

    private void FinalMove()
    {
        myNormal = Vector3.Lerp(myNormal, surfaceNormal, lerpSpeed * Time.deltaTime); 
        Vector3 myForward = Vector3.Cross(transform.right, myNormal); //calculate where the new "forward" will be when changing surfaces
        Quaternion targetRot = Quaternion.LookRotation(myForward, myNormal);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, lerpSpeed * Time.deltaTime);

        vel = velocity + momentum;

        transform.position += vel * Time.deltaTime;
    }
    #endregion

    #region Gravity/Grounding

    private void Gravity()
    {
        if (!grounded)//in air
        {
            momentum.y -= gravity;//pulled down by gravity
            surfaceNormal = Vector3.up;//player should face upwards in midair
        }
    }

    private void GroundChecking()
    {
        Vector3 v = move;
        Ray groundRay = new Ray(transform.TransformPoint(liftPoint), -transform.up);//Ray in direction of "feet"
        Ray moveRay = new Ray(transform.TransformPoint(Vector3.up * 0.3f) - v.normalized * 0.5f, v.normalized);//Ray in direction of movement
        RaycastHit tempHit = new RaycastHit();//to store results of ray

        Debug.DrawRay(transform.TransformPoint(Vector3.up * 0.3f) - v.normalized * 0.5f, v.normalized * 1.1f,Color.red);

        if (Physics.SphereCast(moveRay, 0.17f, out tempHit, 1.1f, discludePlayer) && wallWalk)
        {//section of code to handle "walking" onto a wall
            surfaceNormal = tempHit.normal;//Surface Normal
            
            transform.position = Vector3.Lerp(transform.position, tempHit.point, lerpSpeed * Time.deltaTime);
            grounded = true;
        }
        else if (Physics.SphereCast(groundRay, 0.17f, out tempHit, 2.3f, discludePlayer))
        {//section of code handled if walking on a surface
            GroundConfirm(tempHit);
            surfaceNormal = tempHit.normal;
            if (!wallWalk)
            {
                surfaceNormal = Vector3.up;
            }
        }
        else //you are in midair
        {
            grounded = false;
        }
    }

    private void GroundConfirm(RaycastHit tempHit)
    {
        Collider[] col = new Collider[3];
        int num = Physics.OverlapSphereNonAlloc(transform.TransformPoint(groundCheckPoint), groundCheckRadius, col, discludePlayer);
        
        //<gizmo
        S2Pos = transform.TransformPoint(groundCheckPoint);
        S2Radius = groundCheckRadius;
        //gizmo>

        grounded = false;

        for (int i = 0; i < num; i++)//if the ray is impacting the same thing this sphere is impacting, you are going to be grounded to that surface
        {
            if (col[i].transform == tempHit.transform)
            {
                groundHit = tempHit;
                grounded = true;
                if (wallWalk)
                {
                    transform.position = groundHit.point;
                }
                else
                {
                    transform.position = new Vector3(transform.position.x, groundHit.point.y, transform.position.z);
                }
                break;//stop wasting extra loops
            }
        }
        /*
        //in case below several platforms
        if (num <= 1 && tempHit.distance <= 3.1f && !inputJump)//if there is at least one collision
        {
            if (col[0] != null)//if it isn't nothing, then send a ray down
            {
                Ray ray = new Ray(transform.TransformPoint(liftPoint), -transform.up);//Vector3.down
                RaycastHit hit;

                Debug.DrawRay(transform.TransformPoint(liftPoint), -transform.up, Color.cyan);

                if (Physics.Raycast(ray, out hit, 3.1f, discludePlayer))
                {
                    if (hit.transform != col[0].transform)
                    {
                        grounded = false;
                        return;
                    }
                }
            }
        }
        */
    }
    #endregion

    #region Collision
    private void CollisionCheck()
    {
        foreach (SphereCollider sc in sphereCol)//custom added
        {
            Collider[] overlaps = new Collider[4];//sphereCol changed to sc below x2
            int num = Physics.OverlapSphereNonAlloc(transform.TransformPoint(sc.center), sc.radius, overlaps, discludePlayer, QueryTriggerInteraction.UseGlobal);

            for (int i = 0; i < num; i++)
            {
                Transform t = overlaps[i].transform;
                Vector3 dir;
                float dist;
                if (Physics.ComputePenetration(sc, transform.position, transform.rotation, overlaps[i], t.position, t.rotation, out dir, out dist))
                {
                    Vector3 penetrationVector = dir * dist;
                    Vector3 velocityProjected = Vector3.Project(velocity, dir); //helps with sliding
                    transform.position += penetrationVector;
                    vel -= velocityProjected;
                }
            }
        }
    }
    #endregion

    #region Jumping
    private bool inputJump = false;

    private void Jump()
    {
        bool canJump = false;

        //don't jump if no space above
        canJump = !Physics.Raycast(new Ray(transform.position, transform.up), playerHeight, discludePlayer);//Vector3 changed to transform

        if (grounded && canJump)
        {
            if (Input.GetKeyDown(KeyCode.Space))//to be altered
            {
                inputJump = true;
                transform.position += transform.up * 0.05f * 2; //to "unclip" from the ground
                momentum += jumpForce * surfaceNormal;
                //Vector3 v = new Vector3(0, playerBody.localEulerAngles.y);
            }
        }
    }
    #endregion

    public void ChangeWallWalk()
    {
        wallWalk = !wallWalk;
    }

    public void Move(Vector3 input)
    {
        if (grounded)
        {
            move = input;
        }
    }

    public void RotateYaw(float rotateAmount)
    {
        transform.Rotate(Vector3.up, rotateAmount); //to be edited       
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(S2Pos, S2Radius);
    }
}
