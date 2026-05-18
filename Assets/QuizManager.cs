using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using UnityEngine.UI;

[System.Serializable]
public class CountryFact {
    public string text;
    public string category; 
}

[System.Serializable]
public class CountryData {
    public string name;
    public List<CountryFact> facts = new List<CountryFact>();
    public List<string> neighbors = new List<string>();
}

public class QuizManager : MonoBehaviour
{
    [Header("Data Source")]
    public TextAsset csvFile;

    [Header("UI Panels")]
    public GameObject menuPanel;
    public GameObject startSelectPanel; 
    public GameObject scanDummyPanel;   
    public GameObject neighborSelectPanel; 
    public GameObject quizPanel;
    public GameObject profilePanel;
    public GameObject saveSlotPanel;
    public GameObject nameInputPanel;
    public GameObject correctFeedbackPanel;
    public GameObject incorrectFeedbackPanel;
    public GameObject leaderboardPanel;

    [Header("UI Elements")]
    public TMP_Text questionLabel;
    public TMP_Text[] statementButtons;
    public Image profileDisplayImage;

    [Header("Save Slot UI")]
    public TMP_Text[] saveSlotTexts;     public TMP_InputField playerNameInput;
    public TMP_Text leaderboardText;

    [Header("Dynamic Lists")]
    public GameObject buttonPrefab; 
    public Transform startSelectContent; 
    public Transform neighborSelectContent; 

    private List<CountryData> allCountries = new List<CountryData>();
    private List<string> visitedCountries = new List<string>();
    private List<string> availableFrontier = new List<string>();
    private CountryData currentTarget;
    private string correctStatement;

    // Game State variables
    private int currentAttempts = 0;
    private int currentScore = 0;
    private int currentSaveSlot = -1;
    private string currentPlayerName = "";

    void Start() {
        LoadCSV();
        ShowPage("menu");
    }

    public void GoToMenu() { ShowPage("menu"); }

    void LoadCSV() {
        allCountries.Clear();
        string[] lines = csvFile.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        for(int i = 1; i < lines.Length; i++) {
            string[] row = lines[i].Split(';');
            if (row.Length >= 6) {
                CountryData country = new CountryData();
                country.name = row[0].Trim();
                
                for(int j = 1; j <= 4; j++) {
                    if (!string.IsNullOrWhiteSpace(row[j])) {
                        string factText = row[j].Trim();
                        string category = CategorizeFact(factText);

                        if (factText.ToLower().Contains("flag looks like")) {
                            factText = $"{factText}<br><size=100%><sprite=\"{country.name}\" index=0></size>"; 
                        }   
                        country.facts.Add(new CountryFact { text = factText, category = category });
                    }
                }

                string rawNeighbors = row[5].Replace("\"", "");
                country.neighbors = rawNeighbors.Split(',').Select(n => n.Trim()).Where(n => !string.IsNullOrEmpty(n)).ToList();
                allCountries.Add(country);
            }
        }
    }

    string CategorizeFact(string fact) {
        string lower = fact.ToLower();
        if (lower.Contains("flag")) return "flag";
        if (lower.Contains("landmark") || lower.Contains("bridge") || lower.Contains("castle")) return "landmark";
        if (lower.Contains("food") || lower.Contains("dish") || lower.Contains("eat") || lower.Contains("fries")) return "food";
        return "funfact";
    }

    public void OnPlayClicked() {
        RefreshSaveSlots();
        ShowPage("saveSlot");
    }

    void RefreshSaveSlots() {
        for (int i = 0; i < 3; i++) {
            string slotName = PlayerPrefs.GetString($"Slot{i}_Name", "");
            if (string.IsNullOrEmpty(slotName)) {
                saveSlotTexts[i].text = "+ Empty Slot";
            } else {
                int score = PlayerPrefs.GetInt($"Slot{i}_Score", 0);
                saveSlotTexts[i].text = $"{slotName} - {score} pts";
            }
        }
    }

