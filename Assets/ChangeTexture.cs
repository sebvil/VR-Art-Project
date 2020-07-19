using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeTexture : MonoBehaviour
{
    GameObject paper;
    // Start is called before the first frame update
    void Start()
    {
        Renderer rend = paper.GetComponent<Renderer>();
        RenderTexture tex = new RenderTexture(1024,1024, 24, RenderTextureFormat.ARGB32);
        rend.material.mainTexture = tex;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
