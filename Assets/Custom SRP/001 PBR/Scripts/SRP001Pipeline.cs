using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
public class SRP001Pipeline : RenderPipeline
{
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(context, cameras);
        foreach (Camera camera in cameras)
        {
            BeginCameraRendering(context, camera);
            //剔除
            if (!camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParams))
            {
                continue;
            }
            //剔除结果
            m_cullingResults = context.Cull(ref cullingParams);
            //为context设置当前摄像机的视图投影矩阵
            context.SetupCameraProperties(camera);
            //顺序：不透明->天空盒->(透明)
            var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
            //绘制设定
            var drawingSettings = new DrawingSettings(s_shaderTagId, sortingSettings);
            //过滤设定			
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            context.DrawRenderers(m_cullingResults, ref drawingSettings, ref filteringSettings);
            //绘制天空盒
            context.DrawSkybox(camera);
            //灯光相关
            SetupLights();
            //执行缓冲区
            context.ExecuteCommandBuffer(m_buffer);
            //执行完后，如果不用了，要清除
            m_buffer.Clear();
            //提交渲染指令
            context.Submit();
            //着色器标签ID用于引用着色器中的各种名称。
            EndCameraRendering(context, camera);
        }
        EndFrameRendering(context, cameras);
    }

    void SetupLights()
    {
        NativeArray<VisibleLight> visibleLights = m_cullingResults.visibleLights;
        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            if (visibleLight.lightType == LightType.Directional)
            {
                SetupDirectionalLight(dirLightCount++, ref visibleLight);
                if (dirLightCount >= s_maxDirLightCount)
                {
                    break;
                }
            }
        }
        m_buffer.SetGlobalInt(s_dirLightCountId, visibleLights.Length);
        m_buffer.SetGlobalVectorArray(s_dirLightColorsId, s_dirLightColors);
        m_buffer.SetGlobalVectorArray(s_dirLightDirectionsId, s_dirLightDirections);
    }

    /// <summary>
    /// 根据index设置灯光
    /// </summary>
    /// <param name="index"></param>
    /// <param name="visibleLight"></param>
	void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
    {
        s_dirLightColors[index] = visibleLight.finalColor;
        s_dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
    }

    #region 字段

    /// <summary>
    /// 着色器标签ID用于引用着色器中的各种名称，"SPR001_PBR_Pass"
    /// </summary>
    /// <returns></returns>
    private static ShaderTagId s_shaderTagId = new ShaderTagId("SPR001_PBR_Pass");

    /// <summary>
    /// 最大支持的灯光数量
    /// </summary>
    static int s_maxDirLightCount = 4;


    static int
    s_dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
    s_dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
    s_dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");

    static Vector4[]
        s_dirLightColors = new Vector4[s_maxDirLightCount],
        s_dirLightDirections = new Vector4[s_maxDirLightCount];

    /// <summary>
    /// 剔除结果
    /// /// </summary>
    private CullingResults m_cullingResults;

    /// <summary>
    /// buffer的名字
    /// </summary>
    const string m_bufferName = "Render Camera";

    /// <summary>
    /// Command Buffer
    /// </summary>
    /// <value></value>
    private CommandBuffer m_buffer = new CommandBuffer
    {
        name = m_bufferName
    };


    #endregion
}
