using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.VFX;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

// ReSharper disable PossibleNullReferenceException
// ReSharper disable UnusedMethodReturnValue.Local

// ReSharper disable InconsistentNaming

// ReSharper disable UseNullPropagation

// ReSharper disable LoopCanBeConvertedToQuery

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
// ReSharper disable CheckNamespace
// ReSharper disable MergeConditionalExpression

namespace Harpia.IconCreator
{
    public class IconCreatorScript : EditorWindow
    {
        /// The png format to save the image
        private TextureFormat PNGFormat => _icAdvanced.GetTextureFormat();

        /// The jpg quality to save the image (0 to 100)
        private int JPGQuality => _icAdvanced.jpgQualitySlider.value;

        /// The render texture format to use on the camera
        private RenderTextureFormat RenderTextureFormat =>
            _icAdvanced.GetRenderFormat();

        ///  The FOV zoom speed with the mouse wheel 
        private float ZoomSpeed => _icAdvanced.zoomSpeedSlider.value;

        /// The object rotation speed with the mouse
        private float RotationSpeed => _icAdvanced.rotationSpeedSlider.value;

        /// The default distance the object will be from the camera
        private const int DistanceFromCamera = 10;

        ///The default Icon Creator GameObject extension
        public const string IconCreatorExtension = " (Icon Creator)";

        /// The camera position in world space
        private static Vector3 CameraStartPosition => IcAdvancedOptionsData.SpawnPosition;

        /// The default object direction offset from the camera
        private Vector3 ObjectOffsetDir => _icAdvanced.directionVec3Field.value.normalized;

        /// If true, Icon creator will try to create folders if they don't exist
        public const bool AllowFolderCreation = true;

        //-----------------------------------------------------------------------

        private enum FileFormat
        {
            PNG,
            JPG,
        }

        private VisualElement _root;
        private VisualElement _flashImage;

        private VisualElement _renderTextureElement;
        private VisualElement _transparentBackgroundElement;
        private VisualElement _animationControlsSection;

        private ProgressBar _labelIndexCount;

        private Label _pathLabel;
        private Label _checkLabel;
        private Label _labelSelectedGameObject;

        private TextField _fileNameInput;

        private Toggle _toggleFov;
        private Toggle _toggleGoNext;
        private Toggle _transparentBackgroundToggle;

        private Slider _sliderCameraFov;

        public Button createIconButton;
        public Button nextButton;
        public Button previousButton;

        private DropdownField _lookAtDropdown;

        private ObjectField _fieldLookAtTransform;
        private ObjectField _foregroundCanvasField;
        private ObjectField _backgroundCanvasField;

        private Vector3Field _rotationField;
        private Vector2IntField _resolutionInput;

        private static string _treeGuid;

        private static int _ticksCount;

        private static Vector3 RotationOffset => new(360, 360, 0);
        private static Color _cameraPreviousColor;
        private static Color _flashColor = Color.clear;

        private static IconCreatorScript _instance;
        private static Canvas _foregroundCanvas;
        private static Canvas _backgroundCanvas;

        private Camera _camera;

        private RenderTexture _renderTexture;
        private Transform LookAtTransform => handler.GetCurrentObject().lookAtTransform;
        private EnumField _fileFormatDropdown;
        private Foldout _renderersAndMaterialsFoldout;
        private IcAdvancedOptionsData _icAdvanced;
        private IC_ObjectListHandler handler;

        private static Dictionary<GameObject, string> _pathDict;
        private const string GuideLinesColorKey = "IconCreator_GuideLinesColor";

        public int TotalObjects => handler.TotalObjects;

        private static readonly List<GameObject> SelectedObjects = new();

        private VisualElement _guideLines;
        private DateTime _lastUndoTime;

        private Foldout _foldoutParticles;

        private static bool IsLoadingPlayMode => EditorApplication.isPlayingOrWillChangePlaymode; // && Application.isPlaying == false;

        private static string CurrentUserPath => EditorPrefs.GetString("IconCreator_CurrentPath", "");
        private static bool AutomaticallyFov => EditorPrefs.GetBool("IconCreator_AutomaticallyFov", true);
        private static bool AutomaticallyGoNext => EditorPrefs.GetBool("IconCreator_AutomaticallyGoNext", true);

        public Action onRotationUpdate;
        private ColorField _cameraBgColorField;
        private ColorField _guideLinesColorField;
        private Button _autoFovButton;
        private const string CameraGameObjectName = "Icon Creator - Camera";

        private static void CreateWindow()
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] Creating Window... ");
#endif

            IconCreatorScript window = null;
            try
            {
                Type inspectorType = Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll");
                window = GetWindow<IconCreatorScript>(new[] { inspectorType });
            }
            catch (TargetInvocationException)
            {
                window = GetWindow<IconCreatorScript>();
            }
            catch (Exception e)
            {
                HarpiaLog($"{e}", true);
                Debug.LogError("[Icon Creator] Could not create Icon creator window. " +
                               "Please try to reset your editor layout to the default one and open Icon Creator tool again" +
                               $"\n\n{e}");
            }

            if (window == null)
            {
                Debug.LogError("[Icon Creator] Could not create Icon creator window");
                ReleaseStaticMemory();
                return;
            }

