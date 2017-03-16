﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SpecialTerrain : MonoBehaviour {

	abstract public void StandEvent (GameObject gObject);
	abstract public bool JumpEvent (GameObject gObject);
}
