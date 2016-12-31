using UnityEngine;

public class SubSurfWrite1 : MonoBehaviour {
    public Material[] mat;
    public Renderer rend;
    public ComputeBuffer cb;
    bool hasCb;

    void Awake()
    {
        mat = rend.materials;
        foreach (Material m in mat)
        {
            if (!m.shader.isSupported)
                Debug.LogWarning("shader not supported!");
            else if (m.shader.name != "Unlit/ApplyBuffer")
                Debug.LogWarning("wrong shader!");
        }
    }

    public void SetPosTex(RenderTexture rt, int size)
    {
        foreach (Material m in mat)
        {
            m.SetTexture("_oriTex", rt);
            m.SetInt("_oriTexSize", size);
        }
    }

    public void SetBuffer (SubSurfRead.oriBuffer[] buff)
    {
        if (!hasCb)
        {
            cb = new ComputeBuffer(buff.Length, 16);
            foreach (Material m in mat)
                m.SetBuffer("ssBuffer", cb);
        }
        hasCb = true;
        cb.SetData(buff);
        //Debug.Log("buffer wrote: " + buff.Length);
    }

    void OnApplicationQuit()
    {
        if (hasCb)
            cb.Release();
    }
}
