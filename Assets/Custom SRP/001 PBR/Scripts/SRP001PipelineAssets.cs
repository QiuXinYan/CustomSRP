using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName ="Custom Assets/Custom Render Pipeline/001 PBR")]
public class SRP001PipelineAssets : RenderPipelineAsset
{
   
    protected override RenderPipeline CreatePipeline()
    {
        return new SRP001Pipeline();
    }
    
}
