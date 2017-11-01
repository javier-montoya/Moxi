﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickable : MonoBehaviour {

	protected bool pickable = false;
	private Transform grabPoint;
	protected DamageManager damageManager;
	private int throwDamage = 5;
	Rigidbody2D rbody;

	[HideInInspector]public bool beingThrown = false;
	[HideInInspector]public bool isHardThrown = false;


	void Awake(){
		grabPoint = transform.Find ("GrabPoint");
		rbody = GetComponent<Rigidbody2D> ();
		damageManager = GetComponent<DamageManager> ();
	}

	public bool IsPickable(){
		return pickable;
	}

	public void SetPickable(bool pickable){
		this.pickable = pickable;
	}

	public virtual GameObject BecomeHeld(){
		rbody.isKinematic = false;
		return this.transform.root.gameObject;
	}

	public void BecomeThrown(float throwStrength, bool right, bool hardThrow){
		rbody.isKinematic = false;
		rbody.velocity = Vector2.zero;
		beingThrown = true;
		isHardThrown = hardThrow;

		if (right)
			rbody.AddForce (Vector2.right * throwStrength);
		else
			rbody.AddForce (Vector2.left * throwStrength);
	}

	public void BecomeDropped(){
	}

	public void CollisionBehavior(){
		beingThrown = false;
		isHardThrown = false;
	}

	public Vector3 GetGrabPoint(){

		if (grabPoint == null) {
			return Vector3.zero;
		}
		return grabPoint.localPosition;
	}

	void OnTriggerStay2D(Collider2D col){
		if (beingThrown) {
			if (col.tag == "Enemy" ||  col.tag == "Interactable") {
				var targetDamageManager = col.gameObject.GetComponentInParent<DamageManager> ();
				targetDamageManager.ReceiveDamage (throwDamage);
				damageManager.DestroySelf ();
			}
		}
	}
}

