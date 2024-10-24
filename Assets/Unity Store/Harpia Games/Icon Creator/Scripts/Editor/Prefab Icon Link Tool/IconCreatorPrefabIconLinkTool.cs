using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace Harpia.IconCreator
{
    public class IconCreatorPrefabIconLinkTool : EditorWindow
    {
        private VisualElement _searchResultParent;
        private VisualElement _comparisonResults;
        
        private Label _resultLabel;
        private Label _prefabsRootLabel;
        private Label _iconsDestPathLabel;
        
        private Button _comparisonButton;
        private Button _createIconsButton;

        private List<SearchResult> _searchResults;

        private static string PrefabsRootPath => EditorPrefs.GetString(key_PrefabsRootPath);
        private static bool CopyFolderStructure => EditorPrefs.GetBool(key_CopyFolderStructure, true);
        private static string IconsDestinationPath => EditorPrefs.GetString(key_IconsDestinationPath);

        private static string _visualTreeGuid;
        
        private const string key_PrefabsRootPath = "IconCreator_rootPath";
        private const string key_IconsDestinationPath = "IconCreator_destinationPath";
        private const string key_CopyFolderStructure = "IconCreator_copyFolderStructure";
        private const string xmlFileName = "IconCreator_PrefabIconLinkToolXML";

        [MenuItem("Tools/Icon Creator/Prefab Icon Link Tool")]
        public static void ShowWindow()
        {
            //log opening
            Debug.Log($"[ICON CREATOR] Opening Icon Creator");
            IconCreatorPrefabIconLinkTool wnd = GetWindow<IconCreatorPrefabIconLinkTool>();
            wnd.titleContent = new GUIContent("Prefab Icon Link Tool");
        }

        public void CreateGUI()
        {
            //Find the XML file path
            string xmlFilePath = AssetDatabase.GUIDToAssetPath(_visualTreeGuid);
            if (string.IsNullOrEmpty(xmlFilePath))
            {
                string[] foundedGUIDs = AssetDatabase.FindAssets(xmlFileName);

                if (foundedGUIDs.Length == 0)
                {
                    Debug.LogError(
                        $"Could not find the {xmlFileName}.uxml, did you renamed the file? If so rename it {xmlFileName}.uxml",
                        this);
                    return;
                }

                //get the first founded path
                _visualTreeGuid = foundedGUIDs[0];
                xmlFilePath = AssetDatabase.GUIDToAssetPath(_visualTreeGuid);
            }

            // Each editor window contains a root VisualElement object
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(xmlFilePath);

            if (visualTree == null)
            {
                //log error
                Debug.LogError($"{xmlFilePath}");
                Debug.LogError(
                    $"Could not find the {xmlFileName}.uxml, did you renamed the file? If so rename it {xmlFileName}.uxml",
                    this);
                return;
            }

            VisualElement root = rootVisualElement;

            VisualElement labelFromUxml = visualTree.Instantiate();
            root.Add(labelFromUxml);

            Button findPrefabsButton = root.Q<Button>("find-root-folder");
            findPrefabsButton.RegisterCallback<ClickEvent>(OnFindPrefabsButton);

            Button findIconsDestButton = root.Q<Button>("find-destination-folder");
            findIconsDestButton.RegisterCallback<ClickEvent>(OnFindIconsDestButton);

            _searchResultParent = root.Q<VisualElement>("search-result");

            _prefabsRootLabel = root.Q<Label>("root-folder-label");
            _iconsDestPathLabel = root.Q<Label>("destination-folder-label");

            _prefabsRootLabel.text = $"Root Path:\n<i>{PrefabsRootPath}</i>";
            _iconsDestPathLabel.text = $"Icons Path:\n<i>{IconsDestinationPath}</i>";
            

            Assert.IsNotNull(_prefabsRootLabel);
            Assert.IsNotNull(_iconsDestPathLabel);

            _comparisonButton = root.Q<Button>("compare-button");
            _comparisonButton.RegisterCallback<ClickEvent>(OnCompareFolders);

            //Copy folder structure toggle
            var copyFolderStructureToggle = root.Q<Toggle>("copy-folder-structure");
            copyFolderStructureToggle.SetValueWithoutNotify(EditorPrefs.GetBool(key_CopyFolderStructure, true));
            copyFolderStructureToggle.RegisterValueChangedCallback(evt =>
            {
                EditorPrefs.SetBool(key_CopyFolderStructure, evt.newValue);
            });

            _resultLabel = root.Q<Label>("result-label");
            root.Q<Button>("select-all").RegisterCallback<ClickEvent>(OnSelectAll);
            root.Q<Button>("select-none").RegisterCallback<ClickEvent>(OnDeselectAll);

            _createIconsButton = root.Q<Button>("create-icons");
            _createIconsButton.RegisterCallback<ClickEvent>(OnCreateIcons);

            _comparisonResults = root.Q<VisualElement>("comparison-result");

            _comparisonButton.style.display = DisplayStyle.Flex;

            HideAndClearOldSearch();
        }

        private void OnCreateIcons(ClickEvent evt)
        {
            HarpiaLog("On Create Icons");

            List<SearchResult> selected = _searchResults.Where(x => x.IsSelected).ToList();

            Dictionary<GameObject, string> possiblePaths = new();
            
            foreach (SearchResult result in selected)
            {
                possiblePaths.Add(result.gameObject, result.desiredIconLocation);
            }

            IconCreatorScript.CreateIcons(possiblePaths);
        }

        private void OnDeselectAll(ClickEvent evt)
        {
            HarpiaLog("On deselect all");
            foreach (SearchResult result in _searchResults)
            {
                result.Select(false);
            }
        }

        private void OnSelectAll(ClickEvent evt)
        {
            HarpiaLog("On Select All");

            foreach (SearchResult result in _searchResults)
            {
                result.Select(true);
            }
        }


        private void OnCompareFolders(ClickEvent evt)
        {
            HarpiaLog("On Compare Folders");

            if (Directory.Exists(IconsDestinationPath) == false || string.IsNullOrEmpty(IconsDestinationPath))
            {
                EditorUtility.DisplayDialog("Error", $"Icons destination folder does not exist\n{IconsDestinationPath}", "Ok");
                return;
            }

            if (Directory.Exists(PrefabsRootPath) == false || string.IsNullOrEmpty(PrefabsRootPath))
            {
                EditorUtility.DisplayDialog("Error", $"Root folder does not exist\n{PrefabsRootPath}", "Ok");
                return;
            }

            _searchResultParent.Clear();

            string[] prefabsFiles = Directory.GetFiles(PrefabsRootPath, "*.prefab", SearchOption.AllDirectories);

            if (prefabsFiles.Length == 0)
            {
                //Load the prefab
                Label label = new("No prefabs found");
                _searchResultParent.Add(label);
                return;
            }

            string[] imageFilesArray = Directory.GetFiles(IconsDestinationPath, "*.jpg", SearchOption.AllDirectories);
            IEnumerable<string> imageFiles =
                imageFilesArray.Concat(Directory.GetFiles(IconsDestinationPath, "*.png", SearchOption.AllDirectories));


            _searchResults = new List<SearchResult>();

            int count = 0;

            foreach (string prefabPath in prefabsFiles)
            {
                //Load prefab
                var loadPath = prefabPath.Replace(Application.dataPath, "Assets");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(loadPath);

                if (prefab == null)
                {
                    Debug.LogError($"[Prefab Icon Link Tool] Could not load prefab at {prefabPath}", this);
                    continue;
                }
                if (!IcObjectData.HasAnyRenderer(prefab)) continue;

                string fileName = Path.GetFileName(prefabPath).Split('.')[0].ToLower();
                IEnumerable<string> foundedImages = imageFiles.Where(e => CheckFileName(fileName, e));
                SearchResult searchResult = new(prefabPath, foundedImages.ToList(), CopyFolderStructure, IconsDestinationPath, prefabPath);

                if (searchResult.gameObject == null) continue;

                _searchResults.Add(searchResult);
                searchResult.Display(_searchResultParent);
                count++;
            }

            _resultLabel.text = $"Found {count} prefabs";
            _comparisonResults.style.display = DisplayStyle.Flex;
            _createIconsButton.parent.style.display = DisplayStyle.Flex;
        }

        private bool CheckFileName(string fileNameRef, string fullPath)
        {
            //log both
            string fileName = Path.GetFileName(fullPath).Split('.')[0].ToLower();
            fileNameRef = fileNameRef.ToLower();

            if (!fileName.Contains(fileNameRef)) return false;

            return fileName.StartsWith(fileNameRef[0]);
        }


        private void OnFindIconsDestButton(ClickEvent evt)
        {
            HarpiaLog("On Find Prefab Button");

            //Show the folder selection window
            string temp = EditorUtility.OpenFolderPanel("Select the Icons destination folder", "", "");

            if (!Directory.Exists(temp))
            {
                //Display error
                EditorUtility.DisplayDialog("Error", "Invalid folder", "Ok");
                return;
            }

            EditorPrefs.SetString(key_IconsDestinationPath, temp);
            _iconsDestPathLabel.text = $"Icons Path:\n<i>{IconsDestinationPath}</i>";


            HideAndClearOldSearch();
        }

        private void OnFindPrefabsButton(ClickEvent evt)
        {
            HarpiaLog("On Find Root Button");

            //Show the folder selection window
            string temp = EditorUtility.OpenFolderPanel("Select the Prefabs root folder", "", "");

            if (!Directory.Exists(temp))
            {
                //Display error
                EditorUtility.DisplayDialog("Error", "Invalid folder", "Ok");
                return;
            }


            EditorPrefs.SetString(key_PrefabsRootPath, temp);
            _prefabsRootLabel.text = "Root Path:\n<i>" + PrefabsRootPath + "</i>";

            HideAndClearOldSearch();
        }

        private void HideAndClearOldSearch()
        {
            _searchResultParent?.Clear();
            _searchResults?.Clear();

            _resultLabel.text = "";
            _comparisonResults.style.display = DisplayStyle.None;
            _createIconsButton.parent.style.display = DisplayStyle.None;
        }

        public static void DeleteAllKeys()
        {
            EditorPrefs.DeleteKey(key_PrefabsRootPath);
            EditorPrefs.DeleteKey(key_IconsDestinationPath);
            EditorPrefs.DeleteKey(key_CopyFolderStructure);
        }

        private static void HarpiaLog(string msg, bool isError = false)
        {
#if HARPIA_DEBUG
            msg = $"[Harpia Log] {msg}";
            if (isError)
            {
                Debug.LogError(msg);
                return;
            }

            Debug.Log(msg);
#endif
        }
    }

    public class SearchResult
    {
        public readonly GameObject gameObject;
        public readonly string desiredIconLocation;

        private Toggle _toggle;
        private VisualElement _detailsParent;
        private readonly string _prefabLocation;
        private readonly List<string> _possibleIcons;


        public SearchResult(string fullPrefabLocation, List<string> possibleIcons, bool copyFolderTree,
            string iconDestinationPath, string prefabSearchPath)
        {
            _prefabLocation = fullPrefabLocation.Replace(Application.dataPath, "Assets");
            gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabLocation);

            if (gameObject == null)
            {
                Debug.LogError("[Icon Creator] Error loading prefab at path: " + _prefabLocation);
                return;
            }

            if (!IcObjectData.HasAnyRenderer(gameObject))
            {
                //Object has no renderer
                return;
            }

            _possibleIcons = possibleIcons;

            if (copyFolderTree)
            {
                iconDestinationPath = iconDestinationPath.Replace(Application.dataPath, "Assets").Replace("\\", "/");
                prefabSearchPath = prefabSearchPath.Replace(Application.dataPath, "Assets").Replace("\\", "/");

                //find the same folder structure between the prefab and the destination folder
                string sameFolderStructure = "";
                for (int i = 0; i < prefabSearchPath.Length; i++)
                {
                    if (iconDestinationPath[i] == prefabSearchPath[i])
                    {
                        sameFolderStructure += prefabSearchPath[i];
                        continue;
                    }

                    break;
                }

                sameFolderStructure = sameFolderStructure.Replace(Application.dataPath, "Assets");
                string diff = iconDestinationPath.Replace(sameFolderStructure, "");
                string diff2 = prefabSearchPath.Replace(sameFolderStructure, "");
                
                
                
                diff2 = diff2.Substring(diff2.IndexOf('/'));
                desiredIconLocation = sameFolderStructure + Path.Join(diff, diff2);
                //Debug.Log($"finalPath {this.possibleLocation}");
            }
        }

        public bool IsSelected => _toggle.value;

        static VisualElement _lastSelected;

        public void Display(VisualElement parent)
        {
            string prefabName = Path.GetFileName(_prefabLocation);

            VisualElement outMostParent = new();

            //lets create a new element
            VisualElement mainRow = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    unityTextAlign = TextAnchor.MiddleLeft,
                }
            };


            //Change color on hover
            mainRow.RegisterCallback<MouseEnterEvent>(_ => mainRow.style.backgroundColor = new StyleColor(Color.gray));
            mainRow.RegisterCallback<MouseLeaveEvent>(_ => mainRow.style.backgroundColor = new StyleColor(Color.clear));

            _toggle = new Toggle();
            _toggle.SetValueWithoutNotify(_possibleIcons.Count == 0);
            _toggle.RegisterCallback<ChangeEvent<bool>>(OnToggleChange);


            Label labelName = new()
            {
                text = prefabName,
                style = { flexGrow = 1 }
            };

            labelName.RegisterCallback<ClickEvent>(OnRowClick);

            var labelText = "No Possible Icons Found";
            if (_possibleIcons.Count > 0)
            {
                labelText = $"{_possibleIcons.Count} Possible Icon{(_possibleIcons.Count > 1 ? "s" : "")}";
            }
            
            Label iconCount = new()
            {
                text = labelText,
                style = { unityTextAlign = TextAnchor.MiddleRight }
            };

            mainRow.Add(_toggle);
            mainRow.Add(labelName);
            mainRow.Add(iconCount);

            outMostParent.Add(mainRow);

            _detailsParent = new VisualElement();

            outMostParent.Add(_detailsParent);

            parent.Add(outMostParent);
        }

        private void OnToggleChange(ChangeEvent<bool> evt)
        {
            IconCreatorScript.HarpiaLog($"Toggle changed to {evt.newValue}");
            evt.StopImmediatePropagation();
        }

        private void OnRowClick(ClickEvent evt)
        {
            IconCreatorScript.HarpiaLog($"Clicked");

            if (_possibleIcons.Count == 0)
            {
                EditorGUIUtility.PingObject(gameObject);
                return;
            }
            
            if (_detailsParent.childCount > 0)
            {
                
                return;
            }

            if (_possibleIcons.Count > 0 && _lastSelected != null)
            {
                _lastSelected.parent.Remove(_lastSelected);
                _lastSelected = null;
            }

            foreach (string s in _possibleIcons)
            {
                string iconPath = s.Replace(Application.dataPath, "Assets");

                Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);

                if (icon == null)
                {
                    IconCreatorScript.HarpiaLog($"Could not load icon at {iconPath}", true);
                }

                VisualElement iconDataRow = new()
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        marginLeft = 20,
                    }
                };

                VisualElement iconImage = new()
                {
                    style =
                    {
                        backgroundImage = new StyleBackground(icon),

                        height = new StyleLength()
                        {
                            value = 20,
                        },

                        width = new StyleLength()
                        {
                            value = 20,
                        }
                    }
                };

                Label name = new()
                {
                    text = iconPath,
                    style = { unityTextAlign = TextAnchor.MiddleLeft }
                };


                iconDataRow.Add(iconImage);
                iconDataRow.Add(name);

                iconDataRow.RegisterCallback<MouseEnterEvent>(_ =>
                    iconDataRow.style.backgroundColor = new StyleColor(Color.gray));
                iconDataRow.RegisterCallback<MouseLeaveEvent>(_ =>
                    iconDataRow.style.backgroundColor = new StyleColor(Color.clear));
                iconDataRow.RegisterCallback<ClickEvent>(_ =>
                {
                    //ping the icon
                    EditorGUIUtility.PingObject(icon);
                });

                _lastSelected = iconDataRow;

                _detailsParent.Add(iconDataRow);
            }
        }

        public void Select(bool b)
        {
            _toggle.SetValueWithoutNotify(b);
        }
    }
}