            window.titleContent = new GUIContent("Icon Creator");
            window.maximized = false;
        }

        public void CreateGUI()
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] On Create GUI ");
#endif

            _ticksCount = 0;
            _instance = this;
            _root = rootVisualElement;

            //Find the XML file path
            string xmlFilePath = AssetDatabase.GUIDToAssetPath(_treeGuid);
            if (string.IsNullOrEmpty(xmlFilePath))
            {
                string[] foundedGUIDs = AssetDatabase.FindAssets("IconCreator_XML");

                if (foundedGUIDs.Length == 0)
                {
                    Debug.LogError($"Could not find the AtlasTextureGeneratorXML.uxml, did you renamed the file? If so rename it ModifyUV_XML.uxml", this);
                    return;
                }

                //get the first founded path
                _treeGuid = foundedGUIDs[0];
                xmlFilePath = AssetDatabase.GUIDToAssetPath(_treeGuid);
            }

            if (EditorPrefs.HasKey("IconCreator_CurrentPath") == false)
            {
                //search for a folder called Icon Creator Icons inside the assets
                string[] foundedGUIDs = AssetDatabase.FindAssets("Icon Creator Icons");
                HarpiaLog("Founded GUIDs: " + foundedGUIDs.Length);

                foreach (string guiD in foundedGUIDs)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guiD);
                    HarpiaLog("Founded path: " + path);
                    if (Directory.Exists(path))
                    {
                        EditorPrefs.SetString("IconCreator_CurrentPath", path);
                        break;
                    }
                }
            }

            //Load
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(xmlFilePath);
            VisualElement labelFromUxml = visualTree.Instantiate();
            _root.Add(labelFromUxml);
            _flashColor = new Color(1f, 1, 1f, 0f);

            //Advanced options
            _icAdvanced = new IcAdvancedOptionsData(_root, this);

            //File Format Dropdown
            _fileFormatDropdown = _root.Q<EnumField>("file-format-field");
            string oldFileFormat = EditorPrefs.GetString(IcPrefsKeys.KFileFormat, FileFormat.PNG.ToString());
            if (Enum.TryParse(oldFileFormat, out FileFormat fileFormat) == false) fileFormat = FileFormat.PNG;
            _fileFormatDropdown.Init(fileFormat);
            _fileFormatDropdown.RegisterCallback<ChangeEvent<Enum>>(OnFileFormatChange);

            //Initialize Stuff
            InitializeCamera();
            InitializeRenderTexture(false);

            //Elements
            createIconButton = _root.Q<Button>("create-icon-button");
            nextButton = _root.Q<Button>("next-button");
            previousButton = _root.Q<Button>("previous-button");

            IcAnimatorExtension.dropdownAnimators = _root.Q<DropdownField>("dropdown-animator");
            IcAnimatorExtension.dropdownAnimations = _root.Q<DropdownField>("dropdown-animations");
            IcAnimatorExtension.animationSection = _root.Q<Foldout>("foldout-animations");

            IcAnimatorExtension.animationTimeSlider = _root.Q<Slider>("slider-animation-time");
            IcAnimatorExtension.animationTimeSlider.RegisterCallback<ChangeEvent<float>>(OnAnimationTimeChange);
            IcAnimatorExtension.dropdownAnimators.RegisterCallback<ChangeEvent<string>>(_ => IcAnimatorExtension.OnAnimatorDropdownChanged());
            IcAnimatorExtension.dropdownAnimations.RegisterCallback<ChangeEvent<string>>(OnAnimationStateChange);

            _pathLabel = _root.Q<Label>("path-label");
            _checkLabel = _root.Q<Label>("check-label");
            _labelSelectedGameObject = _root.Q<Label>("label-current-item");
            _labelSelectedGameObject.text = "Selected game object: ";

            _flashImage = _root.Q<VisualElement>("flash");

            _transparentBackgroundElement = _root.Q<VisualElement>("transparent-background");
            _animationControlsSection = _root.Q<VisualElement>("animation-section-play-mode");

            _guideLines = _root.Q<VisualElement>("guide-lines");

            Button resetRotationXZButton = _root.Q<Button>("reset-rot-x-z");
            resetRotationXZButton.RegisterCallback<ClickEvent>(OnResetRotXZButton);

            _fileNameInput = _root.Q<TextField>("input-filename");

            _labelIndexCount = _root.Q<ProgressBar>("progress-index-counter");

            _rotationField = _root.Q<Vector3Field>("object-rotation");
            _rotationField.RegisterValueChangedCallback(OnRotationChanged);

            _toggleFov = _root.Q<Toggle>("toggle-fov");
            _toggleGoNext = _root.Q<Toggle>("toggle-go-next");
            _transparentBackgroundToggle = _root.Q<Toggle>("toggle-transparent");

            _fieldLookAtTransform = _root.Q<ObjectField>("field-look-at-transform");
            _foregroundCanvasField = _root.Q<ObjectField>("foreground-canvas");
            _backgroundCanvasField = _root.Q<ObjectField>("background-canvas");

            _sliderCameraFov = _root.Q<Slider>("slider-fov");
            _sliderCameraFov.RegisterCallback<ChangeEvent<float>>(OnSliderFovChange);

            ColorField ambientLightInput = _root.Q<ColorField>("input-ambient-light");
            ambientLightInput.SetValueWithoutNotify(RenderSettings.ambientLight);
            ambientLightInput.RegisterCallback<ChangeEvent<Color>>(RenderSettingsExtensions.OnAmbientLightChange);

            //Directional Lights
            AdvancedDirectionalLight.Init(rootVisualElement);

            //Elements config
            _foregroundCanvasField.objectType = typeof(Canvas);
            _backgroundCanvasField.objectType = typeof(Canvas);

            _toggleFov.SetValueWithoutNotify(AutomaticallyFov);
            _toggleGoNext.SetValueWithoutNotify(AutomaticallyGoNext);

            //Guide lines
            Toggle showGuideLines = _root.Q<Toggle>("show-grid-toggle");
            showGuideLines.value = EditorPrefs.GetBool("IconCreator_ShowGuideLines", false);
            showGuideLines.RegisterCallback<ChangeEvent<bool>>(OnShowGridToggle);
            _guideLines.style.display = showGuideLines.value ? DisplayStyle.Flex : DisplayStyle.None;

            //Guide lines Color
            _guideLinesColorField = _root.Q<ColorField>("grid-color");
            _guideLinesColorField.value = Color.black;
            _guideLinesColorField.style.display = showGuideLines.value ? DisplayStyle.Flex : DisplayStyle.None;
            _guideLinesColorField.RegisterCallback<ChangeEvent<Color>>(OnGuideLinesColorChange);
            float a = EditorPrefs.GetFloat(GuideLinesColorKey + "a", 1);
            float r = EditorPrefs.GetFloat(GuideLinesColorKey + "r", 0);
            float g = EditorPrefs.GetFloat(GuideLinesColorKey + "g", 0);
            float b = EditorPrefs.GetFloat(GuideLinesColorKey + "b", 0);
            _guideLinesColorField.value = new Color(r, g, b, a);

            //Callbacks
            _toggleFov.RegisterCallback<ChangeEvent<bool>>(OnToggleFov);
            _toggleGoNext.RegisterCallback<ChangeEvent<bool>>(OnToggleGoNext);

            _fileNameInput.RegisterCallback<ChangeEvent<string>>(OnFileNameChange);

            _autoFovButton = _root.Q<Button>("button-update-fov");
            _autoFovButton.RegisterCallback<ClickEvent>(OnAutoFovButton);

            _root.Q<Button>("button-show-folder").RegisterCallback<ClickEvent>(OnShowButton);
            _root.Q<Button>("button-find-folder").RegisterCallback<ClickEvent>(OnFindButton);
            _root.Q<Button>("button-open-light").RegisterCallback<ClickEvent>(OpenLightSettings);
            _root.Q<Button>("button-autoname").RegisterCallback<ClickEvent>(AutoNameButtonClick);
            _root.Q<Button>("button-animator").RegisterCallback<ClickEvent>(OpenAnimatorSettings);
            _root.Q<Button>("button-open-folder").RegisterCallback<ClickEvent>(OnOpenFolderButton);
            _root.Q<Button>("button-open-lighting").RegisterCallback<ClickEvent>(OpenLightingSettings);
            _root.Q<Button>("button-render-texture").RegisterCallback<ClickEvent>(OnRenderTextureButton);
            _root.Q<Button>("button-show-gameobject").RegisterCallback<ClickEvent>(OnShowGameObjectButton);
            _root.Q<Button>("button-open-camera-settings").RegisterCallback<ClickEvent>(OpenCameraSettings);
            _root.Q<Button>("button-open-documentation").RegisterCallback<ClickEvent>(OpenDocumentationButton);
            _root.Q<Button>("apply-rotation-to-all").RegisterCallback<ClickEvent>(OnApplyRotationToAll);
            _root.Q<Button>("button-open-quality").RegisterCallback<ClickEvent>(OnOpenQuality);
            _root.Q<Button>("button-open-graphics").RegisterCallback<ClickEvent>(OnOpenGraphics);

            createIconButton.RegisterCallback<ClickEvent>(OnCreateIconButton);

            nextButton.RegisterCallback<ClickEvent>(OnNextButton);
            previousButton.RegisterCallback<ClickEvent>(OnPreviousButton);

            _lookAtDropdown = _root.Q<DropdownField>("dropdown-look-at");
            _lookAtDropdown.RegisterCallback<ChangeEvent<string>>(OnLookAtChanged);

            //Camera bg color
            _cameraBgColorField = _root.Q<ColorField>("camera-bg-color");
            _cameraBgColorField.RegisterValueChangedCallback(OnCameraBgColorChanged);
            _cameraBgColorField.SetValueWithoutNotify(Color.white);

            //Particles
            _foldoutParticles = _root.Q<Foldout>("foldout-particles");

            //Canvas
            _foregroundCanvasField.RegisterValueChangedCallback(OnForegroundChanged);
            _backgroundCanvasField.RegisterValueChangedCallback(OnBackgroundChanged);

            _renderTextureElement.RegisterCallback<MouseMoveEvent>(OnDragRenderTexture);
            _renderTextureElement.RegisterCallback<MouseDownEvent>(OnMouseDownRenderTexture);
            _renderTextureElement.RegisterCallback<WheelEvent>(OnMouseWellRenderTexture);
            _renderTextureElement.RegisterCallback<FocusInEvent>(OnFocusRenderTexture);
            _renderTextureElement.RegisterCallback<FocusOutEvent>(OnFocusOutRenderTexture);

            _flashImage.style.backgroundColor = _flashColor;

            InitializeRenderTexture(false);
            UpdateAnimatorsDropdown();

            _transparentBackgroundToggle.RegisterCallback<ChangeEvent<bool>>(OnToggleTransparent);
            _transparentBackgroundToggle.value = EditorPrefs.GetBool("IconCreator_TransparentBackground", false);
            OnToggleTransparent(null);

            //Renderers & materials
            _renderersAndMaterialsFoldout = _root.Q<Foldout>("foldout-renderers");

            //Camera
            _fieldLookAtTransform.objectType = typeof(Transform);
            _fieldLookAtTransform.RegisterValueChangedCallback(OnLookAtTransformChanged);
            _fieldLookAtTransform.style.display = DisplayStyle.None;

            _checkLabel.text = "";

            //Events
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += OnUpdate;
            EditorApplication.wantsToQuit -= OnWantsToQuit;
            EditorApplication.wantsToQuit += OnWantsToQuit;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;

            InitializeObjects();
            UpdateUI();
            OnAutoFovButton(null);
            AutoNameButtonClick(null);
            SelectedObjects.Clear();

            Show();
        }

        private void InitializeObjects()
        {
            if (handler != null) handler.Dispose();

            handler = new IC_ObjectListHandler();
            int count = InstantiateObjects(SelectedObjects, true);

            RenderSettingsExtensions.Initialize();
            SelectedObjects.Clear();
            Selection.objects = null;

#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] Spawned {count} objects! ");
#endif
        }

        private void OnFocusOutRenderTexture(FocusOutEvent evt)
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] OnFocusOutRenderTexture");
#endif

            nextButton.text = "Next \u2192";
            previousButton.text = "\u2190 Previous";
            createIconButton.text = "Create Icon";
            _autoFovButton.text = "Auto Fov";
        }

        private void OnFocusRenderTexture(FocusInEvent evt)
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] OnFocusRenderTexture");
#endif

            nextButton.text = "(D) Next \u2192";
            previousButton.text = "\u2190 Previous (A)";
            createIconButton.text = "Create Icon (E)";
            _autoFovButton.text = "Auto Fov (F)";
        }

        private void OnBeforeAssemblyReload()
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] OnBeforeAssemblyReload");
#endif
            if (handler != null) handler.Dispose();
            DestroyCamera();
        }

        private void OnOpenQuality(ClickEvent evt)
        {
            HarpiaLog("OnOpenQuality");
            SettingsService.OpenProjectSettings("Project/Quality");
        }

        private void OnOpenGraphics(ClickEvent evt)
        {
            HarpiaLog("OnOpenGraphics");
            SettingsService.OpenProjectSettings("Project/Graphics");
        }

        private void OnGuideLinesColorChange(ChangeEvent<Color> evt)
        {
            //get all children
            List<VisualElement> children = _guideLines.Children().ToList();
            foreach (VisualElement child in children)
            {
                IEnumerable<VisualElement> newChild = child.Children();
                foreach (VisualElement guideLine in newChild)
                {
                    guideLine.style.backgroundColor = evt.newValue;
                }
            }

            //save to prefs
            EditorPrefs.SetFloat(GuideLinesColorKey + "r", evt.newValue.r);
            EditorPrefs.SetFloat(GuideLinesColorKey + "g", evt.newValue.g);
            EditorPrefs.SetFloat(GuideLinesColorKey + "b", evt.newValue.b);
            EditorPrefs.SetFloat(GuideLinesColorKey + "a", evt.newValue.a);
        }

        private void OnShowGridToggle(ChangeEvent<bool> evt)
        {
            HarpiaLog("On Show Grid Toggle");
            _guideLines.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            _guideLinesColorField.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            EditorPrefs.SetBool("IconCreator_ShowGuideLines", evt.newValue);
        }

        private void OnResetRotXZButton(ClickEvent evt)
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] OnResetRotXZButton");
#endif
            Transform currentTransform = handler.GetCurrentObject().iconObject.transform;

            //Undo
            Undo.RegisterCompleteObjectUndo(currentTransform, "Icon Creator - Reset Rotation");
            Undo.FlushUndoRecordObjects();

            //Rot
            currentTransform.rotation = Quaternion.Euler(0, currentTransform.rotation.eulerAngles.y, 0);

            //UI
            UpdateUI();
            UpdateLookAt();
        }

        private void OnCameraBgColorChanged(ChangeEvent<Color> evt)
        {
#if HARPIA_DEBUG
//            Debug.Log($"[{nameof(IconCreatorScript)}] OnCameraBgColorChanged {evt.newValue}");
#endif

            Color newValue2 = evt.newValue;
            bool isPNG = _fileFormatDropdown.value.ToString() == FileFormat.PNG.ToString();

            if (!isPNG) newValue2.a = 1;

            _camera.backgroundColor = newValue2;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _transparentBackgroundToggle.SetValueWithoutNotify(isPNG && evt.newValue.a == 0);
            _cameraBgColorField.SetValueWithoutNotify(newValue2);
        }

        private void OnUndoRedo()
        {
            string msg = Undo.GetCurrentGroupName();
            HarpiaLog($"OnUndoRedo {msg}");

            if (msg == "Icon Creator - Mouse Down")
            {
                //TODO: undo lookAtOffset
                //CurrentData.lookAtOffset = 

                UpdateUI();
            }
        }

        private void OnMouseDownRenderTexture(MouseDownEvent evt)
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] OnMouseDownRenderTexture");
#endif

            nextButton.text = "(D) Next \u2192";
            previousButton.text = "\u2190 Previous (A)";

            Undo.RegisterCompleteObjectUndo(new Object[]
            {
                handler.GetCurrentObject().iconObject,
                handler.GetCurrentObjectTransform(),
                _camera.gameObject,
                _camera
            }, "Icon Creator - Mouse Down");

            Undo.FlushUndoRecordObjects();
        }

        private void OnApplyRotationToAll(ClickEvent evt)
        {
            HarpiaLog("OnApplyRotationToAll");

            //display a confirmation box

            bool result = EditorUtility.DisplayDialog("Icon Creator",
                "Apply rotation to all\n\n" +
                "Are you sure you want to apply the current rotation to all objects?" +
                "\n\nThis action cannot be undone.", "Yes", "No");

            if (!result) return;

            Quaternion rotation = Quaternion.Euler(_rotationField.value);
            handler.ApplyRotation(rotation);
        }

        private void OnFileFormatChange(ChangeEvent<Enum> evt)
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] OnFileFormatChange - {evt.newValue}");
#endif

            EditorPrefs.SetString(IcPrefsKeys.KFileFormat, evt.newValue.ToString());

            //Check if it is JPG
            bool isJPG = evt.newValue.ToString() == FileFormat.JPG.ToString();

            if (isJPG)
            {
                _transparentBackgroundToggle.value = false;
                Color oldColor = _cameraBgColorField.value;
                Color newColor = new(oldColor.r, oldColor.g, oldColor.b, 1);
                _cameraBgColorField.SetValueWithoutNotify(newColor);
                _camera.backgroundColor = newColor;
            }

            UpdateUI();
        }

        private void OpenLightingSettings(ClickEvent evt)
        {
            HarpiaLog("OpenLightingSettings");
            EditorApplication.ExecuteMenuItem("Window/Rendering/Lighting");
        }

        private void OpenDocumentationButton(ClickEvent evt)
        {
            HarpiaLog("OpenDocumentationButton");
            OpenDocumentation();
        }

        private void OpenAnimatorSettings(ClickEvent evt)
        {
            HarpiaLog("OpenAnimatorSettings");

            Selection.activeObject = IcAnimatorExtension.currentAnimator;
            EditorApplication.ExecuteMenuItem("Window/Animation/Animator");
        }

        private void UpdateCanvas()
        {
            if (EditorPrefs.HasKey("IconCreator_ForegroundCanvas"))
            {
                //log guid
                string guid = EditorPrefs.GetString("IconCreator_ForegroundCanvas");
                _foregroundCanvas = AssetDatabase.LoadAssetAtPath<Canvas>(AssetDatabase.GUIDToAssetPath(guid));
                if (_foregroundCanvas != null) _foregroundCanvasField.value = _foregroundCanvas;
            }

            if (EditorPrefs.HasKey("IconCreator_BackgroundCanvas"))
            {
                string guid = EditorPrefs.GetString("IconCreator_BackgroundCanvas");
                _backgroundCanvas = AssetDatabase.LoadAssetAtPath<Canvas>(AssetDatabase.GUIDToAssetPath(guid));
                if (_backgroundCanvas != null) _backgroundCanvasField.value = _backgroundCanvas;
            }
        }

        private void OnResolutionChange(FocusOutEvent focusOutEvent)
        {
            HarpiaLog("OnResolutionChange");

            const int min = 100;
            const int max = 4096;

            Vector2Int v = new(Mathf.Clamp(_resolutionInput.value.x, min, max),
                Mathf.Clamp(_resolutionInput.value.y, min, max));

            EditorPrefs.SetInt("IconCreator_Height", v.x);
            EditorPrefs.SetInt("IconCreator_Width", v.y);

            _resolutionInput.SetValueWithoutNotify(v);

            UpdateRenderTextureSize();
        }

        private void OnBackgroundChanged(ChangeEvent<Object> evt)
        {
            HarpiaLog("OnBackgroundChanged");

            if (_backgroundCanvas != null) DestroyGameObject(_backgroundCanvas.gameObject);

            if (evt.newValue == null)
            {
                EditorPrefs.DeleteKey("IconCreator_BackgroundCanvas");
                return;
            }

            _backgroundCanvas = evt.newValue as Canvas;
            _backgroundCanvas = Instantiate(_backgroundCanvas);
            _backgroundCanvas.name = "Canvas Background - Icon Creator";
            _backgroundCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            _backgroundCanvas.worldCamera = _camera;
            UpdateCanvasFarPlane();

            //Save guid
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(evt.newValue));
            EditorPrefs.SetString("IconCreator_BackgroundCanvas", guid);

            _backgroundCanvasField.SetValueWithoutNotify(_backgroundCanvas);
        }

        private void OnForegroundChanged(ChangeEvent<Object> evt)
        {
            HarpiaLog("OnForegroundChanged");

            if (_foregroundCanvas != null) DestroyGameObject(_foregroundCanvas.gameObject);

            if (evt.newValue == null)
            {
                EditorPrefs.DeleteKey("IconCreator_ForegroundCanvas");
                return;
            }

            _foregroundCanvas = evt.newValue as Canvas;
            _foregroundCanvas = Instantiate(_foregroundCanvas);
            _foregroundCanvas.name = "Canvas Foreground - Icon Creator";
            _foregroundCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            _foregroundCanvas.worldCamera = _camera;

            UpdateCanvasFarPlane();

            //save guid
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(evt.newValue));
            EditorPrefs.SetString("IconCreator_ForegroundCanvas", guid);

            _foregroundCanvasField.SetValueWithoutNotify(_foregroundCanvas);
        }

        public static void DestroyGameObject(GameObject o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }
        
        public static void DestroyGameObject(MonoBehaviour o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        private void UpdateCanvasFarPlane()
        {
            if (_foregroundCanvas != null)
                _foregroundCanvas.planeDistance = handler.GetCurrentObject().size * (DistanceFromCamera - 2);
            if (_backgroundCanvas != null)
                _backgroundCanvas.planeDistance = handler.GetCurrentObject().size * (DistanceFromCamera + 2);
        }

        private static void OnPlayModeChanged(PlayModeStateChange obj)
        {
            HarpiaLog("Play mode changed");
            _instance._animationControlsSection.SetActive(obj == PlayModeStateChange.EnteredPlayMode);
        }

        private void OnRotationChanged(ChangeEvent<Vector3> evt)
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] OnRotationChanged");
#endif

            handler.GetCurrentObjectTransform().rotation = Quaternion.Euler(evt.newValue - RotationOffset);
            UpdateLookAt();
        }

        private void OnLookAtTransformChanged(ChangeEvent<Object> changeEvent)
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] Changed look at transform");
#endif
            handler.GetCurrentObject().SetLookAtTransform(changeEvent.newValue as Transform);
        }

        private void OnOpenFolderButton(ClickEvent evt)
        {
            string path = CurrentUserPath;

#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] OnOpenFolderButton - path {path}");
#endif

            //Check if the folder exists
            if (!Directory.Exists(path))
            {
                HarpiaLog($"Folder does not exists: {path}", true);
                EditorUtility.DisplayDialog("Icon Creator - Error", $"Folder does not exists: {path}", "Ok");
                return;
            }

            ShowFolder(path);
        }

        private static bool ShowFolder(string path)
        {
            EditorUtility.FocusProjectWindow();

            Object folder = AssetDatabase.LoadAssetAtPath(path, typeof(object));
            if (folder == null) return false;

            Type pt = Type.GetType("UnityEditor.ProjectBrowser,UnityEditor");
            if (pt == null) return false;

            object ins = pt.GetField("s_LastInteractedProjectBrowser", BindingFlags.Static | BindingFlags.Public).GetValue(null);
            MethodInfo showDirMeth = pt.GetMethod("ShowFolderContents", BindingFlags.NonPublic | BindingFlags.Instance);
            if (showDirMeth == null) return false;

            Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            if (obj == null) return false;

            try
            {
                showDirMeth.Invoke(ins, new object[] { obj.GetInstanceID(), true });
                return true;
            }
            catch (TargetException)
            {
                EditorUtility.RevealInFinder(path);
                return false;
            }
        }

        private void OnRenderTextureButton(ClickEvent evt)
        {
            HarpiaLog("On render texture");
            Selection.activeObject = _renderTexture;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }

        private void OnLookAtChanged(ChangeEvent<string> evt)
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] Look at dropdown");
#endif

            IcObjectData currentData = handler.GetCurrentObject();
            currentData.SetLookAt(evt.newValue);
            _fieldLookAtTransform.style.display = evt.newValue == "Custom Transform" ? DisplayStyle.Flex : DisplayStyle.None;

            if (currentData.lookAtTransform == null && _fieldLookAtTransform.value != null)
            {
                currentData.SetLookAtTransform(_fieldLookAtTransform.value as Transform);
            }

            UpdateLookAt();
        }

        private void OnGUI()
        {
            GUI.backgroundColor = Color.white;
        }

        private void OnAnimationTimeChange(ChangeEvent<float> evt)
        {
            HarpiaLog($"OnAnimationTimeChange");
            IcAnimatorExtension.UpdateAnimator(evt.newValue);
            onRotationUpdate?.Invoke();
        }

        private void OnAnimationStateChange(ChangeEvent<string> evt)
        {
            HarpiaLog($"OnAnimationStateChange {evt.newValue}");
            IcAnimatorExtension.currentAnimationState = evt.newValue;
            IcAnimatorExtension.UpdateAnimator(0.2f);
        }

        private void OpenLightSettings(ClickEvent evt)
        {
            HarpiaLog("OpenLightSettings");
            EditorApplication.ExecuteMenuItem("Window/Rendering/Light Explorer");
        }

        private void UpdateAnimatorsDropdown()
        {
            IcAnimatorExtension.Init(handler == null ? null : handler.GetCurrentIconObject());
        }

        private void AutoNameButtonClick(ClickEvent evt)
        {
            IcObjectData current = handler.GetCurrentObject();
            if (current == null) return;

            HarpiaLog("AutoNameButtonClick");
            current.AutoName(current.HasCustomPath() ? current.GetCustomPath() : CurrentUserPath);
            UpdateUI();
        }

        private void OnFileNameChange(ChangeEvent<string> evt)
        {
            HarpiaLog("On file name change");
            string fileName = evt.newValue.Replace(".", "");
            handler.GetCurrentObject().SetLastName(fileName);
            UpdateUI();
        }

        private void OnToggleTransparent(ChangeEvent<bool> evt)
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] On Toggle Transparent -> New value {(evt == null ? "null" : evt.newValue)} ");
#endif

            bool isTransparent = evt != null ? evt.newValue : EditorPrefs.GetBool("IconCreator_TransparentBackground", true);

            EditorPrefs.SetBool("IconCreator_TransparentBackground", isTransparent);

            if (!isTransparent)
            {
                _cameraPreviousColor.a = 1;
                _camera.backgroundColor = _cameraPreviousColor;
                _cameraBgColorField.SetValueWithoutNotify(_camera.backgroundColor);
                return;
            }

            if (_camera.backgroundColor != Color.clear) _cameraPreviousColor = _camera.backgroundColor;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _cameraBgColorField.SetValueWithoutNotify(_camera.backgroundColor);
        }

        private void OnSliderFovChange(ChangeEvent<float> evt)
        {
            HarpiaLog("On slider fov change");
            _camera.fieldOfView = evt.newValue;
            UpdateLookAt();
        }

        private void OnToggleFov(ChangeEvent<bool> evt)
        {
            HarpiaLog("On toggle fov");
            EditorPrefs.SetBool("IconCreator_AutomaticallyFov", evt.newValue);
            if (evt.newValue) OnAutoFovButton(null);
        }

        private void OnToggleGoNext(ChangeEvent<bool> evt)
        {
            HarpiaLog("On toggle go next");
            EditorPrefs.SetBool("IconCreator_AutomaticallyGoNext", evt.newValue);
        }

        private void OnShowGameObjectButton(ClickEvent evt)
        {
            HarpiaLog("On show game object button");
            Selection.activeObject = handler.GetCurrentObject().iconObject;
            EditorGUIUtility.PingObject(handler.GetCurrentObject().iconObject);
        }

        private void OpenCameraSettings(ClickEvent evt)
        {
            HarpiaLog("Open camera settings");
            Selection.activeObject = _camera;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }

        private void OnCreateIconButton(ClickEvent evt)
        {
            HarpiaLog("On create icon button");

            if (SaveImageFile(_renderTexture, out string location))
            {
                handler.GetCurrentObject().tookScreenshot = true;
                AutoNameButtonClick(null);
                if (AutomaticallyGoNext && TotalObjects > 1) OnNextButton(null);
            }
            else return;

            if (!AutomaticallyGoNext) AutoNameButtonClick(null);

            _flashColor = Color.white;
            Debug.Log($"[Icon Creator] Icon created at {location} ", this);
        }

        private void UpdateUI()
        {
            if (_labelIndexCount == null) return;

            IcObjectData current = handler.GetCurrentObject();
            if (current == null) return;

#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] Updating UI for {current.originalObject.name} ");
#endif

            _labelIndexCount.title = $"{handler.currentObjectIndex + 1}/{TotalObjects}";
            _labelIndexCount.highValue = TotalObjects;
            _labelIndexCount.lowValue = 0;
            _labelIndexCount.value = handler.currentObjectIndex + 1;

            _labelSelectedGameObject.text = $"Current GameObject: {current.originalObject.name}";
            _pathLabel.text = $"Save Location: <i>{(current.HasCustomPath() ? current.GetCustomPath() : CurrentUserPath)}</i>";
            _fileNameInput.SetValueWithoutNotify(current.lastName);

            previousButton.style.display = TotalObjects > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            previousButton.tooltip = "Previous Object: " + handler.GetPreviousObject().name;

            nextButton.style.display = TotalObjects > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            nextButton.tooltip = "Next Object: " + handler.GetNextObject().name;

            _lookAtDropdown.choices.Clear();
            _lookAtDropdown.choices.AddRange(current.GetLookAtOptions());
            _lookAtDropdown.SetValueWithoutNotify(current.lookAt);

            _checkLabel.text = current.tookScreenshot ? "Icon Already Created" : "";

            if (current.fieldOfView != -1)
            {
                if (_camera != null) _camera.fieldOfView = current.fieldOfView;
            }

            //Make transparent toggle not intractable
            _transparentBackgroundToggle.SetEnabled(Equals(_fileFormatDropdown.value, FileFormat.PNG));

            if (_camera != null)
            {
                _camera.farClipPlane = Vector3.Distance(current.iconObject.transform.position, _camera.transform.position) + current.size * 4;
                Color backgroundColor = _camera.backgroundColor;
                _cameraBgColorField.value = backgroundColor;
                _cameraBgColorField.SetValueWithoutNotify(backgroundColor);
            }

            _toggleGoNext.style.display = handler.TotalObjects > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            _toggleFov.style.display = handler.TotalObjects > 1 ? DisplayStyle.Flex : DisplayStyle.None;

            UpdateRotationUI();
            UpdateCanvasFarPlane();
            UpdateLookAt();

            //animators
            IcAnimatorExtension.Init(current.iconObject);

            //Particles
            ParticleSystemHandler.Init(_foldoutParticles, current.iconObject, this);
            if (ParticleSystemHandler.IsOnlyParticleSystem(current.iconObject.transform))
            {
                _transparentBackgroundToggle.value = false;
            }

            //Renderers and materials
            _renderersAndMaterialsFoldout ??= rootVisualElement.Q<Foldout>("foldout-renderers");
            _renderersAndMaterialsFoldout.Clear();
            Renderer[] allRenderers = current.iconObject.GetComponentsInChildren<Renderer>();

            for (int index = 0; index < allRenderers.Length; index++)
            {
                Renderer renderer = allRenderers[index];

                if (renderer == null) continue;

                ObjectField rendererField = new()
                {
                    objectType = typeof(Renderer),
                    value = renderer,
                    label = "Renderer #" + (index + 1)
                };
                _renderersAndMaterialsFoldout.Add(rendererField);

                //Click event
                rendererField.RegisterCallback<ClickEvent>((_) =>
                {
                    Selection.activeObject = renderer;
                    EditorGUIUtility.PingObject(renderer);
                    EditorApplication.ExecuteMenuItem("Window/General/Inspector");
                });

                //Value changed event
                rendererField.RegisterCallback<ChangeEvent<Object>>((e) =>
                {
                    ((ObjectField)e.target).value = renderer;
                    Debug.Log("[Icon Creator] Please change the renderer in the object, not here.");
                });

                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    Material material = renderer.sharedMaterials[i];
                    if (material == null) continue;

                    //Add a object field for each material
                    ObjectField materialField = new()
                    {
                        objectType = typeof(Material),
                        value = material,
                        label = "Material #" + (i + 1)
                    };

                    //Click event
                    materialField.RegisterCallback<ClickEvent>((_) =>
                    {
                        Selection.activeObject = material;
                        EditorGUIUtility.PingObject(material);
                        EditorApplication.ExecuteMenuItem("Window/General/Inspector");
                    });

                    //value changed event
                    materialField.RegisterCallback<ChangeEvent<Object>>((e) =>
                    {
                        ((ObjectField)e.target).value = material;
                        Debug.Log("[Icon Creator] Please change the material in the renderer, not here.");
                    });

                    _renderersAndMaterialsFoldout.Add(materialField);
                }

                //Add a empty space
                VisualElement emptySpace = new()
                {
                    style =
                    {
                        height = 10
                    }
                };
                _renderersAndMaterialsFoldout.Add(emptySpace);
            }
        }

        private void OnPreviousButton(ClickEvent evt)
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] On Previous Button ");
#endif

            onRotationUpdate = null;
            handler.SetCurrentObjectActive(false);
            handler.AddToIndex(-1);
            handler.SetCurrentObjectActive(true);

            if (AutomaticallyFov) OnAutoFovButton(null);

            UpdateAnimatorsDropdown();
            UpdateUI();
        }

        private void OnNextButton(ClickEvent evt)
        {
            HarpiaLog("On next button");

            onRotationUpdate = null;
            handler.SetCurrentObjectActive(false);
            handler.AddToIndex(1);
            handler.SetCurrentObjectActive(true);

            UpdateUI();
            if (AutomaticallyFov) OnAutoFovButton(null);
            UpdateAnimatorsDropdown();
            UpdateLookAt();
        }

        private void OnShowButton(ClickEvent evt)
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] On show button");
#endif

            //Check if current path is valid
            if (string.IsNullOrEmpty(CurrentUserPath))
            {
                Debug.LogError($"The current path is invalid", this);

                //panel
                EditorUtility.DisplayDialog("Icon Creator - Error ",
                    "The current path is invalid, hit 'find...' and select a valid path.", "Ok");
                return;
            }

            //Open the folder
            EditorUtility.RevealInFinder(CurrentUserPath);

            //focus on a project window
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(CurrentUserPath);
        }

        private void OnFindButton(ClickEvent evt)
        {
            HarpiaLog("On find button");

            string p = EditorUtility.OpenFolderPanel("Select the folder to save the icons", "", "");

            //Check if the path is valid
            if (string.IsNullOrEmpty(p))
            {
                Debug.LogError($"The selected path is invalid", this);
                EditorUtility.DisplayDialog("Icon Creator - Error ", $"The selected path is invalid: {p}", "Ok");
                return;
            }

            EditorPrefs.SetString("IconCreator_CurrentPath", p);
            handler.GetCurrentObject().possibleIconPath = "";
            UpdateUI();
        }

        private int InstantiateObjects(List<GameObject> originalObjects, bool firstEnable)
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] Instantiating {originalObjects.Count} objects - Firs Enable {firstEnable} ");
#endif

            if (originalObjects.Count == 0) return 0;

            int count = 0;
            for (int index = 0; index < originalObjects.Count; index++)
            {
                string possibleIconPath = _pathDict == null ? "" : _pathDict[originalObjects[index]];
                bool success = InstantiateObject(originalObjects[index], index == 0 && firstEnable, possibleIconPath);
                if (success) count++;
            }

            return count;
        }

        /// <returns>Returns true if successfully instantiated the object</returns>
        private bool InstantiateObject(GameObject prefab, bool startActive, string possibleIconPath)
        {
            if (!IcObjectData.HasAnyRenderer(prefab)) return false;
            if (handler.ContainsObject(prefab)) return false;

            GameObject newObject = Instantiate(prefab);
            newObject.name = newObject.name.Replace("(Clone)", IconCreatorExtension);

            IcObjectData newData = new(prefab, newObject, CurrentUserPath, possibleIconPath);

            if (newData.IsValid() == false)
            {
                newData.DisposeObj(handler);
                return false;
            }

            IcAnimatorExtension.DisableRootMotion(newObject);
            newObject.SetActive(startActive);
            newObject.transform.rotation = Quaternion.identity;
            newObject.hideFlags = HideFlags.DontSave;
            newObject.transform.position = _camera.transform.position + ObjectOffsetDir * newData.size * DistanceFromCamera;

#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] Instantiated Object {newObject.name} at {newObject.transform.position}", newObject);
#endif

            handler.AddObject(newData);
            if (startActive) UpdateLookAt();
            return true;
        }

        private void OnUpdate()
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] Updating Icon Creator {hasFocus}");
#endif

            //Wait for UI Update
            if (_ticksCount < 10)
            {
                _ticksCount++;
                if (_ticksCount == 10)
                {
                    UpdateCanvas();
                    UpdateCanvasAspect();
                }
            }

            if (_camera == null)
            {
                Close();
                HarpiaLog("Closing Scene - Camera is null");
                return;
            }

            bool shouldRepaint = false;
            if (_flashColor.a > 0)
            {
                _flashColor.a -= 0.005f;
                _flashImage.SetBackgroundColor(_flashColor);
                _labelIndexCount.title = _flashColor.a > 0 ? "Icon Created!" : $"{handler.currentObjectIndex + 1}/{TotalObjects}";
                shouldRepaint = true;
            }

            if (hasFocus == false) shouldRepaint = true;

            if (shouldRepaint) Repaint();
        }

        private void UpdateLookAt()
        {
            IcObjectData currentObject = handler.GetCurrentObject();
            Vector3 lookAtOffset = currentObject.lookAtOffset;
            switch (currentObject.lookAt)
            {
                case "Bounds Center":
                    _camera.transform.LookAt(GetBounds(currentObject.iconObject.transform).center + lookAtOffset);
                    break;
                case "Object Pivot":
                    _camera.transform.LookAt(currentObject.iconObject.transform.position + lookAtOffset);
                    break;
                case "Custom Transform":
                    if (LookAtTransform == null) break;
                    _camera.transform.LookAt(LookAtTransform.position + lookAtOffset);
                    break;
                default:
                    HarpiaLog($"Look at invalid {currentObject.lookAt} ", true);
                    break;
            }

            Repaint();
        }

        private void OnAutoFovButton(ClickEvent evt)
        {
            _camera.fieldOfView = 90;
            _sliderCameraFov.SetValueWithoutNotify(_camera.fieldOfView);

            IcObjectData current = handler.GetCurrentObject();
            if (current == null) return;

            Bounds bounds = GetBounds(current.iconObject.transform);

            Vector3[] vertices = new Vector3[8];
            vertices[0] = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            vertices[1] = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z);
            vertices[2] = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z);
            vertices[3] = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z);
            vertices[4] = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z);
            vertices[5] = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z);
            vertices[6] = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z);
            vertices[7] = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z);

            float farMostX = float.MinValue;
            float farMostY = farMostX;
            float lessMostX = float.MaxValue;
            float lessMostY = lessMostX;

            foreach (Vector3 vector3 in vertices)
            {
                //world to screen
                Vector3 screenPoint = _camera.WorldToScreenPoint(vector3);

                if (screenPoint.x > farMostX) farMostX = screenPoint.x;
                if (screenPoint.y > farMostY) farMostY = screenPoint.y;
                if (screenPoint.x < lessMostX) lessMostX = screenPoint.x;
                if (screenPoint.y < lessMostY) lessMostY = screenPoint.y;
            }

            //Get the X distance
            float xDistance = farMostX - lessMostX;
            float yDistance = farMostY - lessMostY;

            //Get the greater distance
            float greaterDistance = Mathf.Max(xDistance, yDistance);

            float fov = Mathf.Clamp(greaterDistance * (20f / 78f), 1f, 170f);
            _camera.fieldOfView = fov;

            _sliderCameraFov.SetValueWithoutNotify(_camera.fieldOfView);
        }

        public static Bounds GetBounds(Transform obj)
        {
            if (ParticleSystemHandler.IsOnlyParticleSystem(obj))
            {
                return new Bounds(obj.position, obj.lossyScale);
            }

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0) return new Bounds(obj.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                //Check if object has particle system
                if (renderers[index].GetComponent<ParticleSystem>() != null) continue;
                Renderer renderer = renderers[index];
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private void OnEnable()
        {
            _instance = this;
        }

        private void OnFocus()
        {
            HarpiaLog($"OnFocus here  - {hasFocus}");

            if (_camera != null && _camera.targetTexture == null)
                _camera.targetTexture = _renderTexture;

            UpdateUI();
        }

        private void OnDisable()
        {
            HarpiaLog($"OnDisable here ");
            _icAdvanced.Save();
        }

        private void OnDestroy()
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] On Destroy here");
#endif

            ReleaseStaticMemory();
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.wantsToQuit -= OnWantsToQuit;
            EditorApplication.update -= OnUpdate;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            Undo.undoRedoPerformed -= OnUndoRedo;
            AdvancedDirectionalLight.Dispose();
            handler.Dispose();
            RenderSettingsExtensions.Dispose();

            DestroyIconCreatorObjects();

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                _renderTexture = null;
            }

            DestroyCamera();

            AssetDatabase.Refresh();
        }

        private void DestroyCamera()
        {
            if (_camera != null) DestroyGameObject(_camera.gameObject);
            GameObject cameraObject = GameObject.Find(CameraGameObjectName);
            if (cameraObject != null) DestroyGameObject(cameraObject);
        }

        private void DestroyIconCreatorObjects()
        {
            Transform[] all = FindObjectsOfType<Transform>(true);
            foreach (Transform o in all)
            {
                if (o.name.Contains(IconCreatorExtension) == false) continue;
                DestroyGameObject(o.gameObject);
            }
        }

        private bool OnWantsToQuit()
        {
            Close();
            return true;
        }

        private static void ReleaseStaticMemory()
        {
            SelectedObjects.Clear();
            IcAnimatorExtension.Dispose();

            if (_foregroundCanvas != null) DestroyGameObject(_foregroundCanvas.gameObject);
            if (_backgroundCanvas != null) DestroyGameObject(_backgroundCanvas.gameObject);

            GC.Collect();
        }

        private void UpdateRenderTextureSize()
        {
            //destroy the render texture
            _renderTexture.Release();

            //Create the render texture
            _renderTexture =
                new RenderTexture(_resolutionInput.value.x, _resolutionInput.value.y, 0, RenderTextureFormat)
                {
                    name = "Icon Creator Render Texture",
                };

            _camera.targetTexture = _renderTexture;
            _renderTextureElement.style.backgroundImage = Background.FromRenderTexture(_renderTexture);

            UpdateCanvasAspect();
        }

        private void UpdateCanvasAspect()
        {
            float w = _resolutionInput.value.x / (float)_resolutionInput.value.y *
                      _renderTextureElement.worldBound.height;

            if (w < 10) return;

            _renderTextureElement.style.width = w;
            _transparentBackgroundElement.style.width = w;
        }

        internal void InitializeRenderTexture(bool forceInitialization)
        {
            if (!forceInitialization && _renderTexture != null)
            {
                SetAsBackground();
                return;
            }

            //UI
            _renderTextureElement = _root.Q<VisualElement>("render-texture");

            _resolutionInput = _root.Q<Vector2IntField>("resolution-input");
            _resolutionInput.RegisterCallback<FocusOutEvent>(OnResolutionChange);
            _resolutionInput.SetValueWithoutNotify(new Vector2Int(EditorPrefs.GetInt("IconCreator_Width", 512), EditorPrefs.GetInt("IconCreator_Height", 512)));

            _renderTextureElement.pickingMode = PickingMode.Position;
            _renderTextureElement.focusable = true;
            _renderTextureElement.RegisterCallback<KeyUpEvent>(OnKeyUp);

            //Camera
            InitializeCamera();

            //Render Texture
            if (_renderTexture != null) _renderTexture.Release();

            //Create the render texture
            _renderTexture = new RenderTexture(_resolutionInput.value.x, _resolutionInput.value.y, 0,
                RenderTextureFormat)
            {
                antiAliasing = 8,
                stencilFormat = GraphicsFormat.None,
                format = RenderTexturesExtensions.GetSavedRenderFormat(),
                depthStencilFormat = GraphicsFormatUtility.GetDepthStencilFormat(16, 4),
            };

            SetAsBackground();

#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] Render Texture Initialized! - Format {_renderTexture.format} - stencilFormat {_renderTexture.stencilFormat} - depthStencilFormat {_renderTexture.depthStencilFormat} - graphicsFormat {_renderTexture.graphicsFormat}");
#endif

            void SetAsBackground()
            {
                _root.Q<VisualElement>("transparent-background").style.width = 512;
                _renderTextureElement.style.backgroundImage = Background.FromRenderTexture(_renderTexture);
                _camera.targetTexture = _renderTexture;
            }
        }

        private void InitializeCamera()
        {
            if (_camera != null) return;

            GameObject found = GameObject.Find(CameraGameObjectName);

            if (found != null)
            {
                _camera = found.GetComponent<Camera>();
                if (_camera == null) _camera = found.AddComponent<Camera>();
            }
            else
            {
                GameObject cameraObj = new(CameraGameObjectName);
                _camera = cameraObj.AddComponent<Camera>();
            }

            _camera.targetTexture = _renderTexture;
            _cameraPreviousColor = _camera.backgroundColor;
            _camera.hideFlags = HideFlags.DontSave;
            _camera.transform.position = CameraStartPosition;

#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] Camera initialized! - Pos {_camera.transform.position}");
#endif
        }

        private void OnKeyUp(KeyUpEvent evt)
        {
            //Check for undo
            if (evt.keyCode == KeyCode.Z && evt.actionKey)
            {
                DateTime now = DateTime.Now;
                TimeSpan diff = now - _lastUndoTime;

                if (diff.TotalMilliseconds > 500) return;

                Undo.PerformUndo();
                evt.StopPropagation();
                //harpia log
                HarpiaLog("Undo");
                _lastUndoTime = now;
                return;
            }

            switch (evt.keyCode)
            {
                case KeyCode.E or KeyCode.Space:
                    OnCreateIconButton(null);
                    return;
                case KeyCode.RightArrow or KeyCode.D:
                    OnNextButton(null);
                    return;
                case KeyCode.LeftArrow or KeyCode.A:
                    OnPreviousButton(null);
                    return;
                case KeyCode.F:
                    OnAutoFovButton(null);
                    return;
            }
        }

        private void OnMouseWellRenderTexture(WheelEvent evt)
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] On mouse well {evt.delta.y}");
#endif

            float v = Mathf.Clamp(evt.delta.y, -ZoomSpeed, ZoomSpeed);

            float fieldOfView = _camera.fieldOfView;
            fieldOfView += fieldOfView * v;
            fieldOfView = Mathf.Clamp(fieldOfView, 1, 170f);

            _camera.fieldOfView = fieldOfView;
            _sliderCameraFov.SetValueWithoutNotify(fieldOfView);
            handler.GetCurrentObject().fieldOfView = fieldOfView;

            //Stop scrolling
            evt.StopPropagation();
        }

        private void OnDragRenderTexture(MouseMoveEvent evt)
        {
            //  HarpiaLog($"On drag - CurrentData == null {CurrentData == null} | evt.pressedButtons {evt.pressedButtons} | _camera == null {_camera == null}");

            IcObjectData CurrentData = handler.GetCurrentObject();

            if (CurrentData == null) return;

            if (evt.pressedButtons == 2)
            {
                //Lets move the lookAtOffset

                Vector3 offset = _camera.transform.TransformDirection(new Vector3(evt.mouseDelta.x, -evt.mouseDelta.y, 0) * 0.002f *
                                                                      CurrentData.size);
                offset *= _camera.fieldOfView / 15f;
                CurrentData.AddOffset(offset);
                UpdateLookAt();
            }

            else if (evt.pressedButtons == 1)
            {
                Transform cameraTransform = _camera.transform;
                Transform objectTransform = CurrentData.iconObject.transform;

                Vector3 rot = evt.mouseDelta * RotationSpeed;
                rot.x = -rot.x;
                rot.z = 0;

                Vector3 relativeUp = cameraTransform.TransformDirection(Vector3.up);
                Vector3 relativeRight = cameraTransform.TransformDirection(Vector3.right);

                Vector3 objectRelativeUp = objectTransform.InverseTransformDirection(relativeUp);
                Vector3 objectRelativeRight = objectTransform.InverseTransformDirection(relativeRight);

                Quaternion rotateBy = Quaternion.AngleAxis(rot.x, objectRelativeUp)
                                      * Quaternion.AngleAxis(-rot.y, objectRelativeRight);

                objectTransform.Rotate(rotateBy.eulerAngles);

                onRotationUpdate?.Invoke();

                UpdateRotationUI();
                UpdateLookAt();
            }
        }

        private void UpdateRotationUI()
        {
            _rotationField.SetValueWithoutNotify(handler.GetCurrentObject().iconObject.transform.rotation.eulerAngles - RotationOffset);
        }

        public static void CreateIcons(Dictionary<GameObject, string> objectDictionary)
        {
            if (objectDictionary.Count == 0)
            {
                Debug.LogError("[Icon Creator] No GameObjects.");
                return;
            }

            _pathDict = objectDictionary;

            ReleaseStaticMemory();
            SelectedObjects.Clear();
            SelectedObjects.AddRange(objectDictionary.Select(x => x.Key));
            CreateWindow();
        }

        [MenuItem("GameObject/Icon Creator/Create Selection Icons")]
        public static void CreateIconsHierarchy()
        {
            CreateIcons(false, true);
        }

        [MenuItem("GameObject/Icon Creator/Create Selection Icons", true)]
        public static bool CreateIconsHierarchyValidate()
        {
            if (Selection.gameObjects.Length == 0) return false;

            return Selection.gameObjects.Any(IcObjectData.HasAnyRenderer);
        }

        [MenuItem("Assets/Icon Creator/Create Icons - Folder")]
        public static void CreateIconsFolder()
        {
            HarpiaLog($"CreateIconsFolder here ");
            CreateIcons(true, false);
        }

        [MenuItem("Assets/Icon Creator/Create Icons - Selected Objects")]
        public static void CreateIconsSelected()
        {
            HarpiaLog($"Create Icons - Selected Objects here ");
            CreateIcons(false, false);
        }

        [MenuItem("Assets/Icon Creator/Create Icons - Selected Objects", true)]
        private static bool CreateIconsSelectedValidate()
        {
            return Selection.gameObjects.Length > 0;
        }

        private static void CreateIcons(bool isFolder, bool isFromHierarchy)
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] Creating Icons... Is from folder: {isFolder} - Is From Hierarchy {isFromHierarchy}");
#endif

            //Already has a instance
            if (_instance != null)
            {
                AddMorePrefabs(AuxClass.GetObjects(isFolder));
                HarpiaLog("CreateIcons - Adding more prefabs");
                _instance.Focus();
                return;
            }

            //Create a new Instance
            ReleaseStaticMemory();
            SelectedObjects.Clear();
            SelectedObjects.AddRange(AuxClass.GetObjects(isFolder));

