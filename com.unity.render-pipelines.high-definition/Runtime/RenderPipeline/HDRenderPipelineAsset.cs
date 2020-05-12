using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    enum ShaderVariantLogLevel
    {
        Disabled,
        OnlyHDRPShaders,
        AllShaders,
    }

    /// <summary>
    /// High Definition Render Pipeline asset.
    /// </summary>
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "HDRP-Asset" + Documentation.endURL)]
    public partial class HDRenderPipelineAsset : RenderPipelineAsset, IVirtualTexturingEnabledRenderPipeline
    {
        [System.NonSerialized]
        internal bool isInOnValidateCall = false;

        HDRenderPipelineAsset()
        {
            
        }

        void Reset() => OnValidate();
        
        /// <summary>
        /// CreatePipeline implementation.
        /// </summary>
        /// <returns>A new HDRenderPipeline instance.</returns>
        protected override RenderPipeline CreatePipeline() 
        {
            if(m_DefaultSettings is null)
                m_DefaultSettings = HDRenderPipeline.CreateDefaultSettings() as HDDefaultSettings;
            return new HDRenderPipeline(this, defaultSettings);
        }

        /// <summary>
        /// OnValidate implementation.
        /// </summary>
        protected override void OnValidate()
        {
            isInOnValidateCall = true;

            //Do not reconstruct the pipeline if we modify other assets.
            //OnValidate is called once at first selection of the asset.
            if (HDRenderPipeline.currentAsset == this)
                base.OnValidate();

            UpdateRenderingLayerNames();

               isInOnValidateCall = false;
        }

        internal HDDefaultSettings m_DefaultSettings;
        public HDDefaultSettings defaultSettings {
            get 
            {             //TODOJENNY only one active at a time
                if(m_DefaultSettings is null)
                    m_DefaultSettings = HDRenderPipeline.CreateDefaultSettings() as HDDefaultSettings;
                return m_DefaultSettings;
            }

            set { m_DefaultSettings = value; }
        }

        internal RenderPipelineResources renderPipelineResources
        {
            get { return defaultSettings.renderPipelineResources; }
            set { defaultSettings.renderPipelineResources = value; }
        }
        /*
        // To be able to turn on/off FrameSettings properties at runtime for debugging purpose without affecting the original one
        // we create a runtime copy (m_ActiveFrameSettings that is used, and any parametrization is done on serialized frameSettings)
        [SerializeField]
        FrameSettings m_RenderingPathDefaultCameraFrameSettings = FrameSettings.NewDefaultCamera();

        [SerializeField]
        FrameSettings m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings = FrameSettings.NewDefaultCustomOrBakeReflectionProbe();

        [SerializeField]
        FrameSettings m_RenderingPathDefaultRealtimeReflectionFrameSettings = FrameSettings.NewDefaultRealtimeReflectionProbe();

        internal ref FrameSettings GetDefaultFrameSettings(FrameSettingsRenderType type)
        {
            switch(type)
            {
                case FrameSettingsRenderType.Camera:
                    return ref m_RenderingPathDefaultCameraFrameSettings;
                case FrameSettingsRenderType.CustomOrBakedReflection:
                    return ref m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings;
                case FrameSettingsRenderType.RealtimeReflection:
                    return ref m_RenderingPathDefaultRealtimeReflectionFrameSettings;
                default:
                    throw new ArgumentException("Unknown FrameSettingsRenderType");
            }
        }
        */
        internal bool frameSettingsHistory { get; set; } = false;

        internal ReflectionSystemParameters reflectionSystemParameters
        {
            get
            {
                return new ReflectionSystemParameters
                {
                    maxPlanarReflectionProbePerCamera = currentPlatformRenderPipelineSettings.lightLoopSettings.maxPlanarReflectionOnScreen,
                    maxActivePlanarReflectionProbe = 512,
                    planarReflectionProbeSize = (int)PlanarReflectionAtlasResolution.PlanarReflectionResolution512,
                    maxActiveReflectionProbe = 512,
                    reflectionProbeSize = (int)currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionCubemapSize
                };
            }
        }

        // Note: having m_RenderPipelineSettings serializable allows it to be modified in editor.
        // And having it private with a getter property force a copy.
        // As there is no setter, it thus cannot be modified by code.
        // This ensure immutability at runtime.

        // Store the various RenderPipelineSettings for each platform (for now only one)
        [SerializeField, FormerlySerializedAs("renderPipelineSettings")]
        RenderPipelineSettings m_RenderPipelineSettings = RenderPipelineSettings.NewDefault();

        /// <summary>Return the current use RenderPipelineSettings (i.e for the current platform)</summary>
        public RenderPipelineSettings currentPlatformRenderPipelineSettings => m_RenderPipelineSettings;

        [SerializeField]
        internal bool allowShaderVariantStripping = true;
        [SerializeField]
        internal bool enableSRPBatcher = true;
        [SerializeField]
        internal ShaderVariantLogLevel shaderVariantLogLevel = ShaderVariantLogLevel.Disabled;

        /// <summary>Available material quality levels for this asset.</summary>
        [FormerlySerializedAs("materialQualityLevels")]
        public MaterialQuality availableMaterialQualityLevels = (MaterialQuality)(-1);

        [SerializeField, FormerlySerializedAs("m_CurrentMaterialQualityLevel")]
        private MaterialQuality m_DefaultMaterialQualityLevel = MaterialQuality.High;

        /// <summary>Default material quality level for this asset.</summary>
        public MaterialQuality defaultMaterialQualityLevel { get => m_DefaultMaterialQualityLevel; }

        [SerializeField]
        [Obsolete("Use diffusionProfileSettingsList instead")]
        internal DiffusionProfileSettings diffusionProfileSettings;

        [SerializeField]
        internal DiffusionProfileSettings[] diffusionProfileSettingsList = new DiffusionProfileSettings[0];

        void UpdateRenderingLayerNames()
        {
            m_RenderingLayerNames = new string[32];

            m_RenderingLayerNames[0] = m_RenderPipelineSettings.lightLayerName0;
            m_RenderingLayerNames[1] = m_RenderPipelineSettings.lightLayerName1;
            m_RenderingLayerNames[2] = m_RenderPipelineSettings.lightLayerName2;
            m_RenderingLayerNames[3] = m_RenderPipelineSettings.lightLayerName3;
            m_RenderingLayerNames[4] = m_RenderPipelineSettings.lightLayerName4;
            m_RenderingLayerNames[5] = m_RenderPipelineSettings.lightLayerName5;
            m_RenderingLayerNames[6] = m_RenderPipelineSettings.lightLayerName6;
            m_RenderingLayerNames[7] = m_RenderPipelineSettings.lightLayerName7;

            // Unused
            for (int i = 8; i < m_RenderingLayerNames.Length; ++i)
            {
                m_RenderingLayerNames[i] = string.Format("Unused {0}", i);
            }
        }

        // HDRP use GetRenderingLayerMaskNames to create its light linking system
        // Mean here we define our name for light linking.
        [System.NonSerialized]
        string[] m_RenderingLayerNames;
        string[] renderingLayerNames
        {
            get
            {
                if (m_RenderingLayerNames == null)
                {
                    UpdateRenderingLayerNames();
                }

                return m_RenderingLayerNames;
            }
        }

        /// <summary>Names used for display of rendering layer masks.</summary>
        public override string[] renderingLayerMaskNames
            => renderingLayerNames;

        [System.NonSerialized]
        string[] m_LightLayerNames = null;
        /// <summary>
        /// Names used for display of light layers.
        /// </summary>
        public string[] lightLayerNames //move to defaultSettings TODOJENNY - double check with Remi/Julien
        {
            get
            {
                if (m_LightLayerNames == null)
                {
                    m_LightLayerNames = new string[8];
                }

                for (int i = 0; i < 8; ++i)
                {
                    m_LightLayerNames[i] = renderingLayerNames[i];
                }

                return m_LightLayerNames;
            }
        }

        /// <summary>HDRP default shader.</summary>
        public override Shader defaultShader
            => defaultSettings.renderPipelineResources?.shaders.defaultPS;


#if UNITY_EDITOR
        /// <summary>HDRP default material.</summary>
        public override Material defaultMaterial
            => defaultSettings.renderPipelineEditorResources.materials.defaultDiffuseMat;

        // call to GetAutodeskInteractiveShaderXXX are only from within editor
        /// <summary>HDRP default autodesk interactive shader.</summary>
        public override Shader autodeskInteractiveShader
            => defaultSettings.renderPipelineEditorResources.shaderGraphs.autodeskInteractive;

        /// <summary>HDRP default autodesk interactive transparent shader.</summary>
        public override Shader autodeskInteractiveTransparentShader
            => defaultSettings.renderPipelineEditorResources.shaderGraphs.autodeskInteractiveTransparent;

        /// <summary>HDRP default autodesk interactive masked shader.</summary>
        public override Shader autodeskInteractiveMaskedShader
            => defaultSettings.renderPipelineEditorResources.shaderGraphs.autodeskInteractiveMasked;

        /// <summary>HDRP default terrain detail lit shader.</summary>
        public override Shader terrainDetailLitShader
            => defaultSettings.renderPipelineEditorResources.shaders.terrainDetailLitShader;

        /// <summary>HDRP default terrain detail grass shader.</summary>
        public override Shader terrainDetailGrassShader
            => defaultSettings.renderPipelineEditorResources.shaders.terrainDetailGrassShader;

        /// <summary>HDRP default terrain detail grass billboard shader.</summary>
        public override Shader terrainDetailGrassBillboardShader
            => defaultSettings.renderPipelineEditorResources.shaders.terrainDetailGrassBillboardShader;

        // Note: This function is HD specific
        /// <summary>HDRP default Decal material.</summary>
        public Material GetDefaultDecalMaterial()
            => defaultSettings.renderPipelineEditorResources.materials.defaultDecalMat;

        // Note: This function is HD specific
        /// <summary>HDRP default mirror material.</summary>
        public Material GetDefaultMirrorMaterial()
            => defaultSettings.renderPipelineEditorResources.materials.defaultMirrorMat;

        /// <summary>HDRP default particles material.</summary>
        public override Material defaultParticleMaterial
            => defaultSettings.renderPipelineEditorResources.materials.defaultParticleMat;

        /// <summary>HDRP default terrain material.</summary>
        public override Material defaultTerrainMaterial
            => defaultSettings.renderPipelineEditorResources.materials.defaultTerrainMat;

        // Array structure that allow us to manipulate the set of defines that the HD render pipeline needs
        List<string> defineArray = new List<string>();

        bool UpdateDefineList(bool flagValue, string defineMacroValue)
        {
            bool macroExists = defineArray.Contains(defineMacroValue);
            if (flagValue)
            {
                if (!macroExists)
                {
                    defineArray.Add(defineMacroValue);
                    return true;
                }
            }
            else
            {
                if (macroExists)
                {
                    defineArray.Remove(defineMacroValue);
                    return true;
                }
            }
            return false;
        }

        // This function allows us to raise or remove some preprocessing defines based on the render pipeline settings
        internal void EvaluateSettings()
        {
            // Grab the current set of defines and split them
            string currentDefineList = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.Standalone);
            defineArray.Clear();
            defineArray.AddRange(currentDefineList.Split(';'));

            // Update all the individual defines
            bool needUpdate = false;
            needUpdate |= UpdateDefineList(HDRenderPipeline.GatherRayTracingSupport(currentPlatformRenderPipelineSettings), "ENABLE_RAYTRACING");

            // Only set if it changed
            if (needUpdate)
            {
                UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.Standalone, string.Join(";", defineArray.ToArray()));
            }
        }

        internal bool AddDiffusionProfile(DiffusionProfileSettings profile)
        {
            if (diffusionProfileSettingsList.Length < 15)
            {
                int index = diffusionProfileSettingsList.Length;
                Array.Resize(ref diffusionProfileSettingsList, index + 1);
                diffusionProfileSettingsList[index] = profile;
                UnityEditor.EditorUtility.SetDirty(this);
                return true;
            }
            else
            {
                Debug.LogError("There are too many diffusion profile settings in your HDRP. Please remove one before adding a new one.");
                return false;
            }
        }
#endif

        // Implement IVirtualTexturingEnabledRenderPipeline
        public bool virtualTexturingEnabled { get { return true; } }
    }
}
