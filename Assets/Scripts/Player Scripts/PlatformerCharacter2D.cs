using System;
using UnityEngine;
using System.Collections;

public class PlatformerCharacter2D : MonoBehaviour
{
	[SerializeField] private float m_MaxSpeed = 3.9f;                    // The fastest the player can travel in the x axis.
	private float originalMaxSpeed;

    public float m_JumpForce = 95f;                  // Amount of force added when the player jumps.
	//private float originalJumpForce;

	[SerializeField] private LayerMask m_WhatIsGround;                  // A mask determining what is ground to the character
	[SerializeField] private float m_KnockbackHeight = 300f;

//    private Transform m_GroundCheck;    // A position marking where to check if the player is grounded.
    const float k_GroundedRadius = .15f; // Radius of the overlap circle to determine if grounded
    public bool m_Grounded;            // Whether or not the player is grounded.

    private Transform m_CeilingCheck;   // A position marking where to check for ceilings
	private Transform m_EdgeCheck;   // A position marking where to check for ceilings

    const float k_CeilingRadius = .01f; // Radius of the overlap circle to determine if the player can stand up
    public Animator animator;            // Reference to the player's animator component.
    private Rigidbody2D m_Rigidbody2D;
    public bool m_FacingRight = true;  // For determining which way the player is currently facing.
	private bool m_Damaged = false;
	private bool jumpLock = false;

	private GroundChecker groundchecker;
	private SpecialTerrainChecker terrainChecker;
	private float lastMove = 0;

	GameObject enemyHolder;

    private void Awake()
    {
        // Setting up references.
//        m_GroundCheck = transform.Find("GroundCheck");
        m_CeilingCheck = transform.Find("CeilingCheck");
        animator = GetComponent<Animator>();
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
		originalMaxSpeed = m_MaxSpeed;
		groundchecker = GetComponentInChildren<GroundChecker>();
		terrainChecker = GetComponentInChildren<SpecialTerrainChecker>();
		enemyHolder = transform.Find ("EnemyHolder").gameObject;
    }
		
    private void FixedUpdate()
    {
		m_Grounded = groundchecker.grounded;
		animator.SetBool("InGround", m_Grounded);
		animator.SetBool ("OnEdge", groundchecker.teetering);
    }


    public void Move(float move, bool crouch, bool jump){

        if (animator.GetBool("InGround") && move != 0)
            animator.SetBool("Run", true);

		//check if there is ceiling on top, if not, the character may stand up
		SetCrouch (crouch);
            
		if(move !=0){
			animator.SetBool("Crouch",false);
			if (animator.GetBool("Attack")){
				animator.SetBool ("Run", false);
			}
		}

		MovementBehavior (move);
        // If the player should jump...
		JumpBehavior(jump);
		animator.SetFloat ("Yspeed", m_Rigidbody2D.velocity.y);
    }



	void SetCrouch(bool crouch){
		// If crouching, check to see if the character can stand up
		if (!crouch && animator.GetBool("Crouch"))
		{
			// If the character has a ceiling preventing them from standing up, keep them crouching
			if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
			{
				if(!animator.GetCurrentAnimatorStateInfo(0).IsTag("Airborne"))
					crouch = true;
			}
		}

		// Set whether or not the character is crouching in the animator
		animator.SetBool("Crouch", crouch);
	}

	void MovementBehavior(float move){

		if (animator.GetCurrentAnimatorStateInfo (0).IsTag ("Damage")) {
			move = 0;
			StopAllCoroutines();
			accelerating = false;
		}

		//if he is attacking, he shouldnt move
		float direction = m_FacingRight ? 1 : -1;
		if (animator.GetCurrentAnimatorStateInfo(0).IsName("SecondAttack")){
			move = 0.3f * direction;
		}else if(animator.GetCurrentAnimatorStateInfo(0).IsName("ThirdAttack")){
			move = 2f * direction;
		}

		SetPlayerVelocityX (move);
		FlipToFaceVelocity(move);

		//If damaged, these force the movement to be a knockback motion instead of normal Movement
		KnockBackWhileDamaged ();
		KnockUpForce ();
	}

	private void FlipToFaceVelocity(float move){
		if (move > 0 && !m_FacingRight)
		{
			// ... flip the player.
			Flip();
		}
		// Otherwise if the input is moving the player left and the player is facing right...
		else if (move < 0 && m_FacingRight)
		{
			// ... flip the player.
			Flip();
		}
	}

