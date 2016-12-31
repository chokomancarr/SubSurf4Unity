using UnityEngine;

public class SubSurfWrite : MonoBehaviour {
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
            else if (m.shader.name != "Unlit/ApplySS")
                Debug.LogWarning("shader not supported!");
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

    public void SetBuffer (SubSurfMod.oriBuffer[] buff) 
    {
        hasCb = true;
        cb = new ComputeBuffer(buff.Length, 640);
        cb.SetData(buff);
        foreach (Material m in mat)
            m.SetBuffer("ssBuffer", cb);
        //cb.Release();
    }

    void OnApplicationQuit()
    {
        if (hasCb)
            cb.Release();
    }
}
