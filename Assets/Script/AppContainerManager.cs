using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro; 

[System.Serializable]
public class InteractiveItem
{
    public string gameID;
    public string title;
    public string grade;
    public string category; 
    public string unit;
}

[System.Serializable]
public class CatalogData
{
    public InteractiveItem[] interactives;
}

public class AppContainerManager : MonoBehaviour
{
    [Header("Server Info")]
    public string serverRoot = "https://raw.githubusercontent.com/MrBearA/Container/main/root/";
    
    [Header("Startup & Transition UI")]
    [Tooltip("Drag a black panel with a CanvasGroup here. It hides the empty book while loading!")]
    public CanvasGroup startupCurtain;  // <--- NEW: Hides the menu until ready!

    [Header("Storybook UI References")]
    public Image previewImageUI;    
    public Button playButton;       
    public Button nextButton;       

    [Header("Loading Screen UI")]
    public GameObject loadingPanel;       
    public TextMeshProUGUI tipsText;      
    public string[] randomTips;           
    public float tipChangeInterval = 3f;  

    private List<InteractiveItem> currentPlaylist = new List<InteractiveItem>();
    private int currentPage = 0;
    private AssetBundle currentLoadedBundle = null;
    
    private Dictionary<string, Sprite> imageCache = new Dictionary<string, Sprite>();
    private Coroutine tipsCoroutine;

    void Start()
    {
        nextButton.onClick.AddListener(TurnPageRight);
        
        playButton.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
        
        if (previewImageUI != null) previewImageUI.transform.localScale = Vector3.zero;

        // NEW: Turn the curtain ON instantly so the player doesn't see the empty book
        if (startupCurtain != null)
        {
            startupCurtain.alpha = 1f;
            startupCurtain.blocksRaycasts = true;
            startupCurtain.gameObject.SetActive(true);
        }

        StartCoroutine(FetchAndCacheCatalog());
    }

