using UnityEngine;

public class SimpleAnimator : MonoBehaviour {

    public SkinnedMeshRenderer r;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        r.SetBlendShapeWeight(0, Mathf.Sin(Time.time * 3) * 50 + 50);
	}
}