#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] Creating Icons For {SelectedObjects.Count} objects ");
#endif

            if (SelectedObjects.Count == 0)
            {
                if (!isFolder)
                {
                    if (!isFromHierarchy) EditorUtility.DisplayDialog("Icon Creator", "No folder selected", "Ok");
                }
                else
                {
                    EditorUtility.DisplayDialog("Icon Creator", "No objects Prefabs selected", "Ok");
                    return;
                }
            }

            CreateWindow();
        }

        private static void AddMorePrefabs(List<GameObject> gameObjects)
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IconCreatorScript)}] Adding Mode {gameObjects.Count} prefabs ");
#endif

            SelectedObjects.AddRange(gameObjects);

            int added = _instance.InstantiateObjects(SelectedObjects, false);

            SelectedObjects.Clear();

            if (added > 0)
                EditorUtility.DisplayDialog("Icon Creator",
                    $"More {gameObjects.Count} were objects added to the current list", "Ok");

            _instance.UpdateUI();
        }

        private bool SaveImageFile(RenderTexture texture, out string finalFullPath)
        {
            finalFullPath = "";

            IcObjectData CurrentData = handler.GetCurrentObject();

            string saveLocation = CurrentData.HasCustomPath() ? CurrentData.GetCustomPath() : CurrentUserPath;

            if (!Directory.Exists(saveLocation))
            {
                //Lets create the folder
                if (AllowFolderCreation)
                {
                    Directory.CreateDirectory(saveLocation);
                }
            }

            string extension = "";
            switch (_fileFormatDropdown.value)
            {
                case FileFormat.PNG:
                    extension = ".png";
                    break;
                case FileFormat.JPG:
                    extension = ".jpg";
                    break;
            }

            string fullPath = saveLocation + "/" + _fileNameInput.value + extension;

            //Check if the file exists
            if (File.Exists(fullPath))
            {
                //Ask for replace
                if (!EditorUtility.DisplayDialog("Icon Creator - Warning", $"The file already exists:\n{fullPath}",
                        "Replace", "Cancel"))
                {
                    return false;
                }
            }

            try
            {
                Texture2D finalTexture = new(texture.width, texture.height, PNGFormat, false);

                RenderTexture.active = texture;

                finalTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);

                finalTexture.Apply();

                byte[] fileBytes;

                switch (_fileFormatDropdown.value)
                {
                    case FileFormat.PNG:
                        fileBytes = finalTexture.EncodeToPNG();
                        break;
                    case FileFormat.JPG:
                        fileBytes = finalTexture.EncodeToJPG(JPGQuality);
                        break;
                    default:
                        fileBytes = finalTexture.EncodeToPNG();
                        break;
                }

                DestroyImmediate(finalTexture);

                File.WriteAllBytes(fullPath, fileBytes);

                finalFullPath = fullPath;

                return true;
            }
            catch (Exception e)
            {
                //display error
                EditorUtility.DisplayDialog("Icon Creator - Error",
                    $"Error while saving the file:\n\n{e.Message}\n\n" +
                    "Please check the console log\n\n" +
                    "Trying changing the texture format on advanced options foldout.",
                    "Ok");
            }

            return false;
        }

        [MenuItem("Tools/Icon Creator/Open Documentation", false, 1)]
        private static void OpenDocumentation()
        {
            HarpiaLog("Open Documentation");
            Application.OpenURL("https://harpiagames.gitbook.io/icon-creator-documentation/selecting-objects");
        }

        [MenuItem("Tools/Icon Creator/Rate This Asset", false, 22)]
        private static void RateAsset()
        {
            HarpiaLog("Rate Asset");
            Application.OpenURL("https://u3d.as/2CcY");
        }

        [MenuItem("Tools/Icon Creator/Tutorial Videos", false, 21)]
        private static void OpenYoutube()
        {
            HarpiaLog("Open Youtube");
            Application.OpenURL("https://www.youtube.com/playlist?list=PLE4cvbnHS1NzwOv2Jgkhvzw6VbR0eH9C5");
        }

        [MenuItem("Tools/Icon Creator/Open Last Icons Folder", false, 2)]
        private static void OpenFolder()
        {
            HarpiaLog("Open Folder");

            if (Directory.Exists(CurrentUserPath) == false)
            {
                EditorUtility.DisplayDialog("Icon Creator - Error", $"The folder doesn't exist: {CurrentUserPath}",
                    "Ok");
                return;
            }

            EditorUtility.RevealInFinder(CurrentUserPath);
        }