    public void SelectSaveSlot(int slotIndex) {
        currentSaveSlot = slotIndex;
        string slotName = PlayerPrefs.GetString($"Slot{slotIndex}_Name", "");

        if (string.IsNullOrEmpty(slotName)) {
            playerNameInput.text = "";
            ShowPage("nameInput");
        } else {
            currentPlayerName = slotName;
            currentScore = PlayerPrefs.GetInt($"Slot{slotIndex}_Score", 0);
            string visitedStr = PlayerPrefs.GetString($"Slot{slotIndex}_Visited", "");
            visitedCountries = string.IsNullOrEmpty(visitedStr) ? new List<string>() : visitedStr.Split(',').ToList();
            
            GoToNeighborSelection(); 
        }
    }

    public void ConfirmNewPlayerName() {
        if (!string.IsNullOrWhiteSpace(playerNameInput.text)) {
            currentPlayerName = playerNameInput.text;
            currentScore = 0;
            visitedCountries.Clear();
            SaveGame();
            
            PopulateList(allCountries.Select(c => c.name).ToList(), startSelectContent, OnStartCountryPicked);
            ShowPage("startSelect");
        }
    }

    public void DeleteSaveSlot(int slotIndex) {
        PlayerPrefs.DeleteKey($"Slot{slotIndex}_Name");
        PlayerPrefs.DeleteKey($"Slot{slotIndex}_Score");
        PlayerPrefs.DeleteKey($"Slot{slotIndex}_Visited");
        RefreshSaveSlots();
    }

    void SaveGame() {
        if (currentSaveSlot == -1) return;
        PlayerPrefs.SetString($"Slot{currentSaveSlot}_Name", currentPlayerName);
        PlayerPrefs.SetInt($"Slot{currentSaveSlot}_Score", currentScore);
        PlayerPrefs.SetString($"Slot{currentSaveSlot}_Visited", string.Join(",", visitedCountries));
        PlayerPrefs.Save();
    }

    void OnStartCountryPicked(string countryName) {
        visitedCountries.Add(countryName);
        currentTarget = allCountries.Find(c => c.name == countryName); 
        SaveGame();
        ShowPage("scanDummy");
    }

    public void GoToNeighborSelection() {
        UpdateFrontier();
        if (availableFrontier.Count == 0 && visitedCountries.Count > 0) {
            EndGame();
            return;
        }
        PopulateList(availableFrontier, neighborSelectContent, OnNeighborPicked);
        ShowPage("neighborSelect");
    }

    void UpdateFrontier() {
        availableFrontier.Clear();
        foreach (string visited in visitedCountries) {
            var node = allCountries.Find(c => c.name == visited);
            if (node != null) {
                foreach (string n in node.neighbors) {
                    if (!visitedCountries.Contains(n) && !availableFrontier.Contains(n))
                        availableFrontier.Add(n);
                }
            }
        }
    }

    void OnNeighborPicked(string countryName) {
        currentTarget = allCountries.Find(c => c.name == countryName);
        currentAttempts = 0; 
        GenerateQuizOptions();
        ShowPage("quiz");
    }

    public void RefreshQuiz() {
        GenerateQuizOptions();
        ShowPage("quiz");
    }

    void GenerateQuizOptions() {
        correctStatement = GetWeightedFact(currentTarget.facts);
        questionLabel.text = $"Identify the fact about {currentTarget.name}:";

        List<string> options = new List<string> { correctStatement };
        
        List<string> wrongPool = allCountries.Where(c => c.name != currentTarget.name)
                                             .SelectMany(c => c.facts)
                                             .Select(f => f.text)
                                             .Distinct() 
                                             .ToList();
        
        options.AddRange(wrongPool.OrderBy(x => Random.value).Take(3));
        options = options.OrderBy(x => Random.value).ToList();

        for (int i = 0; i < statementButtons.Length; i++) {
            statementButtons[i].text = options[i];
        }
    }

    string GetWeightedFact(List<CountryFact> facts) {
        int roll = Random.Range(0, 100);
        string targetCategory = "funfact";

        if (roll < 30) targetCategory = "flag";
        else if (roll < 60) targetCategory = "landmark";
        else if (roll < 85) targetCategory = "funfact";
        else targetCategory = "food";

        var matchingFacts = facts.Where(f => f.category == targetCategory).ToList();
        if (matchingFacts.Count > 0) {
            return matchingFacts[Random.Range(0, matchingFacts.Count)].text;
        }
        return facts[Random.Range(0, facts.Count)].text;
    }

