using UnityEngine;
using UnityEngine.Rendering;
public partial class CameraRenderer 
{
	/// <summary>
	/// 渲染当前摄像机看到的物体
	/// </summary>
	/// <param name="context"></param>
	/// <param name="camera"></param>
	public void Render(
			ScriptableRenderContext context, 
			Camera camera,
			bool useDynamicBatching, 
			bool useGPUInstancing,
			ShadowSettings shadowSettings
	)
	{
		//设置当前上下文
		this.context = context;
		//设置摄像机
		this.camera = camera;
		PrepareBuffer();
		PrepareForSceneWindow();
		//如果剔除失败，返回
		if (!cull(shadowSettings.maxDistance))
        {
			return;
        }	
		buffer.BeginSample(SampleName);
		ExecuteBuffer();
		//设置灯光信息
		lighting.Setup(context, cullingResults, shadowSettings);
		buffer.EndSample(SampleName);		
		//渲染前准备
		Setup();
		//绘制可见物体
		DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
		//绘制不支持pass的物体
		DrawUnsupportedShaders();
		//绘制Gizmos
		DrawGizmos();
		lighting.Cleanup();
		//提交context
		Submit();
	}
	
	/// <summary>
	/// 渲染前准备：
	/// </summary>
	void Setup()
    {
		//1:设置摄像机参数：上下文中设置view-projection matrix.
		context.SetupCameraProperties(camera);
		CameraClearFlags flags = camera.clearFlags;
		//清除render target，清除旧内容。
		//第一个第二个参数分别是the depth and color data should be cleared
		//第三个参数是用于清除的颜色，Color.clear
		buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color, flags == CameraClearFlags.Color ?
				camera.backgroundColor.linear : Color.clear);
		//buffer显示在profiler和frame debugger中
		buffer.BeginSample(SampleName);
		//执行清除工作
		lighting.Cleanup();
		// 执行缓冲区
		ExecuteBuffer();
	}

	/// <summary>
	/// 提交上下文
	/// </summary>
	void Submit()
    {
		//buffer停止显示在profiler和frame debugger中
		buffer.EndSample(SampleName);
		// 执行缓冲区
		ExecuteBuffer();
		//上下文提交
		context.Submit();
    }

	/// <summary>
	/// 执行缓冲区
	/// </summary>
	void ExecuteBuffer()
    {
		//执行缓冲区
		context.ExecuteCommandBuffer(buffer);
		//执行完后，如果不用了，要清除
		buffer.Clear();
    }


	/// <summary>
	/// 绘制可见物体
	/// </summary>
	void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
		//绘制不透明->天空盒->透明的物体
		//sortingSettings：摄像机用作决定是否正交或者是否应用基于距离的排序
		var sortingSettings = new SortingSettings(camera)
		{
			criteria = SortingCriteria.CommonOpaque
		};
		//结构：drawingSettings,
		//第一个参数是
		var drawingSettings = new DrawingSettings(unlitShaderTagId,sortingSettings) {
			enableDynamicBatching = useDynamicBatching,
			enableInstancing = useGPUInstancing
		};
		drawingSettings.SetShaderPassName(1, litShaderTagId);
		//过滤设置 filteringSettings
		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
		context.DrawRenderers(cullingResults,ref drawingSettings,ref filteringSettings);
		//绘制天空盒，但这只是决定这个摄像机是否要渲染天空盒，摄像机的参数不能影响天空盒
		context.DrawSkybox(camera);
		//透明物体setting：
		sortingSettings.criteria = SortingCriteria.CommonTransparent;
		drawingSettings.sortingSettings = sortingSettings;
		filteringSettings.renderQueueRange = RenderQueueRange.transparent;
		//渲染物体使用context.DrawRenderers
		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}
	
	/// <summary>
	/// 是否剔除：剔除成功并且结果存在了字段里
	/// </summary>
	/// <returns></returns>
	bool cull(float maxShadowDistance)
    {
		//keep track of multiple camera settings and matrices
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
			p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
			cullingResults = context.Cull(ref p);
			return true;
        }
		return false;
    }

	/// <summary>
	/// we also have to indicate which kind of shader passes are allowed.
	/// </summary>
	static ShaderTagId 
	unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
	litShaderTagId = new ShaderTagId("CustomLit");

	/// <summary>
	/// 交给GPU渲染的上下文
	/// </summary>
	ScriptableRenderContext context;

	/// <summary>
	/// 剔除结果
	/// </summary>
	CullingResults cullingResults;

	/// <summary>
	/// 当前渲染的摄像机
	/// </summary>
	Camera camera;

	/// <summary>
	/// buffer的名字
	/// </summary>
	const string bufferName = "Render Camera";

	/// <summary>
	/// CommandBuffer
	/// </summary>
	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};

	Lighting lighting = new Lighting();

}
