using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(DepthOfField))]
    sealed class DepthOfFieldEditor : VolumeComponentWithQualityEditor
    {
        static partial class Styles
        {
            public static GUIContent k_NearSampleCount = new GUIContent("Sample Count", "Sets the number of samples to use for the near field.");
            public static GUIContent k_NearMaxBlur = new GUIContent("Max Radius", "Sets the maximum radius the near blur can reach.");
            public static GUIContent k_FarSampleCount = new GUIContent("Sample Count", "Sets the number of samples to use for the far field.");
            public static GUIContent k_FarMaxBlur = new GUIContent("Max Radius", "Sets the maximum radius the far blur can reach");

            public static GUIContent k_NearFocusStart = new GUIContent("Start", "Sets the distance from the Camera at which the near field blur begins to decrease in intensity.");
            public static GUIContent k_FarFocusStart = new GUIContent("Start", "Sets the distance from the Camera at which the far field starts blurring.");

            public static GUIContent k_NearFocusEnd = new GUIContent("End", "Sets the distance from the Camera at which the near field does not blur anymore.");
            public static GUIContent k_FarFocusEnd = new GUIContent("End", "Sets the distance from the Camera at which the far field blur reaches its maximum blur radius.");
        }

        SerializedDataParameter m_FocusMode;

        // Physical mode
        SerializedDataParameter m_FocusDistance;

        // Manual mode
        SerializedDataParameter m_NearFocusStart;
        SerializedDataParameter m_NearFocusEnd;
        SerializedDataParameter m_FarFocusStart;
        SerializedDataParameter m_FarFocusEnd;

        // Shared settings
        SerializedDataParameter m_NearSampleCount;
        SerializedDataParameter m_NearMaxBlur;
        SerializedDataParameter m_FarSampleCount;
        SerializedDataParameter m_FarMaxBlur;

        // Advanced settings
        SerializedDataParameter m_HighQualityFiltering;
        SerializedDataParameter m_Resolution;

        public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<DepthOfField>(serializedObject);

            m_FocusMode = Unpack(o.Find(x => x.focusMode));

            m_FocusDistance = Unpack(o.Find(x => x.focusDistance));

            m_NearFocusStart = Unpack(o.Find(x => x.nearFocusStart));
            m_NearFocusEnd = Unpack(o.Find(x => x.nearFocusEnd));
            m_FarFocusStart = Unpack(o.Find(x => x.farFocusStart));
            m_FarFocusEnd = Unpack(o.Find(x => x.farFocusEnd));

            m_NearSampleCount = Unpack(o.Find("m_NearSampleCount"));
            m_NearMaxBlur = Unpack(o.Find("m_NearMaxBlur"));
            m_FarSampleCount = Unpack(o.Find("m_FarSampleCount"));
            m_FarMaxBlur = Unpack(o.Find("m_FarMaxBlur"));

            m_HighQualityFiltering = Unpack(o.Find("m_HighQualityFiltering"));
            m_Resolution = Unpack(o.Find("m_Resolution"));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_FocusMode);

            int mode = m_FocusMode.value.intValue;
            if (mode == (int)DepthOfFieldMode.Off)
            {
                GUI.enabled = false;
            }

            using (new HDEditorUtils.IndentScope())
            {
                // Draw the focus mode controls
                DrawFocusSettings(mode);
            }

            EditorGUILayout.Space();

            base.OnInspectorGUI();

            using (new HDEditorUtils.IndentScope())
            {
                // Draw the quality controls
                GUI.enabled = GUI.enabled && base.overrideState;
                DrawQualitySettings();
            }

            GUI.enabled = true;
        }

        void DrawFocusSettings(int mode)
        {
            if (mode == (int)DepthOfFieldMode.Off)
            {
                // When DoF is off, display a focus distance at infinity
                var val = m_FocusDistance.value.floatValue;
                m_FocusDistance.value.floatValue = Mathf.Infinity;
                PropertyField(m_FocusDistance);
                m_FocusDistance.value.floatValue = val;
            }
            else if (mode == (int)DepthOfFieldMode.UsePhysicalCamera)
            {
                PropertyField(m_FocusDistance);
            }
            else if (mode == (int)DepthOfFieldMode.Manual)
            {
                EditorGUILayout.LabelField("Near Range", EditorStyles.miniLabel);
                PropertyField(m_NearFocusStart, EditorGUIUtility.TrTextContent("Start"));
                PropertyField(m_NearFocusEnd, EditorGUIUtility.TrTextContent("End"));

                EditorGUILayout.LabelField("Far Range", EditorStyles.miniLabel);
                PropertyField(m_FarFocusStart, EditorGUIUtility.TrTextContent("Start"));
                PropertyField(m_FarFocusEnd, EditorGUIUtility.TrTextContent("End"));
            }
        }

        void DrawQualitySettings()
        {
            QualitySettingsBlob oldSettings = SaveCustomQualitySettingsAsObject();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Near Blur", EditorStyles.miniLabel);
            PropertyField(m_NearSampleCount, EditorGUIUtility.TrTextContent("Sample Count"));
            PropertyField(m_NearMaxBlur, EditorGUIUtility.TrTextContent("Max Radius"));

            EditorGUILayout.LabelField("Far Blur", EditorStyles.miniLabel);
            PropertyField(m_FarSampleCount, EditorGUIUtility.TrTextContent("Sample Count"));
            PropertyField(m_FarMaxBlur, EditorGUIUtility.TrTextContent("Max Radius"));

            if (isInAdvancedMode)
            {
                EditorGUILayout.LabelField("Advanced Tweaks", EditorStyles.miniLabel);
                PropertyField(m_Resolution);
                PropertyField(m_HighQualityFiltering);
            }

            if (EditorGUI.EndChangeCheck())
            {
                QualitySettingsBlob newSettings = SaveCustomQualitySettingsAsObject();

                if (!DepthOfFieldQualitySettingsBlob.IsEqual(oldSettings as DepthOfFieldQualitySettingsBlob, newSettings as DepthOfFieldQualitySettingsBlob))
                    QualitySettingsWereChanged();
            }
        }

        class DepthOfFieldQualitySettingsBlob : QualitySettingsBlob
        {
            public int nearSampleCount;
            public float nearMaxBlur;
            public int farSampleCount;
            public float farMaxBlur;
            public DepthOfFieldResolution resolution;
            public bool hqFiltering;

            public DepthOfFieldQualitySettingsBlob() : base(6) {}

            public static bool IsEqual(DepthOfFieldQualitySettingsBlob left, DepthOfFieldQualitySettingsBlob right)
            {
                return QualitySettingsBlob.IsEqual(left, right)
                    && left.nearSampleCount == right.nearSampleCount
                    && left.nearMaxBlur == right.nearMaxBlur
                    && left.farSampleCount == right.farSampleCount
                    && left.farMaxBlur == right.farMaxBlur
                    && left.resolution == right.resolution
                    && left.hqFiltering == right.hqFiltering;
            }
        }

        public override void LoadSettingsFromObject(QualitySettingsBlob settings)
        {
            DepthOfFieldQualitySettingsBlob qualitySettings = settings as DepthOfFieldQualitySettingsBlob;

            m_NearSampleCount.value.intValue = qualitySettings.nearSampleCount;
            m_NearMaxBlur.value.floatValue = qualitySettings.nearMaxBlur;
            m_FarSampleCount.value.intValue = qualitySettings.farSampleCount;
            m_FarMaxBlur.value.floatValue = qualitySettings.farMaxBlur;
            m_Resolution.value.intValue = (int) qualitySettings.resolution;
            m_HighQualityFiltering.value.boolValue = qualitySettings.hqFiltering;

            m_NearSampleCount.overrideState.boolValue = qualitySettings.overrideState[0];
            m_NearMaxBlur.overrideState.boolValue = qualitySettings.overrideState[1];
            m_FarSampleCount.overrideState.boolValue = qualitySettings.overrideState[2];
            m_FarMaxBlur.overrideState.boolValue = qualitySettings.overrideState[3];
            m_Resolution.overrideState.boolValue = qualitySettings.overrideState[4];
            m_HighQualityFiltering.overrideState.boolValue = qualitySettings.overrideState[5];
        }

        public override void LoadSettingsFromQualityPreset(RenderPipelineSettings settings, int level)
        {
            m_NearSampleCount.value.intValue = settings.postProcessQualitySettings.NearBlurSampleCount[level];
            m_NearMaxBlur.value.floatValue = settings.postProcessQualitySettings.NearBlurMaxRadius[level];

            m_FarSampleCount.value.intValue = settings.postProcessQualitySettings.FarBlurSampleCount[level];
            m_FarMaxBlur.value.floatValue = settings.postProcessQualitySettings.FarBlurMaxRadius[level];

            m_Resolution.value.intValue = (int) settings.postProcessQualitySettings.DoFResolution[level];
            m_HighQualityFiltering.value.boolValue = settings.postProcessQualitySettings.DoFHighQualityFiltering[level];

            // set all quality override states to true, to indicate that these values are actually used
            m_NearSampleCount.overrideState.boolValue = true;
            m_NearMaxBlur.overrideState.boolValue = true;
            m_FarSampleCount.overrideState.boolValue = true;
            m_FarMaxBlur.overrideState.boolValue = true;
            m_Resolution.overrideState.boolValue = true;
            m_HighQualityFiltering.overrideState.boolValue = true;

        }

        public override QualitySettingsBlob SaveCustomQualitySettingsAsObject(QualitySettingsBlob history = null)
        {
            DepthOfFieldQualitySettingsBlob qualitySettings = (history != null) ? history as DepthOfFieldQualitySettingsBlob : new DepthOfFieldQualitySettingsBlob();
            
            qualitySettings.nearSampleCount = m_NearSampleCount.value.intValue;
            qualitySettings.nearMaxBlur = m_NearMaxBlur.value.floatValue;
            qualitySettings.farSampleCount = m_FarSampleCount.value.intValue;
            qualitySettings.farMaxBlur = m_FarMaxBlur.value.floatValue;
            qualitySettings.resolution = (DepthOfFieldResolution) m_Resolution.value.intValue;
            qualitySettings.hqFiltering = m_HighQualityFiltering.value.boolValue;

            qualitySettings.overrideState[0] = m_NearSampleCount.overrideState.boolValue;
            qualitySettings.overrideState[1] = m_NearMaxBlur.overrideState.boolValue;
            qualitySettings.overrideState[2] = m_FarSampleCount.overrideState.boolValue;
            qualitySettings.overrideState[3] = m_FarMaxBlur.overrideState.boolValue;
            qualitySettings.overrideState[4] = m_Resolution.overrideState.boolValue;
            qualitySettings.overrideState[5] = m_HighQualityFiltering.overrideState.boolValue;

            return qualitySettings;
        }
    }
}
