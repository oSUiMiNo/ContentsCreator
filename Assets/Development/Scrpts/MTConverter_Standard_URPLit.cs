using System.Collections.Generic;
using UnityEngine;

public class MTConverter_Standard_URPLit : MonoBehaviour
{
    [SerializeField]
    public List<GameObject> targets; // 対象となるオブジェクトの配列
    [SerializeField]
    public Shader urpShader => Shader.Find("Universal Render Pipeline/Lit"); // URPへ切り替えるためのシェーダー
    [SerializeField]
    public bool toExport = false;

    void Start()
    {
        string[,] propNames_Orig_Dest_Array =
        {
                { "_Color", "_BaseColor "},
                { "_MainTex","_BaseMap" },

                { "_Cutoff", "_Cutoff" },

                { "_Glossiness",  "_Smoothness" },
                { "_GlossMapScale", "NONE" }, //片方無し
                { "_SmoothnessTextureChannel", "_SmoothnessTextureChannel" },

                { "_Metallic", "_Metallic" },
                { "_MetallicGlossMap", "_MetallicGlossMap" },

                //{ "_Metallic", "_SpecColor" },
                { "_MetallicGlossMap", "_SpecGlossMap" },

                { "_SpecularHighlights", "_SpecularHighlights" },
                { "_GlossyReflections", "_EnvironmentReflections" },

                { "_BumpMap", "_BumpMap" },
                { "_BumpScale", "_BumpScale" },

                { "_Parallax", "_Parallax" },
                { "_ParallaxMap", "_ParallaxMap" },

                { "_OcclusionStrength", "_OcclusionStrength" },
                { "_OcclusionMap", "_OcclusionMap" },

                { "_EmissionColor", "_EmissionColor" },
                { "_EmissionMap", "_EmissionMap" },

                { "_DetailMask", "_DetailMask" },
                { "_DetailAlbedoMap", "_DetailAlbedoMap" },
                { "NONE", "_DetailAlbedoMapScale" }, //片方無し
                { "_DetailNormalMap", "_DetailNormalMap" },
                { "_DetailNormalMapScale", "_DetailNormalMapScale" },

                { "_ShadeColor", "_ShadeColor" },
                { "_ShadeTexture","_ShadeTex" },
        };
        targets.ForEach(a =>
        {
            new MTConverter(a, urpShader)
            {
                propNames_Orig_Dest_Array = propNames_Orig_Dest_Array,
                toExport = this.toExport
            }.Execute();
        });
    }
}
