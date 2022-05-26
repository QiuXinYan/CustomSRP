using UnityEngine;
using UnityEngine.Rendering;


public class Shadows
{

    /// <summary>
    /// 准备阶段
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cullingResults"></param>
    /// <param name="settings"></param>
	public void Setup(
        ScriptableRenderContext context,
        CullingResults cullingResults,
        ShadowSettings settings
    )
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = settings;
        ShadowedDirectionalLightCount = 0;
    }

    /// <summary>
    /// 执行缓冲后要立即清除
    /// </summary>
	void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    /// <summary>
    /// figure out which light gets shadows
    /// 它的工作是在阴影地图集中为shadow map预留空间(是否达到maxShadowedDirectionalLightCount)，并存储渲染它们所需的信息。
    /// </summary>
    /// <param name="light">光</param>
    /// <param name="visibleLightIndex">可见光索引参数</param>
    public Vector2 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount
            && light.shadows != LightShadows.None
            && light.shadowStrength > 0f &&
            cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {//最后一个条件是直接设置它不影响
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] =
                new ShadowedDirectionalLight
                {
                    visibleLightIndex = visibleLightIndex
                };
                return new Vector2(
				light.shadowStrength, ShadowedDirectionalLightCount++
			);
        }
        return Vector2.zero;
    }

    /// <summary>
    /// 渲染阴影
    /// </summary>
    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            buffer.GetTemporaryRT(
                dirShadowAtlasId, 1, 1,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
            );
            buffer.SetRenderTarget(
                dirShadowAtlasId,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            buffer.ClearRenderTarget(true, false, Color.clear);
            ExecuteBuffer();
        }
    }

    /// <summary>
    /// 渲染阴影的具体方法
    /// </summary>
	void RenderDirectionalShadows()
    {
        int atlasSize = (int)settings.directional.atlasSize;
        buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        buffer.SetRenderTarget(
            dirShadowAtlasId,
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
        );
        buffer.ClearRenderTarget(true, false, Color.clear);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        int split = ShadowedDirectionalLightCount <= 1 ? 1 : 2;
        int tileSize = atlasSize / split;
        //遍历所有灯光，渲染
        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }


    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
		float scale = 1f / split;
		m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
		m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
		m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
		m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
		m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
		m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
		m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
		m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        return m;
    }

    /// <summary>
    /// 调整渲染视图的大小
    /// </summary>
    /// <param name="index"></param>
    /// <param name="split"></param>
    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(
         offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index">the shadowed light index</param>
    /// <param name="tileSize">the size of its tile in the atlas</param>
    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        var shadowSettings =
            new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        //arg0: light的索引
        //arg1-3: 两个整数和一个vector3：控制级联阴影
        //arg4：纹理大小，
        //arg5: 平面近平面附近的阴影
        //arg6：输出参数，视图矩阵
        //arg7: 输出参数，投影矩阵
        //arg8: 输出参数，ShadowSplitData 结构
        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
            light.visibleLightIndex, 0, 1, Vector3.zero, tileSize, 0f,
            out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
            out ShadowSplitData splitData
        );
        shadowSettings.splitData = splitData;

        dirShadowMatrices[index] = ConvertToAtlasMatrix(
            projectionMatrix * viewMatrix,
            SetTileViewport(index, split, tileSize), split
        );
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
    }

    /// <summary>
    ///  release it when we're done with it. 
    /// </summary>
    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }

    #region 字段

    /// <summary>
    /// Command Buffer的名字
    /// </summary>
    const string bufferName = "Shadows";

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    /// <summary>
    /// 上下文
    /// </summary>
	ScriptableRenderContext context;

    /// <summary>
    /// 剔除结果
    /// </summary>
	CullingResults cullingResults;

    /// <summary>
    /// 阴影参数设置
    /// </summary>
	ShadowSettings settings;

    /// <summary>
    /// 最多能够产生阴影的灯光数量
    /// </summary>
    const int maxShadowedDirectionalLightCount = 4;

    /// <summary>
    /// 产生阴影的平行光：记录灯光的序索引
    /// </summary>
	struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
    }

    /// <summary>
    /// 跟踪能够投射阴影的光源
    /// </summary>
	ShadowedDirectionalLight[] ShadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

    /// <summary>
    /// 已经预留空间的灯光的数量
    /// </summary>
    private int ShadowedDirectionalLightCount;

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    static int
        dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
        dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");

    /// <summary>
    /// 
    /// </summary>
    static Matrix4x4[]
    dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount];


    #endregion
}
