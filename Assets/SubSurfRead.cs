using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class SubSurfRead : MonoBehaviour {

	public Object _o;
	public string _s;
    //public int _txl = 1;
    //public RenderTexture _rt;
    //public Camera _cam;
    public SubSurfWrite1 _wrScr;
    public bool invX, invY, invZ;
    public bool finvX, finvY, finvZ;

    public SkinnedMeshRenderer oriMeshR;
	public Mesh oriMesh;
    public Vector3[] oriBaked;
    public Transform o2w;
    [HideInInspector]
	public string oriMeshData;
    [HideInInspector]
    public string[] oriMeshDataSplit;
    //public int oriSize;

    struct oriRBuffer
    {
        public float x, y, z, w;
    }
    ComputeBuffer oriReadBuff;
    bool doRead;

    //Texture2D uvTex;

	public VertexInfo[] oriVerts;
	public FaceInfo[] oriFaces;

	public SS_FaceV[] subVertsF;
	public SS_EdgeV[] subVertsE;
	public SS_OriV[] subVertsV;

	public Vector3[] finalVerts;
	//public Vector3[] finalNorms;

	public Mesh finalMesh;
	public MeshFilter finalMeshF;

	void Start () {
		ReadOri ();
        CreateRemap();
		CreateSS();
		UpdateSS();
        IndexSSnBuffer();
	}

	void ReadOri () {
        doRead = false;
		List<Vector3> v = new List<Vector3>();
		List<int[]> f = new List<int[]> ();
        int x = 0;
		for (int q = System.Array.IndexOf(oriMeshDataSplit, _s), qq = oriMeshDataSplit.Length; q < qq;) {
			string sq = oriMeshDataSplit [q];
            if (sq == "]")
                break;
            if (sq == "vrt" && int.Parse(oriMeshDataSplit[q + 1]) == x)
            {
                v.Add(new Vector3(float.Parse(oriMeshDataSplit[q + 2]), float.Parse(oriMeshDataSplit[q + 3]), float.Parse(oriMeshDataSplit[q + 4])));
                q += 4;
                x++;
            }
            else if (sq == "tri")
            {
                int a = 0;
                List<int> l = new List<int>();
                q++;
                while (int.TryParse(oriMeshDataSplit[q], out a))
                {
                    l.Add(a);
                    q++;
                }
                f.Add(l.ToArray());
            }
            else q++;
		}
		print ("Read " + v.Count + " verts, " + f.Count + " faces.");

		oriVerts = new VertexInfo[v.Count];
		for (int a = v.Count - 1; a >= 0; a--) {
			oriVerts [a] = new VertexInfo ();
			oriVerts [a].pos = o2w.TransformPoint(new Vector3(v[a].x, -v[a].z, v[a].y));
            List<int> q = new List<int> ();
			foreach (int[] fi in f) {
				foreach (int ii in fi) {
					if (ii == a) {
						foreach (int ii2 in fi) {
							if (ii2 != a && !q.Contains (ii2)) {
								q.Add (ii2);
							}
						}
						break;
					}
				}
            }
            oriVerts[a].connIndexs = q.ToArray();
        }

		oriFaces = new FaceInfo[f.Count];
		for (int a = f.Count - 1; a >= 0; a--) {
			oriFaces [a] = new FaceInfo ();
			oriFaces [a].verts = f [a];
			oriFaces [a].sides = f [a].Length;
            if (oriFaces[a].sides > 5)
                Debug.LogWarning("face " + a + " has more than 5 vertices (" + oriFaces[a].sides + "). SubSurf will not work properly.");
		}
	}

    void CreateRemap()
    {
        oriMesh = oriMeshR.sharedMesh;
        Vector3[] oriV = oriMesh.vertices;
        Vector2[] uvs = new Vector2[oriV.Length];
        //oriSize = GetClosestPw2(Mathf.Sqrt(oriVerts.Length));

        /*
        _rt = new RenderTexture(oriSize, oriSize, 16, RenderTextureFormat.ARGBFloat);
        _rt.filterMode = FilterMode.Point;
        _rt.generateMips = false;
        _rt.useMipMap = false;
        //_cam.targetTexture = _rt;
        _cam.enabled = false;
        */
        //_cam.enabled = false;
        oriReadBuff = new ComputeBuffer(oriVerts.Length, Marshal.SizeOf(typeof(oriRBuffer)));
        oriMeshR.material.SetBuffer("ssBuffer", oriReadBuff);
        Graphics.SetRandomWriteTarget(1, oriReadBuff);

        //_wrScr.SetPosTex(_rt, oriSize);
        //Dictionary<int, int> remap = new Dictionary<int, int>();
        int[] idx = new int[oriV.Length];

        for (int a = oriVerts.Length - 1; a >= 0; a--)
        {
            Vector3 vv = oriVerts[a].pos;
            if (invX) vv.x = -vv.x;
            if (invY) vv.y = -vv.y;
            if (invZ) vv.z = -vv.z;
            Debug.DrawLine(o2w.transform.TransformPoint(vv), o2w.transform.TransformPoint(vv) + Vector3.up * 0.1f, Color.yellow, 5);
        }
        for (int y = oriV.Length - 1; y >= 0; y--)
        {
            Debug.DrawLine(oriV[y], oriV[y] + Vector3.up * 0.1f, Color.red);
        }

        for (int y = oriV.Length - 1; y >= 0; y--)
        {
            idx[y] = y;
            bool e = false;
            for (int a = oriVerts.Length - 1; a >= 0; a--)
            {
                Vector3 vv = oriVerts[a].pos;
                if (invX) vv.x = -vv.x;
                if (invY) vv.y = -vv.y;
                if (invZ) vv.z = -vv.z;
                if (D(oriV[y], vv) < 0.001f)
                {
                    float yy = Mathf.Floor(y / 100f);
                    uvs[y] = new Vector2((y - yy*100)*0.01f, yy*0.01f);//new Vector2(((a % oriSize) + 0.5f) / oriSize, 1-(((a / oriSize) + 0.5f) / oriSize));
                    //remap[a] = y;
                    oriVerts[a].i = y;
                    e = true;
                    break;
                }
            }
            if (!e)
            {
                Debug.LogError("? " + oriV[y]);
                //Debug.DrawLine(oriV[y], oriV[y] + Vector3.up * 0.1f, Color.red);
            }
        }
        oriMesh.uv = uvs;
        oriBaked = new Vector3[oriVerts.Length];
        print("total real verts " + oriV.Length);
        oriMesh.triangles = new int[0];
        oriMesh.SetIndices(idx, MeshTopology.Points, 0);

        oriRBuffer[] b = new oriRBuffer[oriBaked.Length];
        for (int q = oriBaked.Length - 1; q >= 0; q--)
            b[q].w = q;
        oriReadBuff.SetData(b);
    }

    void OnApplicationQuit()
    {
        oriReadBuff.Release();
    }

    void CreateSS()
	{
		Dictionary<int, int> faceOriF = new Dictionary<int, int>();
		Dictionary<int, int[]> faceVInfo = new Dictionary<int, int[]>();
		Dictionary<int, int[]> edgeVInfo = new Dictionary<int, int[]>();
		List<int[]> doneEdges = new List<int[]>();
		Dictionary<int, List<int>> edgeOfV = new Dictionary<int, List<int>>();

		int i = oriVerts.Length; //total
		int i2 = oriVerts.Length; //start of fp
		for (int o = 0; o < i2; o++)
			edgeOfV[o] = new List<int>();
		for (int f = oriFaces.Length-1; f >= 0; f--) //create face points
		{
			//VertexInfo vv = new VertexInfo();
			faceOriF[i] = f;
			faceVInfo[i] = oriFaces[f].verts;
            i++;
		}
        int vi = 0;
        foreach (VertexInfo vv in oriVerts) //clean connindexs for real edges
        {
            for (int q = vv.connIndexs.Length - 1; q >= 0; q--)
            {
                for (int f = oriFaces.Length - 1; f >= 0; f--)
                {
                    if (isEdgeOf(oriFaces[f].verts, vi, vv.connIndexs[q]))
                        goto found;
                }
                vv.connIndexs[q] = -1;
            found:;
            }
            vi++;
            List<int> dvv = new List<int>(vv.connIndexs);
            dvv.RemoveAll(qx => qx == -1);
            vv.connIndexs = dvv.ToArray();
        }
        print ("face verts " + (i-i2));
		int i3 = i; //start of ep
		for (int y = oriVerts.Length - 1; y >= 0; y--) //create edge points
		{
			foreach (int e in oriVerts[y].connIndexs)
			{
				if (doneEdges.FindIndex(a => (a[0] == y && a[1] == e) || (a[0] == e && a[1] == y)) >= 0)
					continue;
				//VertexInfo vv = new VertexInfo();
				doneEdges.Add(new int[] { y, e });
				edgeVInfo[i] = new int[] { y, e };
				edgeOfV[y].Add(i);
				edgeOfV[e].Add(i);
				i++;
			}
		}
		print ("edge verts " + (i-i3));
		print ("i = " + i + " i3 = " + i3 + " i2 = " + i2);

		//List<int[]> fBuff = new List<int[]>();
		Dictionary<int, List<int>> edgesOfF = new Dictionary<int, List<int>>();

		//make faces
		for (int w = i2; w < i3; w++)
		{
			edgesOfF[w] = new List<int>();
			int[] connVerts = faceVInfo[w];
			for (int v = connVerts.Length - 1; v >= 0; v--)
			{
				foreach (int e in edgeOfV[connVerts[v]])
				{
					if (System.Array.Exists(faceVInfo[w], x => x == edgeVInfo[e][0]) && System.Array.Exists(faceVInfo[w], x => x == edgeVInfo[e][1]))
					{
						//fBuff.Add(new int[] { w, e, connVerts[v] });
						edgesOfF[w].Add(e);
					}
				}
			}
		}
        

		subVertsF = new SS_FaceV[i3 - i2];
		subVertsE = new SS_EdgeV[i - i3];
		subVertsV = new SS_OriV[i2];
		finalVerts = new Vector3[i]; //update later
		//finalFaces = new int[fBuff.Count * 3];

		for (int w = i3; w < i; w++)
		{
			subVertsE[w - i3] = new SS_EdgeV();
			subVertsE[w - i3].id = w;
			subVertsE[w - i3].ve1 = edgeVInfo[w][0];
			subVertsE[w - i3].ve2 = edgeVInfo[w][1];
			subVertsE[w - i3].vf1 = -1;
			subVertsE[w - i3].vf2 = -1;
		}
		for (int w = i2; w < i3; w++)
		{
			subVertsF[w - i2] = new SS_FaceV();
			subVertsF[w - i2].id = w;
			subVertsF[w - i2].fIndex = faceOriF[w];

			foreach (int ee in edgesOfF[w])
			{
				if (subVertsE[ee - i3].vf1 == -1)
					subVertsE[ee - i3].vf1 = w;
				else
					subVertsE[ee - i3].vf2 = w;
			}
		}
		for (int w = 0; w < i2 ; w++)
		{
			subVertsV[w] = new SS_OriV();
			subVertsV[w].id = w;
            subVertsV[w].n = oriVerts[w].connIndexs.Length;
			subVertsV[w].op = w;
			List<int> l = new List<int>();
			for (int ww = i2; ww < i3; ww++)
			{
				if (System.Array.Exists(faceVInfo[ww], x => x == w))
				{
					l.Add(ww);
				}
			}
			//if (l.Count != subVertsV[w].n)
				//Debug.LogError("vert " + w + " fp found " + l.Count + " n=" + subVertsV[w].n);
			subVertsV[w].setFp(this, l.ToArray());
		}

		/*
		for (int t = fBuff.Count-1; t >= 0; t--) {
			finalFaces[t*3] = fBuff[t][0];
			finalFaces[t*3+1] = fBuff[t][1];
			finalFaces[t*3+2] = fBuff[t][2];
		}
		*/
	}

    void IndexSSnBuffer ()
    {
        finalMesh = finalMeshF.mesh;
        Vector3[] verts2 = finalMesh.vertices;
        Vector2[] uvs = new Vector2[verts2.Length];
        print("Total pre-sub verts: " + finalVerts.Length);
        print("Scanning matches for " + verts2.Length + " verts");
        int matches = 0;
        bool warn = false;
        for (int v2 = verts2.Length - 1; v2 >= 0; v2--)
        {
            for (int v = finalVerts.Length - 1; v >= 0; v--)
            {
                if (D(oriMeshR.transform.TransformPoint(finalVerts[v]), verts2[v2]) < 0.001f)
                {
                    float y = Mathf.Floor(v / 100f);
                    uvs[v2] = new Vector2((v - y*100)*0.01f, y*0.01f);
                    matches++;
                    //Debug.Log(v2 + "->" + uvs[v2].x*100);
                    goto found;
                }
            }
            warn = true;
            Debug.LogWarning("vert " + v2 + "@ " + verts2[v2] + "no match");
            Debug.DrawLine(finalMeshF.transform.TransformPoint(verts2[v2]) + Vector3.up * -0.1f, finalMeshF.transform.TransformPoint(verts2[v2]) + Vector3.up * 0.1f, Color.yellow, 100);
        found:;
        }
        if (warn)
        {
            //for (int v3 = finalVerts.Length - 1; v3 >= 0; v3--)
            //{
                //Debug.Log("vert " + v3 + "@ " + finalVerts[v3]);
            //    Debug.DrawLine(finalVerts[v3] + Vector3.right * -0.1f, finalVerts[v3] + Vector3.right * 0.1f, Color.green, 100);
            //}
        }
        print(matches + " matches found");
        finalMesh.uv4 = uvs;

        doRead = true;
    }

	float D (Vector3 a, Vector3 b) {
		Vector3 c = a - b;
		return c.x * c.x + c.y * c.y + c.z * c.z;
	}

    int GetClosestPw2 (float f)
    {
        int x = 2;
        while (x < f)
            x = x << 1;
        return x;
    }

    bool isEdgeOf (int[] face, int e1, int e2)
    {
        for (int q = 0, l = face.Length; q < l; q++)
        {
            if (face[q] == e1)
            {
                if (face[pp(q - 1, l)] == e2 || face[pp(q + 1, l)] == e2)
                    return true;
            }
            else if (face[q] == e2)
            {
                if (face[pp(q - 1, l)] == e1 || face[pp(q + 1, l)] == e1)
                    return true;
            }
        }
        return false;
    } 

    int pp (int i, int l)
    {
        if (i < 0)
            i += l;
        else if (i >= l)
            i -= l;
        return i;
    }

    // enable ori verts for skinning
    void Update ()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
            UpdateSS();
        //oriMeshR.enabled = true;
    }

	void LateUpdate () {
        //oriMeshR.enabled = false;
	}

    void UpdateSS ()
	{
        //Graphics.SetRenderTarget(_rt);
        //_cam.Render();
        //Texture2D t = new Texture2D(_rt.width, _rt.height);
        //t.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
        //t.Apply();
        //Color[] c = t.GetPixels();
        if (doRead)
        {
            oriRBuffer[] b = new oriRBuffer[oriBaked.Length];
            oriReadBuff.GetData(b);
            for (int q = oriBaked.Length - 1; q >= 0; q--)
            {
                //int xx = (((int)c[q].a) & 4) >> 2;
                //int yy = (((int)c[q].a) & 2) >> 1;
                //int zz = ((int)c[q].a) & 1;
                //oriBaked[q] = new Vector3(c[q].r * (xx*2-1), c[q].g * (yy*2-1), c[q].b * (zz*2-1));
                if (Mathf.RoundToInt(b[q].w) != q)
                    Debug.LogWarning("data not set for " + q);
                else
                {
                    //oriVerts[q].pos = (new Vector3(finvX? -b[q].y : b[q].y, finvY ? -b[q].z : b[q].z, finvZ ? -b[q].x : b[q].x));
                    oriVerts[q].pos = (new Vector3(finvX ? -b[q].x : b[q].x, finvY ? -b[q].y : b[q].y, finvZ ? -b[q].z : b[q].z));
                    Vector3 p = (oriVerts[q].pos);
                    oriVerts[q].pos = finalMeshF.transform.InverseTransformPoint((oriVerts[q].pos));
                    //Debug.DrawLine(p, p + Vector3.up*0.1f, Color.white);
                    //foreach (int ii in oriVerts[q].connIndexs)
                    //{
                    //    Debug.DrawLine(p, finalMeshF.transform.TransformPoint(oriMeshR.transform.TransformPoint(oriVerts[ii].pos)), Color.white);
                    //}
                    b[q].w = 0;
                    Debug.DrawLine(p - Vector3.up * 0.2f, p + Vector3.up * 0.2f, Color.white);
                    Debug.DrawLine(p - Vector3.right * 0.2f, p + Vector3.right * 0.2f, Color.white);
                    Debug.DrawLine(p - Vector3.forward * 0.2f, p + Vector3.forward * 0.2f, Color.white);
                }
            }
            oriReadBuff.SetData(b);
            //print(oriVerts[0].pos);
            //Graphics.SetRenderTarget(null);
        }
        /*
        else
        {
            oriRBuffer[] b = new oriRBuffer[oriBaked.Length];
            for (int q = oriBaked.Length - 1; q >= 0; q--)
                b[q].w = q;
            oriReadBuff.SetData(b);
        }
        */

        foreach (SS_FaceV f in subVertsF)
			f.GetPos(this);
		foreach (SS_EdgeV e in subVertsE)
			e.GetPos(this);
		foreach (SS_OriV v in subVertsV)
			v.GetPos(this);

        //--------buffer--------
        int i = finalVerts.Length;
        oriBuffer[] buff = new oriBuffer[i];
        for (int r = i-1; r >= 0; r--)
        {
            buff[r].x = finalVerts[r].x;
            buff[r].y = finalVerts[r].y;
            buff[r].z = finalVerts[r].z;
            buff[r].w = 1;
        }
        _wrScr.SetBuffer(buff);
    }

	public class VertexInfo
	{
        public int i;
		public Vector3 pos;
		public int[] connIndexs; //edge with vert
	};
	public class FaceInfo
	{
		public int[] verts;
		public int sides;
	};

	public class SS_OriV //type 0
	{
		public int id;
		public int[] fp;
        public int[] ep;
		public int op;
		public int n;

        public void setFp (SubSurfRead scr, int[] w)
        {
            fp = w;
            int fl = fp.Length;
            List<FaceInfo> fn = new List<FaceInfo>();
            List<int> evs = new List<int>();
            Dictionary<int, int> fid = new Dictionary<int, int>();
            foreach (int f in fp)
                fn.Add(scr.oriFaces[System.Array.Find(scr.subVertsF, x => x.id == f).fIndex]);
            for (int a = 0; a < fl; a++)
            {
                foreach (int i in fn[a].verts)
                {
                    if (!fid.ContainsKey(i))
                    {
                        fid[i] = a;
                    }
                    else
                    {
                        if (!evs.Contains(i) && i != op)
                            evs.Add(i);
                    }
                }
            }
            if (evs.Count != fl)
                Debug.LogError(id + " n not same with shared edge vert count" + evs.Count + " " + fl);
            else
                ep = evs.ToArray();
        }

		public void GetPos(SubSurfRead scr) //(F + 2R + (n-3)P) / n
		{
            int fl = fp.Length;
            Vector3 _f = Vector3.zero;
			Vector3 _r = Vector3.zero;
			foreach (int f in fp)
				_f += scr.finalVerts[f];
            _f = _f / fl;
            //VertexInfo v = scr.oriVerts[op];
            Vector3 p = scr.oriVerts[op].pos;
            foreach (int e in ep)
                _r += scr.oriVerts[e].pos;// scr.oriVerts[e].pos;
			_r = (p + _r/fl) / 2;

			scr.finalVerts[id] = (_f + 2 * _r + (fl - 3) * p) / fl;
            float s = 0.1f;
            
			//Debug.DrawLine (scr.finalMeshF.transform.TransformPoint(scr.finalVerts [id]) - Vector3.up * s, scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) + Vector3.up * s, Color.green);
            //Debug.DrawLine(scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) - Vector3.right * s, scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) + Vector3.right * s, Color.green);
            //Debug.DrawLine(scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) - Vector3.forward * s, scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) + Vector3.forward * s, Color.green);
        }
    }
    public class SS_EdgeV //type 1
	{
		public int id;
		public int ve1, ve2, vf1, vf2;
        public void GetPos(SubSurfRead scr)
		{
			scr.finalVerts[id] = 0.25f * (scr.oriVerts[ve1].pos + scr.oriVerts[ve2].pos + scr.finalVerts[vf1] + scr.finalVerts[vf2]);
            float s = 0.1f;

            //Debug.DrawLine (scr.finalMeshF.transform.TransformPoint(scr.finalVerts [id]) - Vector3.up * s, scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) + Vector3.up * s, Color.red);
            //Debug.DrawLine(scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) - Vector3.right * s, scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) + Vector3.right * s, Color.red);
            //Debug.DrawLine(scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) - Vector3.forward * s, scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) + Vector3.forward * s, Color.red);
        }
    }

    public class SS_FaceV //type 2
	{
		public int id;
		public int fIndex;
		public void GetPos(SubSurfRead scr)
		{
			Vector3 x = Vector3.zero;
			FaceInfo f = scr.oriFaces[fIndex];
			foreach (int a in f.verts)
			{
				x += scr.oriVerts[a].pos;
			}
			scr.finalVerts[id] = x / f.sides;
            float s = 0.1f;

            //Debug.DrawLine (scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) - Vector3.up * s, scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) + Vector3.up * s, Color.blue);
            //Debug.DrawLine(scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) - Vector3.right * s, scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) + Vector3.right * s, Color.blue);
            //Debug.DrawLine(scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) - Vector3.forward * s, scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) + Vector3.forward * s, Color.blue);
        }
    }

    public struct oriBuffer
    {
        public float x, y, z, w;
    };
}