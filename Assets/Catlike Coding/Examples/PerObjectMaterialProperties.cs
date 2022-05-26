using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
	void Awake () {
		OnValidate();
	}
	/// <summary>
	/// gets invoked in the Unity editor when the component is loaded or changed.
	/// </summary>
    void OnValidate () {
		if (block == null) {
			block = new MaterialPropertyBlock();
		}
		block.SetColor(baseColorId, baseColor);
		block.SetFloat(cutoffId, cutoff);
		block.SetFloat(metallicId, metallic);
		block.SetFloat(smoothnessId, smoothness);
		GetComponent<Renderer>().SetPropertyBlock(block);
	}

	static int baseColorId = Shader.PropertyToID("_BaseColor");
	static int cutoffId = Shader.PropertyToID("_Cutoff");
	static int metallicId = Shader.PropertyToID("_Metallic");
	static int smoothnessId = Shader.PropertyToID("_Smoothness");

    static MaterialPropertyBlock block;

	/// <summary>
	/// 每个球不同的颜色
	/// </summary>
	[SerializeField]
    Color baseColor = Color.white;

	[SerializeField, Range(0f, 1f)]
	float cutoff = 0.5f, metallic = 0f, smoothness = 0.5f;


}
