using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


[CreateAssetMenu(menuName ="Custom Assets/Custom Render Pipeline/002 TAA")]
public class SRP002PipelineAssets : RenderPipelineAsset
{
  
    protected override RenderPipeline CreatePipeline()
    {
        return new SRP002Pipeline();
    }
    
}
