using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiIndividual : MonoBehaviour
{
    [HideInInspector]
    public float GroundHeight;
    public int Health;
    [HideInInspector]
    public float MyRunSpeed;
    [HideInInspector]
    public int State;
    [HideInInspector]
    public int lastState;
    [HideInInspector]
    public uint Team;
    [HideInInspector]
    public AiIndividual Enemy;
    [HideInInspector]
    public float EnemyDistance;
    [HideInInspector]
    public Vector3 position;
    [HideInInspector]
    public SimpleArmy myarmy;
    [HideInInspector]
    public Vector3 LookDir;
    [HideInInspector]
    public Vector3 MoveDir;
    [HideInInspector]
    public int aiindex;
    [HideInInspector]
    public bool Dead;
    [HideInInspector]
    public float AttackTimer;
    [HideInInspector]
    public float DeathTimer;


}
