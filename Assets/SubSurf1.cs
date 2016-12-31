using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SubSurf1 : MonoBehaviour {

    public VertexInfo[] oriVerts;
    public FaceInfo[] oriFaces;

    public SS_FaceV[] subVertsF;
    public SS_EdgeV[] subVertsE;
    public SS_OriV[] subVertsV;

    public Vector3[] finalVerts;
    public Vector3[] finalNorms;
    public int[] finalFaces;

    public Mesh finalMesh;
    public MeshFilter mf;
	public Mesh prefabMesh;

	// Use this for initialization
	void Start () {
		//System.Diagnostics.Process.Start ("cmd.exe", "/K 123");
		StartSS();
	}

	void StartSS () {
        finalMesh = new Mesh();
        mf.mesh = finalMesh;
        //try with normal cube
        oriVerts = new VertexInfo[8];
		for (int y = 0; y < 8; y++)
			oriVerts [y] = new VertexInfo ();
        oriVerts[0].pos = new Vector3(0, 0, 0);
        oriVerts[1].pos = new Vector3(2, 0, 0);
        oriVerts[2].pos = new Vector3(0, 0, 2);
        oriVerts[3].pos = new Vector3(2, 0, 2);
        oriVerts[4].pos = new Vector3(0, 2, 0);
        oriVerts[5].pos = new Vector3(2, 2, 0);
        oriVerts[6].pos = new Vector3(0, 2, 2);
        oriVerts[7].pos = new Vector3(2, 2, 2);

        oriVerts[0].connIndexs = new int[] { 1, 2, 4 };
        oriVerts[1].connIndexs = new int[] { 0, 3, 5 };
		oriVerts[2].connIndexs = new int[] { 0, 3, 6 };
        oriVerts[3].connIndexs = new int[] { 1, 2, 7 };
        oriVerts[4].connIndexs = new int[] { 0, 5, 6 };
        oriVerts[5].connIndexs = new int[] { 1, 4, 7 };
        oriVerts[6].connIndexs = new int[] { 2, 4, 7 };
        oriVerts[7].connIndexs = new int[] { 3, 5, 6 };

        oriFaces = new FaceInfo[6];
		for (int a = 0; a < 6; a++) {
			oriFaces [a] = new FaceInfo ();
			oriFaces [a].sides = 4;
		}
        oriFaces[0].verts = new int[] { 0, 3, 2, 1 };
        oriFaces[1].verts = new int[] { 0, 1, 5, 4 };
        oriFaces[2].verts = new int[] { 1, 3, 7, 5 };
        oriFaces[3].verts = new int[] { 2, 3, 7, 6 };
        oriFaces[4].verts = new int[] { 0, 4, 6, 2 };
        oriFaces[5].verts = new int[] { 4, 5, 7, 6 };


        CreateSS();
        UpdateSS();
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
		print (i + " " + i2 + " " + i3);

        List<int[]> fBuff = new List<int[]>();
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
                    if (ArrayHas(faceVInfo[w], edgeVInfo[e][0]) && ArrayHas(faceVInfo[w], edgeVInfo[e][1]))
                    {
                        fBuff.Add(new int[] { w, e, connVerts[v] });
                        edgesOfF[w].Add(e);
                    }
                }
            }
        }

        subVertsF = new SS_FaceV[i3 - i2];
        subVertsE = new SS_EdgeV[i - i3];
        subVertsV = new SS_OriV[i2];
        finalVerts = new Vector3[i]; //update later
        finalFaces = new int[fBuff.Count * 3];

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
                if (ArrayHas(faceVInfo[ww], w))
                {
                    l.Add(ww);
                }
            }
            if (l.Count != subVertsV[w].n)
                Debug.LogError("vert " + w + " fp found " + l.Count + " n=" + subVertsV[w].n);
            subVertsV[w].fp = l.ToArray();
        }
        for (int t = fBuff.Count-1; t >= 0; t--) {
            finalFaces[t*3] = fBuff[t][0];
			finalFaces[t*3+1] = fBuff[t][1];
			finalFaces[t*3+2] = fBuff[t][2];
        }
        
        UpdateSS();
    }

    // Update is called once per frame
    void Update () {
		
	}

    void UpdateSS ()
    {
        finalMesh.Clear();
        foreach (SS_FaceV f in subVertsF)
            f.GetPos(this);
        foreach (SS_EdgeV e in subVertsE)
            e.GetPos(this);
        foreach (SS_OriV v in subVertsV)
            v.GetPos(this);

        finalMesh.vertices = finalVerts;
        finalMesh.triangles = finalFaces;
        //finalMesh.RecalculateNormals();
        finalMesh.RecalculateBounds();


    }


    public bool ArrayHas<T> (IList<T> a, T t)
    {
        foreach (T q in a)
        {
            if (EqualityComparer<T>.Default.Equals(q, t))
                return true;
        }
        return false;
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

	public class SS_OriV
	{
		public int id;
		public int[] fp;
		public int op;
		public int n;

		public void GetPos(SubSurf1 scr) //(F + 2R + (n-3)P) / n
		{
			Vector3 _f = Vector3.zero;
			Vector3 _r = Vector3.zero;
			foreach (int f in fp)
				_f += scr.finalVerts[f];
			_f = _f / n;
			VertexInfo v = scr.oriVerts[op];
			foreach (int e in v.connIndexs)
				_r += 0.5f*(scr.oriVerts[e].pos + scr.oriVerts[id].pos);
			_r = _r / n;
			//
			//scr.finalVerts [id] = scr.oriVerts [id].pos;
			scr.finalVerts[id] = (_f + 2 * _r + (n - 3) * v.pos) / n;

			Debug.DrawLine (scr.finalVerts [id], scr.finalVerts [id] + Vector3.up * 0.5f, Color.green, 10);
		}
	}

	public class SS_EdgeV
	{
		public int id;
		public int ve1, ve2, vf1, vf2;
		public void GetPos(SubSurf1 scr)
		{
			scr.finalVerts[id] = 0.25f * (scr.oriVerts[ve1].pos + scr.oriVerts[ve2].pos + scr.finalVerts[vf1] + scr.finalVerts[vf2]);
			Debug.DrawLine (scr.finalVerts [id], scr.finalVerts [id] + Vector3.up * 0.5f, Color.red, 10);
		}
	}

	public class SS_FaceV
	{
		public int id;
		public int fIndex;
		public void GetPos(SubSurf1 scr)
		{
			Vector3 x = Vector3.zero;
			FaceInfo f = scr.oriFaces[fIndex];
			foreach (int a in f.verts)
			{
				x += scr.oriVerts[a].pos;
			}
			scr.finalVerts[id] = x / f.sides;

			Debug.DrawLine (scr.finalVerts [id], scr.finalVerts [id] + Vector3.up * 0.5f, Color.blue, 10);
		}
	}
}