	private void SetPlayerVelocityX(float move){

		if(move > 1 || move < -1){
			StopAllCoroutines();
			accelerating = false;
		}else{
			if(move == 0 && lastMove != 0 && lastMove <= 1 && lastMove >= -1){
				StopCoroutine("Accelerate");
				accelerating = false;
				StartCoroutine("Decelerate", lastMove);
			}else if(move != 0 && lastMove == 0){
				StopCoroutine("Decelerate");
				StartCoroutine("Accelerate", move);
			}
		}
		

		if(accelerating){
			move = acceleratingSpeed;
		}

		// Move the character
		m_Rigidbody2D.velocity = new Vector2(move * m_MaxSpeed, m_Rigidbody2D.velocity.y);
		m_MaxSpeed = originalMaxSpeed;
		lastMove = move;
	}

	private void KnockBackWhileDamaged(){
		//move back while damaged
		if (animator.GetCurrentAnimatorStateInfo (0).IsName ("Damage")) { 
			if (m_FacingRight) {
				m_Rigidbody2D.velocity = new Vector2 (-originalMaxSpeed, m_Rigidbody2D.velocity.y);
			} else {
				m_Rigidbody2D.velocity = new Vector2(originalMaxSpeed, m_Rigidbody2D.velocity.y);
			}
		}
	}

	private void KnockUpForce(){
		if(m_Damaged){
			m_Damaged = false;
			m_Rigidbody2D.AddForce (new Vector2 (0f, m_KnockbackHeight));
		}
	}

	private void JumpBehavior(bool jump){
		
		if (m_Grounded && jump && animator.GetBool ("InGround")
			&& !animator.GetCurrentAnimatorStateInfo(0).IsTag("Damage")) {

			if (!jumpLock) {
				if (terrainChecker.specialTerrain != null) {
					terrainChecker.specialTerrain.JumpEvent (this.gameObject);
					terrainChecker.specialTerrain = null;
				} else {
					DoJump (m_JumpForce);
				}
			}

			//if the player didnt jump, but is in the air, he should be falling
		} else if (!m_Grounded && !jump && !animator.GetBool ("InGround")
			&& !animator.GetCurrentAnimatorStateInfo(0).IsTag("Damage")) {

			DoFall ();
		}
	}

	public void DoJump(float jumpForce){
		// Add a vertical force to the player.
		jumpLock = true;
		Invoke ("UnlockJumping", 0.1f);

		m_Grounded = false;
		animator.SetBool ("InGround", false);
		animator.SetBool ("Jump", true);
		animator.SetBool ("Crouch", false);
		m_Rigidbody2D.AddForce (new Vector2 (0f, jumpForce));
	}

	public void DoFall(){
		m_Grounded = false;
		animator.SetBool ("InGround", false);
		animator.SetBool ("Fall", true);
		animator.SetBool ("Crouch", false);
	}

    private void Flip()
    {
        // Multiply the player's x local scale by -1.
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
		m_FacingRight = !m_FacingRight;

		if (enemyHolder.transform.childCount > 0) {
			var pickableComponent = enemyHolder.transform.GetChild (0).GetComponent<Movable> ();
			pickableComponent.faceLeft = !pickableComponent.faceLeft;
		}
    }

	public void WasDamaged(){
		m_Damaged = true;
	}

	private void UnlockJumping(){
		jumpLock = false;
	}

	public float GetMaxSpeed(){
		return m_MaxSpeed;
	}

	public float GetOriginalMaxSpeed(){
		return originalMaxSpeed;
	}

	public void SetMaxSpeed(float newSpeed){
		m_MaxSpeed = newSpeed;
	}

	public void RestoreMaxSpeed(){
		m_MaxSpeed = originalMaxSpeed;
	}

	float airDeceleration = 0.05f;
	float groundDeceleration = 0.15f;
	IEnumerator Decelerate (float speed)
    {
		float sign = (speed >= 0) ? sign = 1 : sign = -1;
		while(speed != 0)
		{
			var decelerationSpeed = m_Grounded ? groundDeceleration : airDeceleration;
			speed -= decelerationSpeed * sign;

			if ((speed > 0 && sign < 0) || (speed < 0 && sign > 0) || speed == 0)
				break;

			m_Rigidbody2D.velocity = new Vector2(speed * m_MaxSpeed, m_Rigidbody2D.velocity.y);
			yield return new WaitForSeconds(0.01f);
		}
    }
	bool accelerating;
	float acceleratingSpeed = 0;
	IEnumerator Accelerate (float speed)
    {
		accelerating = true;
		float sign = (speed >= 0) ? sign = 1 : sign = -1;
		acceleratingSpeed = 0.3f * sign;

		while(true)
		{
			acceleratingSpeed += 0.14f * sign;
			Mathf.Clamp(acceleratingSpeed, -1, 1);

			if (acceleratingSpeed >= 1 || acceleratingSpeed <= -1 || !m_Grounded){
				accelerating = false;
				break;
			}

			yield return new WaitForSeconds(0.01f);
		}
		accelerating = false;
    }
}

