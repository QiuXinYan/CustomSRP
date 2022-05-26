using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
	/// <summary>
	/// 初始化的时候enable the SRP batcher
	/// </summary>
	public CustomRenderPipeline (bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, ShadowSettings shadowSettings) {
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
		GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
		GraphicsSettings.lightsUseLinearIntensity = true;
		this.shadowSettings = shadowSettings;
	}

	/// <summary>
	///  use it to render all cameras in a loop.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="cameras"></param>
	protected override void Render(ScriptableRenderContext context, Camera[] cameras)  {
		foreach(Camera camera in cameras)
        {
			renderer.Render(context, camera, useDynamicBatching, useGPUInstancing, shadowSettings);
        }
	}

	/// <summary>
	/// create an instance of the renderer when it gets created
	/// </summary>
	CameraRenderer renderer = new CameraRenderer();

	bool useDynamicBatching, useGPUInstancing;

	/// <summary>
	/// 阴影设置
	/// </summary>
	ShadowSettings shadowSettings;

}
