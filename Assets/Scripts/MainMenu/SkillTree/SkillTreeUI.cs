using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SkillTreeUI : MonoBehaviour
{
    public SkillTreeManager skillTreeManager;
    public GameObject nodePrefab;

    [Header("Tab System")]
    public Button offensiveTabButton;
    public Button defensiveTabButton;
    public Button utilityTabButton;
    public Color selectedTabColor = Color.white;
    public Color unselectedTabColor = Color.gray;

    [Header("Path Parents")]
    public GameObject offensivePathParent;
    public GameObject defensivePathParent;
    public GameObject utilityPathParent;

    [Header("Ability Info")]
    public TextMeshProUGUI abilityNameText;
    public TextMeshProUGUI abilityDescriptionText;
    public GameObject abilityInfoPanel;

    [Header("Action Buttons")]
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;
    public Button optionButton1;
    public Button optionButton2;
    public Button optionButton3;
    public TextMeshProUGUI optionButton1Text;
    public TextMeshProUGUI optionButton2Text;
    public TextMeshProUGUI optionButton3Text;

    [Header("Colors")]
    public Color unlockColor = Color.yellow;
    public Color equipColor = Color.cyan;
    public Color equippedColor = Color.blue;
    public Color ownedColor = Color.gray;
    public Color lockedColor = Color.red;

    [Header("Connection Line")]
    public GameObject lineConnectorPrefab;
    public float lineThickness = 2f;
    public Color lineColor = Color.white;

    private SkillTreeNode selectedNode;
    private Dictionary<GameObject, SkillTreeNode> nodeButtonMap = new Dictionary<GameObject, SkillTreeNode>();

    private enum ActionState { Unlock, Equip, Equipped, ConfirmEquip, Owned }
    private ActionState currentState;

    [Header("UI")]
    public TextMeshProUGUI skillPointsText;

    private enum PathType { Offensive, Defensive, Utility }

    private void Start()
    {
        offensiveTabButton.onClick.AddListener(() => SwitchTab(PathType.Offensive));
        defensiveTabButton.onClick.AddListener(() => SwitchTab(PathType.Defensive));
        utilityTabButton.onClick.AddListener(() => SwitchTab(PathType.Utility));

        // Initialize with Offensive tab selected
        SwitchTab(PathType.Offensive);

        CreateSkillTreeUI();
        UpdateSkillPointsDisplay();
        HideAbilityInfo();
    }

    private void SwitchTab(PathType pathType)
    {
        // Update path visibility
        offensivePathParent.SetActive(pathType == PathType.Offensive);
        defensivePathParent.SetActive(pathType == PathType.Defensive);
        utilityPathParent.SetActive(pathType == PathType.Utility);

        // Update tab button visuals
        UpdateTabButtonVisuals(pathType);
    }

    private void UpdateTabButtonVisuals(PathType selectedPath)
    {
        // Update button colors
        offensiveTabButton.GetComponent<Image>().color =
            selectedPath == PathType.Offensive ? selectedTabColor : unselectedTabColor;
        defensiveTabButton.GetComponent<Image>().color =
            selectedPath == PathType.Defensive ? selectedTabColor : unselectedTabColor;
        utilityTabButton.GetComponent<Image>().color =
            selectedPath == PathType.Utility ? selectedTabColor : unselectedTabColor;

        // Optional: Update text colors if buttons have text
        if (offensiveTabButton.GetComponentInChildren<TextMeshProUGUI>() != null)
        {
            offensiveTabButton.GetComponentInChildren<TextMeshProUGUI>().color =
                selectedPath == PathType.Offensive ? Color.black : Color.white;
            defensiveTabButton.GetComponentInChildren<TextMeshProUGUI>().color =
                selectedPath == PathType.Defensive ? Color.black : Color.white;
            utilityTabButton.GetComponentInChildren<TextMeshProUGUI>().color =
                selectedPath == PathType.Utility ? Color.black : Color.white;
        }
    }

    //// Optional: Add method to check which tab is currently selected
    //public PathType GetCurrentTab()
    //{
    //    if (offensivePathParent.activeSelf) return PathType.Offensive;
    //    if (defensivePathParent.activeSelf) return PathType.Defensive;
    //    return PathType.Utility;
    //}

    private void CreateSkillTreeUI()
    {
        if (skillTreeManager.skillPaths.Count > 0)
            CreatePathUI(skillTreeManager.skillPaths[0], offensivePathParent.transform);

        if (skillTreeManager.skillPaths.Count > 1)
            CreatePathUI(skillTreeManager.skillPaths[1], defensivePathParent.transform);

        if (skillTreeManager.skillPaths.Count > 2)
            CreatePathUI(skillTreeManager.skillPaths[2], utilityPathParent.transform);
    }

    private void CreatePathUI(SkillTreePath path, Transform parent)
    {
        Vector2 currentPosition = Vector2.zero;
        float xOffset = 100f;
        GameObject previousNodeObj = null;

        foreach (var node in path.nodes)
        {
            GameObject nodeObject = Instantiate(nodePrefab, parent);
            Button nodeButton = nodeObject.GetComponent<Button>();
            RectTransform rectTransform = nodeObject.GetComponent<RectTransform>();

            rectTransform.anchoredPosition = currentPosition;
            currentPosition.x += xOffset;
            nodeButtonMap[nodeObject] = node;

            nodeButton.onClick.AddListener(() => SelectNode(node));

            if (previousNodeObj != null)
            {
                CreateConnectionLine(
                    previousNodeObj.GetComponent<RectTransform>(),
                    rectTransform,
                    parent
                );
            }

            // Update for next iteration
            previousNodeObj = nodeObject;

            UpdateNodeUI(nodeObject, node);
        }
    }

    private void CreateConnectionLine(RectTransform startNode, RectTransform endNode, Transform parent)
    {
        // Create line object
        GameObject lineObj = Instantiate(lineConnectorPrefab, parent);
        lineObj.transform.SetAsFirstSibling(); // Put line behind nodes

        RectTransform lineRect = lineObj.GetComponent<RectTransform>();
        Image lineImage = lineObj.GetComponent<Image>();

        // Calculate line position and rotation
        Vector2 direction = endNode.anchoredPosition - startNode.anchoredPosition;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Set line properties
        lineRect.anchoredPosition = startNode.anchoredPosition;
        lineRect.sizeDelta = new Vector2(distance, lineThickness);
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);
        lineImage.color = lineColor;
    }

    private void SelectNode(SkillTreeNode node)
    {
        selectedNode = node;
        ShowAbilityInfo();
        UpdateActionButton();
    }

    private void ShowAbilityInfo()
    {
        if (selectedNode == null) return;

        abilityInfoPanel.SetActive(true);
        abilityNameText.text = selectedNode.ability.abilityName;
        abilityDescriptionText.text = selectedNode.ability.description;
        UpdateActionButton();
    }

    private void HideAbilityInfo()
    {
        abilityInfoPanel.SetActive(false);
        selectedNode = null;
    }

    private void UpdateActionButton()
    {
        bool isUnlocked = PlayerInventory.Instance.IsAbilityUnlocked(selectedNode.nodeID);
        bool canUnlock = skillTreeManager.CanUnlockNode(selectedNode) &&
                        PlayerInventory.Instance.LevelSystem.SkillPoints >= selectedNode.skillPointCost;

        if (!isUnlocked && canUnlock)
        {
            SetActionButtonState(ActionState.Unlock);
        }
        else if (isUnlocked)
        {
            if (selectedNode.ability.isPassive)
            {
                SetActionButtonState(ActionState.Owned);
            }
            else if (IsAbilityEquippedAnywhere())
            {
                SetActionButtonState(ActionState.Equipped);
            }
            else
            {
                SetActionButtonState(ActionState.Equip);
            }
        }
    }

    private void SetActionButtonState(ActionState state)
    {
        currentState = state;
        actionButton.gameObject.SetActive(true);

        switch (state)
        {
case ActionState.Unlock:
            actionButtonText.text = $"Unlock ({selectedNode.skillPointCost} SP)";
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnUnlockClicked);
            actionButton.GetComponent<Image>().color = unlockColor;
            actionButton.interactable = true;
            HideOptionButtons();
            break;

        case ActionState.Equip:
            actionButtonText.text = "Equip";
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnEquipClicked);
            actionButton.GetComponent<Image>().color = equipColor;
            actionButton.interactable = true;
            HideOptionButtons();
            break;

        case ActionState.Equipped:
            actionButtonText.text = "Equipped";
            actionButton.onClick.RemoveAllListeners();
            actionButton.GetComponent<Image>().color = equippedColor;
            actionButton.interactable = false;
            HideOptionButtons();
            break;

        case ActionState.ConfirmEquip:
            actionButtonText.text = "Cancel";
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(CancelEquip);
            actionButton.GetComponent<Image>().color = Color.red;
            actionButton.interactable = true;
            ShowEquipOptions();
            break;

        case ActionState.Owned:
            actionButtonText.text = "Owned";
            actionButton.onClick.RemoveAllListeners();
            actionButton.GetComponent<Image>().color = ownedColor;
            actionButton.interactable = false;
            HideOptionButtons();
            break;
        }
    }

    private void OnUnlockClicked()
    {
        if (PlayerInventory.Instance.LevelSystem.SkillPoints >= selectedNode.skillPointCost)
        {
            if (PlayerInventory.Instance.LevelSystem.SpendSkillPoints(selectedNode.skillPointCost))
            {
                PlayerInventory.Instance.UnlockAbility(selectedNode.nodeID, selectedNode.ability);

                if (selectedNode.ability.isPassive)
                {
                    SetActionButtonState(ActionState.Owned);
                }
                else
                {
                    SetActionButtonState(ActionState.Equip);
                }
                UpdateAllNodesVisuals();
                UpdateSkillPointsDisplay();
            }
        }
    }

    private void OnEquipClicked()
    {
        SetActionButtonState(ActionState.ConfirmEquip);
    }

    private void CancelEquip()
    {
        SetActionButtonState(ActionState.Equip);
    }

    private void EquipAbility(int slot)
    {
        PlayerInventory.Instance.EquipAbility(selectedNode.ability, slot);
        SetActionButtonState(ActionState.Equipped);
    }

    private void ShowEquipOptions()
    {
        optionButton1.gameObject.SetActive(true);
        optionButton2.gameObject.SetActive(true);
        optionButton3.gameObject.SetActive(true);

        optionButton1Text.text = "Slot 1";
        optionButton2Text.text = "Slot 2";
        optionButton3Text.text = "Slot 3";

        optionButton1.onClick.RemoveAllListeners();
        optionButton2.onClick.RemoveAllListeners();
        optionButton3.onClick.RemoveAllListeners();

        optionButton1.onClick.AddListener(() => EquipAbility(0));
        optionButton2.onClick.AddListener(() => EquipAbility(1));
        optionButton3.onClick.AddListener(() => EquipAbility(2));

        optionButton1.interactable = !IsAbilityEquippedInSlot(0);
        optionButton2.interactable = !IsAbilityEquippedInSlot(1);
        optionButton3.interactable = !IsAbilityEquippedInSlot(2);

        optionButton1.GetComponent<Image>().color = equipColor;
        optionButton2.GetComponent<Image>().color = equipColor;
        optionButton3.GetComponent<Image>().color = equipColor;
    }

    private void HideOptionButtons()
    {
        optionButton1.gameObject.SetActive(false);
        optionButton2.gameObject.SetActive(false);
        optionButton3.gameObject.SetActive(false);
    }

    private bool IsAbilityEquippedInSlot(int slot)
    {
        return PlayerInventory.Instance.equippedAbilities[slot] != null &&
               PlayerInventory.Instance.equippedAbilities[slot].abilityID == selectedNode.ability.abilityID;
    }

    private bool IsAbilityEquippedAnywhere()
    {
        for (int i = 0; i < PlayerInventory.Instance.equippedAbilities.Length; i++)
        {
            if (IsAbilityEquippedInSlot(i))
                return true;
        }
        return false;
    }

    private void UpdateNodeVisuals(GameObject nodeObj, SkillTreeNode node)
    {
        Button nodeButton = nodeObj.GetComponent<Button>();
        Image nodeImage = nodeObj.GetComponent<Image>();

        bool isUnlocked = PlayerInventory.Instance.IsAbilityUnlocked(node.nodeID);
        bool canUnlock = skillTreeManager.CanUnlockNode(node);
        bool hasEnoughPoints = PlayerInventory.Instance.LevelSystem.SkillPoints >= node.skillPointCost;

        if (isUnlocked)
        {
            nodeImage.color = ownedColor;
            nodeButton.interactable = !node.ability.isPassive;
        }
        else if (canUnlock && hasEnoughPoints)
        {
            nodeImage.color = Color.white;  // Available to unlock
            nodeButton.interactable = true;
        }
        else if (canUnlock)
        {
            nodeImage.color = Color.yellow; 
            nodeButton.interactable = false;
        }
        else
        {
            nodeImage.color = lockedColor;  // Requirements not met
            nodeButton.interactable = false;
        }
    }

    private void UpdateAllNodesVisuals()
    {
        foreach (var buttonNode in nodeButtonMap)
        {
            UpdateNodeVisuals(buttonNode.Key, buttonNode.Value);
        }
    }

    private SkillTreeNode FindNodeForButton(GameObject buttonObj)
    {
        if (nodeButtonMap.TryGetValue(buttonObj, out SkillTreeNode node))
        {
            return node;
        }
        return null;
    }

    private void UpdateNodeUI(GameObject nodeObject, SkillTreeNode node)
    {
        Button nodeButton = nodeObject.GetComponent<Button>();
        Image nodeImage = nodeObject.GetComponent<Image>();

        bool isUnlocked = PlayerInventory.Instance.IsAbilityUnlocked(node.nodeID);
        bool canUnlock = skillTreeManager.CanUnlockNode(node) &&
                        PlayerInventory.Instance.LevelSystem.SkillPoints >= node.skillPointCost;

        if (isUnlocked)
        {
            nodeImage.color = ownedColor;
            nodeButton.interactable = !node.ability.isPassive;
        }
        else if (canUnlock)
        {
            nodeImage.color = Color.white;
            nodeButton.interactable = true;
        }
        else
        {
            nodeImage.color = lockedColor;
            nodeButton.interactable = false;
        }
    }

    private void UpdateSkillPointsDisplay()
    {
        skillPointsText.text = $"Skill Points: {PlayerInventory.Instance.LevelSystem.SkillPoints}";
    }
}
