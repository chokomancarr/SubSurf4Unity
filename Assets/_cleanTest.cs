using UnityEngine;
using System.Runtime.InteropServices;

public class _cleanTest : MonoBehaviour {

    public Renderer r;

    struct buffer
    {
        public int x, y, z, w;
    }

    ComputeBuffer buff;

	// Use this for initialization
	void Start () {
        buff = new ComputeBuffer(5, Marshal.SizeOf(typeof(buffer)));
        buffer[] data = new buffer[5];
        buff.SetData(data);
        r.material.SetBuffer("buff", buff);
        Graphics.SetRandomWriteTarget(1, buff);
	}

    void OnPostRender()
    {
        buffer[] data = new buffer[5];
        buff.GetData(data);
        print(str(data[0]) + " " + str(data[1]));
    }

    string str (buffer b)
    {
        return b.x + "," + b.y + "," + b.z + "," + b.w;
    }

    void OnApplicationQuit ()
    {
        buff.Release();
    }
}
