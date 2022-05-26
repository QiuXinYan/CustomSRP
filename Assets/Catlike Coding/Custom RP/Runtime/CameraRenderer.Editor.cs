using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine.Profiling;
partial class CameraRenderer
{
	partial void DrawGizmos();

	/// <summary>
	/// 绘制不支持pass的物体
	/// </summary>
	partial void DrawUnsupportedShaders();
	partial void PrepareForSceneWindow();

	/// <summary>
	/// an editor-only PrepareBuffer method that makes the buffer's name equal to the camera's.
	/// </summary>
	partial void PrepareBuffer();
	
//The content of the editor part only needs to exist in the editor, so make it conditional on
#if UNITY_EDITOR
	string SampleName { get; set; }
	
	/// <summary>
	/// 
	/// </summary>
	partial void PrepareForSceneWindow()
	{
		if (camera.cameraType == CameraType.SceneView)
		{
			ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
		}
	}
	partial void PrepareBuffer()
	{
		Profiler.BeginSample("Editor Only");
		buffer.name = SampleName = camera.name;
		Profiler.EndSample();
	}


	/// <summary>
	/// 
	/// </summary>
	partial void DrawGizmos () {
		if (Handles.ShouldRenderGizmos()) {
			// 绘制Gizmos, for before and after image effects. 
			context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
			context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
		}
	}

	/// <summary>
	/// 绘制所有不支持的着色器
	/// </summary>
	partial void DrawUnsupportedShaders()  
	{
		//如果错误材质为空，设置一个新的材质
		if (errorMaterial == null)
		{
			errorMaterial =
				new Material(Shader.Find("Hidden/InternalErrorShader"));
		}

		var drawingSettings = new DrawingSettings(
			legacyShaderTagIds[0], new SortingSettings(camera)
		)
		{
			overrideMaterial = errorMaterial
		};
		// draw multiple passes by invoking SetShaderPassName 
		for (int i = 1; i < legacyShaderTagIds.Length; i++)
		{
			drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
		}
		//可以通过 FilteringSettings.defaultValue 属性获得默认的过滤设置
		var filteringSettings = FilteringSettings.defaultValue;
		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings 
		);
	}

	/// <summary>
	/// 错误的材质
	/// </summary>
	static Material errorMaterial;

	/// <summary>
	/// 使用以下pass的对象会被渲染，使得它们可见
	/// </summary>
	static ShaderTagId[] legacyShaderTagIds = {
		new ShaderTagId("Always"),
		new ShaderTagId("ForwardBase"),
		new ShaderTagId("PrepassBase"),
		new ShaderTagId("Vertex"),
		new ShaderTagId("VertexLMRGBM"),
		new ShaderTagId("VertexLM")
	};

#else
	const string SampleName = bufferName;
#endif
}