    public void SelectAnswer(int index) {
        if (statementButtons[index].text == correctStatement) {
            if (currentAttempts == 0) currentScore += 100;
            else if (currentAttempts == 1) currentScore += 50;
            else if (currentAttempts == 2) currentScore += 25;

            if (!visitedCountries.Contains(currentTarget.name)) {
                visitedCountries.Add(currentTarget.name);
            }
            SaveGame();
            ShowPage("correctFeedback");
        } else {
            currentAttempts++;
            ShowPage("incorrectFeedback");
        }
    }

    public void ProceedFromCorrectFeedback() {
        ShowPage("scanDummy");
    }

    void EndGame() {
        string lbNames = PlayerPrefs.GetString("LeaderboardNames", "");
        string lbScores = PlayerPrefs.GetString("LeaderboardScores", "");
        
        lbNames += currentPlayerName + ",";
        lbScores += currentScore + ",";
        
        PlayerPrefs.SetString("LeaderboardNames", lbNames);
        PlayerPrefs.SetString("LeaderboardScores", lbScores);
        PlayerPrefs.Save();

        DeleteSaveSlot(currentSaveSlot);

        ShowLeaderboard();
    }

    public void ShowLeaderboard() {
        string[] names = PlayerPrefs.GetString("LeaderboardNames", "").Split(new char[]{','}, System.StringSplitOptions.RemoveEmptyEntries);
        string[] scoresStr = PlayerPrefs.GetString("LeaderboardScores", "").Split(new char[]{','}, System.StringSplitOptions.RemoveEmptyEntries);

        var lbList = new List<KeyValuePair<string, int>>();
        for(int i = 0; i < names.Length; i++) {
            lbList.Add(new KeyValuePair<string, int>(names[i], int.Parse(scoresStr[i])));
        }

        // Sort descending
        lbList = lbList.OrderByDescending(x => x.Value).ToList();

        string display = "LEADERBOARD\n\n";
        for(int i = 0; i < lbList.Count; i++) {
            display += $"{i+1}. {lbList[i].Key} - {lbList[i].Value} pts\n";
        }
        leaderboardText.text = display;

        ShowPage("leaderboard");
    }


    public void OnSkipScanClicked() {
        Sprite profileSprite = Resources.Load<Sprite>($"Profiles/{currentTarget.name}");
        if (profileSprite != null) {
            profileDisplayImage.sprite = profileSprite;
            ShowPage("profile");
        } else {
            GoToNeighborSelection();
        }
    }

    public void OnSkipProfile() { GoToNeighborSelection(); }

    void PopulateList(List<string> items, Transform container, System.Action<string> onClickAction) {
        foreach (Transform child in container) Destroy(child.gameObject);
        foreach (string item in items) {
            GameObject btn = Instantiate(buttonPrefab, container);
            btn.GetComponentInChildren<TMP_Text>().text = item;
            btn.GetComponent<Button>().onClick.AddListener(() => onClickAction(item));
        }
    }

    void ShowPage(string page) {
        menuPanel.SetActive(page == "menu");
        startSelectPanel.SetActive(page == "startSelect");
        scanDummyPanel.SetActive(page == "scanDummy");
        neighborSelectPanel.SetActive(page == "neighborSelect");
        quizPanel.SetActive(page == "quiz");
        profilePanel.SetActive(page == "profile");
        if (saveSlotPanel) saveSlotPanel.SetActive(page == "saveSlot");
        if (nameInputPanel) nameInputPanel.SetActive(page == "nameInput");
        if (correctFeedbackPanel) correctFeedbackPanel.SetActive(page == "correctFeedback");
        if (incorrectFeedbackPanel) incorrectFeedbackPanel.SetActive(page == "incorrectFeedback");
        if (leaderboardPanel) leaderboardPanel.SetActive(page == "leaderboard");
    }
}