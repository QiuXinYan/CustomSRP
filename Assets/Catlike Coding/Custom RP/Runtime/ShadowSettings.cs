using UnityEngine;

[System.Serializable]
public class ShadowSettings {
    
	/// <summary>
	/// 平行光阴影图设置
	/// </summary>
    [System.Serializable]
	public struct Directional {

		public TextureSize atlasSize;
	}

	/// <summary>
	/// 实例化一个阴影图
	/// </summary>
	/// <value></value>
	public Directional directional = new Directional {
		atlasSize = TextureSize._1024
	};

	/// <summary>
	/// 用enum结构定义允许范围内的纹理大小
	/// </summary>
	public enum TextureSize {
		_256 = 256, _512 = 512, _1024 = 1024,
		_2048 = 2048, _4096 = 4096, _8192 = 8192
	}
    
	/// <summary>
	/// 渲染的距离
	/// </summary>
    [Min(0f)]
	public float maxDistance = 100f;
}
