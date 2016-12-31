using UnityEngine;
using System.Collections.Generic;

public class SubSurfMod : MonoBehaviour {

	public Object _o;
	public string _s;
    public int _txl = 1;
    public RenderTexture _rt;
    public Camera _cam;
    public SubSurfWrite _wrScr;

    //public Texture2D _idTex;
    //public int idSize;

    //
    public GUITexture tx;

	public MeshFilter oriMeshF;
    public MeshRenderer oriMeshR;
	public Mesh oriMesh;
	public string oriMeshData;
	public string[] oriMeshDataSplit;
    public int oriSize;

    Texture2D uvTex;

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
        CreateUV1();
		CreateSS();
		UpdateSS();
        IndexSSnBuffer();
	}

	void ReadOri () {
		List<Vector3> v = new List<Vector3>();
		List<int[]> f = new List<int[]> ();
        int x = 0;
		for (int q = System.Array.IndexOf(oriMeshDataSplit, _s), qq = oriMeshDataSplit.Length; q < qq;) {
			string sq = oriMeshDataSplit [q];
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
			oriVerts [a].pos = v [a];
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

    void CreateUV1 ()
    {
        oriMesh = oriMeshF.mesh;
        Vector3[] oriV = oriMesh.vertices;
        Vector2[] uvs = new Vector2[oriV.Length];
        oriSize = GetClosestPw2(Mathf.Sqrt(oriVerts.Length));
        _txl = 1;

        _rt = new RenderTexture(oriSize * _txl, oriSize * _txl, 16, RenderTextureFormat.ARGBFloat);
        _rt.filterMode = FilterMode.Point;
        _rt.generateMips = false;
        _rt.useMipMap = false;
        _cam.targetTexture = _rt;
        _cam.enabled = false;

        _wrScr.SetPosTex(_rt, oriSize*_txl);

        //
        if (tx)
            tx.texture = _rt;

        Dictionary<int, int> remap = new Dictionary<int, int>();
        int[] idx = new int[oriV.Length];

        for (int y = oriV.Length - 1; y >= 0; y--)
        {
            idx[y] = y;
            bool e = false;
            for (int a = oriVerts.Length - 1; a >= 0; a--)
            {
                if (D(oriV[y], oriVerts[a].pos) < 0.001f)
                {
                    uvs[y] = new Vector2(((a % oriSize) + 0.5f) / oriSize, ((a / oriSize) + 0.5f) / oriSize);
                    remap[a] = y;
                    e = true;
                    break;
                }
            }
            if (!e) print("? " + oriV[y]);
        }
        oriMesh.uv = uvs;
        //int s = oriVerts.Length;
        print("total real verts " + oriV.Length);
        oriMesh.triangles = new int[0];
        oriMesh.SetIndices(idx, MeshTopology.Points, 0);
        /*
        List<int> newTris = new List<int>();
        int q = 0;
        for (int yy = 0; yy < oriSize - 1; yy++)
        {
            for (int xx = 0; xx < oriSize - 1; xx++)
            {
                int zz = xx + yy * oriSize;
                if (zz + oriSize < s)
                {
                    newTris.Add(remap[zz]);
                    newTris.Add(remap[zz + 1]);
                    newTris.Add(remap[zz + oriSize]);
                    q += 3;
                    if (zz + oriSize + 1 < s)
                    {
                        newTris.Add(remap[zz + 1]);
                        newTris.Add(remap[zz + 1 + oriSize]);
                        newTris.Add(remap[zz + oriSize]);
                        q += 3;
                    }
                    else break;
                }
                else break;
            }
        }
        oriMesh.triangles = newTris.ToArray();
        */
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
        //idSize = GetClosestPw2(Mathf.Sqrt(finalVerts.Length));
        Vector2[] uvs = new Vector2[verts2.Length];
        //_idTex = new Texture2D(idSize * 2, idSize, TextureFormat.RGBAFloat, false);
        //Dictionary<int, int> remap = new Dictionary<int, int>();
        print("Scanning matches for " + verts2.Length + " verts");
        int matches = 0;
        for (int v2 = verts2.Length - 1; v2 >= 0; v2--)
        {
            for (int v = finalVerts.Length - 1; v >= 0; v--)
            {
                if (D(finalVerts[v], verts2[v2]) < 0.001f)
                {
                    uvs[v2] = new Vector2((v % 256) / 256f, (v / 256) / 256f);
                    //remap[v2] = v;
                    matches++;
                    goto found;
                }
            }
            Debug.LogWarning("vert " + v2 + "@ " + verts2[v2] + "no match");
            Debug.DrawLine(finalMeshF.transform.TransformPoint(verts2[v2]) + Vector3.up * -0.1f, finalMeshF.transform.TransformPoint(verts2[v2]) + Vector3.up * 0.1f, Color.yellow, 100);
        found:;
        }
        print(matches + " matches found");
        finalMesh.uv4 = uvs;

        //--------buffer--------
        oriBuffer[] buff = new oriBuffer[finalVerts.Length];
        foreach (SS_FaceV f in subVertsF)
        {
            f.FillBuffer(this, ref buff[f.id]);
        }
        foreach (SS_EdgeV f2 in subVertsE)
        {
            f2.FillBuffer(this, ref buff[f2.id]);
        }
        foreach (SS_OriV f3 in subVertsV)
        {
            f3.FillBuffer(this, ref buff[f3.id]);
        }
        _wrScr.SetBuffer(buff);
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
        oriMeshR.enabled = true;

    }
	
	//after ori verts skinned, apply
	void LateUpdate () {
        _cam.Render();
        oriMeshR.enabled = false;
	}

	void UpdateSS ()
	{
		//finalMesh.Clear();
		foreach (SS_FaceV f in subVertsF)
			f.GetPos(this);
		foreach (SS_EdgeV e in subVertsE)
			e.GetPos(this);
		foreach (SS_OriV v in subVertsV)
			v.GetPos(this);

		//finalMesh.vertices = finalVerts;
		//finalMesh.RecalculateNormals();
		//finalMesh.RecalculateBounds();


	}

	public class VertexInfo
	{
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

        public void setFp (SubSurfMod scr, int[] w)
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

		public void GetPos(SubSurfMod scr) //(F + 2R + (n-3)P) / n
		{
            int fl = fp.Length;
            Vector3 _f = Vector3.zero;
			Vector3 _r = Vector3.zero;
			foreach (int f in fp)
				_f += scr.finalVerts[f];
            _f = _f / fl;
			VertexInfo v = scr.oriVerts[op];
			foreach (int e in ep)
				_r += scr.oriVerts[e].pos;
			_r = (v.pos + _r/fl) / 2;

			scr.finalVerts[id] = (_f + 2 * _r + (fl - 3) * v.pos) / fl;
			Debug.DrawLine (scr.finalMeshF.transform.TransformPoint(scr.finalVerts [id]) - Vector3.up * 0.2f, scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) + Vector3.up * 0.2f, Color.green, 10);
            Debug.DrawLine(scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) - Vector3.right * 0.2f, scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) + Vector3.right * 0.2f, Color.green, 10);
            Debug.DrawLine(scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) - Vector3.forward * 0.2f, scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) + Vector3.forward * 0.2f, Color.green, 10);
        }

        /*
        public int n, //XXXX_TYPE_NNNN_4444 _ 3333_2222_1111_0000
            f00, f01, f10, f11, f20, f21, f30, f31, f40, f41,
            eid, // XXXX_XXX4_4444_3333 _ 3222_2211_1110_0000 (mask: which edge to use for face)
            e0, e1, e2, e3, e4,
            v;
        */
        public void FillBuffer(SubSurfMod scr, ref oriBuffer b)
        {
            int fl = fp.Length;
            List<FaceInfo> fn = new List<FaceInfo>();
            List<int> fvs = new List<int>();
            Dictionary<int, int> fid = new Dictionary<int, int>();
            foreach (int f in fp)
                fn.Add(scr.oriFaces[System.Array.Find(scr.subVertsF, x => x.id == f).fIndex]);
            b.n = (fl << 20) + ((fl > 4 ? fn[4].sides : 0) << 16) + ((fl > 3 ? fn[3].sides : 0) << 12) + (fn[2].sides << 8) + (fn[1].sides << 4) + fn[0].sides;
            for (int a = 0; a < fl; a++)
            {
                foreach (int i in fn[a].verts)
                {
                    if (!fid.ContainsKey(i)) 
                    {
                        fvs.Add(i);
                        fid[i] = a;
                    }
                    else
                    {
                        fvs.Remove(i);
                    }
                }
            }
            FillVF(fn[0].verts, fvs, ref b.f00, ref b.f01);
            FillVF(fn[1].verts, fvs, ref b.f10, ref b.f11);
            FillVF(fn[2].verts, fvs, ref b.f20, ref b.f21);
            if (fn.Count > 3)
            {
                FillVF(fn[3].verts, fvs, ref b.f30, ref b.f31);
                if (fn.Count > 4)
                    FillVF(fn[4].verts, fvs, ref b.f40, ref b.f41);
            }
            int mask = 0;
            for (int a = 0; a < fl; a++) //for each face
            {
                for (int q = 0; q < fl; q++) //for each edge
                {
                    if (System.Array.Exists(fn[a].verts, x => x == ep[q]))
                    {
                        mask |= 1 << q;
                    }
                }
                b.eid |= mask << (a * 5);
                mask = 0;
            }
            b.e0 = ep[0];
            b.e1 = ep[1];
            b.e2 = ep[2];
            if (ep.Length > 3)
            {
                b.e3 = ep[3];
                if (ep.Length > 4)
                    b.e4 = ep[4];
            }
            b.v = op;
        }

        void FillVF(int[] vs, List<int> fvs, ref int a, ref int b)
        {
            bool done1 = false;
            foreach (int q in vs)
            {
                if (fvs.Contains(q))
                {
                    if (done1)
                    {
                        a = q;
                        continue;
                    }
                    else
                    {
                        b = q;
                        done1 = true;
                    }
                }
            }
        }
    }
    public class SS_EdgeV //type 1
	{
		public int id;
		public int ve1, ve2, vf1, vf2;
        public void GetPos(SubSurfMod scr)
		{
			scr.finalVerts[id] = 0.25f * (scr.oriVerts[ve1].pos + scr.oriVerts[ve2].pos + scr.finalVerts[vf1] + scr.finalVerts[vf2]);
            Debug.DrawLine (scr.finalMeshF.transform.TransformPoint(scr.finalVerts [id]) - Vector3.up * 0.2f, scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) + Vector3.up * 0.2f, Color.red, 10);
            Debug.DrawLine(scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) - Vector3.right * 0.2f, scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) + Vector3.right * 0.2f, Color.red, 10);
            Debug.DrawLine(scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) - Vector3.forward * 0.2f, scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) + Vector3.forward * 0.2f, Color.red, 10);
        }

        /*
        ep uses:
          int n; //XXXX_TYPE_XXXX_XXXX _ XXXX_XXXX_1111_0000
          int f00, f01, f20(as f02), f10, f11, f21(as f12);
          int e0, e1;
        */
        public void FillBuffer(SubSurfMod scr, ref oriBuffer b)
        {
            List<int> fvs = new List<int>();
            FaceInfo f1 = scr.oriFaces[System.Array.Find(scr.subVertsF, x => x.id == vf1).fIndex];
            FaceInfo f2 = scr.oriFaces[System.Array.Find(scr.subVertsF, x => x.id == vf2).fIndex];
            b.n = (1 << 24) + (f2.sides << 4) + f1.sides;
            foreach (int i in f1.verts)
            {
                if (i != ve1 && i != ve2)
                    fvs.Add(i);
            }
            b.f00 = fvs[0];
            if (fvs.Count > 3)
            {
                b.f01 = fvs[1];
                if (fvs.Count > 4)
                    b.f20 = fvs[2];
            }
            fvs.Clear();
            foreach (int i in f2.verts)
            {
                if (i != ve1 && i != ve2)
                    fvs.Add(i);
            }
            b.f10 = fvs[0];
            if (fvs.Count > 3)
            {
                b.f11 = fvs[1];
                if (fvs.Count > 4)
                    b.f21 = fvs[2];
            }
            b.e0 = ve1;
            b.e1 = ve2;
        }
    }

    public class SS_FaceV //type 2
	{
		public int id;
		public int fIndex;
		public void GetPos(SubSurfMod scr)
		{
			Vector3 x = Vector3.zero;
			FaceInfo f = scr.oriFaces[fIndex];
			foreach (int a in f.verts)
			{
				x += scr.oriVerts[a].pos;
			}
			scr.finalVerts[id] = x / f.sides;
			Debug.DrawLine (scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) - Vector3.up * 0.2f, scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) + Vector3.up * 0.2f, Color.blue, 10);
            Debug.DrawLine(scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) - Vector3.right * 0.2f, scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) + Vector3.right * 0.2f, Color.blue, 10);
            Debug.DrawLine(scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) - Vector3.forward * 0.2f, scr.finalMeshF.transform.TransformPoint(scr.finalVerts[id]) + Vector3.forward * 0.2f, Color.blue, 10);
        }

        /*
        fp uses:
         int n; //XXXX_TYPE_XXXX_XXXX _ XXXX_XXXX_XXXX_NNNN
         int f00, f01, f10(as f02), f11(as f03), f20(as f04);
         */
        public void FillBuffer(SubSurfMod scr, ref oriBuffer b)
        {
            FaceInfo f = scr.oriFaces[fIndex];
            b.n = (2 << 24) + f.sides;
            b.f00 = f.verts[0];
            b.f01 = f.verts[1];
            b.f10 = f.verts[2];
            if (f.sides > 3)
            {
                b.f11 = f.verts[3];
                if (f.sides > 4)
                    b.f20 = f.verts[4];
            }
        }
    }

    public struct oriBuffer
    {
        public int n; //XXXX_TYPE_NNNN_4444 _ 3333_2222_1111_0000
        public int f00, f01, f10, f11, f20, f21, f30, f31, f40, f41;
        public int eid;
        public int e0, e1, e2, e3, e4;
        public int v;
        public int unused1, unused2;
    };
}