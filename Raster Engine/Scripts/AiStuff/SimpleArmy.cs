using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class SimpleArmy : MonoBehaviour
{
    public bool Threaded = true;
    [Tooltip("Will render as a regular Unity renderer using DrawMesh")]
    public bool RunAsUnityRender;
    public bool UnityRenderShadows;
    public uint Team;
    public int MaxHealth = 100;
    public float AttackRange = 2;
    public int Damage = 20;
    public float RunSpeed = 6;
    public int IdleAnimIndex;
    public int RunAnimIndex = 1;
    public int AttackAnimIndex = 2;
    public int DeathAnimIndex = 3;
    public float IdleAnimSpeedMult = 0.75f;
    public float RunAnimSpeedMult = 1;
    public float AttackSpeedTime = 1; 
    bool Attacking;
    [HideInInspector]
    public BGRenderer[] Renderers;
    [HideInInspector]
    public AiIndividual[] ais;
    static AiIndividual[] allais;
    BgCamera cam;

    static float[][] grid;
    static int[][] aigrid;
    static Vector3 gridcorner;
    public bool DebugGrid;
    public int TotalCharacters;
    static int Characters;
    public Vector3 myforward;
    static Thread mythread;
    static List<SimpleArmy> AllArmies;
    static SimpleArmy masterarmy;
    static int indexcounter;
    static BgCamera.BgTrans[] manualtransforms;
    static List<BgCamera.BgTrans> manualtranslist;

    System.Random randomgen;

    struct AiSettings
    {
        public float GroundHeight;
        public int Health;
        public float MyRunSpeed;
    }

    private void OnDestroy()
    {
        grid = null;
        Characters = 0;

        AllArmies = null;
        masterarmy = null;
        ThreadRunning = false;
        indexcounter = 0;

        manualtranslist = null;
        manualtransforms = null;
    }

    void Start()
    {
        randomgen = new System.Random();
        //ASSIGN MASTER ARMY TO CONTROL ALL
        if (masterarmy == null) masterarmy = this;

        //CHECK IF ALL ARMIES IS NULL
        if (AllArmies == null) AllArmies = new List<SimpleArmy>();

        //ADD MY ARMY TO ALL
        AllArmies.Add(this);

        ais = gameObject.GetComponentsInChildren<AiIndividual>();
            
        Renderers = new BGRenderer[ais.Length];


        Characters += ais.Length;

        for (int i = 0; i < ais.Length; i++)
        {
            Renderers[i] = ais[i].transform.GetComponent<BGRenderer>();
            ais[i].GroundHeight = Renderers[i].transform.position.y;
            ais[i].MyRunSpeed = RunSpeed;
            ais[i].Team = Team;
            ais[i].Health = MaxHealth;
            ais[i].Enemy = null;
            ais[i].EnemyDistance = 999999;
            ais[i].myarmy = this;
        }

        // GENERATE A GRID FOR GROUND HEIGHT REFERENCE
        if(grid == null)
        {
            Terrain terrain = FindObjectOfType<Terrain>();
            TerrainData td = terrain.terrainData;
            float maxw = Mathf.Max(td.size.z, td.size.x);

            grid = new float[(int)maxw][];

            aigrid = new int[(int)maxw][];

            gridcorner = terrain.transform.position;

            Vector3 current = gridcorner;
            current.y += 200;
            RaycastHit ht = new RaycastHit();

            for (int x = 0; x < maxw; x++)
            {
                grid[x] = new float[(int)maxw];
                aigrid[x] = new int[(int)maxw];
                current.z = gridcorner.z;

                for (int y = 0; y < maxw; y++)
                {
                    if (Physics.Raycast(current, Vector3.down * 1000, out ht))
                    {
                        grid[x][y] = ht.point.y;
                        aigrid[x][y] = -1;
                    }

                    current.z += 1;
                }
                current.x += 1;
               
            }


        }

        if (RunAsUnityRender)
        {
          if(manualtranslist == null)  manualtranslist = new List<BgCamera.BgTrans>();
            Quaternion temprot = new Quaternion();
            for (int i = 0; i < ais.Length; i++)
            {
                BgCamera.BgTrans nt = new BgCamera.BgTrans();

                nt.position = ais[i].transform.position;

                temprot = ais[i].transform.rotation;
                nt.rotation.x = temprot.x;
                nt.rotation.y = temprot.y;
                nt.rotation.z = temprot.z;
                nt.rotation.w = temprot.w;

                nt.scale = ais[i].transform.lossyScale;


                manualtranslist.Add(nt);

                Renderers[i].ObjectIndex = manualtranslist.Count-1;

            }
        }

     

    }

    bool listconvert;
    static float delta;
    void RunArmy(SimpleArmy myarmy)
    {

        // GET ALL OBJECT TRANSFORMS FOR FAST POSITION MODIFICATION
        ref BgCamera.BgTrans[] transforms = ref BgCamera.allTransformsarray;
        if (RunAsUnityRender) transforms = ref manualtransforms;

        //TEMP VARS
        Vector3 moveDir = myarmy.myforward;

        RaycastHit ht = new RaycastHit();
       
        AiIndividual myai;


        for (int i = 0; i < Renderers.Length; i++)
            {
            //GET TRANSFORM FOR REDUCED LOOK UP
            ref BGRenderer rend = ref myarmy.Renderers[i];

            ref BgCamera.BgTrans temptrans = ref transforms[rend.ObjectIndex];

      

            myai = myarmy.ais[i];

            myai.position = temptrans.position;

            //ASSIGN TO CURRENT GRID
            int posx = (int)myai.position.x;
            int posy = (int)myai.position.z;
         

            //PROCESS AI STATES


            if (myai.State == 0)
            {
                Idle(ref temptrans, ref myai, ref moveDir, ref rend);
            }
          
            if (myai.State == 1)
            {
                Charge(ref temptrans, ref myai, ref moveDir, ref rend);
            }

            if (myai.State == 2)
            {
                Attack(ref temptrans, ref myai, ref moveDir, ref rend);
            }

            //GO TO DEATH IF HEALTH LESS THAN ZERO
            if (myai.Health <= 0) myai.State = 3;

            if (myai.State == 3)
            {
                Dead(ref temptrans, ref myai, ref moveDir, ref rend);

            }
            else
            {

                //PROCESS ROTATION
                if (myai.LookDir.magnitude > 0)
                {
                    myai.LookDir.y = 0;
                    myai.LookDir = myai.LookDir.normalized;

                    Quaternion quat = Quaternion.LookRotation(myai.LookDir.normalized);

                    Quaternion quat2 = new Quaternion(temptrans.rotation.x, temptrans.rotation.y, temptrans.rotation.z, temptrans.rotation.w);
                    quat = Quaternion.Lerp(quat2, quat, delta * 8);

                    temptrans.rotation.x = quat.x;
                    temptrans.rotation.y = quat.y;
                    temptrans.rotation.z = quat.z;
                    temptrans.rotation.w = quat.w;


                }
            }


            if(myai.State != 3)    Ground(ref temptrans, ref myai);


            //FINALLY UPDATE CHILDREN OBJECTS SINCE WE WON'T DARE USE SLOW UNITY TRANSFORMS
            if (!RunAsUnityRender) myarmy.Renderers[i].UpdateChildren();

            aigrid[posx][posy] = myai.aiindex;
        }
        

    }

    //AI INDIVIDUAL FUNCTIONS
    void Ground(ref BgCamera.BgTrans temptrans, ref AiIndividual myai)
    {
        //GET GRID GROUND HEIGHT AND SMOOTH LERP
        Vector3 raystart = temptrans.position - gridcorner;

        int x = Mathf.RoundToInt(raystart.x);
        int y = Mathf.RoundToInt(raystart.z);

        //SMOOTH LERP GRID GROUND HEIGHT
        myai.GroundHeight = Mathf.Lerp(myai.GroundHeight, grid[x][y], 6 * delta);

        //ASSIGN GROUND HEIGHT
        temptrans.position.y = myai.GroundHeight;
    }

    public static Vector3 GetRight(Vector3 forward, Vector3 up)
    {
        return Vector3.Normalize(Vector3.Cross(up, forward));
    }

    void Attack(ref BgCamera.BgTrans temptrans, ref AiIndividual myai, ref Vector3 moveDir, ref BGRenderer rend)
    {
        myai.EnemyDistance = Vector3.Distance(myai.position, myai.Enemy.position);

        if (myai.EnemyDistance > myai.myarmy.AttackRange + 0.5f || myai.Enemy.Health <= 0)
        {
            myai.Enemy = null;
            myai.EnemyDistance = 99999;
            myai.State = 1;
        }
        else
        {

          Vector3  ahead = myai.position;
            int rnd = randomgen.Next(5);
            if (rnd == 3)
            {
                ahead.x += 1;
            }
            else
            {
                if (rnd == 2)
                {
                    ahead.x -= 1;
                }
                else
                {

                    if (rnd == 1)
                    {
                        ahead.z += 1;
                    }
                    else ahead.z -= 1;
                }
            }

          int  posx = Mathf.RoundToInt(ahead.x);
          int  posy = Mathf.RoundToInt(ahead.z);
           int checkenemy = aigrid[posx][posy];

            if (checkenemy >= 0 && checkenemy != myai.aiindex)
            {
                if (allais[checkenemy].Health > 0)
                {
                    Vector3 dir = allais[checkenemy].position - myai.position;

                    if (Vector3.Dot(dir, myai.LookDir) > 0)
                    {
                        float dist = dir.magnitude;
                       
                            if (dist < 1)
                            {
                                temptrans.position -= dir.normalized * 4 * delta;
                            }
                    }
                    
                }
            }

            myai.LookDir = (myai.Enemy.position - myai.position);
            myai.LookDir.y = 0;
            myai.LookDir = myai.LookDir.normalized;


            myai.AttackTimer += delta;

            //DEAL DAMAGE
            if (myai.AttackTimer >= myai.myarmy.AttackSpeedTime)
            {
                myai.AttackTimer = 0;
                myai.Enemy.Health -= Damage;
            }
            rend.CurrentClip = rend.AnimClipIndex[AttackAnimIndex];
            rend.PlaySpeed = IdleAnimSpeedMult;
        }

    }

    void Dead(ref BgCamera.BgTrans temptrans, ref AiIndividual myai, ref Vector3 moveDir, ref BGRenderer rend)
    {
        if (!myai.Dead)
        {
            // Debug.Log("I DIED! " + myai.name);
            rend.DisableBgRender = true;
            myai.Dead = true;
            rend.PlayMode = 1;
            rend.CurrentClip = rend.AnimClipIndex[DeathAnimIndex];
            rend.PlaySpeed = IdleAnimSpeedMult;
        }
        else
        {
            if (myai.DeathTimer < 5)
            {
                myai.DeathTimer += delta;

                if (myai.DeathTimer >= 5)
                {
                    temptrans.position.y -= 500;
                }

            }
        }
    }


        void Charge(ref BgCamera.BgTrans temptrans, ref AiIndividual myai, ref Vector3 moveDir, ref BGRenderer rend)
    {
      


        //FIND ENEMY

        //CHECK FOR RANDOM

       

        if (myai.Enemy != null)
        {
            if (myai.Enemy.Health <= 0)
            {
                myai.Enemy = null;
                myai.EnemyDistance = 99999;
            }
        }

        int checkenemy = Mathf.Min(allais.Length - 1, randomgen.Next(0, allais.Length));

        if (allais[checkenemy].Team != myai.Team)
        {
            if (allais[checkenemy].Health > 0)
            {
                float dist = Vector3.Distance(myai.position, allais[checkenemy].position);

                if (dist < myai.EnemyDistance)
                {
                    myai.Enemy = allais[checkenemy];
                    myai.EnemyDistance = dist;
                }

            }
        }


        //CHARGE IF HAS ENEMY
        if (myai.Enemy != null)
        {
            myai.LookDir = (myai.Enemy.position - myai.position).normalized;
            Vector3 goal = myai.LookDir;

            myai.EnemyDistance = Vector3.Distance(myai.position, myai.Enemy.position);

            if(myai.EnemyDistance < myai.myarmy.AttackRange)
            {
                myai.State = 2;
            }

            bool stop = false;

            Vector3 ahead = myai.position + goal;
            int posx = Mathf.RoundToInt(ahead.x);
            int posy = Mathf.RoundToInt(ahead.z);

            //CHECK GRID FOR UNIT AHEAD FOR AVOIDANCE
            checkenemy = aigrid[posx][posy];

            if (checkenemy >= 0 && checkenemy != myai.aiindex)
            {
                if (allais[checkenemy].Health > 0)
                {
                    float dist = Vector3.Distance(myai.position, allais[checkenemy].position);

                    if (allais[checkenemy].Team != myai.Team)
                    {
                        if (dist < myai.EnemyDistance)
                        {
                            myai.Enemy = allais[checkenemy];
                            myai.EnemyDistance = dist;
                        }
                    }
                    else
                    {
                        if (dist < 1.5f)
                        {
                            Vector3 dir = allais[checkenemy].position - myai.position;
                            if (Vector3.Dot(dir, goal) > 0)
                            {
                                if (dist < 1)
                                {
                                    if (Vector3.Dot(dir, goal) > 0) temptrans.position -= dir.normalized * delta;
                                }

                                stop = true;
                            }
                        }
                    }
                }

            }


            //CHECKING MORE GRIDS FOR AVOIDANCE
            ahead = myai.position + goal * 2;
            posx = Mathf.RoundToInt(ahead.x);
            posy = Mathf.RoundToInt(ahead.z);
            checkenemy = aigrid[posx][posy];

            if (checkenemy >= 0 && checkenemy != myai.aiindex)
            {
                if (allais[checkenemy].Health > 0)
                {
                    float dist = Vector3.Distance(myai.position, allais[checkenemy].position);

                    if (allais[checkenemy].Team != myai.Team)
                    {
                        if (dist < myai.EnemyDistance)
                        {
                            myai.Enemy = allais[checkenemy];
                            myai.EnemyDistance = dist;
                        }
                    }
                    else
                    {
                        if (dist < 1.5f)
                        {
                            stop = true;
                        }
                        else
                        {
                            if (dist < 3)
                            {
                                myai.LookDir = Vector3.Lerp (goal, (myai.position - allais[checkenemy].position).normalized, 0.3f).normalized;
                            }
                        }
                    }
                }
            }

            ahead = myai.position;



            posx = Mathf.RoundToInt(ahead.x);
            posy = Mathf.RoundToInt(ahead.z);
            checkenemy = aigrid[posx][posy];

            if (checkenemy >= 0 && checkenemy != myai.aiindex)
            {
                if (allais[checkenemy].Health > 0)
                {
                    Vector3 dir = allais[checkenemy].position - myai.position;

                    if (Vector3.Dot(dir, goal) > 0)
                    {
                        float dist = dir.magnitude;
                       // myai.LookDir = (myai.LookDir + dir.normalized).normalized;
                        if (allais[checkenemy].Team != myai.Team)
                        {
                            if (dist < myai.EnemyDistance)
                            {
                                myai.Enemy = allais[checkenemy];
                                myai.EnemyDistance = dist;
                            }
                        }
                        else
                        {
                            if (dist < 1.5)
                            {
                                temptrans.position -= dir.normalized * delta;
                                stop = true;
                            }
                        }
                    }
                }
            }


            if (stop) myai.MoveDir = Vector3.Lerp(myai.MoveDir, Vector3.zero, delta * 20);
            else  myai.MoveDir = Vector3.Lerp(myai.MoveDir, myai.LookDir * myai.MyRunSpeed, delta * 5);

            float pspeed = (myai.MoveDir.magnitude / myai.MyRunSpeed) * myai.myarmy.RunAnimSpeedMult;

         
            temptrans.position += myai.MoveDir * delta;
            if (pspeed > 0.1f)
            {
                //ENSURE WE DONT GET STUCK SWITCHING BETWEEN IDLE AND RUN WITH NO CROSSFADES
                if (rend.CurrentClip == rend.AnimClipIndex[IdleAnimIndex])
                {
                    if (rend.CurrentAnimFrame >= rend.CurrentClip.x + ((rend.CurrentClip.y - rend.CurrentClip.x) / 4))
                    {
                        rend.CurrentClip = rend.AnimClipIndex[myai.myarmy.RunAnimIndex];
                        rend.PlaySpeed = pspeed;
                    }
                }
                else
                {
                    rend.CurrentClip = rend.AnimClipIndex[myai.myarmy.RunAnimIndex];
                    rend.PlaySpeed = pspeed;
                }
            }
            else
            {
                rend.CurrentClip = rend.AnimClipIndex[IdleAnimIndex];
                rend.PlaySpeed = IdleAnimSpeedMult;
            }
         

            
        }
    }

    void Idle(ref BgCamera.BgTrans temptrans, ref AiIndividual myai, ref Vector3 moveDir, ref BGRenderer rend)
    {
        rend.CurrentClip = rend.AnimClipIndex[IdleAnimIndex];
        rend.PlaySpeed = IdleAnimSpeedMult;
    }



    bool ThreadRunning;
    void RunThreaded()
    {
        //LOOP THREAD
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        while (ThreadRunning)
        {
            stopwatch.Restart();
           

            for (int i = 0; i < AllArmies.Count; i++)
            {
                RunArmy(AllArmies[i]);
            }

            //CALCULATE DELTA
            stopwatch.Stop(); // Stop measuring time
            delta = (float)stopwatch.Elapsed.TotalSeconds;
        }
    }

    void ChangeAllStates(int state)
    {
        for (int i = 0; i < AllArmies.Count; i++)
        {
            for (int a = 0; a < AllArmies[i].ais.Length; a++)
            {
                AllArmies[i].ais[a].State = state;
            }


        }
    }


    // Update is called once per frame
    void Update()
    {

        if (masterarmy == this)
        {

            if (allais == null)
            {
                int counter = 0;
                for (int i = 0; i < AllArmies.Count; i++)
                {
                    counter += AllArmies[i].ais.Length;
                }
                allais = new AiIndividual[counter];
                Debug.Log("all ai created");
                counter = 0;
                for (int i = 0; i < AllArmies.Count; i++)
                {
                    for (int a = 0; a < AllArmies[i].ais.Length; a++)
                    {

                        allais[counter] = AllArmies[i].ais[a];
                        allais[counter].aiindex = counter;
                        counter++;
                    }
                }
            }


            if (!listconvert && RunAsUnityRender)
            {
                listconvert = true;

                manualtransforms = manualtranslist.ToArray();

             

              


            }
        }


        TotalCharacters = Characters;
        if (DebugGrid)
        {
            Vector3 cur = gridcorner;
            for (int x = 0; x < grid.Length; x++)
            {
                cur.z = gridcorner.z;
                for (int y = 0; y < grid[x].Length; y++)
                {
                    cur.y = grid[x][y];

                    Debug.DrawRay(cur, Vector3.up, Color.red);

                    cur.z += 1;
                }
                cur.x += 1;
            }
        }

       
        myforward = transform.forward;

        if (masterarmy == this)
        {
            if (Input.GetKeyUp(KeyCode.K))
            {
                
                if (!Attacking)  ChangeAllStates(1);
                else ChangeAllStates(0);

                Attacking = !Attacking;
            }



            if (Threaded)
            {
                //START THREAD IF HAVENT
                if (mythread == null)
                {
                    ThreadRunning = true;
                    mythread = new Thread(RunThreaded);
                    mythread.Start();
                }
            }
            else
            {
                delta = Time.deltaTime;

                for (int i = 0; i < AllArmies.Count; i++)
                {
                    RunArmy(AllArmies[i]);
                }


                if (mythread != null)
                {
                    ThreadRunning = false;
                    mythread.Abort();
                    mythread = null;
                }
            }


        }


        //RENDER IN UNITY 
        if (RunAsUnityRender)
        {
            BgCamera.BgTrans trans;
            Material mymat;
            Mesh cm;
            Quaternion rot;

            BGRenderer curld;


            ProcessLodsForUnity(Camera.main.transform.position);
            for (int i = 0; i < Renderers.Length; i++)
            {
            
                int lod = Renderers[i].currenlod;
                //lod = 0;
                if (lod >= 0)
                {
                    int curframe = (int)Renderers[i].CurrentAnimFrame;
                    curld = Renderers[i].Lods[lod].mesh;
                    trans = manualtransforms[Renderers[i].ObjectIndex];

                    cm = curld.AnimFrames[curframe].mymesh;
                    mymat = curld.AnimFrames[curframe].mymat;

                    rot.x = trans.rotation.x;
                    rot.y = trans.rotation.y;
                    rot.z = trans.rotation.z;
                    rot.w = trans.rotation.w;

                    //RENDER
                    Graphics.DrawMesh(cm, trans.position, rot, mymat, 0, null, 0, null, UnityRenderShadows);

                    for (int m = 0; m < curld.MeshGroup.Length; m++)
                    {
                        cm = curld.MeshGroup[m].AnimFrames[curframe].mymesh;
                        mymat = curld.MeshGroup[m].AnimFrames[curframe].mymat;

                        rot.x = trans.rotation.x;
                        rot.y = trans.rotation.y;
                        rot.z = trans.rotation.z;
                        rot.w = trans.rotation.w;

                        for (int ms = 0; ms < cm.subMeshCount; ms++)
                        {
                            //render mesh group
                            Graphics.DrawMesh(cm, trans.position, rot, mymat, 0, null, ms, null, UnityRenderShadows);
                        }


                    }
                }
            }
        }
    }


    void ProcessLodsForUnity(Vector3 cameraPosition)
    {
     float delt =  Time.deltaTime;

        //PLAY ANIMATION
        for (int i = 0; i < Renderers.Length; i++)
        {

            BGRenderer temprend = Renderers[i];

                    temprend.CurrentAnimFrame += delt * 30 * temprend.PlaySpeed;
                    if (temprend.CurrentAnimFrame > temprend.CurrentClip.y || temprend.CurrentAnimFrame < temprend.CurrentClip.x)
                    {
                        temprend.CurrentAnimFrame = temprend.CurrentClip.x;

                    }

            

        }


        //PROCESS LODS
        for (int i = 0; i < Renderers.Length; i++)
        {
            BGRenderer temprend = Renderers[i];
            int ld = 0;

            int chosenlod = -1;
           float distance = Vector3.Distance(manualtransforms[temprend.ObjectIndex].position, cameraPosition);



            for (int l = temprend.Lods.Length - 1; l >= 0; l--)
            {
                if (temprend.Lods[l].EndDistance > distance)
                {
                    chosenlod = l;
                    //  break;
                }
                else break;
            }
               temprend.currenlod = chosenlod;


        }

    }

    private void OnGUI()
    {
        if(masterarmy == this && !Attacking)GUI.Label(new Rect(Screen.width/2, Screen.height/2, 500, 34), "Press K To Start Battle!");
    }

}