    // --- 1. CATALOG & PRE-LOADING ---
    IEnumerator FetchAndCacheCatalog()
    {
        string catalogUrl = serverRoot + "catalog.json";
        string cachePath = Path.Combine(Application.persistentDataPath, "catalog.json");

        using (UnityWebRequest request = UnityWebRequest.Get(catalogUrl))
        {
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                if (File.Exists(cachePath)) ParseCatalog(File.ReadAllText(cachePath));
            }
            else
            {
                File.WriteAllText(cachePath, request.downloadHandler.text);
                ParseCatalog(request.downloadHandler.text);
            }
        }
    }

    void ParseCatalog(string json)
    {
        CatalogData data = JsonUtility.FromJson<CatalogData>(json);
        currentPlaylist = new List<InteractiveItem>(data.interactives);
        currentPage = 0;
        
        if (currentPlaylist.Count > 0)
        {
            StartCoroutine(PreloadFirstThenRest());
        }
    }

    IEnumerator PreloadFirstThenRest()
    {
        // 1. Download the VERY FIRST image and wait for it
        InteractiveItem firstGame = currentPlaylist[0];
        string firstUrl = $"{serverRoot}{firstGame.grade}/{firstGame.category}/{firstGame.unit}/{firstGame.gameID}/preview.jpg".Replace(" ", "%20");
        
        yield return StartCoroutine(DownloadSprite(firstUrl, firstGame.gameID + "_preview", (sprite) => {}));

        UpdateBookPage();

        // 2. THE IMAGE IS READY! Smoothly fade away the curtain to reveal the book!
        if (startupCurtain != null)
        {
            float time = 0;
            while (time < 0.5f)
            {
                time += Time.deltaTime;
                startupCurtain.alpha = Mathf.Lerp(1f, 0f, time / 0.5f);
                yield return null;
            }
            startupCurtain.blocksRaycasts = false;
            startupCurtain.gameObject.SetActive(false);
        }

        // 3. Secretly download the rest of the images in the background
        for (int i = 1; i < currentPlaylist.Count; i++)
        {
            InteractiveItem game = currentPlaylist[i];
            string url = $"{serverRoot}{game.grade}/{game.category}/{game.unit}/{game.gameID}/preview.jpg".Replace(" ", "%20");
            yield return StartCoroutine(DownloadSprite(url, game.gameID + "_preview", (sprite) => {}));
        }
    }

    // --- 2. PAGE FLIPPING ---
    void TurnPageRight()
    {
        if (currentPlaylist.Count > 0)
        {
            StartCoroutine(PunchScale(nextButton.transform));
            
            currentPage++;
            if (currentPage >= currentPlaylist.Count) currentPage = 0;
            
            UpdateBookPage();
        }
    }

    void UpdateBookPage()
    {
        if (currentPlaylist.Count == 0) return;

        InteractiveItem currentGame = currentPlaylist[currentPage];

        nextButton.gameObject.SetActive(currentPlaylist.Count > 1);
        
        playButton.gameObject.SetActive(true);
        playButton.onClick.RemoveAllListeners(); 
        playButton.onClick.AddListener(() => StartGameSequence(currentGame));

        StartCoroutine(FetchAndAnimateImages(currentGame));
    }

    IEnumerator FetchAndAnimateImages(InteractiveItem game)
    {
        yield return StartCoroutine(SmoothScale(previewImageUI.transform, 0f, 0.15f));

        string folderUrl = $"{serverRoot}{game.grade}/{game.category}/{game.unit}/{game.gameID}/".Replace(" ", "%20");
        
        yield return StartCoroutine(DownloadSprite(folderUrl + "preview.jpg", game.gameID + "_preview", (sprite) => {
            if (sprite != null) previewImageUI.sprite = sprite;
        }));

        yield return StartCoroutine(SmoothScale(previewImageUI.transform, 1f, 0.25f));
    }

    // --- UPGRADED HARD DRIVE CACHING SYSTEM ---
    IEnumerator DownloadSprite(string url, string cacheKey, System.Action<Sprite> onComplete)
    {
        // 1. Check Fast RAM (Instant)
        if (imageCache.ContainsKey(cacheKey))
        {
            onComplete(imageCache[cacheKey]);
            yield break;
        }

        // 2. Check Permanent Phone Memory / Hard Drive (Instant)
        string localPath = Path.Combine(Application.persistentDataPath, cacheKey + ".jpg");
        if (File.Exists(localPath))
        {
            byte[] fileData = File.ReadAllBytes(localPath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); 
            Sprite newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            
            imageCache[cacheKey] = newSprite; // Save to RAM for quick flipping
            onComplete(newSprite);
            yield break;
        }

        // 3. Not on the phone? Download from Internet (Takes a few seconds)
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (!request.isNetworkError && !request.isHttpError)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(request);
                
                // BOOM! Save it to the phone forever so we never have to wait again!
                File.WriteAllBytes(localPath, request.downloadHandler.data);

                Sprite newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                imageCache[cacheKey] = newSprite;
                onComplete(newSprite);
            }
            else onComplete(null);
        }
    }

    // --- 3. LOADING SCREEN & DOWNLOADING ---
    void StartGameSequence(InteractiveItem game)
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        
        if (tipsCoroutine != null) StopCoroutine(tipsCoroutine);
        tipsCoroutine = StartCoroutine(CycleTipsRoutine());

        string downloadUrl = $"{serverRoot}{game.grade}/{game.category}/{game.unit}/{game.gameID}/{game.gameID}";
        string cacheFilePath = Path.Combine(Application.persistentDataPath, $"{game.gameID}");

        if (File.Exists(cacheFilePath))
        {
            StartCoroutine(LoadBundleAndPlay(cacheFilePath));
        }
        else
        {
            StartCoroutine(DownloadWithProgress(downloadUrl, cacheFilePath));
        }
    }

    IEnumerator CycleTipsRoutine()
    {
        if (randomTips.Length == 0) yield break;

        while (true)
        {
            tipsText.text = randomTips[Random.Range(0, randomTips.Length)];
            yield return new WaitForSeconds(tipChangeInterval);
        }
    }

    IEnumerator DownloadWithProgress(string url, string savePath)
    {
        string webSafeUrl = url.Replace(" ", "%20");

        using (UnityWebRequest request = UnityWebRequest.Get(webSafeUrl))
        {
            request.SendWebRequest(); 

            while (!request.isDone) yield return null; 

            if (request.isNetworkError || request.isHttpError)
            {
                tipsText.text = "Download Failed! Check Internet.";
                yield return new WaitForSeconds(3f);
                loadingPanel.SetActive(false); 
            }
            else
            {
                File.WriteAllBytes(savePath, request.downloadHandler.data);
                StartCoroutine(LoadBundleAndPlay(savePath));
            }
        }
    }

    IEnumerator LoadBundleAndPlay(string bundlePath)
    {
        if (currentLoadedBundle != null) currentLoadedBundle.Unload(true);

        AssetBundleCreateRequest bundleLoadRequest = AssetBundle.LoadFromFileAsync(bundlePath);
        
        while (!bundleLoadRequest.isDone) yield return null;

        currentLoadedBundle = bundleLoadRequest.assetBundle;

        if (currentLoadedBundle != null)
        {
            string[] scenePaths = currentLoadedBundle.GetAllScenePaths();
            if (scenePaths.Length > 0)
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scenePaths[0]);
                while (!asyncLoad.isDone) yield return null;
            }
        }
    }

    // --- 4. NATIVE UNITY ANIMATIONS ---
    IEnumerator PunchScale(Transform target)
    {
        Vector3 originalScale = Vector3.one;
        Vector3 punchedScale = new Vector3(1.2f, 1.2f, 1f); 
        
        float upTime = 0.1f;
        float downTime = 0.15f;
        float t = 0;
        
        while (t < upTime)
        {
            t += Time.deltaTime;
            target.localScale = Vector3.Lerp(originalScale, punchedScale, t / upTime);
            yield return null;
        }
        
        t = 0;
        while (t < downTime)
        {
            t += Time.deltaTime;
            target.localScale = Vector3.Lerp(punchedScale, originalScale, t / downTime);
            yield return null;
        }
        
        target.localScale = originalScale;
    }

    IEnumerator SmoothScale(Transform target, float targetScale, float duration)
    {
        Vector3 startScale = target.localScale;
        Vector3 endScale = new Vector3(targetScale, targetScale, targetScale);
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            t = t * t * (3f - 2f * t); 
            
            target.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
        
        target.localScale = endScale;
    }
}