#if HARPIA_DEBUG
        [MenuItem("Tools/Icon Creator/DEV Delete All Prefs")]
        private static void DeleteAllPrefs()
        {
            EditorPrefs.DeleteKey("IconCreator_CurrentPath");
            EditorPrefs.DeleteKey("IconCreator_AutomaticallyGoNext");
            EditorPrefs.DeleteKey("IconCreator_AutomaticallyFov");
            EditorPrefs.DeleteKey("IconCreator_TransparentBackground");
            EditorPrefs.DeleteKey("IconCreator_Height");
            EditorPrefs.DeleteKey("IconCreator_Width");
            EditorPrefs.DeleteKey("IconCreator_BackgroundCanvas");
            EditorPrefs.DeleteKey("IconCreator_ForegroundCanvas");
            EditorPrefs.DeleteKey("IconCreator_ShowGuideLines");
            EditorPrefs.DeleteKey(IcPrefsKeys.KFileFormat);

            IconCreatorPrefabIconLinkTool.DeleteAllKeys();

            HarpiaLog("Delete all prefs");
        }

#endif

        public static void HarpiaLog(string msg, bool isError = false, Object context = null)
        {
#if HARPIA_DEBUG
            msg = $"[Harpia Log] {msg}";
            if (isError)
            {
                Debug.LogError(msg, context);
                return;
            }

            Debug.Log(msg, context);
#endif
        }

        public static string GetCurrentExtension()
        {
            return Equals(_instance._fileFormatDropdown.value, FileFormat.PNG) ? ".png" : ".jpg";
        }
    }

    public static class AuxClass
    {
        public static List<GameObject> GetObjects(bool isFolder)
        {
            List<GameObject> objectsToRender = new();

            //Get all the prefabs in the folder

            if (isFolder)
            {
                string folder = GetSelectedPathOrFallback();

                if (string.IsNullOrEmpty(folder) == false && (Selection.objects.Length == 1 ||
                                                              (Selection.objects.Length == 0 &&
                                                               Selection.gameObjects.Length == 0)))
                {
                    string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { folder });

                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        objectsToRender.Add(obj);
                    }
                }

                return objectsToRender;
            }

            objectsToRender.AddRange(Selection.gameObjects);
            objectsToRender = objectsToRender.Where(x => x != null).ToList();

            return objectsToRender;
        }

        private static string GetSelectedPathOrFallback()
        {
            string path = "";
            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                path = Path.GetDirectoryName(path);
                break;
            }

            return path;
        }
    }

    public static class IcAnimatorExtension
    {
        public static Foldout animationSection;
        public static Slider animationTimeSlider;

        public static DropdownField dropdownAnimators;
        public static DropdownField dropdownAnimations;

        public static string currentAnimationState;
        public static Animator currentAnimator;
        private static List<Animator> _animators;
        private static GameObject _lastObject;

        public static void Init(GameObject o)
        {
            if (_lastObject != null && _lastObject == o) return;

            _lastObject = o;
            _animators = GetValidAnimators(o);

            if (_animators.Count == 0)
            {
                animationSection.SetActive(false);
                return;
            }

#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IcAnimatorExtension)}] Initializing animators - found {_animators.Count} valid animators");
#endif

            animationSection.SetActive(true);
            dropdownAnimators.choices = _animators.Select(x => x.name).ToList();
            dropdownAnimators.value = dropdownAnimators.choices[0];

            currentAnimator = _animators[0];
            currentAnimator.speed = 0;

            animationTimeSlider.SetValueWithoutNotify(0);
            UpdateAnimationsStates();
            UpdateAnimator(.2f);
        }

        static List<Animator> GetValidAnimators(GameObject root)
        {
            if (root == null)
            {
                return new List<Animator>();
            }

            Animator[] animatorsInChildren = root.GetComponentsInChildren<Animator>(true);
            List<Animator> validAnimators = new();

            foreach (Animator animator in animatorsInChildren)
            {
                if (animator.runtimeAnimatorController == null) continue;
                if (animator.runtimeAnimatorController.animationClips == null) continue;
                //  if(animator.runtimeAnimatorController.animationClips.Length == 0) continue;
                validAnimators.Add(animator);
            }

            return validAnimators;
        }

        public static void DisableRootMotion(GameObject o)
        {
            Animator[] animators = o.GetComponentsInChildren<Animator>(true);
            foreach (Animator animator in animators) animator.applyRootMotion = false;
        }

        public static void OnAnimatorDropdownChanged()
        {
            IconCreatorScript.HarpiaLog($"OnAnimatorChange");
            int index = dropdownAnimators.choices.IndexOf(dropdownAnimators.value);
            currentAnimator = _animators[index];
            currentAnimator.StartPlayback();
            UpdateAnimationsStates();
            UpdateAnimator(.2f);
        }

        private static void UpdateAnimationsStates()
        {
            AnimatorController controller = currentAnimator.runtimeAnimatorController as AnimatorController;

            if (controller == null)
            {
                dropdownAnimations.choices.Clear();
                return;
            }

            AnimatorState[] states = controller.layers[0].stateMachine.states.Select(x => x.state).ToArray();

            //Add the states
            dropdownAnimations.choices.Clear();
            foreach (AnimatorState state in states)
            {
                if (state == null) continue;
                dropdownAnimations.choices.Add(state.name);
            }

            dropdownAnimations.value = dropdownAnimations.choices[0];
        }

        public static void UpdateAnimator(float time)
        {
            if (currentAnimator == null) return;
            currentAnimator.Play(currentAnimationState, 0, time);
            currentAnimator.speed = 0;
            currentAnimator.Update(0.001f);
        }

        public static void Dispose()
        {
            currentAnimator = null;
            if (_animators != null) _animators.Clear();
            _animators = null;
        }
    }

    public static class AdvancedDirectionalLight
    {
        private static Dictionary<Light, float> _originalValues;

        public static void Init(VisualElement root)
        {
            VisualElement holder = root.Q<VisualElement>("directional-lights-section");

            //Get the directional lights
            _originalValues = new Dictionary<Light, float>();
            List<Light> directionalLights = Object.FindObjectsOfType<Light>().Where(light => light.type == LightType.Directional).ToList();

            foreach (Light light in directionalLights)
            {
                _originalValues.Add(light, light.intensity);

                Slider slider = new(0, light.intensity * 2, SliderDirection.Vertical)
                {
                    showInputField = true
                };

                slider.RegisterValueChangedCallback(evt =>
                {
                    if (light == null) return;
                    light.intensity = evt.newValue;
                });

                slider.SetValueWithoutNotify(light.intensity);

                holder.Add(slider);

                slider.tooltip = "Controls " + light.name;
                slider.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
                slider.style.width = new StyleLength(new Length(30, LengthUnit.Pixel));

                //add a context menu
                ContextualMenuManipulator menuManipulator = new(evt =>
                {
                    evt.menu.AppendAction("Reset", _ =>
                    {
                        slider.SetValueWithoutNotify(_originalValues[light]);
                        light.intensity = _originalValues[light];
                    });

                    evt.menu.AppendAction("Select Light", _ => { Selection.activeGameObject = light.gameObject; });
                });

                slider.AddManipulator(menuManipulator);
            }

#if HARPIA_DEBUG
            Debug.Log($"[{nameof(AdvancedDirectionalLight)}] Created advanced lighting menu  ");
#endif
        }

        public static void Dispose()
        {
            if (_originalValues == null) return;
            foreach (KeyValuePair<Light, float> value in _originalValues)
            {
                value.Key.intensity = value.Value;
            }
        }
    }

    [Serializable]
    public class IcObjectData
    {
        public GameObject originalObject;
        public GameObject iconObject;
        public float size;

        public string lastName;
        public string possibleIconPath;
        public bool tookScreenshot;
        public Vector3 lookAtOffset;
        public Transform lookAtTransform;
        public string lookAt = "Bounds Center";
        public float fieldOfView = -1;

        public IcObjectData(GameObject originalObject, GameObject iconObject, string path, string possibleIconPath)
        {
            //Check if original object is null
            if (!HasAnyRenderer(originalObject)) return;

            this.originalObject = originalObject;
            this.iconObject = iconObject;
            this.possibleIconPath = possibleIconPath;

            lastName = iconObject.name;

            if (!string.IsNullOrEmpty(path))
            {
                AutoName(path);
            }

            while (true)
            {
                Bounds bounds = IconCreatorScript.GetBounds(iconObject.transform);

                if (bounds.size.magnitude == 0) break;

                size = new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z).magnitude;

                if (size < 10)
                {
                    Vector3 localScale = iconObject.transform.localScale;
                    localScale += localScale;
                    iconObject.transform.localScale = localScale;
                    continue;
                }

                break;
            }

            Transform[] transforms = iconObject.GetComponentsInChildren<Transform>();

            foreach (Transform t in transforms)
            {
                RemoveUselessComponents(t.gameObject);
            }
        }

        public static bool HasAnyRenderer(GameObject obj)
        {
            if (obj.GetComponentInChildren<Renderer>(true) != null) return true;
            if (obj.GetComponentInChildren<ParticleSystem>(true) != null) return true;

#if UNITY_POST_PROCESSING_STACK_V2
            if (obj.GetComponentInChildren<VisualEffect>(true) != null) return true;
#endif

            return false;
        }

        public void SetLastName(string name)
        {
            lastName = name;
        }

        public bool IsInstantiated()
        {
            return iconObject != null;
        }

        public List<string> GetLookAtOptions()
        {
            List<string> options = new()
            {
                "None",
                "Bounds Center",
                "Object Pivot",
                "Custom Transform"
            };

            return options;
        }

        public void SetLookAt(string evtNewValue)
        {
            lookAtOffset = Vector3.zero;
            lookAt = evtNewValue;
        }

        public void AutoName(string currentPath)
        {
            string objName = originalObject.name.Replace(IconCreatorScript.IconCreatorExtension, "");
            string fullPath = objName + IconCreatorScript.GetCurrentExtension();

            if (!File.Exists(fullPath))
            {
                objName = objName.Trim();
                SetLastName(objName);
                return;
            }

            int counter = 1;

            fullPath = Path.Combine(currentPath, objName + " " + counter + IconCreatorScript.GetCurrentExtension());

            while (File.Exists(fullPath))
            {
                counter++;
                fullPath = Path.Combine(currentPath, objName + " " + counter + IconCreatorScript.GetCurrentExtension());
            }

            objName = objName + " " + counter.ToString();
            objName = objName.Trim();

            SetLastName(objName);
        }

        public void SetLookAtTransform(Transform evtNewValue)
        {
            lookAtTransform = evtNewValue;
        }

        public void AddOffset(Vector3 offset)
        {
            lookAtOffset += offset;
        }

        private void RemoveUselessComponents(GameObject a)
        {
            Transform[] allTransforms = a.GetComponentsInChildren<Transform>();

            foreach (Transform transform in allTransforms)
            {
                Component[] all = transform.GetComponents<Component>();

                //Lets remove all the components that are not needed

                foreach (Component component in all)
                {
                    try
                    {
                        Type type = component.GetType();

                        if (type == typeof(Transform)) continue;
                        if (type == typeof(MeshFilter)) continue;
                        if (type == typeof(MeshRenderer)) continue;
                        if (type == typeof(Animator)) continue;
                        if (type == typeof(Animation)) continue;
                        if (type == typeof(SkinnedMeshRenderer)) continue;
                        if (type == typeof(Renderer)) continue;
                        if (type == typeof(TrailRenderer)) continue;
                        if (type == typeof(ParticleSystem))
                        {
                            ParticleSystem ps = (ParticleSystem)component;
                            ps.scalingMode = ParticleSystemScalingMode.Hierarchy;
                            continue;
                        }

                        if (type == typeof(Rigidbody))
                        {
                            Rigidbody rb = (Rigidbody)component;
                            rb.isKinematic = true;
                            continue;
                        }

                        if (type == typeof(Collider))
                        {
                            Collider col = (Collider)component;
                            col.enabled = false;
                            continue;
                        }

                        MonoBehaviour mono = component as MonoBehaviour;
                        mono.enabled = false;
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }
        }

        public bool IsValid() => iconObject != null;

        public bool HasCustomPath() => !string.IsNullOrEmpty(possibleIconPath);

        public string GetCustomPath()
        {
            string ret = possibleIconPath;
            //remove filename
            ret = Path.GetDirectoryName(ret);
            return ret;
        }

        public void DisposeObj(IC_ObjectListHandler handler)
        {
            handler.RemoveObject(this);
            if (iconObject == null) return;
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IcObjectData)}] Disposing object {iconObject.gameObject.name}");
#endif
            IconCreatorScript.DestroyGameObject(iconObject);
        }
    }

    public class IcAdvancedOptionsData
    {
        public float zoomSpeed;
        public float rotationSpeed;
        public float distanceFromCamera;

        public Vector3 offsetDirection;
        public static Vector3 SpawnPosition => new(9999, 9999, 9999);

        public TextureFormat pngFormat;
        public RenderTextureFormat renderTextureFormat;

        //public KeyCode createIconShortcutKey;
        //public KeyCode nextIconShortcutKey;
        //public KeyCode previousIconShortcutKey;

        public int jpgQuality;

        //--------------------------- UI

        public readonly Slider zoomSpeedSlider;
        public readonly Slider rotationSpeedSlider;
        public readonly FloatField distanceFromCameraInput;
        public readonly Vector3Field directionVec3Field;

        public readonly DropdownField pngFormatEnumField;
        public readonly DropdownField renderTextureFormatEnumField;

        //public readonly EnumField createIconShortcut;
        //public readonly EnumField nextIconShortcut;
        //public readonly EnumField previousIconShortcut;
        public readonly SliderInt jpgQualitySlider;
        public readonly Vector3Field spawnPositionField;

        private readonly IconCreatorScript _iconCreatorScript;

        public IcAdvancedOptionsData(VisualElement root, IconCreatorScript iconCreatorScript)
        {
            _iconCreatorScript = iconCreatorScript;

            //Live variables
            zoomSpeedSlider = root.Q<Slider>("zoom-speed");
            rotationSpeedSlider = root.Q<Slider>("rotation-speed");

            renderTextureFormatEnumField = root.Q<DropdownField>("render-texture-format");
            renderTextureFormatEnumField.RegisterValueChangedCallback(OnRenderTextureFormatChanged);
            renderTextureFormatEnumField.choices = RenderTexturesExtensions.GetRenderFormatChoices();

            //Save variables
            pngFormatEnumField = root.Q<DropdownField>("png-format");
            pngFormatEnumField.RegisterValueChangedCallback(OnPngFormatChanged);
            pngFormatEnumField.choices = RenderTexturesExtensions.GetTextureFormatChoices();

            jpgQualitySlider = root.Q<SliderInt>("jpg-quality");

            //Reset variables
            spawnPositionField = root.Q<Vector3Field>("spawn-pos");
            directionVec3Field = root.Q<Vector3Field>("offset-direction");
            distanceFromCameraInput = root.Q<FloatField>("distance-from-camera");

            directionVec3Field.SetValueWithoutNotify(directionVec3Field.value.normalized);

            root.Q<Button>("reset-advanced").RegisterCallback<ClickEvent>(Reset);

            directionVec3Field.RegisterValueChangedCallback(OnDirectionChanged);

            Load();
        }

        private void OnPngFormatChanged(ChangeEvent<string> changeEvent)
        {
            IconCreatorScript.HarpiaLog($"OnPngFormatChanged - {changeEvent.newValue}");
        }

        private void OnDirectionChanged(ChangeEvent<Vector3> evt)
        {
            Vector3 normalized = evt.newValue.normalized;
            directionVec3Field.SetValueWithoutNotify(normalized);
        }

        private void OnRenderTextureFormatChanged(ChangeEvent<string> changeEvent)
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IcAdvancedOptionsData)}] OnRenderTextureFormatChanged - {changeEvent.newValue}");
#endif
            RenderTexturesExtensions.SetSavedFormat(changeEvent.newValue);
            _iconCreatorScript.InitializeRenderTexture(true);
        }

        private void Load()
        {
            string json = EditorPrefs.GetString("IconCreatorAdvancedOptions", "");

            if (string.IsNullOrEmpty(json)) return;

            JsonUtility.FromJsonOverwrite(json, this);

            zoomSpeedSlider.SetValueWithoutNotify(zoomSpeed);
            rotationSpeedSlider.SetValueWithoutNotify(rotationSpeed);
            distanceFromCameraInput.SetValueWithoutNotify(distanceFromCamera);
            directionVec3Field.SetValueWithoutNotify(offsetDirection);
            pngFormatEnumField.SetValueWithoutNotify(pngFormat.ToString());
            renderTextureFormatEnumField.SetValueWithoutNotify(renderTextureFormat.ToString());

            jpgQualitySlider.SetValueWithoutNotify(jpgQuality);
            spawnPositionField.SetValueWithoutNotify(SpawnPosition);
        }

        public void Save()
        {
            zoomSpeed = zoomSpeedSlider.value;
            rotationSpeed = rotationSpeedSlider.value;
            distanceFromCamera = distanceFromCameraInput.value;
            offsetDirection = directionVec3Field.value;
            pngFormat = RenderTexturesExtensions.ParseTexture(pngFormatEnumField.value);
            Enum.TryParse(renderTextureFormatEnumField.value, out renderTextureFormat);
            jpgQuality = jpgQualitySlider.value;
            //SpawnPosition = spawnPositionField.value;

            string json = JsonUtility.ToJson(this);
            EditorPrefs.SetString("IconCreatorAdvancedOptions", json);

            //log
            IconCreatorScript.HarpiaLog($"Saved advanced options: {json}");
        }

        public void Reset(ClickEvent evt)
        {
#if HARPIA_DEBUG
            Debug.Log($"[{nameof(IcAdvancedOptionsData)}]Reset advanced options ");
#endif

            zoomSpeedSlider.value = .14f;
            rotationSpeedSlider.value = 1f;
            distanceFromCameraInput.value = 10f;
            directionVec3Field.value = new Vector3(0, -0.5f, 1).normalized;
            pngFormatEnumField.value = RenderTexturesExtensions.GetDefaultTextureFormatValue();
            renderTextureFormatEnumField.value = RenderTexturesExtensions.GetDefaultRenderValue();
            jpgQualitySlider.value = 100;
            spawnPositionField.value = new Vector3(9999, -9999, 9999);
        }

        public RenderTextureFormat GetRenderFormat()
        {
            bool parsed = Enum.TryParse(typeof(RenderTextureFormat), renderTextureFormatEnumField.value, out object result);
            if (parsed) return (RenderTextureFormat)result;
            return (RenderTextureFormat)Enum.Parse(typeof(RenderTextureFormat), RenderTexturesExtensions.GetDefaultRenderValue());
        }

        public TextureFormat GetTextureFormat()
        {
            bool parsed = Enum.TryParse(typeof(TextureFormat), pngFormatEnumField.value, out object result);
            if (parsed) return (TextureFormat)result;
            return (TextureFormat)Enum.Parse(typeof(TextureFormat), RenderTexturesExtensions.GetDefaultTextureFormatValue());
        }
    }

    public class ParticleSystemHandler
    {
        public readonly ParticleSystem particleSystem;
        public readonly Slider timeSlider;
        private static GameObject _lastSelectedGameObject;

        public ParticleSystemHandler(ParticleSystem particleSystem, VisualElement sliderParent, IconCreatorScript script)
        {
            this.particleSystem = particleSystem;
            this.particleSystem.Stop();
            particleSystem.useAutoRandomSeed = false;
            particleSystem.randomSeed = (uint)Random.Range(0, 50);
            particleSystem.Stop();

            string goName = particleSystem.gameObject.name.Replace(IconCreatorScript.IconCreatorExtension, "");

            ParticleSystem.MainModule mainModule = this.particleSystem.main;
            float max = Mathf.Max(mainModule.duration, mainModule.startLifetime.constantMax, 1);
            timeSlider = new Slider(0, max)
            {
                value = .3f,
                showInputField = true
            };
            timeSlider.RegisterValueChangedCallback(OnTimeSliderChanged);
            timeSlider.label = $"{goName}";
            sliderParent.Add(timeSlider);

            script.onRotationUpdate += OnRotationUpdated;
            OnRotationUpdated();
        }

        private void OnRotationUpdated()
        {
            if (particleSystem == null) return;
            if (timeSlider == null) return;
            particleSystem.Simulate(timeSlider.value);
        }

        private void OnTimeSliderChanged(ChangeEvent<float> evt)
        {
            ParticleSystem.MainModule particleSystemMain = particleSystem.main;
            particleSystemMain.simulationSpeed = 1;
            particleSystem.Simulate(evt.newValue);
            particleSystemMain.simulationSpeed = 0;
        }

        public static void Init(Foldout foldoutParticles, GameObject currentObject, IconCreatorScript script)
        {
            if (currentObject == null) return;
            if (_lastSelectedGameObject != null)
            {
                if (_lastSelectedGameObject == currentObject) return;
            }

            _lastSelectedGameObject = currentObject;

            foldoutParticles.Clear();
            ParticleSystem[] particleSystems = currentObject.GetComponentsInChildren<ParticleSystem>();
            if (particleSystems.Length == 0)
            {
                foldoutParticles.SetActive(false);
                return;
            }

            foldoutParticles.SetActive(true);

            foreach (ParticleSystem particleSystem in particleSystems)
            {
                //Check null
                if (particleSystem == null) continue;
                particleSystem.useAutoRandomSeed = false;
                ParticleSystemHandler particleSystemHandler = new(particleSystem, foldoutParticles, script);
            }
        }

        public static bool IsOnlyParticleSystem(Transform transform)
        {
            MeshRenderer r = transform.GetComponentInChildren<MeshRenderer>();
            if (r != null) return false;

            SkinnedMeshRenderer s = transform.GetComponentInChildren<SkinnedMeshRenderer>();
            if (s != null) return false;

            SpriteRenderer spriteRenderer = transform.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) return false;

            ParticleSystem particleSystem = transform.GetComponentInChildren<ParticleSystem>();
            if (particleSystem == null) return false;

            return true;
        }
    }

    public static class VisualElementsExtension
    {
        public static VisualElement focusElement;

        public static void SetBackgroundColor(this VisualElement e, Color c)
        {
            if (c == Color.clear)
            {
                e.style.backgroundColor = new StyleColor(StyleKeyword.Null);
                return;
            }

            e.style.backgroundColor = c;
        }

        public static void SetBackgroundTexture(this VisualElement e, Texture n)
        {
#if HARPIA_DEBUG
            if (n == null) Debug.LogError($"Texture is null for {e.name}");
#endif
            e.style.backgroundImage = (StyleBackground)n;
        }

        public static void SetBackgroundTexture(this VisualElement e, Texture2D n)
        {
#if HARPIA_DEBUG
            if (e == null) Debug.LogError($"visual element is null");
            if (n == null) Debug.LogError($"Texture2D is null for {e.name}");
#endif
            e.style.backgroundImage = n;
        }

        public static Texture GetBackgroundTexture(this VisualElement e)
        {
            Background img = e.style.backgroundImage.value;
            if (img == null) return null;

            if (img.texture != null) return img.texture;
            if (img.sprite != null) return img.sprite.texture;
            if (img.renderTexture != null) return img.renderTexture;

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetActive(this VisualElement e, bool n)
        {
            e.style.display = n ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static void SetVisible(this VisualElement e, bool n)
        {
            e.style.visibility = new StyleEnum<Visibility>(n ? Visibility.Visible : Visibility.Hidden);
        }

        public static bool IsActive(this VisualElement e)
        {
            return e.style.display == DisplayStyle.Flex;
        }

        public static void SetBorderColor(this VisualElement element, Color c, float width = 2f)
        {
            element.style.borderBottomWidth = width;
            element.style.borderTopWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;

            element.style.borderBottomColor = c;
            element.style.borderTopColor = c;
            element.style.borderLeftColor = c;
            element.style.borderRightColor = c;
        }

        public static void SetBorderRadius(this VisualElement element, float r)
        {
            element.style.borderBottomLeftRadius = r;
            element.style.borderBottomRightRadius = r;
            element.style.borderTopLeftRadius = r;
            element.style.borderTopRightRadius = r;
        }

        public static void SetBorderPadding(this VisualElement element, float r)
        {
            element.style.paddingBottom = r;
            element.style.paddingTop = r;
            element.style.paddingLeft = r;
            element.style.paddingRight = r;
        }

        public static void BorderColorOnHover(this VisualElement e, Color c)
        {
            e.RegisterCallback<MouseEnterEvent>(_ => e.SetBorderColor(c));
            e.RegisterCallback<MouseOutEvent>(_ => e.SetBorderColor(Color.clear));
        }

        public static void RegisterFocusEvents(this VisualElement element)
        {
            element.RegisterCallback<FocusInEvent>(_ => { focusElement = element; });

            element.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (focusElement == null) return;
                if (focusElement == element) focusElement = null;
            });
        }
    }

    public static class RenderTexturesExtensions
    {
        private static List<RenderTextureFormat> _supportedRenderFormats;
        private static List<TextureFormat> _supportedTextureFormats;
        private static string _savedFormat;

        private static List<RenderTextureFormat> GetSupportedRenderFormats()
        {
            List<RenderTextureFormat> ret = new();

            foreach (object v in Enum.GetValues(typeof(RenderTextureFormat)))
            {
                RenderTextureFormat format = (RenderTextureFormat)v;
                if (!SystemInfo.SupportsRenderTextureFormat(format)) continue;
                ret.Add(format);
            }

            return ret;
        }

        private static List<TextureFormat> GetSupportedTextureFormats()
        {
            List<TextureFormat> ret = new();

            foreach (object v in Enum.GetValues(typeof(TextureFormat)))
            {
                TextureFormat format = (TextureFormat)v;

                try
                {
                    if (!SystemInfo.SupportsTextureFormat(format)) continue;
                    ret.Add(format);
                }
                catch (ArgumentException exception)
                {
#if HARPIA_DEBUG
                    Debug.Log($"[{nameof(RenderTexturesExtensions)}] Could not add {format} to the list - {exception.Message} ");
#endif
                }
            }

            return ret;
        }

        public static RenderTextureFormat GetSavedRenderFormat()
        {
            bool parsed = Enum.TryParse(typeof(RenderTextureFormat), _savedFormat, out object result);
            if (parsed) return (RenderTextureFormat)result;
            return (RenderTextureFormat)Enum.Parse(typeof(RenderTextureFormat), GetDefaultRenderValue());
        }

        public static List<string> GetRenderFormatChoices()
        {
            _supportedRenderFormats ??= GetSupportedRenderFormats();
            return _supportedRenderFormats.Select(x => x.ToString()).ToList();
        }

        public static List<string> GetTextureFormatChoices()
        {
            _supportedTextureFormats ??= GetSupportedTextureFormats();
            return _supportedTextureFormats.Select(x => x.ToString()).ToList();
        }

        public static void SetSavedFormat(string changeEventNewValue)
        {
            _savedFormat = changeEventNewValue;
        }

        public static TextureFormat ParseTexture(string value) => (TextureFormat)Enum.Parse(typeof(TextureFormat), value);

        public static string GetDefaultRenderValue()
        {
            _supportedRenderFormats ??= GetSupportedRenderFormats();

            if (_supportedRenderFormats.Contains(RenderTextureFormat.ARGB32)) return RenderTextureFormat.ARGB32.ToString();

            return _supportedRenderFormats[0].ToString();
        }

        public static string GetDefaultTextureFormatValue()
        {
            _supportedTextureFormats ??= GetSupportedTextureFormats();
            if (_supportedTextureFormats.Contains(TextureFormat.ARGB32)) return RenderTextureFormat.ARGB32.ToString();
            return _supportedTextureFormats[0].ToString();
        }
    }

    public static class IcPrefsKeys
    {
        /// <summary>
        /// PNG or JPG
        /// </summary>
        public const string KFileFormat = "IconCreator_FileFormat";
    }

    public class IC_ObjectListHandler
    {
        public List<IcObjectData> currentList;
        public int TotalObjects => currentList.Count;
        public int currentObjectIndex = -1;

        public void ApplyRotation(Quaternion rotation)
        {
            foreach (IcObjectData data in currentList)
            {
                if (data == null) continue;
                if (data.iconObject == null) continue;
                data.iconObject.transform.rotation = rotation;
            }
        }

        public void Dispose()
        {
            currentObjectIndex = -1;

            if (currentList == null) return;

            while (currentList.Count > 0)
            {
                if (currentList[0] == null)
                {
                    currentList.RemoveAt(0);
                    continue;
                }

                currentList[0].DisposeObj(this);
            }
        }

        public GameObject GetNextObject()
        {
            int index = currentObjectIndex + 1;
            if (index >= TotalObjects) index = 0;
            return currentList[index].iconObject;
        }

        public IcObjectData GetCurrentObject()
        {
            if (currentList == null) return null;
            if (currentList.Count == 0) return null;
            currentObjectIndex = Mathf.Clamp(currentObjectIndex, 0, currentList.Count - 1);
            return currentList[currentObjectIndex];
        }

        public bool ContainsObject(GameObject prefab)
        {
            if (currentList == null) return false;
            return currentList.Any(e => e.originalObject == prefab);
        }

        public void SetCurrentObjectActive(bool b)
        {
            GetCurrentObject().iconObject.SetActive(b);
        }

        public GameObject GetPreviousObject()
        {
            int index = currentObjectIndex - 1;
            if (index < 0) index = TotalObjects - 1;
            return currentList[index].iconObject;
        }

        public void AddToIndex(int i)
        {
            currentObjectIndex += i;
            if (currentObjectIndex < 0) currentObjectIndex = TotalObjects - 1;
            else if (currentObjectIndex > TotalObjects - 1) currentObjectIndex = 0;
        }

        public Transform GetCurrentObjectTransform()
        {
            return GetCurrentObject().iconObject.transform;
        }

        public void AddObject(IcObjectData newObject)
        {
            if (newObject == null) return;
            currentList ??= new();
            currentList.Add(newObject);
            if (currentObjectIndex < 0) currentObjectIndex = 0;
        }

        public void RemoveObject(IcObjectData icObjectData)
        {
            if (currentList.Contains(icObjectData) == false) return;
            currentList.Remove(icObjectData);
            if (currentObjectIndex > TotalObjects - 1) currentObjectIndex = TotalObjects - 1;
        }

        public GameObject GetCurrentIconObject()
        {
            IcObjectData current = GetCurrentObject();
            if (current == null) return null;
            return current.iconObject;
        }
    }

    public static class RenderSettingsExtensions
    {
        //The ambient color when the icons start to being created
        private static readonly Color AmbientStartColor = new(0.89f, 0.89f, 0.89f);
        private static Color _oldAmbientLight;
        private static AmbientMode _oldAmbientMode;
        private static bool initialized;

        public static void Initialize()
        {
            if (initialized) return;

            _oldAmbientLight = RenderSettings.ambientLight;
            _oldAmbientMode = RenderSettings.ambientMode;

            RenderSettings.ambientLight = AmbientStartColor;
            RenderSettings.ambientMode = AmbientMode.Flat;

            initialized = true;
        }

        public static void Dispose()
        {
            if (!initialized) return;
            RenderSettings.ambientLight = _oldAmbientLight;
            RenderSettings.ambientMode = _oldAmbientMode;
            initialized = false;
        }

        public static void OnAmbientLightChange(ChangeEvent<Color> evt)
        {
            RenderSettings.ambientLight = evt.newValue;
            RenderSettings.ambientMode = AmbientMode.Flat;

            //force scene update
            EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}