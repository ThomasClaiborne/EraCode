using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class SkillTreeUI : MonoBehaviour
{
    [Header("Skill Tree")]
    public SkillTreeManager skillTreeManager;
    public GameObject nodePrefab;

    [Header("Path Parents")]
    public GameObject offensivePathParent;
    public GameObject defensivePathParent;
    public GameObject utilityPathParent;

    [Header("Tab System")]
    public Button offensiveTabButton;
    public Button defensiveTabButton;
    public Button utilityTabButton;

    [Header("Ability Info")]
    public TextMeshProUGUI abilityNameText;
    public TextMeshProUGUI abilityDescriptionText;
    public GameObject abilityInfoPanel;

    [Header("Action Buttons")]
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;

    [Header("Action Bar")]
    [SerializeField] private ActionBarSlot[] actionBarSlots;
    [SerializeField] private ConfirmationPrompt confirmPrompt;

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

    [Header("UI")]
    public TextMeshProUGUI skillPointsText;

    private SkillTreeNode selectedNode;
    private Dictionary<GameObject, SkillTreeNode> nodeButtonMap = new Dictionary<GameObject, SkillTreeNode>();
    private enum ActionState { Normal, Unlock, Equip, Equipped, ConfirmEquip }
    private ActionState currentState;


    private void Start()
    {
        offensiveTabButton.onClick.AddListener(() => SwitchPath("Offensive"));
        defensiveTabButton.onClick.AddListener(() => SwitchPath("Defensive"));
        utilityTabButton.onClick.AddListener(() => SwitchPath("Utility"));

        CreateSkillTreeUI();
        UpdateSkillPointsDisplay();
        UpdateActionBar();
        HideAbilityInfo();

        // Set default path
        SwitchPath("Offensive");
    }
    public void UpdateUI()
    {
        UpdateSkillPointsDisplay();
        UpdateActionBar();
        UpdateActionButton();
        UpdateAllNodesVisuals();
    }

    private void UpdateActionBar()
    {
        var equippedAbilities = PlayerInventory.Instance.equippedAbilities;

        for (int i = 0; i < actionBarSlots.Length; i++)
        {
            if (i < equippedAbilities.Length)
            {
                actionBarSlots[i].SetAbility(equippedAbilities[i]);
                actionBarSlots[i].SetInteractable(equippedAbilities[i] != null);
            }
            else
            {
                actionBarSlots[i].SetAbility(null);
                actionBarSlots[i].SetInteractable(false);
            }
        }
    }

    public void OnActionBarSlotClicked(int slotIndex)
    {
        if (currentState == ActionState.ConfirmEquip)
        {
            if (slotIndex >= 0 && slotIndex < actionBarSlots.Length)
            {
                EquipAbility(slotIndex);
                currentState = ActionState.Normal;
                SetActionBarSlotsHighlighted(false);
                UpdateUI();
            }
        }
        else
        {
            var equippedAbilities = PlayerInventory.Instance.equippedAbilities;
            if (equippedAbilities[slotIndex] != null)
            {
                confirmPrompt.Show(
                    $"Unequip {equippedAbilities[slotIndex].abilityName}?",
                    () => UnequipAbility(slotIndex),
                    UpdateUI
                );
            }
        }
    }
    private void SetActionBarSlotsHighlighted(bool highlighted)
    {
        for (int i = 0; i < actionBarSlots.Length; i++)
        {
            actionBarSlots[i].SetHighlighted(highlighted && currentState == ActionState.ConfirmEquip);
            actionBarSlots[i].SetInteractable(highlighted || PlayerInventory.Instance.equippedAbilities[i] != null);
        }
    }

    private void SwitchPath(string pathName)
    {
        offensivePathParent.SetActive(pathName == "Offensive");
        defensivePathParent.SetActive(pathName == "Defensive");
        utilityPathParent.SetActive(pathName == "Utility");
    }

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
        float xOffset = 250f;
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

            //if (previousNodeObj != null)
            //{
            //    CreateConnectionLine(
            //        previousNodeObj.GetComponent<RectTransform>(),
            //        rectTransform,
            //        parent
            //    );
            //}

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
        if (selectedNode == null) return;

        bool isUnlocked = PlayerInventory.Instance.IsAbilityUnlocked(selectedNode.nodeID);
        bool canUnlock = skillTreeManager.CanUnlockNode(selectedNode) &&
                        PlayerInventory.Instance.LevelSystem.SkillPoints >= selectedNode.skillPointCost;

        if (!isUnlocked && canUnlock)
        {
            SetActionButtonState(ActionState.Unlock);
        }
        else if (isUnlocked && !selectedNode.ability.isPassive)
        {
            if (IsAbilityEquipped(selectedNode.ability))
            {
                SetActionButtonState(ActionState.Equipped);
            }
            else
            {
                SetActionButtonState(ActionState.Equip);
            }
        }
        else if (isUnlocked)
        {
            SetActionButtonState(ActionState.Normal);
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
                break;

            case ActionState.Equip:
                actionButtonText.text = "Equip";
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(OnEquipClicked);
                actionButton.GetComponent<Image>().color = equipColor;
                actionButton.interactable = true;
                break;

            case ActionState.Equipped:
                actionButtonText.text = "Equipped";
                actionButton.onClick.RemoveAllListeners();
                actionButton.GetComponent<Image>().color = equippedColor;
                actionButton.interactable = false;
                break;

            case ActionState.ConfirmEquip:
                actionButtonText.text = "Cancel";
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(CancelAction);
                actionButton.GetComponent<Image>().color = Color.red;
                SetActionBarSlotsHighlighted(true);
                break;
        }
    }

    private void OnUnlockClicked()
    {
        confirmPrompt.Show(
            $"Spend {selectedNode.skillPointCost} Skill Points to unlock {selectedNode.ability.abilityName}?",
            ConfirmUnlock,
            CancelAction
        );
    }

    private void OnEquipClicked()
    {
        currentState = ActionState.ConfirmEquip;
        SetActionButtonState(ActionState.ConfirmEquip);
        SetActionBarSlotsHighlighted(true);
    }

    private void ConfirmUnlock()
    {
        if (PlayerInventory.Instance.LevelSystem.SpendSkillPoints(selectedNode.skillPointCost))
        {
            PlayerInventory.Instance.UnlockAbility(selectedNode.nodeID, selectedNode.ability);
            UpdateAllNodesVisuals();
            UpdateSkillPointsDisplay();
            UpdateActionButton();
        }
    }

    private void EquipAbility(int slot)
    {
        PlayerInventory.Instance.EquipAbility(selectedNode.ability, slot);
        UpdateActionBar();
        UpdateUI();
    }

    private void UnequipAbility(int slot)
    {
        PlayerInventory.Instance.UnequipAbility(slot);
        UpdateActionBar();
        UpdateUI();
    }
    private void CancelAction()
    {
        currentState = ActionState.Normal;
        SetActionBarSlotsHighlighted(false);
        UpdateUI();
    }

    private bool IsAbilityEquipped(Ability ability)
    {
        return System.Array.Exists(PlayerInventory.Instance.equippedAbilities, a => a != null && a.abilityID == ability.abilityID);
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
