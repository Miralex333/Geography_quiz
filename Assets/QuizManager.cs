using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Video;

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
    public GameObject leaderboardPanel;
    public GameObject howToPlayPanel;          
    public GameObject confirmSlotDeletePanel;  
    public GameObject confirmHomePanel;   
      
    [Header("New Flow Panels (Comics & Scan)")]
    public GameObject howToScanPanel;
    public GameObject luxembourgScanPanel;
    public GameObject euComicPanel1;
    public GameObject euComicPanel2;

    [Header("UI Elements")]
    public TMP_Text questionLabel;
    public TMP_Text[] statementButtons;
    public Image profileDisplayImage;
    public TMP_Text puzzleInstructionText;

    [Header("Save Slot UI")]
    public TMP_Text[] saveSlotTexts;     
    public TMP_InputField playerNameInput;
    public TMP_Text[] leaderboardSlotTexts;
    public TMP_Text leaderboardText;
    public TMP_Text leaderboardEndScoreText;

    [Header("Fade Feedback Settings")]
    public Image fadeOverlayImage; 
    public float fadeSpeed = 2.5f;  

    [Header("Profile Panel Settings")]
    public Button profileNextButton;      
    public Button profileHomeButton;

    [Header("Dynamic Lists")]
    public GameObject buttonPrefab; 
    public Transform startSelectContent; 
    public Transform neighborSelectContent; 

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip incorrectSound;

    [Header("Credits Gallery Settings")]
    public GameObject creditsPanel;
    public Image creditsDisplayContainer;
    public Sprite[] creditsPictures;
    public Button creditsNextButton;
    private int currentCreditsIndex = 0;

    [Header("Pagination Settings")]
    public int itemsPerPage = 4;
    public Button[] pageUpButtons;
    public Button[] pageDownButtons;
    private List<string> activePaginationList;
    private Transform activePaginationContainer;
    private System.Action<string> activePaginationAction;
    private int currentPageIndex = 0;

    [Header("End Game Assets")]
    public GameObject confettiVideoObject;
    public GameObject friendshipBookButton;

    private List<CountryData> allCountries = new List<CountryData>();
    private List<string> visitedCountries = new List<string>();
    private List<string> availableFrontier = new List<string>();
    private CountryData currentTarget;
    private string correctStatement;

    private int currentAttempts = 0;
    private int currentScore = 0;
    private int currentSaveSlot = -1;
    private string currentPlayerName = "";
    
    private int slotToDeleteIndex = -1;         
    private Coroutine profileButtonCoroutine;

    private bool isAnswering = false;
    private bool hasGameJustEnded = false;

     [Header("ARReq")]
     public GameObject BackImg;
     public GameObject MAincam;
     public GameObject GlobalLite;
     public GameObject ARsesh;
     public GameObject ARCam;
     public GameObject Lite3d;
     
    public string CurrentCountry="Null";
    public string factText;
    public string category;
    public string arCategory;
    public string startingCountry;

    public float bounceCooldownDuration = 4.0f; 
    private float sharedBounceCooldown = 0f;
    public Button scanNextButton;      
    public Button luxembourgScanNextButton;
    private float scanDelayTimer = 0f; 



    void Start() {
        LoadCSV();
        if (friendshipBookButton != null) friendshipBookButton.SetActive(false);
        if (confettiVideoObject != null) confettiVideoObject.SetActive(false);
        StartCoroutine(BouncePaginationLoop());
        
        ShowPage("menu");
    }

    public void GoToMenu() { 
        if (friendshipBookButton != null) friendshipBookButton.SetActive(false);
        
        if (leaderboardEndScoreText != null) {
            leaderboardEndScoreText.text = "";
        }

        if (confettiVideoObject != null) {
            VideoPlayer vp = confettiVideoObject.GetComponent<VideoPlayer>();
            if (vp != null) vp.Stop();
            confettiVideoObject.SetActive(false);
        }

        ResetGameState();
        ShowPage("menu"); 
    }

    void ResetGameState() {
        currentSaveSlot = -1;
        currentPlayerName = "";
        currentScore = 0;
        currentAttempts = 0;
        visitedCountries.Clear();
        availableFrontier.Clear();
        currentTarget = null;
        CurrentCountry= null;
        correctStatement = "";
        isAnswering = false;
    }

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
                        factText = row[j].Trim();
                        category = "funfact";

                        // string upperFact = factText.ToUpper();
                        string upperFact;
                        upperFact=factText.ToUpper();
                        if (upperFact.StartsWith("[FLAG]"))
                        {
                            category = "flag";
                            factText = factText.Substring(6).Trim();
                        }
                        else if (upperFact.StartsWith("[FOOD]"))
                        {
                            category = "food";
                            factText = factText.Substring(6).Trim();
                        }
                        else if (upperFact.StartsWith("[LANDMARK]"))
                        {
                            category = "landmark";
                            factText = factText.Substring(10).Trim();
                        }
                        else if (upperFact.StartsWith("[FUNFACT]"))
                        {
                            category = "funfact";
                            factText = factText.Substring(9).Trim();
                        }
                        else
                        {
                            category = CategorizeFact(factText);
                        }

                        if (factText.ToLower().Contains("flag looks like"))
                        {
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
        Debug.Log($"DEBUG: CSV parsed. Found {allCountries.Count} countries.");
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
        for (int i = 0; i < 4; i++) {
            string slotName = PlayerPrefs.GetString($"Slot{i}_Name", "");
            if (string.IsNullOrEmpty(slotName)) {
                saveSlotTexts[i].text = "+ Empty Slot";
            } else {
                int score = PlayerPrefs.GetInt($"Slot{i}_Score", 0);
                saveSlotTexts[i].text = $"{slotName} - {score} pts";
            }
        }
    }

    public void SelectSaveSlot(int slotIndex)
    {
        currentSaveSlot = slotIndex;
        string slotName = PlayerPrefs.GetString($"Slot{slotIndex}_Name", "");

        if (string.IsNullOrEmpty(slotName))
        {
            currentPlayerName = "";
            currentScore = 0;
            visitedCountries.Clear();
            availableFrontier.Clear();
            playerNameInput.text = "";
            ShowPage("nameInput");
        }
        else
        {
            currentPlayerName = slotName;
            currentScore = PlayerPrefs.GetInt($"Slot{slotIndex}_Score", 0);
            string visitedStr = PlayerPrefs.GetString($"Slot{slotIndex}_Visited", "");

            if (string.IsNullOrEmpty(visitedStr))
            {
                visitedCountries.Clear();
                SaveGame();
                PopulateList(allCountries.Select(c => c.name).ToList(), startSelectContent, OnStartCountryPicked);
                ShowPage("startSelect");
            }
            else
            {
                visitedCountries = visitedStr.Split('|').ToList();
                GoToNeighborSelection();
            }
        }
    }

    public void ConfirmNewPlayerName() {
        if (!string.IsNullOrWhiteSpace(playerNameInput.text)) {
            currentPlayerName = playerNameInput.text;
            currentScore = 0;
            visitedCountries.Clear();
            SaveGame();
            PopulateList(allCountries.Select(c => c.name).ToList(), startSelectContent, OnStartCountryPicked);
            ShowPage("euComic1");
        }
    }

    public void ProceedToComic2() { ShowPage("euComic2"); }
    public void ProceedToStartSelect() { ShowPage("startSelect"); }
    public void ProceedToScanDummy() { 
        
        if (startingCountry == CurrentCountry) {
            OnSkipScanClicked();
        } else {
            ShowPage("scanDummy");
        }    
    }

    public void DeleteSaveSlot(int slotIndex) {
        slotToDeleteIndex = slotIndex;
        if (confirmSlotDeletePanel) confirmSlotDeletePanel.SetActive(true);
    }

    public void ConfirmDeleteSaveSlot() {
        if (slotToDeleteIndex != -1) {
            ExecuteDeleteSaveSlot(slotToDeleteIndex);
            slotToDeleteIndex = -1;
        }
        if (confirmSlotDeletePanel) confirmSlotDeletePanel.SetActive(false);
    }

    public void CancelDeleteSaveSlot() {
        slotToDeleteIndex = -1;
        if (confirmSlotDeletePanel) confirmSlotDeletePanel.SetActive(false);
    }

    void ExecuteDeleteSaveSlot(int slotIndex) {
        PlayerPrefs.DeleteKey($"Slot{slotIndex}_Name");
        PlayerPrefs.DeleteKey($"Slot{slotIndex}_Score");
        PlayerPrefs.DeleteKey($"Slot{slotIndex}_Visited");
        RefreshSaveSlots();
    }

    void SaveGame() {
        if (currentSaveSlot == -1) return;
        PlayerPrefs.SetString($"Slot{currentSaveSlot}_Name", currentPlayerName);
        PlayerPrefs.SetInt($"Slot{currentSaveSlot}_Score", currentScore);
        
        PlayerPrefs.SetString($"Slot{currentSaveSlot}_Visited", string.Join("|", visitedCountries));
        PlayerPrefs.Save();
    }

    void OnStartCountryPicked(string countryName) {
        visitedCountries.Add(countryName);
        currentTarget = allCountries.Find(c => c.name == countryName); 
        CurrentCountry=currentTarget.name;
        startingCountry=currentTarget.name;
        SaveGame();
       // OnCorrectAnswerSelected();
        //ShowPage("profile");
        //OnSkipScanClicked();

        if (currentTarget != null && currentTarget.name == "Luxembourg") {
            ShowPage("luxembourgScan");
        } else {
            ShowPage("howToScan");
        }
        
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
        CurrentCountry=currentTarget.name;
        currentAttempts = 0; 
        GenerateQuizOptions();
        ShowPage("quiz");
    }

    public void RefreshQuiz() {
        GenerateQuizOptions();
    }

    void GenerateQuizOptions() {
        isAnswering = false; // Reset the safety lock when a new set of options is generated
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

        if (roll < 25) targetCategory = "flag";
        else if (roll < 60) targetCategory = "landmark";
        else if (roll < 85) targetCategory = "funfact";
        else targetCategory = "food";

        var matchingFacts = facts.Where(f => f.category == targetCategory).ToList();
        if (matchingFacts.Count > 0) {
            arCategory = matchingFacts[0].category;
            return matchingFacts[Random.Range(0, matchingFacts.Count)].text;
        }
        return facts[Random.Range(0, facts.Count)].text;
    }


    public void SelectAnswer(int index) {
        if (quizPanel == null || !quizPanel.activeSelf) return;
        if (isAnswering) return;
        

        if (statementButtons[index].text == correctStatement) {
            isAnswering = true; 
            if (currentAttempts == 0) currentScore += 100;
            else if (currentAttempts == 1) currentScore += 50;
            else if (currentAttempts == 2) currentScore += 25;

            if (!visitedCountries.Contains(currentTarget.name)) {
                visitedCountries.Add(currentTarget.name);
            }
            SaveGame();
            if (audioSource != null && correctSound != null) audioSource.PlayOneShot(correctSound);
            StartCoroutine(FadeFeedback(true));
            OnCorrectAnswerSelected();
            //ShowPage("correctFeedback");
            Debug.Log(arCategory);
        } else {
            currentAttempts++;
            if (audioSource != null && incorrectSound != null) audioSource.PlayOneShot(incorrectSound);
            StartCoroutine(FadeFeedback(false));
            //ShowPage("incorrectFeedback");
        }
    }


    void EndGame() {
        hasGameJustEnded = true; 
        
        string[] names = PlayerPrefs.GetString("LeaderboardNames", "").Split(new char[]{','}, System.StringSplitOptions.RemoveEmptyEntries);
        string[] scoresStr = PlayerPrefs.GetString("LeaderboardScores", "").Split(new char[]{','}, System.StringSplitOptions.RemoveEmptyEntries);

        var lbList = new List<KeyValuePair<string, int>>();
        for(int i = 0; i < names.Length; i++) {
            if (i < scoresStr.Length) {
                lbList.Add(new KeyValuePair<string, int>(names[i], int.Parse(scoresStr[i])));
            }
        }

        lbList.Add(new KeyValuePair<string, int>(currentPlayerName, currentScore));
        lbList = lbList.OrderByDescending(x => x.Value).Take(5).ToList();

        string newNames = string.Join(",", lbList.Select(x => x.Key));
        string newScores = string.Join(",", lbList.Select(x => x.Value));

        PlayerPrefs.SetString("LeaderboardNames", newNames);
        PlayerPrefs.SetString("LeaderboardScores", newScores);
        PlayerPrefs.Save();

        ExecuteDeleteSaveSlot(currentSaveSlot);
        if (friendshipBookButton != null) friendshipBookButton.SetActive(true);
        if (leaderboardEndScoreText != null) {
            leaderboardEndScoreText.text = $"{currentScore} pts";
        }
        ShowLeaderboard();
    }

    void PlayConfetti() {
        confettiVideoObject.SetActive(true);
        VideoPlayer vp = confettiVideoObject.GetComponent<VideoPlayer>();
        
        if (vp != null) {
            vp.loopPointReached -= DisableConfetti; 
            vp.loopPointReached += DisableConfetti; 
            vp.Play();
        }
    }

    void DisableConfetti(VideoPlayer vp) {
        vp.loopPointReached -= DisableConfetti; 
        if (confettiVideoObject != null) confettiVideoObject.SetActive(false);
    }

    public void ShowLeaderboard() {
        string[] names = PlayerPrefs.GetString("LeaderboardNames", "").Split(new char[]{','}, System.StringSplitOptions.RemoveEmptyEntries);
        string[] scoresStr = PlayerPrefs.GetString("LeaderboardScores", "").Split(new char[]{','}, System.StringSplitOptions.RemoveEmptyEntries);

        var lbList = new List<KeyValuePair<string, int>>();
        for(int i = 0; i < names.Length; i++) {
            if (i < scoresStr.Length) {
                lbList.Add(new KeyValuePair<string, int>(names[i], int.Parse(scoresStr[i])));
            }
        }
        lbList = lbList.OrderByDescending(x => x.Value).ToList();

        // TASK 2: Use the exact individual UI Text boxes to guarantee perfect mobile alignment
        if (leaderboardSlotTexts != null && leaderboardSlotTexts.Length > 0) {
            for(int i = 0; i < leaderboardSlotTexts.Length; i++) {
                if (i < lbList.Count) {
                    leaderboardSlotTexts[i].text = $"{lbList[i].Key} - {lbList[i].Value} pts";
                } else {
                    leaderboardSlotTexts[i].text = ""; // Clear empty slots
                }
            }
        } 
        
        // Fallback to the old method if they haven't set up the new boxes yet
        string display = "";
        for(int i = 0; i < lbList.Count; i++) {
            display += $"{lbList[i].Key} - {lbList[i].Value} pts\n";
        }
        if (leaderboardText != null) leaderboardText.text = display;
        
        ShowPage("leaderboard");
        
        // TASK 3: Only play confetti if the game was just completed
        if (confettiVideoObject != null) {
        if (hasGameJustEnded) {
            PlayConfetti();
            hasGameJustEnded = false; // Reset the flag
        } else {
            // If we are just viewing from the menu, make absolutely sure it stays hidden!
            confettiVideoObject.SetActive(false);
        }
    }
    }

    public void OnSkipScanClicked() {
        Sprite profileSprite = Resources.Load<Sprite>($"Profiles/{currentTarget.name}");
        if (profileSprite != null) {
            profileDisplayImage.sprite = profileSprite;
            ShowPage("profile");
            BackToGame();
        } else {
            GoToNeighborSelection();
            BackToGame();
        }
    }

    public void OnSkipProfile() { GoToNeighborSelection(); }

    System.Collections.IEnumerator EnableButtonsAfterDelay() {
    if (profileNextButton != null && profileHomeButton != null) {
        Vector3 originalScale = Vector3.one; 
        profileNextButton.transform.localScale = originalScale;

        // Automatically get or add a CanvasGroup so the entire button + text blinks together
        CanvasGroup canvasGroup = profileNextButton.GetComponent<CanvasGroup>();
        if (canvasGroup == null) {
            canvasGroup = profileNextButton.gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1f; // Make sure it starts fully visible

        profileNextButton.interactable = false;
        profileHomeButton.interactable = false;
        
        // Wait initial 3 seconds to enable buttons
        yield return new WaitForSeconds(3f);
        profileNextButton.interactable = true;
        profileHomeButton.interactable = true;

        // Wait an additional 2 seconds (5s total), then begin highlighting
        yield return new WaitForSeconds(2f);

        float timer = 0f;
        while (profilePanel.activeSelf) {
            // Speed modifier (Decrease to slow down BOTH the bounce and the blink)
            timer += Time.deltaTime * 0.15f; 

            // 1. BOUNCING LOGIC (Scales between 1.0 and 1.1)
            float scaleAmount = 1f + Mathf.PingPong(timer, 0.1f); 
            profileNextButton.transform.localScale = originalScale * scaleAmount;
            
            // 2. BLINKING LOGIC (Fades alpha down to 0.4, then back to 1.0)
            // Mathf.PingPong(timer, 0.6f) bounces between 0.0 and 0.6
            // Subtracting it from 1.0f leaves us with a perfect 1.0 to 0.4 oscillation
            // canvasGroup.alpha = 1f - Mathf.PingPong(timer, 0.6f); 

            yield return null;
        }

        // Safety reset when the loop ends
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        profileNextButton.transform.localScale = originalScale;
    }
}

    public void OpenHomeConfirmation() {
        if (confirmHomePanel) confirmHomePanel.SetActive(true);
    }

    public void ConfirmGoToMenu() {
        if (confirmHomePanel) confirmHomePanel.SetActive(false);
        GoToMenu();
    }

    public void CancelGoToMenu() {
        if (confirmHomePanel) confirmHomePanel.SetActive(false);
    }

    public void ShowHowToPlay() {
        currentCreditsIndex = 0;
        if (creditsPictures.Length > 0 && creditsDisplayContainer != null) {
            creditsDisplayContainer.sprite = creditsPictures[0];
        }
        UpdateCreditsButtonState();
        ShowPage("howToPlay");
    }

    void PopulateList(List<string> items, Transform container, System.Action<string> onClickAction) {
        activePaginationList = items;
        activePaginationContainer = container;
        activePaginationAction = onClickAction;
        currentPageIndex = 0;
        RenderCurrentPage();
    }

    void RenderCurrentPage() {
        foreach (Transform child in activePaginationContainer) Destroy(child.gameObject);

        int startIndex = currentPageIndex * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, activePaginationList.Count);

        for (int i = startIndex; i < endIndex; i++) {
            string item = activePaginationList[i];
            GameObject btn = Instantiate(buttonPrefab, activePaginationContainer);
            btn.GetComponentInChildren<TMP_Text>().text = item;
            
            btn.GetComponent<Button>().onClick.RemoveAllListeners();
            btn.GetComponent<Button>().onClick.AddListener(() => activePaginationAction(item));
        }

        foreach (Button btn in pageUpButtons) {
            if (btn != null) btn.interactable = (currentPageIndex > 0);
        }
        
        foreach (Button btn in pageDownButtons) {
            if (btn != null) btn.interactable = (endIndex < activePaginationList.Count);
        }
    }

    public void PageUp() {
        if (currentPageIndex > 0) {
            currentPageIndex--;
            RenderCurrentPage();
            
            sharedBounceCooldown = bounceCooldownDuration; 
        }
    }

    public void PageDown() {
        if ((currentPageIndex + 1) * itemsPerPage < activePaginationList.Count) {
            currentPageIndex++;
            RenderCurrentPage();
            
            sharedBounceCooldown = bounceCooldownDuration; 
        }
    }

    System.Collections.IEnumerator BouncePaginationLoop() {
        float bounceTimer = 0f;
        while (true) {
            if (sharedBounceCooldown > 0f) sharedBounceCooldown -= Time.deltaTime;

            bounceTimer += Time.deltaTime * 0.15f; 
            float scaleAmount = 1f + Mathf.PingPong(bounceTimer, 0.08f); 

            foreach (Button btn in pageUpButtons) {
                if (btn != null) {
                    btn.transform.localScale = (btn.interactable && sharedBounceCooldown <= 0f) 
                        ? Vector3.one * scaleAmount 
                        : Vector3.one;
                }
            }

            foreach (Button btn in pageDownButtons) {
                if (btn != null) {
                    btn.transform.localScale = (btn.interactable && sharedBounceCooldown <= 0f) 
                        ? Vector3.one * scaleAmount 
                        : Vector3.one;
                }
            }

            if (howToScanPanel != null && howToScanPanel.activeSelf) {
                scanDelayTimer += Time.deltaTime; 
                
                if (scanNextButton != null) {
                    scanNextButton.transform.localScale = (scanDelayTimer >= 3f) 
                        ? Vector3.one * scaleAmount 
                        : Vector3.one;
                }
            } else if (scanNextButton != null) {
                scanNextButton.transform.localScale = Vector3.one;
            }

            if (luxembourgScanPanel != null && luxembourgScanPanel.activeSelf) {
                scanDelayTimer += Time.deltaTime; 
                
                if (luxembourgScanNextButton != null) {
                    luxembourgScanNextButton.transform.localScale = (scanDelayTimer >= 3f) 
                        ? Vector3.one * scaleAmount 
                        : Vector3.one;
                }
            } else if (luxembourgScanNextButton != null) {
                luxembourgScanNextButton.transform.localScale = Vector3.one;
            }



            yield return null;
        }
    }

    void ShowPage(string page) {
        if ((page == "howToScan"||page=="luxembourgScan") && currentTarget != null) {
            if (puzzleInstructionText != null) {
                puzzleInstructionText.text = $"Find the puzzle piece for {currentTarget.name} and put it in the puzzle board.";
            }
            scanDelayTimer = 0f; 
        }
        menuPanel.SetActive(page == "menu");
        startSelectPanel.SetActive(page == "startSelect");
        scanDummyPanel.SetActive(page == "scanDummy");
        neighborSelectPanel.SetActive(page == "neighborSelect");
        quizPanel.SetActive(page == "quiz");
        profilePanel.SetActive(page == "profile");
        
        if (saveSlotPanel) saveSlotPanel.SetActive(page == "saveSlot");
        if (nameInputPanel) nameInputPanel.SetActive(page == "nameInput");
        if (leaderboardPanel) leaderboardPanel.SetActive(page == "leaderboard");
        if (howToPlayPanel) howToPlayPanel.SetActive(page == "howToPlay");

        if (howToScanPanel) howToScanPanel.SetActive(page == "howToScan");
        if (luxembourgScanPanel) luxembourgScanPanel.SetActive(page == "luxembourgScan");
        if (euComicPanel1) euComicPanel1.SetActive(page == "euComic1");
        if (euComicPanel2) euComicPanel2.SetActive(page == "euComic2");

        if (page == "profile") {
            if (profileButtonCoroutine != null) StopCoroutine(profileButtonCoroutine);
            profileButtonCoroutine = StartCoroutine(EnableButtonsAfterDelay());
        } else {
            // Reset button scale and alpha completely if they leave the profile page
            if (profileNextButton != null) {
                profileNextButton.transform.localScale = Vector3.one;
                CanvasGroup cg = profileNextButton.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;
            }
        }
    }

    public void ViewProfiles(){
        Application.OpenURL("https://drive.google.com/file/d/1ZFMfvn9OtI7hcJj2usIbQrqKIPCCqWjb/view?usp=drivesdk");
    }

    System.Collections.IEnumerator FadeFeedback(bool isCorrect) {
        fadeOverlayImage.gameObject.SetActive(true);
        Color targetColor = isCorrect ? Color.green : Color.red;
        
        float alpha = 0f;
        while (alpha < 1f) {
            alpha += Time.deltaTime * fadeSpeed;
            targetColor.a = Mathf.Clamp01(alpha);
            fadeOverlayImage.color = targetColor;
            yield return null;
        }

        if (isCorrect) {
            if (currentTarget != null && currentTarget.name == "Luxembourg") {
                ShowPage("luxembourgScan");
            } else {
                ShowPage("howToScan");
            }
        } else {
            GenerateQuizOptions(); 
            ShowPage("quiz");      
        }

        yield return new WaitForSeconds(0.15f);

        while (alpha > 0f) {
            alpha -= Time.deltaTime * fadeSpeed;
            targetColor.a = Mathf.Clamp01(alpha);
            fadeOverlayImage.color = targetColor;
            yield return null;
        }

        fadeOverlayImage.gameObject.SetActive(false);
    }

  public void OnCorrectAnswerSelected()
 {
    MAincam.SetActive(false);
    BackImg.SetActive(false);
    GlobalLite.SetActive(false);
    ARCam.SetActive(true);
    ARsesh.SetActive(true);
    Lite3d.SetActive(true);
 }

 public void BackToGame()
 {
    MAincam.SetActive(true);
    BackImg.SetActive(true);
    GlobalLite.SetActive(true);
    ARCam.SetActive(false);
    ARsesh.SetActive(false);
    Lite3d.SetActive(false);
 }

 public void OpenCreditsPanel() {
        currentCreditsIndex = 0;
        if (creditsPictures.Length > 0 && creditsDisplayContainer != null) {
            creditsDisplayContainer.sprite = creditsPictures[0];
        }
        UpdateCreditsButtonState();
        ShowPage("credits");
    }

    public void ShowNextCreditsPicture() {
        if (creditsPictures.Length == 0) return;

        if (currentCreditsIndex < creditsPictures.Length - 1) {
            currentCreditsIndex++;
            creditsDisplayContainer.sprite = creditsPictures[currentCreditsIndex];
        }
        UpdateCreditsButtonState();
    }

    private void UpdateCreditsButtonState() {
        if (creditsNextButton != null) {
            creditsNextButton.interactable = (currentCreditsIndex < creditsPictures.Length - 1);
        }
    }
}